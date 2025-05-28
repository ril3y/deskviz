using System;
using System.Globalization;
using System.Windows.Data;

namespace DeskViz.App.Converters
{
    public class TemperatureToStringConverter : IValueConverter
    {
        // Static property for enabling Fahrenheit display
        public static bool UseFahrenheit { get; set; } = false;

        public object Convert(object value, object parameter, CultureInfo culture)
        {
            return Convert(value, typeof(string), parameter, culture);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float temperature)
            {
                if (float.IsNaN(temperature))
                {
                    return "N/A"; // Or maybe string.Empty, depending on desired display
                }

                // Check if parameter is specifying Fahrenheit
                bool useFahrenheit = UseFahrenheit;
                
                // If parameter is provided, it overrides the static property
                if (parameter is bool paramBool)
                {
                    useFahrenheit = paramBool;
                }
                else if (parameter is string paramString && bool.TryParse(paramString, out bool result))
                {
                    useFahrenheit = result;
                }

                if (useFahrenheit)
                {
                    // Convert Celsius to Fahrenheit: F = C * 9/5 + 32
                    float fahrenheit = temperature * 9 / 5 + 32;
                    return $"{fahrenheit:F1} °F";
                }

                // Return as Celsius by default
                return $"{temperature:F1} °C"; 
            }
            return "N/A"; // Return N/A if value is not a float
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack is not needed for one-way binding
            throw new NotImplementedException();
        }
    }
}
