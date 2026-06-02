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

    public string Description =>
       $"Объединить {_first.Name} + {_second.Name}";

    public MergeColumnsCommand(
            ObservableCollection<FormColumnVm> columns,
            FormColumnVm first,
            FormColumnVm second)
    {
      _columns = columns ?? throw new ArgumentNullException(nameof(columns));
      _first = first ?? throw new ArgumentNullException(nameof(first));
      _second = second ?? throw new ArgumentNullException(nameof(second));
    }
    public void Execute()
    {
      _firstIndex = _columns.IndexOf(_first);
      _secondIndex = _columns.IndexOf(_second);

      var mergedPositions = _first.Component.Entry.Imported.Positions
        .Concat(_second.Component.Entry.Imported.Positions)
        .Distinct()
        .OrderBy(p => p, PositionComparer.Instance)
        .ToList();

      _merged = _first.CloneWithPositions(mergedPositions);

      var insertAt = Math.Min(_firstIndex, _secondIndex);
      _columns.Remove(_first);
      _columns.Remove(_second);
      _columns.Insert(insertAt, _merged);
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
    }
  }
}
