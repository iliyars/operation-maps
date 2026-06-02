using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace OperationMaps.Wpf.Shared.Converters
{
    public class CornerVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility verticalVisibility && verticalVisibility == Visibility.Visible)
            {
                // Если вертикальный виден, угол нужен
                return Visibility.Visible;
            }
            return Visibility.Collapsed; // Иначе прячем
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
