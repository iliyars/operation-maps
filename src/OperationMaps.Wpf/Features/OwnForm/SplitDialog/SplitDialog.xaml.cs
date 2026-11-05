using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using OperationMaps.Wpf.Shared.Controls;

namespace OperationMaps.Wpf.Features.OwnForm.SplitDialog
{
  public partial class SplitDialog : Window
  {

    private SplitDialogViewModel Vm => (SplitDialogViewModel)DataContext;

    public IReadOnlyList<IReadOnlyList<string>>? Result { get; private set; }

    private PositionItemVm[]? _draggedItems;
    private Point _dragStart;
    private PositionItemVm? _dragCandidate;

    public SplitDialog(IReadOnlyList<string> positions)
    {
      InitializeComponent();
      WindowChromeFixes.Apply(this);
      DataContext = new SplitDialogViewModel(positions);

      // Source list drop — move back to unassigned

    }

    private void Pill_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (sender is not FrameworkElement fe) return;
      if (fe.DataContext is not PositionItemVm item) return;

      Vm.SelectPosition(item, (Keyboard.Modifiers & ModifierKeys.Control) != 0);
      _dragCandidate = item.IsSelected ? item : null;
      _dragStart = e.GetPosition(this);
      e.Handled = true;
    }

    private void Pill_MouseMove(object sender, MouseEventArgs e)
    {
      if (e.LeftButton != MouseButtonState.Pressed)
      {
        _dragCandidate = null;
        return;
      }
      if (_dragCandidate is null || sender is not FrameworkElement fe
          || !ReferenceEquals(fe.DataContext, _dragCandidate)) return;
      var point = e.GetPosition(this);
      if (Math.Abs(point.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance
          && Math.Abs(point.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
      _draggedItems = Vm.SelectedPositions.ToArray();
      _dragCandidate = null;
      try { DragDrop.DoDragDrop(fe, _draggedItems, DragDropEffects.Move); }
      finally { _draggedItems = null; }
      e.Handled = true;
    }

    private void SourceList_DragOver(object sender, DragEventArgs e)
    {
      e.Effects = _draggedItems is null ? DragDropEffects.None : DragDropEffects.Move;
      e.Handled = true;
    }

    private void SourceList_Drop(object sender, DragEventArgs e)
    {
      if (_draggedItems is null) return;
      foreach (var item in _draggedItems) Vm.MoveToSource(item);
      e.Handled = true;
    }

    private void GroupBorder_DragOver(object sender, DragEventArgs e)
    {
      e.Effects = _draggedItems is null ? DragDropEffects.None : DragDropEffects.Move;
      e.Handled = true;
    }

    private void GroupBorder_Drop(object sender, DragEventArgs e)
    {
      if (_draggedItems is null) return;
      if (sender is not FrameworkElement fe) return;
      if (fe.DataContext is not SplitGroupVm group) return;

      foreach (var item in _draggedItems) Vm.MoveToGroup(item, group);
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
