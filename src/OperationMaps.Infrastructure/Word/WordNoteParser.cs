using System.Text.RegularExpressions;

namespace OperationMaps.Infrastructure.Word;

/// <summary>Links unambiguous note definitions and removes only resolved markers from NTD values.</summary>
public static partial class WordNoteParser
{
  public static bool HasContent(string? text) => !string.IsNullOrWhiteSpace(text)
    && text.Trim() is not ("—" or "–" or "-");

  public static Dictionary<int, List<string>> Extract(string noteText, Dictionary<int, string> values,
      ICollection<string>? errors = null)
  {
    var source = noteText.Replace("\r\n", "\n").Replace('\r', '\n');
    var definitions = DefinitionRegex().Matches(source);
    var notes = new Dictionary<string, string>();
    var ambiguous = new HashSet<string>();
    foreach (Match definition in definitions)
    {
      var marker = definition.Groups[1].Value.TrimEnd('.', ')');
      var text = definition.Groups[2].Value.Trim();
      if (text.Length == 0)
      {
        ambiguous.Add(marker);
        errors?.Add($"У маркера «{marker}» пустой текст примечания.");
      }
      else if (notes.TryGetValue(marker, out var previous) && previous != text)
      {
        ambiguous.Add(marker);
        errors?.Add($"Маркер «{marker}» определён несколько раз с разным текстом.");
      }
      else notes[marker] = text;
    }
    if (HasContent(source) && (definitions.Count == 0 || HasContent(source[..definitions[0].Index])))
      errors?.Add("Есть текст примечания без определённого маркера; уточните его связь со значениями.");
    var used = new HashSet<string>();
    var result = new Dictionary<int, List<string>>();
    foreach (var (row, value) in values.ToArray())
    {
      var linked = new List<string>();
      var unresolved = false;
      var clean = ReferenceRegex().Replace(value, match =>
      {
        if (!match.Value.StartsWith('*') && InsideParentheses(value, match.Index))
          return match.Value; // base-row numbers, temperature ranges, tolerances
        var marker = match.Value.TrimEnd(')');
        if (ambiguous.Contains(marker) || !notes.TryGetValue(marker, out var text))
        {
          unresolved = true;
          errors?.Add($"Строка {row}: маркер «{match.Value}» не имеет однозначного примечания. Исходное значение сохранено.");
          return match.Value;
        }
        used.Add(marker);
        if (!linked.Contains(text)) linked.Add(text);
        return "";
      });
      if (unresolved) continue;
      values[row] = clean.Trim().TrimEnd(',').Trim();
      if (linked.Count > 0) result[row] = linked;
    }
    foreach (var marker in notes.Keys.Where(m => !used.Contains(m) && !ambiguous.Contains(m)))
      errors?.Add($"Примечание «{marker}» не связано ни с одной строкой НТД.");
    return result;
  }

  private static bool InsideParentheses(string value, int index)
  {
    var depth = 0;
    foreach (var character in value.AsSpan(0, index))
    {
      if (character == '(') depth++;
      else if (character == ')' && depth > 0) depth--;
    }
    return depth > 0;
  }

  [GeneratedRegex(@"(?ms)^\s*(\*+[.)]?|\d+[.)])\s*(.+?)(?=^\s*(?:\*+[.)]?|\d+[.)])\s*|\z)")]
  private static partial Regex DefinitionRegex();
  [GeneratedRegex(@"\*+\)?|(?<![\d(])\d+\)")]
  private static partial Regex ReferenceRegex();
}
