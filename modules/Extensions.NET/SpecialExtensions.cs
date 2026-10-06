namespace JemExtensions
{
    public static class SpecialExtensions
    {
        public static bool IfAny<T>(T variable, params T[] values)
        {
            foreach (T value in values)
                if (variable.Equals(value)) return true;
            return false;
        }
    }
}
