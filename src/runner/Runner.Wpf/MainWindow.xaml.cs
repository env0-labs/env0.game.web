using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using Env0.Core;
using Env0.Terminal;
using env0.maintenance;
using env0.records;

namespace Env0.Runner.Wpf
{
    public partial class MainWindow : Window
    {
        private readonly SessionState _session = new SessionState();
        private readonly RecordsModule _recordsModule = new RecordsModule();
        private readonly Paragraph _paragraph = new Paragraph();
        private readonly Run _inputRun = new Run();
        private readonly InputHistory _inputHistory = new InputHistory();
        private IContextModule _currentModule;
        private string _originalDirectory = string.Empty;
        private bool _suppressSelectionChanged;

        public MainWindow()
        {
            InitializeComponent();
            _currentModule = new MaintenanceModule();
            TranscriptBox.Document = new FlowDocument(_paragraph);
            _paragraph.Inlines.Add(_inputRun);
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _originalDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = AppContext.BaseDirectory;
            StartModule(_currentModule);
            FocusInput();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_originalDirectory))
            {
                Environment.CurrentDirectory = _originalDirectory;
            }
        }

        private void OnTranscriptPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back)
            {
                _inputHistory.Backspace();
                UpdateInputDisplay();

                e.Handled = true;
                return;
            }

            if (e.Key == Key.F1 ||
                (e.Key == Key.O && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
            {
                _inputHistory.Set("options");
                UpdateInputDisplay();
                CommitInput();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                CommitInput();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Space)
            {
                _inputHistory.Append(" ");
                UpdateInputDisplay();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down ||
                e.Key == Key.Home || e.Key == Key.End || e.Key == Key.PageUp || e.Key == Key.PageDown)
            {
                HandleHistoryNavigation(e.Key);
                e.Handled = true;
            }
        }

        private void OnTranscriptTextInput(object sender, TextCompositionEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text))
                return;

            if (e.Text == "\r" || e.Text == "\n")
                return;

            _inputHistory.Append(e.Text);
            UpdateInputDisplay();
            e.Handled = true;
        }

        private void OnTranscriptPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            FocusInput();
            e.Handled = true;
        }

        private void OnTranscriptSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (_suppressSelectionChanged)
                return;

            FocusInput();
        }

        private void StartModule(IContextModule module)
        {
            _currentModule = module;
            _session.IsComplete = false;
            _session.NextContext = ContextRoute.None;
            AppendOutput(module.Handle(string.Empty, _session));
            AdvanceRoutingIfNeeded();
        }

        private void AdvanceRoutingIfNeeded()
        {
            while (_session.IsComplete && _session.NextContext != ContextRoute.None)
            {
                var next = _session.NextContext;
                _session.IsComplete = false;
                _session.NextContext = ContextRoute.None;
                _currentModule = CreateModule(next);
                AppendOutput(_currentModule.Handle(string.Empty, _session));
            }
        }

        private IContextModule CreateModule(ContextRoute route)
        {
            return route switch
            {
                ContextRoute.Maintenance => new MaintenanceModule(),
                ContextRoute.Records => _recordsModule,
                ContextRoute.Terminal => new TerminalModule(),
                _ => throw new InvalidOperationException($"Unknown route: {route}")
            };
        }

        private void SubmitInput(string input)
        {
            if (_session.IsComplete && _session.NextContext != ContextRoute.None)
                AdvanceRoutingIfNeeded();

            if (_session.IsComplete && _session.NextContext == ContextRoute.None)
                return;

            AppendOutput(_currentModule.Handle(input, _session));
            AdvanceRoutingIfNeeded();
            FocusInput();
        }

        private void AppendOutput(IEnumerable<OutputLine> lines)
        {
            if (lines == null)
            {
                EnsureInputRun();
                UpdateInputDisplay();
                return;
            }

            _inputHistory.Clear();
            RemoveInputRun();

            foreach (var line in lines)
            {
                AppendOutputLine(line);
            }

            EnsureInputRun();
            TranscriptBox.ScrollToEnd();
            UpdateInputDisplay();
        }

        private void AppendOutputLine(OutputLine line)
        {
            var run = OutputLineStyler.CreateRun(line);
            _paragraph.Inlines.Add(run);

            if (line.NewLine)
                _paragraph.Inlines.Add(new LineBreak());
        }

        private void FocusInput()
        {
            _suppressSelectionChanged = true;
            TranscriptBox.Focus();
            TranscriptBox.CaretPosition = _paragraph.ContentEnd;
            Dispatcher.BeginInvoke(() => { _suppressSelectionChanged = false; });
        }

        private void CommitInput()
        {
            var input = _inputHistory.Commit();
            RemoveInputRun();
            _paragraph.Inlines.Add(new LineBreak());
            SubmitInput(input);
        }

        private void UpdateInputDisplay()
        {
            EnsureInputRun();
            _inputRun.Text = _inputHistory.Buffer;
            TranscriptBox.CaretPosition = _paragraph.ContentEnd;
        }

        private void HandleHistoryNavigation(Key key)
        {
            if (key == Key.Up)
            {
                _inputHistory.NavigateUp();
                UpdateInputDisplay();
                return;
            }

            if (key == Key.Down)
            {
                _inputHistory.NavigateDown();
                UpdateInputDisplay();
                return;
            }

            FocusInput();
        }

        private void RemoveInputRun()
        {
            if (_paragraph.Inlines.Contains(_inputRun))
                _paragraph.Inlines.Remove(_inputRun);
        }

        private void EnsureInputRun()
        {
            if (!_paragraph.Inlines.Contains(_inputRun))
                _paragraph.Inlines.Add(_inputRun);
        }
    }
}
