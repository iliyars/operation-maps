using System.Windows;
using OperationMaps.Wpf.Shared.Controls;

namespace OperationMaps.Wpf.Shared.Dialogs;

public partial class MessageDialog : Window
{
  public bool? Answer { get; private set; }

  public MessageDialog(string message, string title, MessageDialogKind kind)
  {
    var area = SystemParameters.WorkArea;
    DataContext = new MessageDialogContent(title, message, kind, Math.Clamp(area.Height - 240, 120, 520));
    InitializeComponent();
    Width = Math.Min(680, Math.Max(320, area.Width - 48));
    MaxHeight = Math.Max(240, area.Height - 32);
    WindowChromeFixes.Apply(this);
  }

  private void Yes_Click(object sender, RoutedEventArgs e) { Answer = true; DialogResult = true; }
  private void No_Click(object sender, RoutedEventArgs e) { Answer = false; DialogResult = true; }
  private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
