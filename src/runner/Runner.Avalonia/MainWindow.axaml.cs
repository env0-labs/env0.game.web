using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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

    public MainWindow()
    {
        InitializeComponent();

        _module = new MaintenanceModule();

        Opened += OnOpened;
        Input.KeyDown += OnInputKeyDown;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        // Start current module (boot).
        Print(_module.Handle(string.Empty, _session));
        Input.Focus();
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        var text = Input.Text ?? string.Empty;
        Input.Text = string.Empty;

        // Echo the input like a terminal.
        Terminal.Append(text, newLine: true);

        Print(_module.Handle(text, _session));
        RouteIfNeeded();

        e.Handled = true;
    }

    private void Print(IEnumerable<OutputLine> lines)
    {
        if (lines == null)
            return;

        foreach (var line in lines)
        {
            Terminal.Append(line.Text ?? string.Empty, newLine: line.NewLine);
        }
    }

    private void RouteIfNeeded()
    {
        while (_session.IsComplete && _session.NextContext != ContextRoute.None)
        {
            var next = _session.NextContext;

            _session.IsComplete = false;
            _session.NextContext = ContextRoute.None;

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
