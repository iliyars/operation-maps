using CommunityToolkit.Mvvm.ComponentModel;

namespace OperationMaps.Wpf.ViewModels;

public partial class FamilyVm : ObservableObject
{
  public int Id { get; init; }
  public int ComponentTypeId { get; init; }

  [ObservableProperty]
  private string _name = "";
}
