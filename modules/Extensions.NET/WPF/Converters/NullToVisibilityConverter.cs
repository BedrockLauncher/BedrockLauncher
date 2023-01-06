using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace JemExtensions.WPF.Converters
{
    public sealed class NullToVisibilityConverter : NullConverterBase<Visibility>
    {
        public NullToVisibilityConverter() : base(Visibility.Visible, Visibility.Collapsed) { }
    }
}
