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
using System.Windows.Shapes;

namespace OperationMaps.Wpf.Features.OwnForm.SplitDialog
{
  public partial class SplitDialog : Window
  {

    private SplitDialogViewModel Vm => (SplitDialogViewModel)DataContext;

    public IReadOnlyList<IReadOnlyList<string>>? Result { get; private set; }

    private PositionItemVm? _draggedItem;

    public SplitDialog(IReadOnlyList<string> positions)
    {
      InitializeComponent();
      DataContext = new SplitDialogViewModel(positions);

      // Source list drop — move back to unassigned

    }

    private void Pill_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (sender is not FrameworkElement fe) return;
      if (fe.DataContext is not PositionItemVm item) return;

      _draggedItem = item;

      DragDrop.DoDragDrop(fe, item, DragDropEffects.Move);

      _draggedItem = null;
      e.Handled = true;
    }

    private void SourceList_DragOver(object sender, DragEventArgs e)
    {
      e.Effects = DragDropEffects.Move;
      e.Handled = true;
    }

    private void SourceList_Drop(object sender, DragEventArgs e)
    {
      if (_draggedItem is null) return;
      Vm.MoveToSource(_draggedItem);
      e.Handled = true;
    }

    private void GroupBorder_DragOver(object sender, DragEventArgs e)
    {
      e.Effects = DragDropEffects.Move;
      e.Handled = true;
    }

    private void GroupBorder_Drop(object sender, DragEventArgs e)
    {
      if (_draggedItem is null) return;
      if (sender is not FrameworkElement fe) return;
      if (fe.DataContext is not SplitGroupVm group) return;

      Vm.MoveToGroup(_draggedItem, group);
      e.Handled = true;
    }

    // ── Footer buttons ────────────────────────────────────────────────────────

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
      Result = Vm.GetResult();
      DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
    }

  }
}
