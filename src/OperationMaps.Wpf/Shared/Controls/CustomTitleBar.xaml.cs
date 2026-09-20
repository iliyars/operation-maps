using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;

namespace OperationMaps.Wpf.Shared.Controls;

/// <summary>
/// VS-Code-style title bar: app icon, window title, and minimize/maximize-
/// restore/close buttons. Placed as ordinary content in the first row of a
/// window's own Grid (the window itself gets WindowStyle="None" and a
/// WindowChrome via the implicit Window style in WindowStyles.xaml, which
/// hands drag-move and resize back to Windows for anything in the
/// CaptionHeight band that isn't one of these buttons).
/// </summary>
public partial class CustomTitleBar : UserControl
{
  public CustomTitleBar()
  {
    InitializeComponent();

    Loaded += (_, _) =>
    {
      if (Window.GetWindow(this) is not { } window) return;

      window.StateChanged += (_, _) => UpdateMaximizeRestoreGlyph(window);
      UpdateMaximizeRestoreGlyph(window);
    };
  }

  private void OnMinimizeClick(object sender, RoutedEventArgs e)
  {
    if (Window.GetWindow(this) is { } window)
      SystemCommands.MinimizeWindow(window);
  }

  private void OnMaximizeRestoreClick(object sender, RoutedEventArgs e)
  {
    if (Window.GetWindow(this) is not { } window) return;

    if (window.WindowState == WindowState.Maximized)
      SystemCommands.RestoreWindow(window);
    else
      SystemCommands.MaximizeWindow(window);
  }

  private void OnCloseClick(object sender, RoutedEventArgs e)
  {
    if (Window.GetWindow(this) is { } window)
      SystemCommands.CloseWindow(window);
  }

  private void UpdateMaximizeRestoreGlyph(Window window)
  {
    // ChromeRestore (E923) when maximized so the button un-maximizes;
    // ChromeMaximize (E922) otherwise — same convention as every other
    // Windows title bar.
    var isMaximized = window.WindowState == WindowState.Maximized;
    PART_MaximizeRestoreButton.Content = isMaximized ? "" : "";
    PART_MaximizeRestoreButton.ToolTip = isMaximized ? "Восстановить" : "Развернуть";
  }
}
