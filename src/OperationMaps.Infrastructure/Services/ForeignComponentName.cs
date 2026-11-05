using System.Text.RegularExpressions;

namespace OperationMaps.Infrastructure.Services;

internal static class ForeignComponentName
{
  // Remove only a recognized manufacturer after a complete Latin part number.
  // Arbitrary trailing words may be meaningful values or qualification marks.
  public static string WithoutManufacturer(string name) => Regex.Replace(name.Trim(),
    @"(?<part>\b(?=[A-Za-z0-9/._+-]*[A-Za-z])(?=[A-Za-z0-9/._+-]*[0-9])[A-Za-z0-9/._+-]+)\s+Microsemi\s*$",
    "${part}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
}
