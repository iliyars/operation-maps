namespace OperationMaps.Wpf.Features.OwnForm;

/// <summary>One selectable candidate parameter for load-factor calculation.</summary>
public sealed record LoadFactorOptionVm(int FormParameterId, int RowNumber, string DisplayName)
{
  public string Label => $"{DisplayName} (стр. {RowNumber})";
}
