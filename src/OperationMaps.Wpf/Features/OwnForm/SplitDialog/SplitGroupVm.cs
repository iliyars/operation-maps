using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Drawing;

namespace OperationMaps.Wpf.Features.OwnForm.SplitDialog
{
  public sealed partial class SplitGroupVm : ObservableObject
  {
    public string Name { get; }

    public ObservableCollection<PositionItemVm> Positions { get; } = [];

    public event Action<SplitGroupVm>? RemoveRequested;

    [RelayCommand]
    private void Remove() => RemoveRequested?.Invoke(this);

    public SplitGroupVm(string name)
    {
      Name = name;
    }

  }
}
