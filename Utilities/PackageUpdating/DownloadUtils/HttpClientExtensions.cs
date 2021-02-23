using AemulusModManager.Utilities.PackageUpdating.DownloadUtils;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AemulusModManager
{
    // Taken from https://stackoverflow.com/a/46497896 
    public static class HttpClientExtensions
    {
        public static async Task DownloadAsync(this HttpClient client, string requestUri, Stream destination, string fileName, IProgress<DownloadProgress> progress = null, CancellationToken cancellationToken = default)
        {
            // Get the http headers first to examine the content length
            using (var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead))
            {
                var contentLength = response.Content.Headers.ContentLength;

                using (var download = await response.Content.ReadAsStreamAsync())
                {

                    // Ignore progress reporting when no progress reporter was 
                    // passed or when the content length is unknown
                    if (progress == null || !contentLength.HasValue)
                    {
                        await download.CopyToAsync(destination);
                        return;
                    }

                    // Convert absolute progress (bytes downloaded) into relative progress (0% - 100%)
                    var relativeProgress = new Progress<long>(totalBytes => progress.Report(new DownloadProgress((float)totalBytes / contentLength.Value, totalBytes, contentLength.Value, fileName)));
                    // Use extension method to report progress while downloading
                    await download.CopyToAsync(destination, 81920, relativeProgress, cancellationToken);
                    progress.Report(new DownloadProgress(1, contentLength.Value, contentLength.Value, fileName));
                }
            }
        }
    }

}
