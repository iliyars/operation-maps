using System.Collections.ObjectModel;
using OperationMaps.Wpf.Features.Components.Commands;

namespace OperationMaps.Wpf.Features.OwnForm.Commands;

internal static class ColumnPositionOrder
{
  public static void Sort(ObservableCollection<FormColumnVm> columns)
    => Restore(columns, columns.OrderBy(c => c.Component.Entry.Imported.Positions
      .OrderBy(p => p, PositionComparer.Instance).FirstOrDefault() ?? "\uffff", PositionComparer.Instance).ToArray());

  public static void Restore(ObservableCollection<FormColumnVm> columns, IReadOnlyList<FormColumnVm> order)
  {
    for (var index = 0; index < order.Count; index++)
    {
      var current = columns.IndexOf(order[index]);
      if (current != index) columns.Move(current, index);
    }
  }
}
