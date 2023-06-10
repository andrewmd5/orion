namespace Ptk;

public abstract class BaseLauncher {
    protected readonly string _winePrefix;

    public abstract string Name { get; }

    public abstract string ExecutablePath { get; }

    public abstract bool IsInstalled();

    public abstract List<App> GetInstalledApps();

    public abstract Task Install(CancellationToken cancellationToken);

    public BaseLauncher(string winePrefix) {
        _winePrefix = winePrefix;
    }
}