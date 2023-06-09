using Ptk;
using Spectre.Console;


AnsiConsole.Write(new FigletText("orion").LeftJustified().Color(Color.Green));
try
{
    await ShellWrapper.EnsureZshAvailabilityAsync();
    await ShellWrapper.EnsureRosettaAvailabilityAsync();
    ShellWrapper.EnsureBrewAvailability();
    await ShellWrapper.EnsureGamePortingToolkitAvailability();
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine("[red]An error occurred while checking for dependencies.[/]");
    AnsiConsole.WriteException(ex);
    Environment.Exit(1);
}
Console.TreatControlCAsInput = false;
Console.CancelKeyPress += (s, ev) =>
{
    Utils.KillWine();
    ev.Cancel = true;
};
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
    await steam.Install();
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
Utils.KillWine();
AnsiConsole.MarkupLine("Launching [green]{0}[/]...", app.Name);
AnsiConsole.MarkupLine("Press [red]Ctrl+C[/] to exit.");
await ShellWrapper.ExecuteGamePortingToolkit(config.WinePrefix, app.ExecutablePath, app.Arguments, enableHud, enableEsync);
AnsiConsole.MarkupLine("[yellow]Cleaning up...[/]");
Utils.KillWine();
Environment.Exit(0);