using System;
using System.Globalization;
using System.Windows.Data;

namespace DeskViz.App.Converters
{
    /// <summary>
    /// Converts MHz to GHz for display
    /// </summary>
    public class MHzToGHzConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float mhz)
            {
                return mhz / 1000f;
            }
            return 0f;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float ghz)
            {
                return ghz * 1000f;
            }
            return 0f;
        }
    }
}