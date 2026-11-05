using OperationMaps.Wpf.Features.OwnForm.SplitDialog;

namespace OperationMaps.Tests.Wpf;

public sealed class SplitSelectionTests
{
  [Fact]
  public void Control_toggles_selection_and_plain_click_on_another_item_replaces_it()
  {
    var vm = new SplitDialogViewModel(["VD1", "VD2", "VD3"]);
    var items = vm.SourcePositions.ToArray();
    vm.SelectPosition(items[0], false);
    vm.SelectPosition(items[1], true);
    Assert.Equal(items.Take(2), vm.SelectedPositions);
    vm.SelectPosition(items[0], true);
    Assert.Equal([items[1]], vm.SelectedPositions);
    vm.SelectPosition(items[2], false);
    Assert.Equal([items[2]], vm.SelectedPositions);
  }

  [Fact]
  public void Selected_positions_move_together_without_duplicates_including_back_to_source()
  {
    var vm = new SplitDialogViewModel(["VD1", "VD2", "VD3"]);
    var items = vm.SourcePositions.ToArray();
    vm.MoveToGroup(items[1], vm.Groups[0]);
    vm.SelectPosition(items[0], true);
    vm.SelectPosition(items[1], true);
    var selection = vm.SelectedPositions.ToArray();
    foreach (var item in selection) vm.MoveToGroup(item, vm.Groups[1]);
    Assert.Equal(items.Take(2), vm.Groups[1].Positions);
    Assert.Empty(vm.Groups[0].Positions);
    Assert.Equal(1, vm.UnassignedCount);
    foreach (var item in selection) vm.MoveToSource(item);
    vm.MoveToSource(items[0]);
    Assert.Equal(items, vm.SourcePositions);
    Assert.Equal(3, vm.UnassignedCount);
    Assert.Empty(vm.Groups[1].Positions);
  }
}
