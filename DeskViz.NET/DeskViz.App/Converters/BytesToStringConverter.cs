using System;
using System.Globalization;
using System.Windows.Data;

namespace DeskViz.App.Converters
{
    /// <summary>
    /// Converts bytes to human-readable format (B, KB, MB, GB, TB)
    /// </summary>
    public class BytesToStringConverter : IValueConverter
    {
        private static readonly string[] SizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB" };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                return FormatBytes(bytes);
            }
            return "0 B";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes == 0) return "0 B";

            int magnitude = 0;
            double adjustedSize = bytes;

            while (adjustedSize >= 1024 && magnitude < SizeSuffixes.Length - 1)
            {
                magnitude++;
                adjustedSize /= 1024;
            }

            return $"{adjustedSize:F1} {SizeSuffixes[magnitude]}";
        }
    }
}