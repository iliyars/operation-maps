using System.Text.RegularExpressions;

namespace OperationMaps.Application.Word;

/// <summary>
/// Collapses consecutive positional designations into ranges for Word export,
/// e.g. ["R1","R2","R3","R5","R7","R8","R9"] → "R1-R3, R5, R7-R9".
/// Only sequences of 3 or more consecutive numbers are collapsed;
/// shorter runs (1-2 items) are listed individually.
/// </summary>
public static class PositionRangeFormatter
{
  private static readonly Regex PositionRegex = new(@"^(?<prefix>\D*)(?<number>\d+)$", RegexOptions.Compiled);

  /// <summary>
  /// Formats a list of position strings (already expanded, e.g. from PositionExpander)
  /// into a compact range string for display in Word documents.
  /// </summary>
  public static string Format(IReadOnlyList<string> positions)
  {
    if (positions.Count == 0) return "";
    if (positions.Count == 1) return positions[0];

    // Group consecutive positions by prefix (e.g. "R", "C") preserving original order
    var groups = new List<(string Prefix, List<int> Numbers, List<string> Raw)>();

    foreach (var pos in positions)
    {
      var match = PositionRegex.Match(pos.Trim());
      if (!match.Success)
      {
        groups.Add((pos, new List<int>(), new List<string> { pos }));
        continue;
      }

      var prefix = match.Groups["prefix"].Value;
      var number = int.Parse(match.Groups["number"].Value);

      if (groups.Count > 0
                && groups[^1].Prefix == prefix
                && groups[^1].Numbers.Count > 0
                && number == groups[^1].Numbers[^1] + 1)
      {
        // Continues the current run
        groups[^1].Numbers.Add(number);
        groups[^1].Raw.Add(pos);
      }
      else
      {
        // Starts a new run
        groups.Add((prefix, new List<int> { number }, new List<string> { pos }));
      }
    }
    var parts = new List<string>();
    foreach (var (prefix, numbers, raw) in groups)
    {
      if (numbers.Count >= 3)
      {
        // Collapse into range: prefix+first - prefix+last
        parts.Add($"{prefix}{numbers[0]}-{prefix}{numbers[^1]}");
      }
      else
      {
        // List individually (1-2 items, or non-numeric raw entries)
        parts.AddRange(raw);
      }
    }

    return string.Join(", ", parts);

  }
}
