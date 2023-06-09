using Spectre.Console;
using System.Diagnostics;
namespace Ptk;

public static class Utils
{
    public static void KillWine()
    {
        Process[] array = Process.GetProcesses();
        for (int i = 0; i < array.Length; i++)
        {
            using Process? process = array[i];
             if (process is not null && (process.ProcessName == "wineserver"|| process.ProcessName.Contains("wine64"))) {
                 process.Kill();         
             }
         
        }
    }
    /// <summary>
    /// Download the file at the specified uri
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> used to download</param>
    /// <param name="fileUri">The file uri</param>
    /// <param name="outputStream">The output <see cref="Stream"/></param>
    /// <param name="progress">The <see cref="ProgressTask"/> to use</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/></param>
    /// <returns>A new awaitable <see cref="Task"/></returns>
    public static async Task DownloadAsync(this HttpClient client, string fileUri, Stream outputStream, ProgressTask progress, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await client.GetAsync(fileUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        progress.MaxValue(response.Content.Headers.ContentLength ?? 0);
        progress.StartTask();
        var filename = fileUri[(fileUri.LastIndexOf('/') + 1)..];
        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[8192];
        while (true)
        {
            var bytesRead = await contentStream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
                break;
            progress.Increment(bytesRead);
            await outputStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }
    }
}