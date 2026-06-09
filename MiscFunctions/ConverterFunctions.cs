using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace YAESU_FT_891_Front_End
{
    public class HeightToParentConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            double height = (double)value;
            double adjustment = Convert.ToDouble(parameter);

            if (height - adjustment > 0)
                return height - adjustment;
            else
                return height;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            double height = (double)value;
            double adjustment = Convert.ToDouble(parameter);

            return height - adjustment;
        }
    }
}
