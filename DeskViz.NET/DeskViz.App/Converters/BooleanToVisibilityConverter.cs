using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DeskViz.App.Converters
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return booleanValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed; // Default to collapsed if input is not bool
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}
