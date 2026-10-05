using System.Windows.Media.Imaging;

namespace JemExtensions.WPF.Converters
{
    public sealed class BooleanToBitmapImageConverter : BooleanConverterBase<BitmapImage>
    {
        public BooleanToBitmapImageConverter() : base(null, null) { }
    }
}
