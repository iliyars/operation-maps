using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OperationMaps.Wpf.Shared.Converters;

/// <summary>
/// Converts an enum value to Visibility.Visible when its string representation
/// matches the converter parameter, Collapsed otherwise.
/// Usage: Visibility="{Binding SomeEnumProperty, Converter={StaticResource EnumEqualsToVis}, ConverterParameter=SomeValue}"
/// </summary>
public sealed class EnumEqualsToVisConverter : IValueConverter
{
  public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is null || parameter is null) return Visibility.Collapsed;
    return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;
  }

  public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
      => throw new NotSupportedException();
}
