using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace JemExtensions.WPF.Converters
{
    public sealed class NullToBoolConverter : NullConverterBase<bool>
    {
        public NullToBoolConverter() : base(true, false) { }
    }
}
