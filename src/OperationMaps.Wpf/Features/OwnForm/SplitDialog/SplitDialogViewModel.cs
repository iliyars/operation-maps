using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OperationMaps.Wpf.Features.Components.Commands;

namespace OperationMaps.Wpf.Features.OwnForm.SplitDialog
{
  public sealed partial class SplitDialogViewModel : ObservableObject
  {
    public ObservableCollection<PositionItemVm> SourcePositions { get; } = [];
    public ObservableCollection<SplitGroupVm> Groups { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private int _unassignedCount;

    public bool CanConfirm => UnassignedCount == 0
                           && Groups.Count >= 2
                           && Groups.All(g => g.Positions.Count > 0);

    public SplitDialogViewModel(IReadOnlyList<string> positions)
    {
      foreach (var pos in positions.OrderBy(p => p, PositionComparer.Instance))
        SourcePositions.Add(new PositionItemVm(pos));

      UnassignedCount = SourcePositions.Count;

      AddGroupInternal("Группа 1");
      AddGroupInternal("Группа 2");
    }

    [RelayCommand]
    private void AddGroup() => AddGroupInternal($"Группа {Groups.Count + 1}");

    public IEnumerable<PositionItemVm> SelectedPositions => SourcePositions
        .Concat(Groups.SelectMany(g => g.Positions)).Where(p => p.IsSelected);

    public void SelectPosition(PositionItemVm item, bool toggle)
    {
      if (toggle) item.IsSelected = !item.IsSelected;
      else if (!item.IsSelected)
      {
        foreach (var selected in SelectedPositions.ToArray()) selected.IsSelected = false;
        item.IsSelected = true;
      }
    }

    public void MoveToGroup(PositionItemVm item, SplitGroupVm targetGroup)
    {
      if (SourcePositions.Remove(item))
        UnassignedCount--;
      else
        foreach (var g in Groups)
          g.Positions.Remove(item);

      InsertSorted(targetGroup.Positions, item);
      RefreshCanConfirm();
    }

    public void MoveToSource(PositionItemVm item)
    {
      if (SourcePositions.Contains(item)) return;
      foreach (var g in Groups)
        g.Positions.Remove(item);

      InsertSorted(SourcePositions, item);
      UnassignedCount = SourcePositions.Count;
      RefreshCanConfirm();
    }

    public IReadOnlyList<IReadOnlyList<string>> GetResult()
       => Groups
           .Where(g => g.Positions.Count > 0)
           .Select(g => (IReadOnlyList<string>)g.Positions.Select(p => p.Value).ToList())
           .ToList();

    private void AddGroupInternal(string name)
    {
      var group = new SplitGroupVm(name);
      group.RemoveRequested += OnGroupRemoveRequested;
      Groups.Add(group);
    }

    private void OnGroupRemoveRequested(SplitGroupVm group)
    {
      foreach (var pos in group.Positions.ToList())
        MoveToSource(pos);

      group.RemoveRequested -= OnGroupRemoveRequested;
      Groups.Remove(group);
      RefreshCanConfirm();
    }

    private void RefreshCanConfirm()
    {
      UnassignedCount = SourcePositions.Count;
      OnPropertyChanged(nameof(CanConfirm));
    }

    private static void InsertSorted(
        ObservableCollection<PositionItemVm> collection,
        PositionItemVm item)
    {
      int index = 0;
      while (index < collection.Count
             && PositionComparer.Instance.Compare(collection[index].Value, item.Value) < 0)
        index++;
      collection.Insert(index, item);
    }

  }
}
