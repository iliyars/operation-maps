using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using OperationMaps.Wpf.Infrastructure.Commands;

namespace OperationMaps.Wpf.Features.OwnForm.Commands
{
  public sealed class SplitColumnCommand : IUndoableCommand
  {
    private readonly ObservableCollection<FormColumnVm> _columns;
    private readonly FormColumnVm _original;
    private readonly IReadOnlyList<string> _leftPositions;
    private readonly IReadOnlyList<string> _rightPositions;

    private FormColumnVm? _left;
    private FormColumnVm _right;

    public string Description =>
        $"Разбить {_original.Name} [{string.Join(", ", _leftPositions)}] / [{string.Join(", ", _rightPositions)}]";

    public SplitColumnCommand(
        ObservableCollection<FormColumnVm> columns,
        FormColumnVm original,
        IReadOnlyList<string> leftPositions,
        IReadOnlyList<string> rightPositions)
    {
      _columns = columns ?? throw new ArgumentNullException(nameof(columns));
      _original = original ?? throw new ArgumentNullException(nameof(original));
      _leftPositions = leftPositions ?? throw new ArgumentNullException(nameof(leftPositions));
      _rightPositions = rightPositions ?? throw new ArgumentNullException(nameof(rightPositions));
    }

    public void Execute()
    {
      var index = _columns.IndexOf(_original);

      // Clone with positions — cell values are copied
      _left = _original.CloneWithPositions(_leftPositions);
      _right = _original.CloneWithPositions(_rightPositions);

      _columns.RemoveAt(index);
      _columns.Insert(index, _left);
      _columns.Insert(index + 1, _right);
    }

    public void Undo()
    {
      if (_left is null || _right is null) return;

      var index = _columns.IndexOf(_left);
      _columns.Remove(_left);
      _columns.Remove(_right);
      _columns.Insert(index, _original);
    }
  }
}
