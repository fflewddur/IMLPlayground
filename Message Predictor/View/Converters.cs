using LibIML;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace MessagePredictor.Converters
{
    public class LabelToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Label label = value as Label;
            if (label != null) {
                return label.ToString();
            } else {
                return "Unknown";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culter)
        {
            return DependencyProperty.UnsetValue;
        }
    }

}
