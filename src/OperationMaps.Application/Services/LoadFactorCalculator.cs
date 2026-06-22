using System.Globalization;

namespace OperationMaps.Application.Services;

/// <summary>
/// Computes the "коэффициент нагрузки" (load factor) for an own-form
/// component: ratio of the user's "в схеме" value to the catalog's
/// "по НТД" value for a chosen base parameter, clamped to a 0.1 minimum,
/// with the base parameter's RowNumber appended in parentheses.
/// </summary>
public static class LoadFactorCalculator
{
  private const double MinimumValue = 0.1;

  /// <summary>
  /// Parses <paramref name="schemeValue"/> and <paramref name="ntdValue"/>
  /// as numbers (accepting both '.' and ',' as the decimal separator,
  /// matching how values are typically entered/stored in this app),
  /// computes scheme/ntd, clamps it to a minimum of 0.1, and formats the
  /// result as e.g. "0,35 (13)" — using the same decimal separator
  /// convention as the rest of the app (comma).
  /// Returns null when either value is missing, non-numeric, or the
  /// NTD value is zero (division is undefined).
  /// </summary>
  public static string? Calculate(string? schemeValue, string? ntdValue, int baseRowNumber)
  {
    if (!TryParse(schemeValue, out var scheme)) return null;
    if (!TryParse(ntdValue, out var ntd)) return null;
    if (ntd == 0) return null;

    var ratio = Math.Abs(scheme / ntd);
    if (ratio < MinimumValue) ratio = MinimumValue;

    var formatted = ratio.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');
    return $"{formatted} ({baseRowNumber})";
  }

  private static bool TryParse(string? value, out double result)
  {
    result = 0;
    if (string.IsNullOrWhiteSpace(value)) return false;

    var normalized = value.Trim().Replace(',', '.');
    return double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
  }
}
