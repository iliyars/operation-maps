using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OperationMaps.Wpf.Features.Catalog
{
  public partial class ComponentTypeVm : ObservableObject
  {
    public int Id { get; init; }

    [ObservableProperty]
    private string _name = "";

  }
}
