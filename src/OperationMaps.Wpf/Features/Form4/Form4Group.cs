using CommunityToolkit.Mvvm.ComponentModel;
using OperationMaps.Wpf.Features.Components;

namespace OperationMaps.Wpf.Features.Form4;

public sealed partial class Form4Group : ObservableObject
{
  public string DisplayName { get; init; } = "";
  public string Positions { get; init; } = "";
  public IReadOnlyList<NtdParameterVm> NtdValues { get; init; } = [];
  public IReadOnlyList<ProjectComponentVm> SourceComponents { get; init; } = [];

  public bool HasNtdValues => NtdValues.Count > 0;

  [ObservableProperty] private bool _isSelected;
}
