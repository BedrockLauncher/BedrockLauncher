using System.Windows;

namespace JemExtensions.WPF.Converters
{
    public sealed class NullToVisibilityConverter : NullConverterBase<Visibility>
    {
        public NullToVisibilityConverter() : base(Visibility.Visible, Visibility.Collapsed) { }
    }
}
