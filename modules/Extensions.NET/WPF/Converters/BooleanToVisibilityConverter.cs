using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

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
