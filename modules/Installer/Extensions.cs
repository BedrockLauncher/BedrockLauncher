using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Installer
{

    public static class FileExtentions
    {
        public static void CopyTo(this DirectoryInfo source, DirectoryInfo target)
        {
            if (!target.Exists)
                target.Create();

            foreach (var file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);

            foreach (var subdir in source.GetDirectories())
                subdir.CopyTo(target.CreateSubdirectory(subdir.Name));
        }

        public static bool IsFileInUse(string fileFullPath, bool throwIfNotExists)
        {
            if (System.IO.File.Exists(fileFullPath))
            {
                try
                {
                    //if this does not throw exception then the file is not use by another program
                    using (FileStream fileStream = File.OpenWrite(fileFullPath))
                    {
                        if (fileStream == null)
                            return true;
                    }
                    return false;
                }
                catch
                {
                    return true;
                }
            }
            else if (!throwIfNotExists)
            {
                return true;
            }
            else
            {
                throw new FileNotFoundException("Specified path is not exsists", fileFullPath);
            }
        }
    }
    public static class Extensions
    {
        private const int _DefaultBufferSize = 1000;

        public static async Task CopyToAsync(this Stream source, Stream destination, IProgress<long> progress, CancellationToken cancellationToken = default(CancellationToken), int bufferSize = 0x1000)
        {
            var buffer = new byte[bufferSize];
            int bytesRead;
            long totalRead = 0;
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                totalRead += bytesRead;
                Thread.Sleep(10);
                progress.Report(totalRead);
            }
        }
    }
}
