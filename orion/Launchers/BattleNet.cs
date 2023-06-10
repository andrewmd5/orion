using ProtoBuf;
namespace Ptk;

public class BattleNet : BaseLauncher {
    public override string Name => "Battle.net";


    public override string ExecutablePath => "C:\\Program Files (x86)/Battle.net/Battle.net.exe";

    public string LauncherPath => "C:\\Program Files (x86)/Battle.net/Battle.net Launcher.exe";
    public override bool IsInstalled() => File.Exists(Path.Join(_winePrefix, "drive_c/Program Files (x86)/Battle.net/Battle.net.exe"));

    public BattleNet(string winePrefix) : base(winePrefix) { }



    public override List<App> GetInstalledApps() {
        var apps = new List<App>();
        var path = Path.Join(_winePrefix, "drive_c/ProgramData/Battle.net/Agent/product.db");
        if (!File.Exists(path)) return apps;
        
        using var fileStream = File.OpenRead(path);
        var productDb = Serializer.Deserialize<List<BlizzardProduct>>(fileStream);
        if (productDb is null) return apps;
        foreach (var product in productDb) {
            if (product is { Data.Path: not null, ShortId: not null, Id: not null }) {
                var blizzardId = product.Id;
                if (BlackList.Contains(product.ShortId)) continue;
                if (!BlizzardProducts.ContainsKey(product.Id)) {
                    if (!BlizzardProducts.ContainsKey(product.ShortId)) continue;
                    blizzardId = product.ShortId;
                }
                var installDir = product.Data.Path.Replace('\\', '/');
                var wineDir = Path.Join(_winePrefix, installDir.Replace("C:", "drive_c"));
                if (!Directory.Exists(wineDir)) {
                    continue;
                }
                var title = BlizzardProducts[blizzardId].Title;
                var exeFileName = BlizzardProducts[blizzardId].Exe;
                var exePath = Path.Combine(installDir, exeFileName);
                var exeWinePath = Path.Join(_winePrefix, exePath.Replace("C:", "drive_c"));
                if (!File.Exists(exeWinePath)) {
                    // Replacements for launchers for older versions of these games
                    if (exePath.Contains("World of Warcraft Launcher.exe")) {
                        exePath = Path.Combine(installDir, "WoW.exe");
                        exeWinePath = Path.Join(_winePrefix, exePath.Replace("C:", "drive_c"));
                    } else if (exePath.Contains("Overwatch Launcher.exe")) {
                        exePath = Path.Combine(installDir, "Overwatch.exe");
                        exeWinePath = Path.Join(_winePrefix, exePath.Replace("C:", "drive_c"));
                    }
                }
                if (!File.Exists(exeWinePath)) {
                    Console.WriteLine($"Could not find {exeWinePath} for {title}");
                    continue;
                }
                var arguments = BlizzardProducts[blizzardId].Arguments;
                var familyId = BlizzardProducts[blizzardId].Family ?? product.Id;
                // try to launch the executable directly rather than through the launcher
              //  if (directLaunch) {
                    apps.Add(new App(title, installDir, exePath, arguments, familyId, AppPlatform.BattleNet));
               // } else {
              //      apps.Add(new App(title, installDir, ExecutablePath, $"--exec=\"launch {familyId}\"", familyId, AppPlatform.BattleNet));
             //   }
            }
        }

        return apps;
    }

    public override Task Install(CancellationToken cancellationToken) {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Hardcoded collection of all the games on the Battle.net.
    /// </summary>
    private static readonly Dictionary<string, (string Title, string Exe, string Arguments, string? Family)> BlizzardProducts = new() {
        {"diablo3_enus",     ("Diablo III", "x64/Diablo III64.exe",  "-launch", "D3") },
        {"diablo3",  ("Diablo III", "x64/Diablo III64.exe","-launch", "D3") },
        {"d3cn",     ("Diablo III", "x64/Diablo III64.exe","-launch", "D3CN") },
        {"anbs", ("Diablo Immortal", "Engine/Binaries/Win64/DiabloImmortal.exe", "--dx11 --sound-api=wwise --start=Python", "ANBS") },
        {"d3", ("Diablo III", "x64/Diablo III64.exe", "-launch", "D3") },
        {"fenris", ("Diablo IV", "Diablo IV Launcher.exe", string.Empty, "Fen") },
        {"hero", ("Heroes of the Storm", "Support64/HeroesSwitcher_x64.exe", "-launch", "Hero") },
        {"hsb", ("Hearthstone", "Hearthstone.exe", "-launch", "HSB") },
        {"lazr", ("Call of Duty Modern Warfare 2 Campaign Remastered", "MW2CR.exe", string.Empty, "LAZR") },
        {"odin", ("Call of Duty Modern Warfare", "bootstrapper.exe", "ModernWarfare.exe", "ODIN") },
        {"osi", ("Diablo II Resurrected", "D2R.exe", string.Empty, "OSI") },
        {"pro", ("Overwatch", "Overwatch.exe", string.Empty, "PRO") },
        {"rtro", ("Blizzard Arcade Collection", "client.exe", string.Empty, "RTRO") },
        {"s1", ("StarCraft", "x86_64/StarCraft.exe", "-launch", "S1") },
        {"s2", ("StarCraft II", "Support64/SC2Switcher_x64.exe", "-launch", "S2") },
        {"viper", ("Call of Duty Black Ops 4", "BlackOps4_boot.exe", string.Empty, "VIPER") },
        {"w3", ("Warcraft III", "x86_64/Warcraft III.exe", "-launch", "W3") },
        {"wlby", ("Crash Bandicoot 4", "CrashBandicoot4.exe", string.Empty, "WLBY") },
        {"wow", ("World of Warcraft", "WoW.exe", string.Empty, "WoW") },
        {"wow_classic", ("Burning Crusade Classic", "WoWClassic.exe", string.Empty, "WoWC") },
};

    /// <summary>
    /// Product "short" ids we don't support
    /// </summary>
    private static readonly HashSet<string> BlackList = new() {
            "bna", "mobile", "agent"
    };

    /// <summary>
    ///     Used to read/store the product.db protobuf entry
    /// </summary>
    [ProtoContract]
    public class BlizzardProduct {
        [ProtoMember(1)] public string Id { get; set; }

        [ProtoMember(2)] public string ShortId { get; set; }

        [ProtoMember(3)] public GameData Data { get; set; }

        [ProtoMember(6)] public string Family { get; set; }

        [ProtoContract]
        public class GameData {
            [ProtoMember(1)] public string Path { get; set; }

            [ProtoMember(2)] public string Region { get; set; }

            [ProtoMember(10)] public string Branch { get; set; }

            [ProtoMember(13)] public string Subfolder { get; set; }
        }
    }
}




