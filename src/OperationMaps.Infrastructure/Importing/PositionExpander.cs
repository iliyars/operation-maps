using System.Text.RegularExpressions;

namespace OperationMaps.Infrastructure.Importing;

/// <summary>
/// Развёртывание позиционных обозначений: "R1 - R3" -> R1, R2, R3
/// </summary>
public static partial class PositionExpander
{
  // префикс(буквы) + число, опц. второй префикс, число — допускает любые пробелы и тире/дефис
  [GeneratedRegex(@"^([A-Za-zА-Яа-яЁё]+)\s*(\d+)\s*[-–—]\s*([A-Za-zА-Яа-яЁё]+)?\s*(\d+)$")]
  private static partial Regex RangeRegex();

  public static IReadOnlyList<string> Expand(string raw)
  {
    var result = new List<string>();
    if (string.IsNullOrWhiteSpace(raw)) return result;

    foreach (var rawPart in raw.Split(','))
    {
      var part = rawPart.Trim();
      if (part.Length == 0) continue;

      var m = RangeRegex().Match(part);
      if (m.Success)
      {
        var pfx1 = m.Groups[1].Value;
        var n1 = int.Parse(m.Groups[2].Value);
        var pfx2 = m.Groups[3].Success && m.Groups[3].Value.Length > 0
            ? m.Groups[3].Value : pfx1;
        var n2 = int.Parse(m.Groups[4].Value);

        if (pfx1 == pfx2 && n2 >= n1)
        {
          for (var i = n1; i <= n2; i++)
            result.Add($"{pfx1}{i}");
          continue;
        }
      }
      result.Add(part); // не диапазон — одиночная позиция
    }
    return result;
  }
}

