using System.Text.RegularExpressions;
using OperationMaps.Application.Importing;

namespace OperationMaps.Application.Word;

public static partial class Form3DataBuilder
{
  public const string Title = "Перечень комплектов карт сборочных единиц низшей ступени";

  public static bool IsBoard(ImportedComponent component) =>
    string.Equals(component.DetectedCategory, "Плата", StringComparison.OrdinalIgnoreCase);

  public static WordFormData Build(IEnumerable<ImportedComponent> components, string designation) => new()
  {
    FormNumber = "3",
    DocumentDesignation = designation,
    Components = components.Where(IsBoard).OrderBy(c => c.ImportIndex).Select(c =>
    {
      var match = DecimalNumberRegex().Match(c.RawName);
      var name = match.Success ? c.RawName.Remove(match.Index, match.Length).Trim() : c.RawName.Trim();
      if (!name.StartsWith("Плата", StringComparison.OrdinalIgnoreCase)) name = $"Плата {name}".Trim();
      return new WordComponentData
      {
        Name = name,
        DecimalNumber = match.Value,
        Quantity = c.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture)
      };
    }).ToList()
  };

  [GeneratedRegex(@"[\p{L}\p{N}]+\.\d+\.\d+(?:[.-][\p{L}\p{N}]+)*")]
  private static partial Regex DecimalNumberRegex();
}
