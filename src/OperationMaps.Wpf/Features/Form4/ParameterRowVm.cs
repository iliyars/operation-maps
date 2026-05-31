using CommunityToolkit.Mvvm.ComponentModel;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Wpf.Infrastructure.ViewModels;

namespace OperationMaps.Wpf.Features.Form4;

/// <summary>
/// Represents one row header in the Form 4 table (parameter name + unit).
/// </summary>
public sealed class ParameterRowVm : ObservableObject
{
  public int RowNumber { get; }
  public string Name { get; }
  public string? Unit { get; }
  public string DisplayName => Unit is not null ? $"{Name}, {Unit}" : Name;

  public ParameterRowVm(FormParameter parameter)
  {
    ArgumentNullException.ThrowIfNull(parameter);
    RowNumber = parameter.RowNumber;
    Name = parameter.Name;
    Unit = parameter.Unit;
  }
}
