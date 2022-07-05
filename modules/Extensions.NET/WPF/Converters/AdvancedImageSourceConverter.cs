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
    public class AdvancedImageSourceConverter : IValueConverter
    {
        public BitmapCacheOption CacheOption { get; set; } = BitmapCacheOption.Default;
        public UriKind UriKind { get; set; } = UriKind.RelativeOrAbsolute;
        public BitmapCreateOptions CreateOptions { get; set; } = BitmapCreateOptions.None;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Uri.TryCreate(value.ToString(), UriKind, out Uri result))
            {
                try
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = CacheOption;
                    image.CreateOptions = CreateOptions;
                    image.UriSource = result;
                    image.EndInit();

                    return image;
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
