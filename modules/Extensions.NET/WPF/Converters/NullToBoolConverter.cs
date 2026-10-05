namespace JemExtensions.WPF.Converters
{
    public sealed class NullToBoolConverter : NullConverterBase<bool>
    {
        public NullToBoolConverter() : base(true, false) { }
    }
}
