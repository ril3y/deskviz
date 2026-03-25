using System;
using System.Globalization;
using System.Windows.Data;

namespace DeskViz.App.Converters
{
    public class TemperatureToStringConverter : IValueConverter
    {
        /// <summary>
        /// Instance property — each widget's XAML converter gets independent state.
        /// </summary>
        public bool UseFahrenheit { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float temperature)
            {
                if (float.IsNaN(temperature))
                    return "N/A";

                // ConverterParameter override (e.g. ConverterParameter=True)
                bool useFahrenheit = UseFahrenheit;
                if (parameter is bool paramBool)
                    useFahrenheit = paramBool;
                else if (parameter is string paramString && bool.TryParse(paramString, out bool result))
                    useFahrenheit = result;

                if (useFahrenheit)
                {
                    float fahrenheit = temperature * 9f / 5f + 32f;
                    return $"{fahrenheit:F1} °F";
                }

                return $"{temperature:F1} °C";
            }
            return "N/A";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
