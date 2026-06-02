using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using OperationMaps.Wpf.Infrastructure.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OperationMaps.Wpf.Features.OwnForm.Commands
{
  public sealed class MultiSplitColumnCommand : IUndoableCommand
  {
    private readonly ObservableCollection<FormColumnVm> _columns;
    private readonly FormColumnVm _original;
    private readonly IReadOnlyList<IReadOnlyList<string>> _groups;
    private readonly List<FormColumnVm> _newColumns = [];

    public string Description =>
        $"Разбить {_original.Name} на {_groups.Count} группы";

    public MultiSplitColumnCommand(
   ObservableCollection<FormColumnVm> columns,
   FormColumnVm original,
   IReadOnlyList<IReadOnlyList<string>> groups)
    {
      _columns = columns ?? throw new ArgumentNullException(nameof(columns));
      _original = original ?? throw new ArgumentNullException(nameof(original));
      _groups = groups ?? throw new ArgumentNullException(nameof(groups));
    }

    public void Execute()
    {

            System.Diagnostics.Debug.WriteLine($"Original in Columns: {_columns.Contains(_original)}");
            System.Diagnostics.Debug.WriteLine($"Columns count: {_columns.Count()}");
            System.Diagnostics.Debug.WriteLine($"Groups count: {_groups.Count}");
            foreach (var g in _groups)
                System.Diagnostics.Debug.WriteLine($"  Group: [{string.Join(", ", g)}]");
            var index = _columns.IndexOf(_original);
      _newColumns.Clear();

      // Build all new columns first
      foreach (var groupPositions in _groups)
        _newColumns.Add(_original.CloneWithPositions(groupPositions));

      // Remove original, insert all new columns in order
      _columns.RemoveAt(index);
      for (int i = 0; i < _newColumns.Count; i++)
        _columns.Insert(index + i, _newColumns[i]);
    }

    public void Undo()
    {
      if (_newColumns.Count == 0) return;

      var index = _columns.IndexOf(_newColumns[0]);

      // Remove all new columns
      foreach (var col in _newColumns)
        _columns.Remove(col);

      // Restore original
      _columns.Insert(index, _original);
    }
  }
}
