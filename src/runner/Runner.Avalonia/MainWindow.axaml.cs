using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Env0.Core;
using env0.maintenance;
using env0.records;
using Env0.Terminal;

namespace Runner.Avalonia;

public partial class MainWindow : Window
{
    private readonly SessionState _session = new();
    private readonly RecordsModule _records = new();

    private IContextModule _module;
    private ContextRoute _route = ContextRoute.Maintenance;

    private readonly TerminalInputModel _input = new();
    private readonly List<string> _history = new();
    private int _historyIndex = -1;

    public MainWindow()
    {
        InitializeComponent();

        _module = new MaintenanceModule();

        Opened += OnOpened;

        InputSink.KeyDown += OnKeyDown;
        InputSink.TextInput += OnTextInput;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        // Start current module (boot).
        Print(_module.Handle(string.Empty, _session));
        RouteIfNeeded();
        EnsurePromptAndInput();

        InputSink.Focus();
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text))
            return;

        // ignore enter; handled in KeyDown
        if (e.Text == "\r" || e.Text == "\n")
            return;

        InsertText(e.Text);
        e.Handled = true;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                SubmitLine();
                e.Handled = true;
                return;

            case Key.Back:
                Backspace();
                e.Handled = true;
                return;

            case Key.Left:
                _input.Caret = Math.Max(0, _input.Caret - 1);
                SyncInlineInput();
                e.Handled = true;
                return;

            case Key.Right:
                _input.Caret = Math.Min(_input.Text.Length, _input.Caret + 1);
                SyncInlineInput();
                e.Handled = true;
                return;

            case Key.Up:
                HistoryPrev();
                e.Handled = true;
                return;

            case Key.Down:
                HistoryNext();
                e.Handled = true;
                return;
        }
    }

    private void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        // Basic ASCII-ish: keep it simple for PoC.
        var before = _input.Text.Substring(0, _input.Caret);
        var after = _input.Text.Substring(_input.Caret);
        _input.Text = before + text + after;
        _input.Caret += text.Length;
        SyncInlineInput();
    }

    private void Backspace()
    {
        if (_input.Caret <= 0 || _input.Text.Length == 0)
            return;

        var before = _input.Text.Substring(0, _input.Caret - 1);
        var after = _input.Text.Substring(_input.Caret);
        _input.Text = before + after;
        _input.Caret -= 1;
        SyncInlineInput();
    }

    private void SubmitLine()
    {
        var text = _input.Text;

        // Commit the line visually.
        Terminal.CommitInlineInput(text);

        // Save history if non-empty.
        if (!string.IsNullOrWhiteSpace(text))
        {
            _history.Add(text);
            _historyIndex = _history.Count;
        }

        _input.Clear();

        Print(_module.Handle(text, _session));
        RouteIfNeeded();
        EnsurePromptAndInput();

        InputSink.Focus();
    }

    private void HistoryPrev()
    {
        if (_history.Count == 0)
            return;

        if (_historyIndex < 0)
            _historyIndex = _history.Count;

        _historyIndex = Math.Max(0, _historyIndex - 1);
        _input.Text = _history[_historyIndex];
        _input.Caret = _input.Text.Length;
        SyncInlineInput();
    }

    private void HistoryNext()
    {
        if (_history.Count == 0)
            return;

        if (_historyIndex < 0)
            _historyIndex = _history.Count;

        _historyIndex = Math.Min(_history.Count, _historyIndex + 1);
        if (_historyIndex >= _history.Count)
        {
            _input.Clear();
        }
        else
        {
            _input.Text = _history[_historyIndex];
            _input.Caret = _input.Text.Length;
        }

        SyncInlineInput();
    }

    private void SyncInlineInput()
    {
        if (!Terminal.InlineInputActive)
            Terminal.BeginInlineInput();

        Terminal.UpdateInlineInput(_input.Text);
    }

    private void EnsurePromptAndInput()
    {
        // If the module ended with a non-newline output (likely a prompt), we can start input immediately.
        // If not, inject a default prompt for this context.
        if (!Terminal.InlineInputActive)
        {
            var prompt = GetDefaultPrompt(_route);
            if (!string.IsNullOrEmpty(prompt))
                Terminal.Append(prompt, newLine: false);

            Terminal.BeginInlineInput();
        }

        // Ensure any pending input buffer is rendered.
        Terminal.UpdateInlineInput(_input.Text);
    }

    private static string GetDefaultPrompt(ContextRoute route)
    {
        return route switch
        {
            ContextRoute.Records => "record> ",
            _ => "> ",
        };
    }

    private void Print(IEnumerable<OutputLine> lines)
    {
        if (lines == null)
            return;

        // Track whether the module itself emitted a prompt (OutputLine.NewLine == false)
        bool endedWithPromptCandidate = false;

        foreach (var line in lines)
        {
            Terminal.Append(line.Text ?? string.Empty, newLine: line.NewLine);
            endedWithPromptCandidate = line.NewLine == false;
        }

        // If the module ended with a prompt-like line, immediately enter inline input.
        if (endedWithPromptCandidate)
        {
            Terminal.BeginInlineInput();
        }
    }

    private void RouteIfNeeded()
    {
        while (_session.IsComplete && _session.NextContext != ContextRoute.None)
        {
            var next = _session.NextContext;

            _session.IsComplete = false;
            _session.NextContext = ContextRoute.None;

            _route = next;
            _module = CreateModule(next);

            // boot the next module
            Print(_module.Handle(string.Empty, _session));
        }
    }

    private IContextModule CreateModule(ContextRoute route)
    {
        return route switch
        {
            ContextRoute.Maintenance => new MaintenanceModule(),
            ContextRoute.Records => _records,
            ContextRoute.Terminal => new TerminalModule(),
            _ => throw new InvalidOperationException($"Unknown route: {route}")
        };
    }
}
