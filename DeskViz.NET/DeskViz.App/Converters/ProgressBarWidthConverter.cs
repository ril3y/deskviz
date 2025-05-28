using System;
using System.Globalization;
using System.Linq; 
using System.Windows; 
using System.Windows.Data;

namespace DeskViz.App.Converters
{
    /// <summary>
    /// Converter to calculate the width of a progress bar indicator
    /// </summary>
    public class ProgressBarWidthConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts values for a progress bar width calculation
        /// </summary>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if we have the expected number of values and if any are null or unset
            if (values == null || values.Length != 4 || values.Any(v => v == null || v == DependencyProperty.UnsetValue))
            {
                // If values are not ready, return 0 width.
                return 0.0;
            }

            // Safely attempt to parse doubles using invariant culture
            if (!double.TryParse(values[0]?.ToString(), System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out double value) ||
                !double.TryParse(values[1]?.ToString(), System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out double minimum) ||
                !double.TryParse(values[2]?.ToString(), System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out double maximum) ||
                !double.TryParse(values[3]?.ToString(), System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out double actualWidth))
            {
                // Parsing failed for one of the values
                return 0.0;
            }

            // Avoid division by zero if min and max are the same
            if (Math.Abs(maximum - minimum) < 0.0001) // Use tolerance for double comparison
            {
                return 0.0;
            }

            // Ensure width is not negative
            if (actualWidth < 0) actualWidth = 0;
            
            // Calculate percentage, clamping between 0 and 1
            double percentage = Math.Clamp((value - minimum) / (maximum - minimum), 0.0, 1.0);

            double calculatedWidth = percentage * actualWidth;

            // Ensure final width is not NaN and non-negative
            return double.IsNaN(calculatedWidth) ? 0.0 : Math.Max(0.0, calculatedWidth);
        }

        /// <summary>
        /// Not used
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
