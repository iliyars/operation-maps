using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OperationMaps.Wpf.Features.OwnForm.SplitDialog
{
  public sealed partial class PositionItemVm : ObservableObject
  {
    public PositionItemVm(string value)
    {
      Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;


  }
}
