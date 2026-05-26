using System.Text.RegularExpressions;
using OperationMaps.Application.Importing;

namespace OperationMaps.Infrastructure.Services;

public class ComponentNameParser : IComponentNameParser
{
  // Types where family is separated from name (R / C / L components)
  private static readonly string[] RlcTypes =
  {
        "Резистор",
        "Конденсатор",
        "Дроссель",
        "Индуктивность",
    };

  // Types where type is strictly first word, but Family == Name
  private static readonly string[] SingleWordTypes =
  {
        "Микросхема",
        "Генератор",
        "Диод",
        "Светодиод",
        "Вилка",
        "Микросборка",
        "Реле",
        "Транзистор",
    };

  private static readonly List<string> KnownFamilies = new()
    {
        "К10-17-4в", "К10-17а", "К10-17б", "К10-17в",
        "К10-42", "К10-43а", "К10-43б", "К10-43в",
        "К10-43Ма", "К10-43Мб", "К10-43Мв",
        "К10-50в", "К10-79", "К10-84в",
        "ОС-К53-68", "ОСМ Р1-8МП", "Р2-105",
        "Р1-8В", "К53-67", "ОС К53-68",
        "ОС К52-18", "Р2-108Б", "HC49S", "C0805",
        "C293D", "CR0603", "CR0805", "PLS-2", "PLD-10"
    };

  private static readonly Regex TypeAndRestRegex = new(
      @"^\s*(?<type>\S+)\s+(?<rest>.+)$",
      RegexOptions.Compiled);

  private static readonly Regex TuTailRegex = new(
      @"^(?<name>.+?)\s+[\p{L}\p{N}\.\-/]*ТУ.*",
      RegexOptions.Compiled | RegexOptions.CultureInvariant);

  public ParsedComponentName Parse(string rawName)
  {
    if (string.IsNullOrWhiteSpace(rawName))
      throw new ArgumentException("Component name is empty.", nameof(rawName));

    var working = rawName
        .Replace("?", string.Empty)
        .Trim();

    // 1. Split into Type + Rest
    var match = TypeAndRestRegex.Match(working);
    if (!match.Success)
    {
      return new ParsedComponentName
      {
        Raw = rawName,
        Type = string.Empty,
        Family = string.Empty,
        Name = working
      };
    }

    var firstWordType = match.Groups["type"].Value;
    var firstWordRest = match.Groups["rest"].Value;

    string type;
    string rest;

    bool isRcl = RlcTypes.Contains(firstWordType);
    bool isSingleWordType = SingleWordTypes.Contains(firstWordType);

    if (isRcl || isSingleWordType)
    {
      type = firstWordType;
      rest = firstWordRest;
    }
    else
    {
      // Universal heuristic: type = all words before first "designation"
      var tokens = working.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      int designationIndex = FindDesignationIndex(tokens);

      if (designationIndex > 0)
      {
        type = string.Join(" ", tokens[..designationIndex]);
        rest = string.Join(" ", tokens[designationIndex..]);
      }
      else
      {
        type = firstWordType;
        rest = firstWordRest;
      }
    }

    // 2. Remove TU / normative tail
    var tuMatch = TuTailRegex.Match(rest);
    if (tuMatch.Success)
      rest = tuMatch.Groups["name"].Value;

    rest = rest.Trim().TrimEnd(',', ';');

    // 3. Branch by type
    if (RlcTypes.Contains(type))
    {
      var family = ExtractFamilyForRcl(rest);
      return new ParsedComponentName
      {
        Raw = rawName,
        Type = type,
        Family = family,
        Name = rest
      };
    }
    else
    {
      var name = BuildNameFromRest(rest);
      return new ParsedComponentName
      {
        Raw = rawName,
        Type = type,
        Family = name,
        Name = name
      };
    }
  }

  private static string BuildNameFromRest(string rest)
  {
    if (string.IsNullOrWhiteSpace(rest))
      return string.Empty;

    var tokens = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (tokens.Length == 0)
      return string.Empty;

    int iDigit = FindDesignationIndex(tokens);

    if (iDigit < 0)
      return rest.Trim().TrimEnd(',', ';');

    if (iDigit == 0)
      return tokens[0].Trim().TrimEnd(',', ';');

    var nameTokens = new[] { tokens[iDigit - 1], tokens[iDigit] };
    return string.Join(' ', nameTokens).Trim().TrimEnd(',', ';');
  }

  private static int FindDesignationIndex(string[] tokens)
  {
    for (int i = 0; i < tokens.Length; i++)
    {
      if (tokens[i].Any(char.IsDigit))
        return i;
    }
    return -1;
  }

  private static string ExtractFamilyForRcl(string rest)
  {
    if (string.IsNullOrWhiteSpace(rest))
      return string.Empty;

    // Try to match any known family
    var known = KnownFamilies
        .FirstOrDefault(f => rest.Contains(f, StringComparison.OrdinalIgnoreCase));

    if (known is not null)
      return known;

    // Fallback: use first token and split by '-'
    var firstSpaceIndex = rest.IndexOf(' ');
    var token = firstSpaceIndex > 0 ? rest[..firstSpaceIndex] : rest;
    token = token.Trim();

    var segments = token.Split('-', StringSplitOptions.RemoveEmptyEntries);

    if (segments.Length >= 2)
      return $"{segments[0]}-{segments[1]}";

    return token;
  }
}
