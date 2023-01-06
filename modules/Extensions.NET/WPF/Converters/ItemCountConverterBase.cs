using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace JemExtensions.WPF.Converters
{
    public class ItemCountConverterBase<T> : IValueConverter
    {
        public ItemCountConverterBase(T trueValue, T falseValue, int targetValue)
        {
            True = trueValue;
            False = falseValue;
            TargetValue = targetValue;
        }

        public T True { get; set; }
        public T False { get; set; }
        public int TargetValue { get; set; }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int && ((int)value == TargetValue) ? True : False;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is T && EqualityComparer<T>.Default.Equals((T)value, True);
        }
    }
}
