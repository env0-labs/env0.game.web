using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Env0.Core;
using env0.maintenance;
using env0.records;
using Env0.Terminal;

namespace Runner.Avalonia;

public partial class MainWindow : Window
{
    private readonly SessionState _session = new();
    private readonly RecordsModule _records = new();

    private readonly string _originalDirectory;

    private IContextModule _module;
    private ContextRoute _route = ContextRoute.Maintenance;

    private readonly TerminalInputModel _input = new();
    private readonly List<string> _history = new();
    private int _historyIndex = -1;

    public MainWindow()
    {
        InitializeComponent();

        Terminal.StreamDrained += () =>
        {
            // Once streaming finishes, ensure a prompt and inline input exist.
            Dispatcher.UIThread.Post(() => EnsurePromptAndInput());
        };

        _originalDirectory = Environment.CurrentDirectory;
        // Match the console runner: contexts expect AppContext.BaseDirectory as cwd.
        Environment.CurrentDirectory = AppContext.BaseDirectory;

        _module = new MaintenanceModule();

        Opened += OnOpened;
        Closed += OnClosed;

        // Capture input at the Window level (tunnel) so we don't depend on any specific control
        // successfully holding focus.
        AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(InputElement.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Environment.CurrentDirectory = _originalDirectory;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        // Start current module (boot).
        Print(SafeHandle(string.Empty));
        RouteIfNeeded();
        EnsurePromptAndInput();

        // Try to focus the terminal; keep the hidden sink as a backup.
        Terminal.Focus();
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
        // Escape to exit.
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        // CRT effect toggles (temporary dev controls)
        // Use Shift+1/2/3 so plain 1/2/3 remain usable for gameplay/terminal.
        // (UK keyboard: Shift+1=! Shift+2=" Shift+3=£)
        var shift = (e.KeyModifiers & KeyModifiers.Shift) != 0;
        if (shift && e.Key == Key.D1)
        {
            Terminal.EnableScanlines = !Terminal.EnableScanlines;
            e.Handled = true;
            return;
        }
        if (shift && e.Key == Key.D2)
        {
            Terminal.EnableBarrelDistortion = !Terminal.EnableBarrelDistortion;
            e.Handled = true;
            return;
        }
        if (shift && e.Key == Key.D3)
        {
            Terminal.EnableRgbSplit = !Terminal.EnableRgbSplit;
            e.Handled = true;
            return;
        }

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

        // Records/Maintenance are scene-based: clear each turn.
        // Terminal keeps scrollback like a real terminal.
        if (_route != ContextRoute.Terminal)
            Terminal.Clear();

        // Save history if non-empty.
        if (!string.IsNullOrWhiteSpace(text))
        {
            _history.Add(text);
            _historyIndex = _history.Count;
        }

        _input.Clear();

        Print(SafeHandle(text));
        RouteIfNeeded();
        EnsurePromptAndInput();

        Terminal.Focus();
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
        // Don't inject prompts while output is still streaming.
        if (Terminal.IsStreaming)
            return;

        // If the module ended with a non-newline output (likely a prompt), we can start input immediately.
        // If not, inject a default prompt for this context.
        if (!Terminal.InlineInputActive)
        {
            var prompt = GetDefaultPrompt(_route);
            if (!string.IsNullOrEmpty(prompt))
                Terminal.Append(prompt, newLine: false, wordWrap: false);

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

    private IEnumerable<OutputLine> SafeHandle(string input)
    {
        try
        {
            return _module.Handle(input, _session) ?? Array.Empty<OutputLine>();
        }
        catch (Exception ex)
        {
            // Never crash the UI because a context threw.
            var msg = $"EXCEPTION: {ex.GetType().Name}: {ex.Message}";
            var stack = (ex.StackTrace ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            var lines = new List<OutputLine>
            {
                new OutputLine(OutputType.Error, msg),
            };
            foreach (var s in stack.Take(12))
            {
                if (!string.IsNullOrWhiteSpace(s))
                    lines.Add(new OutputLine(OutputType.Error, s.Trim()));
            }
            lines.Add(new OutputLine(OutputType.Standard, string.Empty));

            // Keep session alive.
            _session.IsComplete = false;
            _session.NextContext = ContextRoute.None;
            return lines;
        }
    }

    private void Print(IEnumerable<OutputLine> lines)
    {
        if (lines == null)
            return;

        // Track whether the module itself emitted a prompt (OutputLine.NewLine == false)
        bool endedWithPromptCandidate = false;

        foreach (var line in lines)
        {
            var text = line.Text ?? string.Empty;

            // Prompts should appear instantly (and not be typed out).
            if (line.NewLine == false)
            {
                Terminal.Append(text, newLine: false, wordWrap: false);
                endedWithPromptCandidate = true;
                continue;
            }

            // Stream content lines, wrapped to the visible width.
            Terminal.EnqueueStreamWrapped(text);
            endedWithPromptCandidate = false;
        }

        // If the module ended with a prompt-like line, immediately enter inline input.
        // Otherwise: StreamDrained event will re-arm input.
        if (endedWithPromptCandidate && !Terminal.IsStreaming)
            Terminal.BeginInlineInput();
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
            Print(SafeHandle(string.Empty));
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
