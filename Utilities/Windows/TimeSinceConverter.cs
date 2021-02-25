using AemulusModManager.Utilities.PackageUpdating;
using System;
using System.Windows.Data;

namespace AemulusModManager.Utilities.Windows
{
    public class TimeSinceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return StringConverters.FormatTimeSpan(DateTime.UtcNow - (DateTimeOffset)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

    }
}
