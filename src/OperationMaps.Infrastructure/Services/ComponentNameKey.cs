using System.Text;
using System.Text.RegularExpressions;

namespace OperationMaps.Infrastructure.Services;

/// <summary>Comparison only. Never use this key as a displayed or stored designation.</summary>
internal static class ComponentNameKey
{
  public static string Create(string name)
  {
    name = ForeignComponentName.WithoutManufacturer(name);
    var result = new StringBuilder(name.Length);
    foreach (var character in name.Normalize(NormalizationForm.FormC))
    {
      if (char.IsWhiteSpace(character) || character is '\u200b' or '\ufeff' or '\u00ad') continue;
      result.Append(character switch
      {
        '\u2010' or '\u2011' or '\u2012' or '\u2013' or '\u2014' or '\u2212' or '\ufe63' or '\uff0d' => '-',
        '«' or '»' or '“' or '”' or '„' or '‟' => '"',
        'А' => 'A', 'В' => 'B', 'Е' => 'E', 'К' => 'K', 'М' => 'M', 'Н' => 'H',
        'О' => 'O', 'Р' => 'P', 'С' => 'C', 'Т' => 'T', 'Х' => 'X',
        'а' => 'a', 'е' => 'e', 'о' => 'o', 'р' => 'p', 'с' => 'c', 'х' => 'x',
        _ => character
      });
    }
    // The separator before a letter suffix after a tolerance is typographical:
    // "...+-1% А" and "...+-1%-А" designate the same suffix. Keep all numeric separators.
    return Regex.Replace(result.ToString(), @"(?<=%)-(?=\p{L})", "");
  }
}
