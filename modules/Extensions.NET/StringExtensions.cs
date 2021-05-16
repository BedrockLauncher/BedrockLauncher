using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtensionsDotNET
{
    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
        public static string Remove(this string source, string oldString, StringComparison comparison)
        {
            int index = source.IndexOf(oldString, comparison);

            while (index > -1)
            {
                source = source.Remove(index, oldString.Length);
                source = source.Insert(index, string.Empty);

                index = source.IndexOf(oldString, index + string.Empty.Length, comparison);
            }

            return source;
        }
        public static string Replace(this string source, string oldString, string newString, StringComparison comparison)
        {
            int index = source.IndexOf(oldString, comparison);

            while (index > -1)
            {
                source = source.Remove(index, oldString.Length);
                source = source.Insert(index, newString);

                index = source.IndexOf(oldString, index + newString.Length, comparison);
            }

            return source;
        }
    }
}
