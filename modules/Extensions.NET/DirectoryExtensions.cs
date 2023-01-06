using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace JemExtensions
{
    public static class DirectoryExtensions
    {
        public delegate void ProgressDelegate(long current, long total, string text = null);

        public static void Copy(string sourceDirectory, string targetDirectory)
        {
            var diSource = new DirectoryInfo(sourceDirectory);
            var diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        public static async Task DeleteAsync(string strpath, ProgressDelegate progress, string phase1Text = null, string phase2Text = null)
        {
            await Task.Run(() =>
            {
                if (Directory.Exists(strpath))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(strpath);
                    var files = dirInfo.GetFiles();
                    progress(0, files.Length);

                    foreach (FileInfo file in files)
                    {
                        file.Delete();
                        progress(0, files.Length, phase1Text);
                    }

                    var dirs = dirInfo.GetDirectories();

                    progress(0, dirs.Length);

                    foreach (DirectoryInfo dir in dirs)
                    {
                        dir.Delete(true);
                        progress(0, files.Length, phase2Text);
                    }

                }
            });

        }
    }
}
