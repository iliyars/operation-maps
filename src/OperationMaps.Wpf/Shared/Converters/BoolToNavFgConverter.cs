using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace OperationMaps.Wpf.Shared.Converters
{
  /// <summary>
  /// Converts a boolean <c>IsActive</c> flag to a foreground brush:
  /// <c>true</c>  → accent blue #2B579A
  /// <c>false</c> → dark grey  #3A3A3A
  /// </summary>
  [ValueConversion(typeof(bool), typeof(SolidColorBrush))]
  public sealed class BoolToNavFgConverter : IValueConverter
  {
    private static readonly SolidColorBrush Active = Brush("#2B579A");
    private static readonly SolidColorBrush Inactive = Brush("#3A3A3A");

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Active : Inactive;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;

    private static SolidColorBrush Brush(string hex)
    {
      var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
      brush.Freeze();
      return brush;
    }
  }
}
