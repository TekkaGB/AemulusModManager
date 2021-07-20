using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace AemulusModManager.Utilities.Windows
{
    public class HiddenIconConverter : IValueConverter
    {
        public object Convert(object showingHidden, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)showingHidden ? "Solid_Eye" : "Solid_EyeSlash";
        }

        public object ConvertBack(object showingHidden, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

    }

}
