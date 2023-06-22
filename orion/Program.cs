using Ptk;
using Spectre.Console;


var isLaunching = false;
var cancellationTokenSource = new CancellationTokenSource();
Console.TreatControlCAsInput = false;
Console.CancelKeyPress += (s, ev) => {
    cancellationTokenSource.Cancel();
    ev.Cancel = true;
    if (!isLaunching) {
        Environment.Exit(0);
    }
};

var skipSonomaCheck = args.Where(a => a.Equals("--skip-sonoma-check", StringComparison.OrdinalIgnoreCase)).Any();

AnsiConsole.Write(new FigletText("orion").LeftJustified().Color(Color.Green));
var config = Config.Load();
if (config is null) {
    if (!AnsiConsole.Confirm("No config file found. Would you like to create one now?")) {
        Environment.Exit(0);
    }
    var brewPath = AnsiConsole.Ask<string>("Enter the path to your x86-64 Homebrew binary:", defaultValue: ShellWrapper.DefaultBrewPath);
    brewPath = brewPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    if (string.IsNullOrWhiteSpace(brewPath) || !File.Exists(brewPath)) {
        AnsiConsole.MarkupLine("[red]Invalid Homebrew path.[/]");
        Environment.Exit(1);
    }
    var winePrefix = AnsiConsole.Ask<string>("Enter the path to your Wine prefix:");
    winePrefix = winePrefix.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    if (string.IsNullOrEmpty(winePrefix) || !Directory.Exists(winePrefix)) {
        AnsiConsole.MarkupLine("[red]Invalid Wine prefix path.[/]");
        Environment.Exit(1);
    }
    config = new Config {
        WinePrefix = winePrefix,
        BrewPath = brewPath,
        Apps = Array.Empty<App>().ToList()
    };
}


try {
    ShellWrapper.BrewPath = config.BrewPath ?? throw new Exception("Brew path is not defined.");

    if (config.HasDependencies is null or false) {
        AnsiConsole.MarkupLine("[yellow]Checking for dependencies...[/]");
        await ShellWrapper.EnsureZshAvailabilityAsync(cancellationTokenSource.Token);
        if (!skipSonomaCheck) await ShellWrapper.EnsureMacOsSonoma(cancellationTokenSource.Token);
        await ShellWrapper.EnsureRosettaAvailabilityAsync(cancellationTokenSource.Token);
        ShellWrapper.EnsureBrewAvailability();
        await ShellWrapper.EnsureGamePortingToolkitAvailability(cancellationTokenSource.Token);

        await ShellWrapper.ChangeWinVersion(config.WinePrefix ?? throw new Exception("Wine prefix is not defined."), "19042", cancellationTokenSource.Token);
        AnsiConsole.MarkupLine("[green]All dependencies are installed.[/]");
        config.HasDependencies = true;
        // TODO maybe save on set for properties?
        config.Save();
    }
    AnsiConsole.MarkupLine("[yellow]Checking for updates...[/]");
    var latestVersion = await Utils.GetLatestReleaseAsync(cancellationTokenSource.Token);
    if (latestVersion is null) {
        AnsiConsole.MarkupLine("[red]Unable to check for updates.[/]");
    } else {
        if (latestVersion > Utils.CurrentVersion) {
            AnsiConsole.MarkupLine($"[yellow]Version {latestVersion} is available for download at [u]{Utils.ReleaseUrl}[/][/]");
        } else {
            AnsiConsole.MarkupLine("[green]No updates are available.[/]");
        }
    }
} catch (OperationCanceledException) {
    Environment.Exit(0);
} catch (Exception ex) {
    AnsiConsole.MarkupLine("[red]An error occurred while checking for dependencies.[/]");
    AnsiConsole.WriteException(ex);
    Environment.Exit(1);
}

try {

    var steam = new Steam(config.WinePrefix ?? throw new Exception("Wine prefix is not defined."));
    if (!steam.IsInstalled() && AnsiConsole.Confirm("Steam is not installed. Would you like to install it?")) {
        await steam.Install(cancellationTokenSource.Token);
    }
    // eventually we'll probably do something with the saved apps, but for now just get them
    config.Apps = new List<App>();
    if (steam.IsInstalled()) config.Apps.AddRange(steam.GetInstalledApps());
    var battleNet = new BattleNet(config.WinePrefix);
    if (battleNet.IsInstalled()) config.Apps.AddRange(battleNet.GetInstalledApps());
    config.Save();

    //prepend steam (and other launchers) to the list of apps
    if (steam.IsInstalled()) {
        config.Apps = config.Apps.Prepend(new App(
            "Steam", string.Empty, steam.ExecutablePath,
            "-nofriendsui -noverifyfiles -udpforce -allosarches",
            string.Empty,
            AppPlatform.Steam)
        ).ToList();
    }
    if (battleNet.IsInstalled()) {
        config.Apps = config.Apps.Prepend(new App(
            "Battle.net", string.Empty, battleNet.LauncherPath,
            string.Empty,
            string.Empty,
            AppPlatform.BattleNet)
        ).ToList();
    }

    var enableHud = AnsiConsole.Confirm("Enable HUD?", defaultValue: true);
    var enableEsync = AnsiConsole.Confirm("Enable esync?", defaultValue: false);
    var enableRetinaMode = AnsiConsole.Confirm("Enable Retina (high resolution) mode?", defaultValue: false);
    if (enableRetinaMode) {
        AnsiConsole.MarkupLine("[yellow]Enabling Retina mode. Please note, some games will not run with Retina mode enabled.[/]");
    }
    await ShellWrapper.ToggleRetinaMode(config.WinePrefix, enableRetinaMode, cancellationTokenSource.Token);

    var app = AnsiConsole.Prompt(
            new SelectionPrompt<App>()
                .Title("Select an app to launch")
                .PageSize(25)
                .AddChoices(config.Apps)
                .UseConverter((app) => $"{app.Name} ({app.Platform})"));


    AnsiConsole.MarkupLine("[yellow]Killing previous instances of Wine processes...[/]");
    isLaunching = true;
    Utils.KillWine();
    AnsiConsole.MarkupLine("Launching [green]{0}[/]...", app.Name);
    AnsiConsole.MarkupLine("Press [red]Control+C[/] to exit.");
    await ShellWrapper.ExecuteGamePortingToolkit(config.WinePrefix, app.ExecutablePath, app.Arguments, cancellationTokenSource.Token, enableHud, enableEsync);
    AnsiConsole.MarkupLine("[yellow]Cleaning up...[/]");
} catch (OperationCanceledException) {
    AnsiConsole.WriteLine("Exiting...");
} catch (Exception ex) {
    AnsiConsole.MarkupLine("[red]An error occurred[/]");
    AnsiConsole.WriteException(ex);
    Environment.Exit(1);
} finally {
    Utils.KillWine();
    Environment.Exit(0);
}