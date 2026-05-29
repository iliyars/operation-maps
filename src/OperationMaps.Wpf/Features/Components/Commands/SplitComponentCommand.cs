using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using OperationMaps.Application.Importing;
using OperationMaps.Wpf.Infrastructure.Commands;

namespace OperationMaps.Wpf.Features.Components.Commands
{
  public class SplitComponentCommand : IUndoableCommand
  {
    private readonly ObservableObject<ProjectComponentVm> _list;
    private readonly ProjectComponentVm _original;
    private readonly IReadOnlyList<string> _leftPositions;
    private readonly IReadOnlyList<string> _rightPositions;

    private ProjectComponentVm? _left;
    private ProjectComponentVm? _right;

    public string Description =>
        $"Разбить {_original.Name} [{string.Join(", ", _leftPositions)}] / [{string.Join(", ", _rightPositions)}]";

    public SplitComponentCommand(
      ObservableCollection<ProjectComponentVm> list,
      ProjectComponentVm original,
      IReadOnlyList<string> leftPositions,
      IReadOnlyList<string> rightPositions)
    {
      _list = list ?? throw new ArgumentNullException(nameof(list));
      _original = original ?? throw new ArgumentNullException(nameof(original));
      _leftPositions = leftPositions ?? throw new ArgumentNullException(nameof(leftPositions));
      _rightPositions = rightPositions ?? throw new ArgumentNullException(nameof(rightPositions));
    }

    public void Execute()
    {
      var index = _list.IndexOf(_original);

      // Clone the entry for the right half
      _left = CloneWithPositions(_original, _leftPositions);
      _right = CloneWithPositions(_original, _rightPositions);

      _list.RemoveAt(index);
      _list.Insert(index, _left);
      _list.Insert(index + 1, _right);
    }

    public void Undo()
    {
      if (_left is null || _right is null) return;

      var index = _list.IndexOf(_left);

      _list.Remove(_left);
      _list.Remove(_right);
      _list.Insert(index, _original);

      // Restore original positions
      _original.SetPositions(_leftPositions.Concat(_rightPositions).ToList());
    }

    private static ProjectComponentVm CloneWithPosition(
      ProjectComponentVm source,
      IReadOnlyList<string> position)
    {
      // Deep-clone the ImportedComponent with new positions
      var clonedImported = new ImportedComponent
      {
        RawName = source.Entry.Imported.RawName,
        DetectedCategory = source.Entry.Imported.DetectedCategory,
        Positions = positions.ToList(),
        RawPositions = string.Join(", ", positions),
      };

      var clonedEntry = new ComponentMatchEntry
      {
        Imported = clonedImported,
        MatchResult = source.Entry.MatchResult,
      };

      return new ProjectComponentVm(clonedEntry);
    }



  }
}
