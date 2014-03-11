using LibIML;
using System;
using System.Collections;
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

    public class StringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Format(culture, (string)parameter, value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culter)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class LabelToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Label label = value as Label;
            if (label != null) {
                return true;
            } else {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culter)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class DictionaryItemFromLabelConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values != null && values.Length >= 2) {
                var myDict = values[0] as IDictionary;
                var myKey = values[1] as Label;
                if (myDict != null && myKey != null) {
                    //return myDict[myKey].ToString();
                    return myDict[myKey];
                }
            }
            return Binding.DoNothing;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class GraphWidthFromCountConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values != null && values.Length >= 3) {
                var count = values[0] as int?;
                var totalCount = values[1] as int?;
                var maxWidth = values[2] as int?;
                //Console.WriteLine("count={0} totalCount={1} maxWidth={2}", count, totalCount, maxWidth);
                maxWidth = 300; // FIXME hardcode this for now
                if (count != null && totalCount != null) {
                    return (count / (double)totalCount) * maxWidth;
                }
            }
            return Binding.DoNothing;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

