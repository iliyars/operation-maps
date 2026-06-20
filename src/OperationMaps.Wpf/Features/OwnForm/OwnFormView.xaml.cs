using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OperationMaps.Wpf.Features.OwnForm
{
  /// <summary>
  /// Interaction logic for OwnFormView.xaml
  /// </summary>
  public partial class OwnFormView : UserControl
  {
    public OwnFormView()
    {
      InitializeComponent();
    }

    private void NoteInputBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      if (e.NewValue is true && sender is TextBox tb)
      {
        tb.Dispatcher.BeginInvoke(
            () => { tb.Focus(); tb.SelectAll(); },
            System.Windows.Threading.DispatcherPriority.Input);
      }
      else if (e.NewValue is false)
      {
        Keyboard.ClearFocus();
        Focus();
      }
    }

    /// <summary>
    /// Routes a click on a column list item to either single-selection
    /// (plain click) or multi-selection for Merge (Ctrl+click).
    /// MouseBinding cannot branch on Keyboard.Modifiers declaratively,
    /// so the routing happens here.
    /// </summary>
    private void ColumnItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      if (sender is not Border border) return;
      if (border.Tag is not ColumnListItemVm item) return;
      if (DataContext is not OwnFormViewModel vm) return;

      if (Keyboard.Modifiers == ModifierKeys.Control)
        vm.ToggleColumnSelectionCommand.Execute(item.Column);
      else
        vm.SelectColumnCommand.Execute(item.Column);

      e.Handled = true;
    }

  }
}
