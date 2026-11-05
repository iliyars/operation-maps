using System.Globalization;

namespace OperationMaps.Application.Services;

/// <summary>Evaluates the supported rowN+rowM sums independently of the UI.</summary>
public static class ParameterFormulaCalculator
{
  public static string? Calculate(string formula, Func<int, string?> valueOfRow)
  {
    var terms = formula.Split('+');
    double sum = 0;
    var hasValue = false;
    foreach (var term in terms)
    {
      var token = term.Trim();
      if (!token.StartsWith("row", StringComparison.OrdinalIgnoreCase)
          || !int.TryParse(token.AsSpan(3), NumberStyles.None, CultureInfo.InvariantCulture, out var row)
          || row < 1) return null;

      var raw = valueOfRow(row)?.Trim().Replace(',', '.');
      // Preserve the existing sum rule: missing/non-numeric operands are skipped.
      if (!double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)) continue;
      if (!double.IsFinite(value)) return null;
      sum += value;
      if (!double.IsFinite(sum)) return null;
      hasValue = true;
    }
    return hasValue ? sum.ToString("G", CultureInfo.CurrentCulture) : null;
  }
}
