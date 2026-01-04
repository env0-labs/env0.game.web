using System;
using System.Windows;
using System.Windows.Threading;

namespace Env0.Runner.Wpf
{
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString(), "Runner.Wpf Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            var message = exception?.ToString() ?? "Unknown unhandled exception.";
            MessageBox.Show(message, "Runner.Wpf Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
