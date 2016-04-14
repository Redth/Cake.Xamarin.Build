using Cake.Common.Diagnostics;
using Cake.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Cake.Xamarin.Build
{
    internal static class HttpClientExtensions
    {
        private const int DefaultBufferSize = 4096;

        public static async Task DownloadFileAsync(this HttpClient client, Uri requestUri, string path, IProgress<int> progress = null)
        {
            var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            long? contentLength = null;

            if (progress != null && response.Content.Headers.ContentLength.HasValue)
            {
                contentLength = response.Content.Headers.ContentLength.Value;
            }

            using (var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var fileStream = File.Create(path, DefaultBufferSize))
            {
                var bytesRead = 0;
                var totalBytesRead = 0L;
                var buffer = new byte[DefaultBufferSize];

                while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);

                    totalBytesRead += bytesRead;

                    if (contentLength.HasValue)
                    {
                        var progressPercentage = totalBytesRead * 1d / (contentLength.Value * 1d);

                        progress.Report((int)(progressPercentage * 100));
                    }
                }
            }
        }

        internal class DownloadFileProgressReporter : IProgress<int>
        {
            public DownloadFileProgressReporter (ICakeContext context)
            {
                CakeContext = context;
            }

            int lastValue = 0;

            public ICakeContext CakeContext { get; private set; }
            public void Report(int value)
            {
                if (value == lastValue)
                    return;

                lastValue = value;

                if (value % 5 == 0)
                    CakeContext.Information("Downloaded {0}...%", value);
            }
        }
    }
}
