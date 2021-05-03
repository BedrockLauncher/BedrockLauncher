using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Installer
{
    public static class Extensions
    {
        private const int _DefaultBufferSize = 1000;

        /// <summary>
        /// Copys a stream to another stream
        /// </summary>
        /// <param name="source">The source <see cref="Stream"/> to copy from</param>
        /// <param name="sourceLength">The length of the source stream, 
        /// if known - used for progress reporting</param>
        /// <param name="destination">The destination <see cref="Stream"/> to copy to</param>
        /// <param name="bufferSize">The size of the copy block buffer</param>
        /// <param name="progress">An <see cref="IProgress{T}"/> implementation 
        /// for reporting progress</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task representing the operation</returns>
        public static async Task CopyToAsync(
            this Stream source,
            long sourceLength,
            Stream destination,
            int bufferSize,
            IProgress<KeyValuePair<long, long>> progress,
            CancellationToken cancellationToken)
        {
            if (0 == bufferSize)
                bufferSize = _DefaultBufferSize;
            var buffer = new byte[bufferSize];
            if (0 > sourceLength && source.CanSeek)
                sourceLength = source.Length - source.Position;
            var totalBytesCopied = 0L;
            if (null != progress)
                progress.Report(new KeyValuePair<long, long>(totalBytesCopied, sourceLength));
            var bytesRead = -1;
            while (0 != bytesRead && !cancellationToken.IsCancellationRequested)
            {
                bytesRead = await source.ReadAsync(buffer, 0, buffer.Length);
                if (0 == bytesRead || cancellationToken.IsCancellationRequested)
                    break;
                await destination.WriteAsync(buffer, 0, buffer.Length);
                totalBytesCopied += bytesRead;
                if (null != progress)
                    progress.Report(new KeyValuePair<long, long>(totalBytesCopied, sourceLength));
            }
            if (0 < totalBytesCopied)
                progress.Report(new KeyValuePair<long, long>(totalBytesCopied, sourceLength));
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
