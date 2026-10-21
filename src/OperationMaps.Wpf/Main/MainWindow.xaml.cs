using System.ComponentModel;
using System.Windows;
using OperationMaps.Wpf.Shared.Controls;

namespace OperationMaps.Wpf.Main
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Set once the user has answered the "save changes?" question, so the
        // Close() re-issued after the async answer goes through.
        private bool _closeConfirmed;

        public MainWindow()
        {
            InitializeComponent();
            WindowChromeFixes.Apply(this);
        }

        private async void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (_closeConfirmed || DataContext is not MainViewModel vm) return;

            // Closing can't await — cancel now, ask, then close again if allowed.
            e.Cancel = true;
            if (!await vm.Shell.ConfirmCloseProjectAsync()) return;

            _closeConfirmed = true;

            // With nothing to save the await above completes synchronously —
            // we're still inside this Closing event, where WPF forbids Close().
            // Re-issue it once the current (cancelled) close has unwound.
            _ = Dispatcher.BeginInvoke(Close);
        }
    }
}
