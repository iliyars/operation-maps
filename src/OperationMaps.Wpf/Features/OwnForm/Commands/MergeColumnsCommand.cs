using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using OperationMaps.Wpf.Features.Components.Commands;
using OperationMaps.Wpf.Infrastructure.Commands;

namespace OperationMaps.Wpf.Features.OwnForm.Commands
{
  public class MergeColumnsCommand : IUndoableCommand
  {

    private readonly ObservableCollection<FormColumnVm> _columns;
    private readonly FormColumnVm _first;
    private readonly FormColumnVm _second;

    private FormColumnVm? _merged;

    private int _firstIndex;
    private int _secondIndex;
    private readonly ColumnMergePlan _plan;
    private readonly Action<FormColumnVm>? _recalculate;
    private FormColumnVm[]? _beforeMerge;

    public string Description =>
       $"Объединить {_first.Name} + {_second.Name}";

    public MergeColumnsCommand(
            ObservableCollection<FormColumnVm> columns,
            FormColumnVm first,
            FormColumnVm second, ColumnMergePlan? plan = null, Action<FormColumnVm>? recalculate = null)
    {
      _columns = columns ?? throw new ArgumentNullException(nameof(columns));
      _first = first ?? throw new ArgumentNullException(nameof(first));
      _second = second ?? throw new ArgumentNullException(nameof(second));
      _plan = plan ?? new ColumnMergePlan(first, second);
      _recalculate = recalculate;
    }
    public void Execute()
    {
      if (ReferenceEquals(_first, _second) || !string.Equals(_first.Name, _second.Name, StringComparison.Ordinal))
        throw new InvalidOperationException("Объединять можно только два разных столбца с одинаковым полным наименованием компонента.");
      if (!_columns.Contains(_first) || !_columns.Contains(_second))
        throw new InvalidOperationException("Оба объединяемых столбца должны находиться в форме.");
      if (_first.Component.Entry.Imported.Positions.Intersect(_second.Component.Entry.Imported.Positions).Any())
        throw new InvalidOperationException("Позиции объединяемых столбцов не должны повторяться.");
      _firstIndex = _columns.IndexOf(_first);
      _secondIndex = _columns.IndexOf(_second);
      _beforeMerge = _columns.ToArray();

      var mergedPositions = _first.Component.Entry.Imported.Positions
        .Concat(_second.Component.Entry.Imported.Positions)
        .Distinct()
        .OrderBy(p => p, PositionComparer.Instance)
        .ToList();

      _merged = _plan.Build(mergedPositions);
      _recalculate?.Invoke(_merged);

      var insertAt = Math.Min(_firstIndex, _secondIndex);
      _columns.Remove(_first);
      _columns.Remove(_second);
      _columns.Insert(insertAt, _merged);
      ColumnPositionOrder.Sort(_columns);
    }

    public void Undo()
    {
      if (_merged is null) return;

      var index = _columns.IndexOf(_merged);
      _columns.Remove(_merged);

      var insertFirst = Math.Min(_firstIndex, _secondIndex);
      var insertSecond = Math.Max(_firstIndex, _secondIndex);

      _columns.Insert(insertFirst, _firstIndex < _secondIndex ? _first : _second);
      _columns.Insert(insertSecond, _firstIndex < _secondIndex ? _second : _first);
      if (_beforeMerge is not null) ColumnPositionOrder.Restore(_columns, _beforeMerge);
    }
  }
}
