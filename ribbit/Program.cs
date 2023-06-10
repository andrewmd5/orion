

using System.Net.Sockets;
using System.Text;
using Ribbit;

var ribbit = new RibbitClient(Locale.US);

var codes = await ribbit.GetProductCodes();

var products = new List<Product>();
var knownCodes = new HashSet<string>() {
    "agent",
    "rtro",
    "anbs",
    "osi",
    "d3",
    "fenris",
    "hero",
    "storm",
    "hsb",
    "pro",
    "s1",
    "s2",
    "w3",
    "viper",
    "odin",
    "lazr",
    "wlby",
    "wow",
    "wow_classic",
    "fore"

};
foreach (var code in codes) {
    if (!knownCodes.Contains(code))
        continue;
    try {
        var product = await ribbit.GetProductManifest(code);
        if (product is { Manifest: null }) {
            Console.WriteLine($"{code} {product.Name} has no manifest");
            continue;
        }
        Console.WriteLine($"{code} {product.Name} {product.Manifest.LaunchExecutable}");
        products.Add(new(code, product.Name, product.Manifest));
        await Task.Delay(1000);
    } catch (Exception ex) {
        Console.WriteLine($"{code} skipping => {ex}");
    }
}

var data = System.Text.Json.JsonSerializer.Serialize(products, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
await File.WriteAllTextAsync("products.json", data);

var builder = new StringBuilder();
foreach (var product in products) {
    builder.AppendLine($"{{\"{product.Code}\", (\"{product.Name}\", \"{product.Manifest.LaunchExecutable}\", \"{product.Manifest.LaunchCommand}\", \"{product.Code.ToUpper()}\") }},");
}
Console.WriteLine(builder.ToString());
record Product(string Code, string Name, Manifest Manifest);
/// <summary>
/// A signed TCP implementation of retrieving product information
/// <para>See https://wowdev.wiki/Ribbit</para>
/// </summary>
public sealed class RibbitClient {
    private const string Host = ".version.battle.net";
    private readonly string _endpoint;
    private readonly Locale _locale;
    private readonly ushort _port;
    /// <summary>
    /// Creates a new Ribbit Client for the specified locale
    /// </summary>
    /// <param name="locale"></param>
    /// <param name="port"></param>
    public RibbitClient(Locale locale, ushort port = 1119) {
        if (locale == Locale.XX)
            throw new ArgumentException("Invalid locale", paramName: nameof(locale));

        _endpoint = locale + Host;
        _locale = locale;
        _port = port;
    }

    /// <summary>
    /// Hardcoded collection of all the games on the Blizzard App.
    /// https://wowdev.wiki/Battlenet_Licenses
    /// https://wowdev.wiki/TACT
    /// </summary>
    private static readonly Dictionary<string, string> BlizzardProducts = new Dictionary<string, string>
        {
            {"dst2","DST2" },
            {"diablo3_enus",      "D3" },
            {"d3",        "D3" },
            {"diablo3",   "D3" },
            {"d3cn",    "D3CN" },
            {"hsb", "WTCG" },
            {"hero",   "Hero" },
            {"odin",     "ODIN" },
            {"s2",      "S2"},
            {"s1",       "S1" },
            {"pro",      "Pro" },
            {"viper",     "VIPR" },
            {"w3",      "W3" },
            {"wow",      "WoW" },
            {"lazr",      "LAZR" },
            {"fore", "FORE"}
        };
    public async Task<(string Name, Manifest Manifest)> GetProductManifest(string product) {
        var url = $"http://{GetRegion(product)}.patch.battle.net:1119/{product}/versions";
        using var http = new HttpBuddy();
        var versionInfo = await http.GetBytes(url);
        await using var versionStream = new MemoryStream(versionInfo.ToArray());
        using var reader = new StreamReader(versionStream);
        string? line;
        var productVersion = string.Empty;
        while ((line = reader.ReadLine()) != null) {
            // skip blank lines and comments
            if (string.IsNullOrWhiteSpace(line) || line[0] == '#')
                continue;
            // split line into tokens
            var tokens = line.Split('|');
            if (!tokens[0].Equals(_locale.ToString().ToLower())) continue;
            if (tokens.Length < 7) continue;
            if (tokens[6].Length != 32) continue;
            productVersion = tokens[6];
            break;
        }
        if (string.IsNullOrWhiteSpace(productVersion)) return (null, null);

        var tokenParent = $"{productVersion[0]}{productVersion[1]}";
        var tokenChild = $"{productVersion[2]}{productVersion[3]}";
        var content = await http.GetContent(string.Format(BlizzardConstants.ProductConfigUrl, tokenParent,
                           tokenChild, productVersion));

        var productInfo = BlizzardProductConfig.FromJson(content);

        if (productInfo?.Platform?.Windows?.Config?.Binaries?.Game == null) {
            return (null, null);
        }
        var config = productInfo.Platform.Windows.Config.Binaries.Game;
        var manifest = new Manifest {
            LaunchExecutable = string.IsNullOrWhiteSpace(config.RelativePath64) ? config.RelativePath : config.RelativePath64,
            LaunchCommand = config.LaunchArguments == null ? "" : string.Join(" ", config.LaunchArguments),
            ApplicationUserModelId = BlizzardProducts.ContainsKey(product) ? BlizzardProducts[product] : null
        };

        if (string.IsNullOrWhiteSpace(manifest.LaunchExecutable)) {
            Console.WriteLine($"No executable information in manifest for {product}");
            return (null, null);
        }
        var installLanguage = product.EndsWith("cn") ? productInfo?.Zhcn : productInfo?.Enus;
        var displayName = installLanguage?.Config?.Install?.FirstOrDefault(i => !string.IsNullOrWhiteSpace(i?.AddRemoveProgramsKey?.DisplayName))?.AddRemoveProgramsKey?.DisplayName;

        return (displayName, manifest);
    }
    public async Task<string> GetString(string payload) {
        await using var stream = new TcpClient(_endpoint, _port).GetStream();
        // apply the terminator
        if (!payload.EndsWith("\r\n"))
            payload += "\r\n";

        await stream.WriteAsync(Encoding.ASCII.GetBytes(payload));
        await stream.FlushAsync();
        try {
            await using var memoryStream = new MemoryStream();
            using var reader = new StreamReader(stream);
            var text = reader.ReadToEnd().Split('\n');
            var boundary = text.FirstOrDefault(x => x.Trim().StartsWith("Content-Type:"))?.Split(';').FirstOrDefault(x => x.Trim().StartsWith("boundary="))?.Split('"')[1].Trim();
            var data = text.SkipWhile(x => x.Trim() != "--" + boundary).Skip(1).TakeWhile(x => x.Trim() != "--" + boundary).Skip(1);
            await using StreamWriter writer = new(memoryStream, Encoding.ASCII, 1024, true);
            foreach (var line in data) {
                writer.WriteLine(line);
            }
            await writer.FlushAsync();
            memoryStream.Position = 0;

            return Encoding.ASCII.GetString(memoryStream.ToArray());
        } catch (FormatException ex) {
            Console.WriteLine(ex);
            return string.Empty;
        }
    }

    public async Task<HashSet<string>> GetProductCodes() {
        var productIds = new HashSet<string>();
        await using var summaryStream = await GetStream(RibbitCommand.Summary);
        using var reader = new StreamReader(summaryStream);
        string? line;
        while ((line = reader.ReadLine()) != null) {
            // skip blank lines and comments
            if (string.IsNullOrWhiteSpace(line) || line[0] == '#' || line.Contains("!STRING"))
                continue;
            // split line into tokens
            var productId = line.Split('|').FirstOrDefault();
            productIds.Add(productId);
        }
        return productIds;
    }


    public async Task<Stream> GetStream(RibbitCommand command, string product = "") {
        return await GetStream(CommandToPayload(command, product));
    }

    public async Task<Stream> GetStream(string payload) {
        var response = await GetString(payload);
        return new MemoryStream(Encoding.ASCII.GetBytes(response));
    }

    public async Task<string> GetString(RibbitCommand command, string product = "") {
        return await GetString(CommandToPayload(command, product));
    }

    private string GetRegion(string product) {
        return (((Locale[])Enum.GetValues(typeof(Locale))).FirstOrDefault(locale => product.EndsWith(locale.ToString().ToLower()))).ToString().ToLower();
    }

    private string CommandToPayload(RibbitCommand command, string product) {
        return command switch {
            RibbitCommand.Bgdl => $"v1/products/{product}/bgdl",
            RibbitCommand.CDNs => $"v1/products/{product}/cdns",
            RibbitCommand.Summary => $"v1/summary",
            RibbitCommand.Versions => $"v1/products/{product}/versions",
            _ => throw new Exception($"Invalid command: {command}"),
        };
    }

}
/// <summary>
/// A list of the common known Ribbit commands
/// </summary>
public enum RibbitCommand {
    /// <summary>
    /// A list of all products and their current sequence number
    /// </summary>
    Summary,
    /// <summary>
    /// Regional version information for a specific product
    /// </summary>
    Versions,
    /// <summary>
    /// Regional CDN sever information for a specific product
    /// </summary>
    CDNs,
    /// <summary>
    /// Version information for the Battle.net App background downloader
    /// </summary>
    Bgdl,
}
/// <summary>
/// Locales
/// </summary>
public enum Locale {
    US,
    EU,
    CN,
    KR,
    TW,
    SG,
    XX,
}