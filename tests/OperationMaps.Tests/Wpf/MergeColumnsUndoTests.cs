using System.Collections.ObjectModel;
using OperationMaps.Application.Importing;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Features.OwnForm;
using OperationMaps.Wpf.Features.OwnForm.Commands;
using OperationMaps.Wpf.Infrastructure.Commands;

namespace OperationMaps.Tests.Wpf;

public sealed class MergeColumnsUndoTests
{
  [Theory]
  [InlineData(false, false)]
  [InlineData(true, false)]
  [InlineData(false, true)]
  [InlineData(true, true)]
  public void Undo_and_redo_restore_original_instances_and_order(bool reverseSelection, bool adjacent)
  {
    var first = Column("C2");
    var second = Column("C10");
    var before = Column("C1");
    var between = Column("C5");
    var after = Column("C20");
    FormColumnVm[] original = adjacent
      ? [before, first, second, after]
      : [before, first, between, second, after];
    var list = new ObservableCollection<FormColumnVm>(original);
    var history = new UndoRedoStack();
    var command = new MergeColumnsCommand(list,
      reverseSelection ? second : first, reverseSelection ? first : second);

    history.Execute(command);
    AssertMerged();
    for (var cycle = 0; cycle < 2; cycle++)
    {
      Assert.True(history.Undo());
      Assert.Equal(original.Length, list.Count);
      for (var index = 0; index < original.Length; index++)
        Assert.Same(original[index], list[index]);
      Assert.Equal(["C2"], first.Component.Entry.Imported.Positions);
      Assert.Equal(["C10"], second.Component.Entry.Imported.Positions);
      Assert.True(history.Redo());
      AssertMerged();
    }

    void AssertMerged()
    {
      Assert.Equal(original.Length - 1, list.Count);
      Assert.Same(before, list[0]);
      Assert.Equal(["C2", "C10"], list[1].Component.Entry.Imported.Positions);
      Assert.Same(after, list[^1]);
      if (!adjacent) Assert.Same(between, list[2]);
      Assert.DoesNotContain(first, list);
      Assert.DoesNotContain(second, list);
    }
  }

  private static FormColumnVm Column(string position) => new(new ProjectComponentVm(new ComponentMatchEntry
  {
    Imported = new ImportedComponent
    {
      RawName = "Одинаковый компонент", DetectedCategory = "Конденсатор", Positions = [position],
    },
    MatchResult = new MatchResult { IsMatched = false },
  }));
}
