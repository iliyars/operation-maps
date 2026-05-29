namespace OperationMaps.Application.Importing;

/// <summary>
/// Одна импортированная строка перечня (до сохранения в ЬД)
/// </summary>
public sealed class ImportedComponent
{
  public required string RawName { get; init; }          // field_5, наименование целиком
  public required string DetectedCategory { get; init; }  // первое слово: "Резистор"...
  public required IReadOnlyList<string> Positions { get; set; } // развёрнутые: R1,R2,R3
  public string? RawPositions { get; init; }             // как было в файле: "R1 - R3"
  public int Quantity => Positions.Count;
}
