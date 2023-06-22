using System.Diagnostics;
using System.Text.Json;
using Spectre.Console;
namespace Ptk;

public static class Utils {
    public const string ReleaseUrl = "https://github.com/AndrewMD5/orion/releases";
    public const string ReleaseApi = "https://api.github.com/repos/andrewmd5/orion/releases";
    public static readonly Version CurrentVersion = new(0, 0, 7, 0);

    public static void KillWine() {
        Process[] array = Process.GetProcesses();
        for (int i = 0; i < array.Length; i++) {
            using Process? process = array[i];
            if (process is not null && (process.ProcessName == "wineserver" || process.ProcessName.Contains("wine64"))) {
                process.Kill();
            }

        }
    }

    /// <summary>
    /// Get the latest release from the github api
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/></param>
    public static async Task<Version?> GetLatestReleaseAsync(CancellationToken cancellationToken) {
        try {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "orion");
            using var response = await client.GetAsync(ReleaseApi, cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var element = doc.RootElement;
            var release = element.EnumerateArray().FirstOrDefault();
            if (!release.TryGetProperty("tag_name", out var tag)) return null;
            var version = tag.GetString();
            if (string.IsNullOrWhiteSpace(version)) return null;
            if (!Version.TryParse(version, out var result)) return null;
            return result;
        } catch {
            // so it works without internet
            return null;
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
    public static async Task DownloadAsync(this HttpClient client, string fileUri, Stream outputStream, ProgressTask progress, CancellationToken cancellationToken = default) {
        using HttpResponseMessage response = await client.GetAsync(fileUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        progress.MaxValue(response.Content.Headers.ContentLength ?? 0);
        progress.StartTask();
        var filename = fileUri[(fileUri.LastIndexOf('/') + 1)..];
        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[8192];
        while (true) {
            var bytesRead = await contentStream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
                break;
            progress.Increment(bytesRead);
            await outputStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }
    }
}