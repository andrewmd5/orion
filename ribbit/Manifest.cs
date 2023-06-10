namespace Ribbit;

/// <summary>
/// Contains information that can be used to verify the installation or launch of a game.
/// </summary>
public class Manifest {
    /// <summary>
    /// The primary executable that will be launched.
    /// In some cases such as with Origin, this will actually be a path of where to find the executable.
    /// </summary>
    public string LaunchExecutable { get; set; }
    /// <summary>
    /// Any startup arguments that should be appended to the executable
    /// </summary>
    public string LaunchCommand { get; set; }

    /// <summary>
    /// The directory the game will be launched in.
    /// In some cases such as with Origin, this will actually be a path of where to find the directory.
    /// </summary>
    public string WorkingDirectory { get; set; }

    /// <summary>
    /// This property represents a static root directory where a game is installed.
    /// </summary>
    public string RootDirectory { get; set; }

    /// <summary>
    /// Application User Model IDs (AUMID) are used extensively in Windows and some programs
    /// to associate processes, files, and windows with a particular application.
    /// For example, to launch a Microsoft Store game you need the AUMID.
    /// Or a Blizzard game, which might need a specific family ID to launch via URI.
    /// Because this ID will likely be unique to a product, we need to be able to look a product up via it.
    /// This is useful in situations such as the Microsoft store where you can query installed packages,
    /// but you can't know anything about them without querying from elsewhere.
    /// </summary>

    public string ApplicationUserModelId { get; set; }
}