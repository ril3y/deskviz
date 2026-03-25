using System;
using System.Globalization;
using System.Windows.Data;

namespace DeskViz.Widgets.Cpu
{
    /// <summary>
    /// Converter that returns temperature unit symbol based on boolean (true = °F, false = °C)
    /// </summary>
    public class TemperatureUnitConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool useFahrenheit)
            {
                return useFahrenheit ? "°F" : "°C";
            }
            return "°C";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}