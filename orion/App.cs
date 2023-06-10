using System.Text.Json.Serialization;

namespace Ptk;

public enum AppPlatform {
    None,
    Steam,
    BattleNet
}

public record App(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("installDir")] string InstallDir,
    [property: JsonPropertyName("executablePath")] string ExecutablePath,
    [property: JsonPropertyName("arguments")] string Arguments,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("platform")] AppPlatform Platform
);