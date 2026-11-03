using System.Text.RegularExpressions;

namespace OperationMaps.Infrastructure.Word;

/// <summary>Links note definitions to NTD values and removes star markers from stored values.</summary>
public static partial class WordNoteParser
{
  public static Dictionary<int, List<string>> Extract(string noteText, Dictionary<int, string> values)
  {
    var definitions = DefinitionRegex().Matches(noteText.Replace("\r\n", "\n"));
    var notes = new Dictionary<string, string>();
    foreach (Match definition in definitions)
      notes[definition.Groups[1].Value.TrimEnd('.', ')')] = definition.Groups[2].Value.Trim();
    var result = new Dictionary<int, List<string>>();
    foreach (var (row, value) in values.ToArray())
    {
      var linked = new List<string>();
      var clean = ReferenceRegex().Replace(value, match =>
      {
        if (!notes.TryGetValue(match.Value.TrimEnd(')'), out var text))
          return match.Value.StartsWith('*') ? "" : match.Value;
        if (!linked.Contains(text)) linked.Add(text);
        return "";
      });
      values[row] = clean.Trim().TrimEnd(',').Trim();
      if (linked.Count > 0) result[row] = linked;
    }
    return result;
  }

  [GeneratedRegex(@"(?ms)^\s*(\*+|\d+[.)])\s*(.+?)(?=^\s*(?:\*+|\d+[.)])\s*|\z)")]
  private static partial Regex DefinitionRegex();
  [GeneratedRegex(@"\*+|\d+\)")]
  private static partial Regex ReferenceRegex();
}
