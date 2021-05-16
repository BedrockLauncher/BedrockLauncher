using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ExtensionsDotNET
{
    public static class FileExtensions
    {
        public static string GetAvaliableFileName(string fileName, string directory, string format = "_{0}")
        {
            int i = 0;

            string newFileName = fileName;

            if (!File.Exists(Path.Combine(directory, newFileName))) return newFileName;
            else while (File.Exists(Path.Combine(directory, newFileName)))
                {
                    i++;
                    newFileName = Path.GetFileNameWithoutExtension(fileName) + string.Format(format, i) + "." + Path.GetExtension(fileName);
                }

            return string.Empty;
        }
    }
}
