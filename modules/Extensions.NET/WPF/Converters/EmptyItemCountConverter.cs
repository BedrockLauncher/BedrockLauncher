using System;
using System.Windows;
using System.Windows.Data;

namespace Extensions.WPF.Converters
{
    public sealed class EmptyItemCountConverter : ItemCountConverterBase<Visibility>
    {
        public EmptyItemCountConverter() : base(Visibility.Visible, Visibility.Collapsed, 0) { }
    }
}