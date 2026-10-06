using System.Windows;

namespace JemExtensions.WPF.Converters
{
    public sealed class BooleanToVisibilityConverter : BooleanConverterBase<Visibility>
    {
        public BooleanToVisibilityConverter() : base(Visibility.Visible, Visibility.Collapsed) { }
    }

    public sealed class InvertableBooleanToVisibilityConverter : BooleanConverterBase<Visibility>
    {
        public InvertableBooleanToVisibilityConverter() : base(Visibility.Collapsed, Visibility.Visible) { }
    }
}
