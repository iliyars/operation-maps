namespace OperationMaps.Application.Importing;

public sealed class ImportResult
{
  public required IReadOnlyList<ImportedComponent> Components { get; init; }
  public required IReadOnlyList<string> Warnings { get; init; }

  public string? DocumentTitle { get; init; }      // field_2
  public string? DocumentNumber { get; init; }     // field_3
  public string? DevelopedBy { get; init; }        // field_16
  public string? CheckedBy { get; init; }          // field_17
  public string? ApprovedBy { get; init; }         // field_18

  public int ComponentCount => Components.Count;
  public int PositionCount => Components.Sum(c => c.Quantity);
}
