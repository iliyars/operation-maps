using System.ComponentModel;
using System.Windows;
using OperationMaps.Wpf.Shared.Controls;
using OperationMaps.Wpf.Features.Project;

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
        private readonly ProjectRecoveryService? _recovery;
        private bool _isClosing;

        public MainWindow(ProjectRecoveryService? recovery = null)
        {
            _recovery = recovery;
            InitializeComponent();
            WindowChromeFixes.Apply(this);
        }

        // Below this width the sidebar folds to icons by itself (and unfolds
        // again above it) — only when crossing the line, so a manual choice
        // made at a given size is never fought.
        private const double SidebarAutoCollapseWidth = 1200;

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is not MainViewModel vm || !e.WidthChanged) return;

            var wasWide = e.PreviousSize.Width >= SidebarAutoCollapseWidth;
            var isWide = e.NewSize.Width >= SidebarAutoCollapseWidth;
            if (e.PreviousSize.Width == 0 || wasWide != isWide)
                vm.Shell.IsSidebarCollapsed = !isWide;
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Window-level shortcuts still run when the workspace is disabled.
            if (DataContext is MainViewModel vm && vm.Shell.Operation.IsBusy)
                e.Handled = true;
        }

        private async void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (_closeConfirmed || DataContext is not MainViewModel vm) return;

            // Closing can't await — cancel now, ask, then close again if allowed.
            e.Cancel = true;
            if (vm.Shell.Operation.IsBusy) return;
            if (_isClosing) return;
            _isClosing = true;
            try
            {
                if (!await vm.Shell.ConfirmCloseProjectAsync()) return;
                if (_recovery is not null) await _recovery.CompleteAsync();
                _closeConfirmed = true;
            }
            finally { _isClosing = false; }

            // With nothing to save the await above completes synchronously —
            // we're still inside this Closing event, where WPF forbids Close().
            // Re-issue it once the current (cancelled) close has unwound.
            _ = Dispatcher.BeginInvoke(Close);
        }
    }
}
