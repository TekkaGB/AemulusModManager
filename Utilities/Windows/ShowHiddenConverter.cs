using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace AemulusModManager.Utilities.Windows
{
    public class ShowHiddenConverter : IMultiValueConverter
    {
        public object Convert(object[] showingHidden, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // If the row isn't hidden or hidden are being shown
                return !(bool)showingHidden[0] || (bool)showingHidden[1] ? false : true;
            } 
            catch
            {
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

}
