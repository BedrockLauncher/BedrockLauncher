using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace JemExtensions.WPF.Converters
{
    public class NullConverterBase<T> : IValueConverter
    {
        public NullConverterBase(T trueValue, T falseValue)
        {
            True = trueValue;
            False = falseValue;
        }

        public T True { get; set; }
        public T False { get; set; }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? True : False;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}