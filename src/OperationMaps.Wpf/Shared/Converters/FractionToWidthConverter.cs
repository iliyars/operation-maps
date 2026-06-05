using System.Globalization;
using System.Windows.Data;

namespace OperationMaps.Wpf.Shared.Converters;

/// <summary>
/// Converts (fraction: double, totalWidth: double) → pixel width for progress bar.
/// </summary>
public sealed class FractionToWidthConverter : IMultiValueConverter
{
  public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
  {
    if (values.Length < 2) return 0d;
    if (values[0] is not double fraction) return 0d;
    if (values[1] is not double totalWidth) return 0d;

    return Math.Max(0, Math.Min(totalWidth, totalWidth * fraction));
  }

  public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
      => throw new NotSupportedException();
}
