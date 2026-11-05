using OperationMaps.Wpf.Infrastructure.Commands;
using OperationMaps.Wpf.Features.Components.Commands;
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
    private FormColumnVm[]? _beforeSplit;
    private int _originalIndex;

    public string Description =>
        $"Разбить {_original.Name} на {_groups.Count} группы";

    public MultiSplitColumnCommand(
   ObservableCollection<FormColumnVm> columns,
   FormColumnVm original,
   IReadOnlyList<IReadOnlyList<string>> groups)
    {
      _columns = columns ?? throw new ArgumentNullException(nameof(columns));
      _original = original ?? throw new ArgumentNullException(nameof(original));
      ArgumentNullException.ThrowIfNull(groups);
      _groups = groups.Select(g => (IReadOnlyList<string>)(g?.ToArray()
        ?? throw new ArgumentException("Группа позиций не задана.", nameof(groups)))).ToArray();
    }

    public void Execute()
    {

      var index = _columns.IndexOf(_original);
      var positions = _original.Component.Entry.Imported.Positions;
      var distributed = _groups.SelectMany(g => g).ToArray();
      if (index < 0 || _groups.Count < 2 || _groups.Any(g => g.Count == 0)
          || distributed.Any(string.IsNullOrWhiteSpace)
          || distributed.Distinct(StringComparer.Ordinal).Count() != distributed.Length
          || positions.Count != distributed.Length
          || !positions.ToHashSet(StringComparer.Ordinal).SetEquals(distributed))
        throw new InvalidOperationException("Распределите все позиции исходного столбца ровно по одному разу между как минимум двумя непустыми группами.");
      _originalIndex = index;
      _beforeSplit = _columns.ToArray();
      _newColumns.Clear();

      // Build all new columns first
      foreach (var groupPositions in _groups)
        _newColumns.Add(_original.CloneWithPositions(groupPositions.OrderBy(p => p, PositionComparer.Instance).ToArray()));

      // Remove original, insert all new columns in order
      _columns.RemoveAt(index);
      for (int i = 0; i < _newColumns.Count; i++)
        _columns.Insert(index + i, _newColumns[i]);
      ColumnPositionOrder.Sort(_columns);
    }

    public void Undo()
    {
      if (_newColumns.Count == 0) return;

      // Remove all new columns
      foreach (var col in _newColumns)
        _columns.Remove(col);

      // Restore original
      _columns.Insert(_originalIndex, _original);
      if (_beforeSplit is not null) ColumnPositionOrder.Restore(_columns, _beforeSplit);
    }
  }
}
