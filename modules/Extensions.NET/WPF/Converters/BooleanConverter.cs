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
