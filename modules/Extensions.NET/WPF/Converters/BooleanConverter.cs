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
    public sealed class BooleanConverter : BooleanConverterBase<bool>
    {
        public BooleanConverter() : base(true, false) { }
    }

    public sealed class InverseBooleanConverter : BooleanConverterBase<bool>
    {
        public InverseBooleanConverter() : base(false, true) { }
    }
}
