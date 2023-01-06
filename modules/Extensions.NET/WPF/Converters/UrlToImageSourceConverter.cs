using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Globalization;

namespace JemExtensions.WPF.Converters
{
    public class UrlToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Uri.TryCreate(value.ToString(), UriKind.Absolute, out Uri result))
            {
                try
                {
                    var img = new BitmapImage(result);
                    return img;
                }
                catch
                {
                    return null;
                }
            }
            else return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
