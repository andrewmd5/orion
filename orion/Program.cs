using Ptk;
using Spectre.Console;

var isLaunching = false;
var cancellationTokenSource = new CancellationTokenSource();
Console.TreatControlCAsInput = false;
Console.CancelKeyPress += (s, ev) =>
{
    cancellationTokenSource.Cancel();
    ev.Cancel = true;
    if (!isLaunching)
    {
        Environment.Exit(0);
    }
};

AnsiConsole.Write(new FigletText("orion").LeftJustified().Color(Color.Green));
try
{
    await ShellWrapper.EnsureZshAvailabilityAsync(cancellationTokenSource.Token);
    await ShellWrapper.EnsureRosettaAvailabilityAsync(cancellationTokenSource.Token);
    ShellWrapper.EnsureBrewAvailability();
    await ShellWrapper.EnsureGamePortingToolkitAvailability(cancellationTokenSource.Token);
}
catch (OperationCanceledException) {
    Environment.Exit(0);
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine("[red]An error occurred while checking for dependencies.[/]");
    AnsiConsole.WriteException(ex);
    Environment.Exit(1);
}

try
{
    var config = Config.Load();
    if (config is null)
    {
        if (!AnsiConsole.Confirm("No config file found. Would you like to create one now?"))
        {
            Environment.Exit(0);
        }
        var winePrefix = AnsiConsole.Ask<string>("Enter the path to your Wine prefix:");
        winePrefix = winePrefix.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        if (string.IsNullOrEmpty(winePrefix) || !Directory.Exists(winePrefix))
        {
            Console.WriteLine(winePrefix);
            AnsiConsole.MarkupLine("[red]Invalid Wine prefix path.[/]");
            Environment.Exit(1);
        }
        config = new Config
        {
            WinePrefix = winePrefix,
            Apps = Array.Empty<App>().ToList()
        };
        config.Save();
    }

    if (string.IsNullOrWhiteSpace(config.WinePrefix) || !Directory.Exists(config.WinePrefix))
    {
        AnsiConsole.MarkupLine("[red]Wine prefix path does not exist[/]");
        Environment.Exit(1);
    }

    var steam = new Steam(config.WinePrefix);
    if (!steam.IsInstalled() && AnsiConsole.Confirm("Steam is not installed. Would you like to install it?"))
    {
        await steam.Install(cancellationTokenSource.Token);
    }

    // eventually we'll probably do something with the saved apps, but for now just get them
    config.Apps = steam.GetInstalledApps();
    config.Save();

    //prepend steam (and other launchers) to the list of apps
    config.Apps = config.Apps.Prepend(new App(
        "Steam", string.Empty, steam.GetExecutablePath(),
        "-nofriendsui",
        string.Empty,
        AppPlatform.Steam)
    ).ToList();

    var enableHud = AnsiConsole.Confirm("Enable HUD?", defaultValue: true);
    var enableEsync = AnsiConsole.Confirm("Enable esync?", defaultValue: false);

    var app = AnsiConsole.Prompt(
            new SelectionPrompt<App>()
                .Title("Select an app to launch")
                .PageSize(10)
                .AddChoices(config.Apps)
                .UseConverter((app) => app.Name));


    AnsiConsole.MarkupLine("[yellow]Killing previous instances of Wine processes...[/]");
    isLaunching = true;
    Utils.KillWine();
    AnsiConsole.MarkupLine("Launching [green]{0}[/]...", app.Name);
    AnsiConsole.MarkupLine("Press [red]Command+C[/] to exit.");
    await ShellWrapper.ExecuteGamePortingToolkit(config.WinePrefix, app.ExecutablePath, app.Arguments, cancellationTokenSource.Token, enableHud, enableEsync);
    AnsiConsole.MarkupLine("[yellow]Cleaning up...[/]");
}
catch (OperationCanceledException)
{
    AnsiConsole.WriteLine("Exiting...");
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine("[red]An error occurred[/]");
    AnsiConsole.WriteException(ex);
    Environment.Exit(1);
}
finally
{
    Utils.KillWine();
    Environment.Exit(0);
}