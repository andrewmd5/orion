using System.Diagnostics;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace Ptk;

public partial class ShellWrapper {
    private static readonly string _shellPath = "/bin/zsh";
    private static readonly string _architectureFlag = "-x86_64";
    public static readonly string DefaultBrewPath = "/usr/local/bin/brew";
    public static string BrewPath { get; set; } = DefaultBrewPath;

    /// <summary>
    /// Launches a process under the game porting toolkit.
    /// </summary>
    /// <param name="winePrefix">The wine prefix where the executable is located</param>
    /// <param name="executablePath">The fully qualified path to the executable</param>
    /// <param name="args">optional arguments to provide the executable</param>
    public static async Task ExecuteGamePortingToolkit(string winePrefix, string executablePath, string args, CancellationToken cancellationToken, bool enableHud = false, bool enableEsync = false) {
        string hudEnabled = enableHud ? "MTL_HUD_ENABLED=1 " : "";
        string esyncEnabled = enableEsync ? "WINEESYNC=1 " : "";
        string escapedArgs = string.IsNullOrWhiteSpace(args) ? string.Empty : ArgumentEscaper.EscapeAndConcatenate(args.Split(' '));

        using var gptProcess = new Process() {
            StartInfo = new ProcessStartInfo {
                FileName = _shellPath,
                Arguments = $"-c \"arch {_architectureFlag} {_shellPath} -c 'eval \\\"$({BrewPath} shellenv)\\\"; {hudEnabled}{esyncEnabled} WINEPREFIX=\\\"{winePrefix}\\\" `{BrewPath} --prefix game-porting-toolkit`/bin/wine64 \\\"{executablePath}\\\" {escapedArgs} 2>&1 | grep D3DM'\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        gptProcess.OutputDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) {
                AnsiConsole.MarkupLine("[white]Output[/]: [u]{0}[/]", e.Data);
            }
        };

        gptProcess.ErrorDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) {
                AnsiConsole.MarkupLine("[red]Error[/]: [u]{0}[/]", e.Data);
            }
        };

        gptProcess.Start();


        gptProcess.BeginOutputReadLine();
        gptProcess.BeginErrorReadLine();

        await gptProcess.WaitForExitAsync(cancellationToken);
    }



    /// <summary>
    /// Ensures that the current system is running macOS 14.0 or later.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception">Thrown when the current system is not running macOS 14.0 or later.</exception>
    public static async Task EnsureMacOsSonoma(CancellationToken cancellationToken) {
        using var swVersProcess = new Process() {
            StartInfo = new ProcessStartInfo {
                FileName = _shellPath,
                Arguments = "-c \"sw_vers\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        swVersProcess.Start();
        var result = await swVersProcess.StandardOutput.ReadToEndAsync(cancellationToken);
        await swVersProcess.WaitForExitAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(result)) {
            throw new Exception("Unable to determine macOS version: sw_vers command returned no output.");
        }
        var match = ProductRegex().Match(result);
        if (!match.Success) {
            throw new Exception("Unable to parse macOS version from sw_vers command output.");
        }
        var osVersion = new Version(match.Groups[1].Value);
        var minimumVersion = new Version("14.0");
        if (osVersion.CompareTo(minimumVersion) < 0) {
            throw new Exception("macOS version must be at least 14.0.");
        }
    }

    /// <summary>
    /// Ensures that zsh is installed and available.
    /// </summary>
    /// <exception cref="Exception">Thrown when zsh is not installed or unavailable.</exception>
    public static async Task EnsureZshAvailabilityAsync(CancellationToken cancellationToken) {
        using var zshShell = new Process() {
            StartInfo = new ProcessStartInfo {
                FileName = "/bin/bash",
                Arguments = $"-c \"command -v {_shellPath} --verison\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        zshShell.Start();
        var result = await zshShell.StandardOutput.ReadToEndAsync(cancellationToken);
        await zshShell.WaitForExitAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(result)) {
            throw new Exception($"zsh is not installed on this system.");
        }
    }

    /// <summary>
    /// Ensures that Homebrew (x86_64) is installed and available.
    /// </summary>
    /// <exception cref="Exception">Thrown when Homebrew is not installed or unavailable.</exception>
    public static void EnsureBrewAvailability() {
        if (!File.Exists(BrewPath)) {
            throw new Exception("The x86_64 version of Homebrew is not installed on this system.");
        }
    }

    /// <summary>
    /// Ensures that Rosetta 2 is installed and available.
    /// </summary>
    /// <exception cref="Exception">Thrown when Rosetta 2 is not installed or unavailable.</exception>
    public static async Task EnsureRosettaAvailabilityAsync(CancellationToken cancellationToken) {
        using var machProcess = new Process() {
            StartInfo = new ProcessStartInfo {
                FileName = _shellPath,
                Arguments = "-c \"sysctl -n machdep.cpu.brand_string\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        machProcess.Start();
        var result = await machProcess.StandardOutput.ReadToEndAsync(cancellationToken);
        await machProcess.WaitForExitAsync(cancellationToken);
        if (!result.Contains("Apple")) throw new Exception("Intel Based Macs are Ineligible for Rosetta 2.");
        using var rosettaProcess = new Process() {
            StartInfo = new ProcessStartInfo {
                FileName = _shellPath,
                Arguments = $"-c \"arch {_architectureFlag} /usr/bin/true\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        rosettaProcess.Start();
        var error = await rosettaProcess.StandardOutput.ReadToEndAsync(cancellationToken);
        await rosettaProcess.WaitForExitAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(error)) {
            throw new Exception("Rosetta 2 is not installed.");
        }
    }

    /// <summary>
    /// Ensures that Game Porting Toolkit is installed and available.
    /// </summary>
    /// <exception cref="Exception">Thrown when Game Porting Toolkit is not installed or unavailable.</exception>
    public static async Task EnsureGamePortingToolkitAvailability(CancellationToken cancellationToken) {
        var gptProcess = new Process() {
            StartInfo = new ProcessStartInfo {
                FileName = _shellPath,
                Arguments = $"-c \"arch {_architectureFlag} {_shellPath} -c 'eval \\\"$({BrewPath} shellenv)\\\"; {BrewPath} list game-porting-toolkit'\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        gptProcess.Start();
        string output = await gptProcess.StandardOutput.ReadToEndAsync(cancellationToken);
        string error = await gptProcess.StandardError.ReadToEndAsync(cancellationToken);
        await gptProcess.WaitForExitAsync(cancellationToken);
        if (!output.Contains("function_grep.pl") || error.Contains("Error: No available formula with the name \"game-porting-toolkit\".")) {
            AnsiConsole.WriteLine($"[red]Error[/]: {error}");
            AnsiConsole.WriteLine($"[yellow]Warning[/]: {output}");
            throw new Exception("gameportingtoolkit is not functioning correctly. Ensure that brew is in the environment.");
        }
    }

    /// <summary>
    /// Sets the Windows version for a given wine prefix.
    /// </summary>
    /// <param name="winePrefix">The wine prefix to set the Windows version for.</param>
    /// <param name="windowsVersion">The Windows version to set.</param>
    /// <exception cref="Exception">Thrown when the Windows version cannot be set.</exception>
    public static async Task ChangeWinVersion(string winePrefix, string windowsVersion, CancellationToken cancellationToken) {
        var commands = new string[] {
            "\\\"HKEY_LOCAL_MACHINE\\\\Software\\\\Microsoft\\\\Windows NT\\\\CurrentVersion\\\" /v CurrentBuild /t REG_SZ /d 19042 /f",
            "\\\"HKEY_LOCAL_MACHINE\\\\Software\\\\Microsoft\\\\Windows NT\\\\CurrentVersion\\\" /v CurrentBuildNumber /t REG_SZ /d 19042 /f"
        };
        foreach (var command in commands) {
            using var regEdit = new Process() {
                StartInfo = new ProcessStartInfo {
                    FileName = _shellPath,
                    Arguments = $"-c \"arch {_architectureFlag} {_shellPath} -c 'eval \\\"$({BrewPath} shellenv)\\\"; WINEPREFIX=\\\"{winePrefix}\\\" `{BrewPath} --prefix game-porting-toolkit`/bin/wine64 reg add {command}'",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            regEdit.Start();
            var output = await regEdit.StandardOutput.ReadToEndAsync(cancellationToken);
            await regEdit.WaitForExitAsync(cancellationToken);
            if (!output.Contains("The operation completed successfully", StringComparison.OrdinalIgnoreCase)) {
                throw new Exception($"Error changing Windows Version: {output}");
            }
        }
        Utils.KillWine();
    }

    /// <summary>
    /// Toggles the Retina mode for a given wine prefix.
    /// </summary>
    /// <param name="winePrefix">The wine prefix to toggle Retina mode for.</param>
    /// <param name="enableRetinaMode">Whether to enable or disable Retina mode.</param>
    /// <exception cref="Exception">Thrown when Retina mode cannot be toggled.</exception>
    public static async Task ToggleRetinaMode(string winePrefix, bool enableRetinaMode, CancellationToken cancellationToken) {
        var value = enableRetinaMode ? 'Y' : 'N';
        var registryCommand = $"\\\"HKEY_CURRENT_USER\\\\Software\\\\Wine\\\\Mac Driver\\\" /v RetinaMode /t REG_SZ /d '{value}' /f";
        using var regEdit = new Process() {
            StartInfo = new ProcessStartInfo {
                FileName = _shellPath,
                Arguments = $"-c \"arch {_architectureFlag} {_shellPath} -c 'eval \\\"$({BrewPath} shellenv)\\\"; WINEPREFIX=\\\"{winePrefix}\\\" `{BrewPath} --prefix game-porting-toolkit`/bin/wine64 reg add {registryCommand}'",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        regEdit.Start();
        var output = await regEdit.StandardOutput.ReadToEndAsync(cancellationToken);
        await regEdit.WaitForExitAsync(cancellationToken);
        if (!output.Contains("The operation completed successfully", StringComparison.OrdinalIgnoreCase)) {
            throw new Exception($"Error Retina mode: {output}");
        }
    }


    [GeneratedRegex(@"ProductVersion:\s+(\d+\.\d+)")]
    private static partial Regex ProductRegex();
}