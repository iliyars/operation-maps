using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OperationMaps.Wpf.Shared.Converters;

/// <summary>
/// Returns <see cref="Visibility.Collapsed"/> when true, <see cref="Visibility.Visible"/> when false.
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
  public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
      => value is true ? Visibility.Collapsed : Visibility.Visible;

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      => Binding.DoNothing;
}
