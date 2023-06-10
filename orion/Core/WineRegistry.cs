using System.Text.RegularExpressions;
namespace Ptk.Core;
public class WineRegistryValue {
    public string Name { get; set; }
    public string Type { get; set; }
    public string Data { get; set; }
}

public class WineRegistryKey {
    public string Name { get; set; }
    public Dictionary<string, WineRegistryKey> SubKeys { get; set; }
    public List<WineRegistryValue> Values { get; set; }
}

public class WineRegistry {
    private WineRegistryKey Root { get; set; }

    private WineRegistry() {
        Root = new WineRegistryKey { Name = "Root", SubKeys = new Dictionary<string, WineRegistryKey>(), Values = new List<WineRegistryValue>() };
    }

    public static WineRegistry FromFile(string filePath) {
        var wineRegistry = new WineRegistry();
        WineRegistryKey currentKey = wineRegistry.Root;

        foreach (var line in File.ReadLines(filePath)) {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith(";"))
                continue;

            if (line.StartsWith("[")) {
                var keyPathLine = line.TrimStart('[');
                var keyPathEndIndex = keyPathLine.IndexOf(']');
                if (keyPathEndIndex == -1)
                    continue; // Malformed line, skip

                var keyPath = keyPathLine.Substring(0, keyPathEndIndex).Split("\\\\");

                currentKey = wineRegistry.Root;
                foreach (var part in keyPath) {
                    if (!currentKey.SubKeys.ContainsKey(part)) {
                        currentKey.SubKeys[part] = new WineRegistryKey { Name = part, SubKeys = new Dictionary<string, WineRegistryKey>(), Values = new List<WineRegistryValue>() };
                    }

                    currentKey = currentKey.SubKeys[part];
                }
            } else {
                var match = Regex.Match(line, "\"(.*?)\"=(.*)");
                if (match.Success && currentKey != null) {
                    var registryValue = new WineRegistryValue {
                        Name = match.Groups[1].Value.Trim(),
                        Data = match.Groups[2].Value.Trim()
                    };

                    if (registryValue.Data.StartsWith("hex:") || registryValue.Data.StartsWith("str:")) {
                        var pair = registryValue.Data.Split(':', StringSplitOptions.RemoveEmptyEntries);
                        if (pair.Length == 2) {
                            registryValue.Type = pair[0].Trim();
                            registryValue.Data = pair[1].Trim();
                        }
                    } else if (registryValue.Data.StartsWith("\"") && registryValue.Data.EndsWith("\"")) {
                        registryValue.Type = "string";
                        registryValue.Data = registryValue.Data.Trim('\"');
                    }

                    currentKey.Values.Add(registryValue);
                }
            }
        }

        return wineRegistry;
    }

    public WineRegistryKey GetKey(string keyPath) {
        var parts = keyPath.Split('\\');
        var currentKey = Root;
        foreach (var part in parts) {
            if (currentKey.SubKeys.ContainsKey(part)) {
                currentKey = currentKey.SubKeys[part];
            } else {
                return null;
            }
        }

        return currentKey;
    }
}