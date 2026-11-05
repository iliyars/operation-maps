using System.Text.RegularExpressions;

namespace OperationMaps.Infrastructure.Services;

internal static partial class WordComponentName
{
  public static string PreserveFamilyPrefix(string fullName, string familyName)
  {
    var prefix = QualityPrefix().Match(fullName.Trim());
    return prefix.Success && !QualityPrefix().IsMatch(familyName)
      ? prefix.Groups[1].Value + " " + familyName : familyName;
  }

  [GeneratedRegex(@"^(ОСМ|ОС)(?:\s+|[-‐‑‒–—]\s*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
  private static partial Regex QualityPrefix();
}
