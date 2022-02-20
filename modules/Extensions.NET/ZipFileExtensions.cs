using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.IO;
using System.Threading;

namespace JemExtensions
{
    public static class ZipFileExtensions
    {
        public class ZipProgress
        {
            public ZipProgress(int total, int processed, string currentItem)
            {
                Total = total;
                Processed = processed;
                CurrentItem = currentItem;
            }
            public int Total { get; }
            public int Processed { get; }
            public string CurrentItem { get; }
        }

        public static void ExtractToDirectory(this ZipArchive source, string destinationDirectoryName, IProgress<ZipProgress> progress, CancellationTokenSource cancelSource)
        {
            ExtractToDirectory(source, destinationDirectoryName, progress, overwrite: false, cancelSource);
        }
        public static void ExtractToDirectory(this ZipArchive source, string destinationDirectoryName, IProgress<ZipProgress> progress)
        {
            ExtractToDirectory(source, destinationDirectoryName, progress, overwrite: false, cancelSource: new CancellationTokenSource());
        }

        public static void ExtractToDirectory(this ZipArchive source, string destinationDirectoryName, IProgress<ZipProgress> progress, bool overwrite, CancellationTokenSource cancelSource)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (destinationDirectoryName == null)
                throw new ArgumentNullException(nameof(destinationDirectoryName));


            // Rely on Directory.CreateDirectory for validation of destinationDirectoryName.

            // Note that this will give us a good DirectoryInfo even if destinationDirectoryName exists:
            DirectoryInfo di = Directory.CreateDirectory(destinationDirectoryName);
            string destinationDirectoryFullPath = di.FullName;

            int count = 0;
            foreach (ZipArchiveEntry entry in source.Entries)
            {

                if (cancelSource.IsCancellationRequested) throw new TaskCanceledException(nameof(source));

                count++;
                string fileDestinationPath = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, entry.FullName));

                if (!fileDestinationPath.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("File is extracting to outside of the folder specified.");

                var zipProgress = new ZipProgress(source.Entries.Count, count, entry.FullName);
                progress.Report(zipProgress);

                if (Path.GetFileName(fileDestinationPath).Length == 0)
                {
                    // If it is a directory:

                    if (entry.Length != 0)
                        throw new IOException("Directory entry with data.");

                    Directory.CreateDirectory(fileDestinationPath);
                }
                else
                {
                    // If it is a file:
                    // Create containing directory:
                    Directory.CreateDirectory(Path.GetDirectoryName(fileDestinationPath));
                    entry.ExtractToFile(fileDestinationPath, overwrite: overwrite);
                }
            }
        }
    }
}
