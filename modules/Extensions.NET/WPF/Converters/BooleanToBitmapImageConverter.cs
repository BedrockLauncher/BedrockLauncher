using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JemExtensions.WPF.Converters
{
    public sealed class BooleanToBitmapImageConverter : BooleanConverterBase<BitmapImage>
    {
        public BooleanToBitmapImageConverter() : base(null, null) { }
    }
}
