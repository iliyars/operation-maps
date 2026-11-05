using System.Collections.ObjectModel;
using OperationMaps.Wpf.Infrastructure.Commands;

namespace OperationMaps.Wpf.Features.OwnForm.Commands;

public sealed class SplitColumnCommand : IUndoableCommand
{
  private readonly MultiSplitColumnCommand _split;
  public string Description { get; }

  public SplitColumnCommand(ObservableCollection<FormColumnVm> columns, FormColumnVm original,
    IReadOnlyList<string> leftPositions, IReadOnlyList<string> rightPositions)
  {
    ArgumentNullException.ThrowIfNull(columns);
    ArgumentNullException.ThrowIfNull(original);
    ArgumentNullException.ThrowIfNull(leftPositions);
    ArgumentNullException.ThrowIfNull(rightPositions);
    Description = $"Разбить {original.Name} [{string.Join(", ", leftPositions)}] / [{string.Join(", ", rightPositions)}]";
    _split = new MultiSplitColumnCommand(columns, original, [leftPositions, rightPositions]);
  }

  public void Execute() => _split.Execute();
  public void Undo() => _split.Undo();
}
