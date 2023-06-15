using System.Text.RegularExpressions;
using Spectre.Console;

namespace Ptk;

public class Steam : BaseLauncher {

    public override string Name => "Steam";

    public Steam(string winePrefix) : base(winePrefix) { }

    public override List<App> GetInstalledApps() {
        var steamAppsDir = Path.Join(_winePrefix, "drive_c/Program Files (x86)/Steam/steamapps");
        if (!Directory.Exists(steamAppsDir)) {
            return new List<App>();
        }
        var acfFiles = Directory.GetFiles(steamAppsDir, "*.acf");
        var apps = new List<App>();
        for (var i = 0; i < acfFiles.Length; i++) {
            var acfFile = acfFiles[i];
            var app = ACFParser.ParseACF(acfFile);
            // not installed
            if (app.StateFlags is not "4") {
                continue;
            }
            if (app is { AppId: not null, LauncherPath: not null, Name: not null, InstallDir: not null }) {
                if (app.Name.Contains("Steamworks")) continue;
                apps.Add(new(app.Name, app.InstallDir, app.LauncherPath, $"-nochatui -nofriendsui -silent -applaunch {app.AppId}", app.AppId, AppPlatform.Steam));
            }
        }
        return apps;
    }

    public override bool IsInstalled() => File.Exists(Path.Join(_winePrefix, "drive_c/Program Files (x86)/Steam/steam.exe"));
    public override string ExecutablePath => "C:\\Program Files (x86)/Steam/steam.exe";

    public override async Task Install(CancellationToken cancellationToken) {
        using var client = new HttpClient();
        using var installer = File.Create(Path.Combine(_winePrefix, "drive_c/SteamSetup.exe"));
        await AnsiConsole.Progress()
               .Columns(new ProgressColumn[]
               {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn(),
                    new SpinnerColumn(),
               })
               .HideCompleted(true)
               .StartAsync(async context => {
                   await Task.Run(async () => {
                       var task = context.AddTask($"Downloading [u]Steam[/]", new ProgressTaskSettings {
                           AutoStart = false
                       });
                       await client.DownloadAsync("https://cdn.akamai.steamstatic.com/client/installer/SteamSetup.exe", installer, task, cancellationToken);
                   });
               });

        await AnsiConsole.Status()
          .StartAsync("Installing [u]Steam[/]...", async ctx => {
              await ShellWrapper.ExecuteGamePortingToolkit(_winePrefix, "C:\\SteamSetup.exe", "/S", cancellationToken);
          });
        // we need to kill Steam and then relaunch it
        Utils.KillWine();

        AnsiConsole.MarkupLine("[green]Steam installed![/]");
    }

    class SteamApp {
        public string? AppId { get; set; }
        public string? LauncherPath { get; set; }
        public string? Name { get; set; }
        public string? InstallDir { get; set; }
        public string? StateFlags { get; set; }
    }

    class ACFParser {
        public static SteamApp ParseACF(string filePath) {
            var result = new SteamApp();
            var content = File.ReadAllText(filePath);
            result.AppId = GetValue(content, "appid");
            result.LauncherPath = GetValue(content, "LauncherPath");
            result.Name = GetValue(content, "name");
            result.InstallDir = GetValue(content, "installdir");
            result.StateFlags = GetValue(content, "StateFlags");
            return result;
        }

        private static string? GetValue(string content, string key) {
            var regex = new Regex($"\"{key}\"\\s+\"(.*?)\"", RegexOptions.Compiled);
            var match = regex.Match(content);
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}