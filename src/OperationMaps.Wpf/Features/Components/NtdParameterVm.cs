using CommunityToolkit.Mvvm.ComponentModel;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Wpf.Infrastructure.ViewModels;

namespace OperationMaps.Wpf.Features.Components;

/// <summary>
/// Represents one row in the NTD parameters table of the detail panel.
/// </summary>
public sealed class NtdParameterVm : ObservableObject
{
  public int RowNumber { get; }
  public string Name { get; }
  public string? Unit { get; }
  public string Value { get; }

  public string DisplayName => Unit is not null ? $"{Name}, {Unit}" : Name;

  public NtdParameterVm(FamilyNtdValue ntdValue)
  {
    ArgumentNullException.ThrowIfNull(ntdValue);

    RowNumber = ntdValue.FormParameter.RowNumber;
    Name = ntdValue.FormParameter.Name;
    Unit = ntdValue.FormParameter.Unit;
    Value = ntdValue.Value;
  }
}
