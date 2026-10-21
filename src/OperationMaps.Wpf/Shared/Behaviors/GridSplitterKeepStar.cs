using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace OperationMaps.Wpf.Shared.Behaviors;

/// <summary>
/// Fixes a GridSplitter quirk: dragging between a star ("take the rest")
/// column and a fixed-width one turns BOTH into fixed pixel widths. After
/// one drag the main table would stop shrinking with the window and its
/// right edge (e.g. the "Статус" column) slid out of view. With this on,
/// columns that were star before the drag are star again afterwards — the
/// fixed-width pane keeps the size the user dragged it to.
/// </summary>
public static class GridSplitterKeepStar
{
  public static readonly DependencyProperty IsEnabledProperty =
      DependencyProperty.RegisterAttached(
          "IsEnabled", typeof(bool), typeof(GridSplitterKeepStar),
          new PropertyMetadata(false, OnIsEnabledChanged));

  public static bool GetIsEnabled(DependencyObject o) => (bool)o.GetValue(IsEnabledProperty);
  public static void SetIsEnabled(DependencyObject o, bool value) => o.SetValue(IsEnabledProperty, value);

  // Star columns of the splitter's grid, captured when a drag starts.
  private static readonly DependencyProperty StarColumnsProperty =
      DependencyProperty.RegisterAttached(
          "StarColumns", typeof(ColumnDefinition[]), typeof(GridSplitterKeepStar));

  private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is not GridSplitter splitter) return;

    splitter.DragStarted -= OnDragStarted;
    splitter.DragCompleted -= OnDragCompleted;
    if (e.NewValue is true)
    {
      splitter.DragStarted += OnDragStarted;
      splitter.DragCompleted += OnDragCompleted;
    }
  }

  private static void OnDragStarted(object sender, DragStartedEventArgs e)
  {
    if (sender is GridSplitter { Parent: Grid grid } splitter)
      splitter.SetValue(StarColumnsProperty,
          grid.ColumnDefinitions.Where(c => c.Width.IsStar).ToArray());
  }

  private static void OnDragCompleted(object sender, DragCompletedEventArgs e)
  {
    if (sender is not GridSplitter splitter
        || splitter.GetValue(StarColumnsProperty) is not ColumnDefinition[] starColumns)
      return;

    foreach (var column in starColumns.Where(c => !c.Width.IsStar))
      column.Width = new GridLength(1, GridUnitType.Star);

    splitter.ClearValue(StarColumnsProperty);
  }
}
