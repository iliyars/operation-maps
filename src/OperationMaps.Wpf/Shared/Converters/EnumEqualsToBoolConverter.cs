using System.Globalization;
using System.Windows.Data;

namespace OperationMaps.Wpf.Shared.Converters;

/// <summary>
/// Converts an enum value to true when its string representation matches the
/// converter parameter, false otherwise. Same comparison as
/// <see cref="EnumEqualsToVisConverter"/>, but for triggers that need a bool
/// (e.g. active-tab styling) rather than Visibility.
/// Usage: Tag="{Binding SomeEnumProperty, Converter={StaticResource EnumEqualsToBoolConverter}, ConverterParameter=SomeValue}"
/// </summary>
public sealed class EnumEqualsToBoolConverter : IValueConverter
{
  public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is null || parameter is null) return false;
    return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
  }

  public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
      => throw new NotSupportedException();
}
