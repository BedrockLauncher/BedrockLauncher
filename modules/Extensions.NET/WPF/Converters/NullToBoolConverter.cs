using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Extensions.WPF.Converters
{
    public sealed class NullToBoolConverter : NullConverterBase<bool>
    {
        public NullToBoolConverter() : base(true, false) { }
    }
}
