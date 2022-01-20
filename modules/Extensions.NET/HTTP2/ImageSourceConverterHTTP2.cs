using Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Extensions.Http2
{

    public class ImageSourceConverterHttp2 : IValueConverter
    {
        static HttpClient Client = new HttpClient(new Http2Handler());
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;
            else return Init(value.ToString());
        }

        private ImageSource Init(string value)
        {
            try
            {
                var imageData = Client.GetByteArrayAsync(value).GetAwaiter().GetResult();
                using (MemoryStream ms = new MemoryStream(imageData))
                {
                    var imageSource = new BitmapImage();
                    imageSource.BeginInit();
                    imageSource.CacheOption = BitmapCacheOption.OnDemand;
                    imageSource.StreamSource = ms;
                    imageSource.EndInit();
                    return imageSource as ImageSource;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
