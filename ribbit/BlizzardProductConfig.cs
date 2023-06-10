using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ribbit {
    internal class BlizzardProductConfig {
        [JsonPropertyName("enus")]
        public InstallLanguage Enus { get; set; }

        [JsonPropertyName("zhcn")]
        public InstallLanguage Zhcn { get; set; }

        [JsonPropertyName("platform")]
        public Platform Platform { get; set; }

        public static BlizzardProductConfig FromJson(string json) => JsonSerializer.Deserialize<BlizzardProductConfig>(json, new JsonSerializerOptions {

        });
    }

    internal partial class Platform {
        [JsonPropertyName("win")]
        public Win Windows { get; set; }
    }
    internal partial class Win {
        [JsonPropertyName("config")]
        public WinConfig Config { get; set; }
    }

    internal partial class WinConfig {
        [JsonPropertyName("binaries")]
        public Binaries Binaries { get; set; }
    }

    internal partial class Binaries {
        [JsonPropertyName("game")]
        public Game Game { get; set; }
    }

    internal partial class Game {
        [JsonPropertyName("launch_arguments")]
        public string[] LaunchArguments { get; set; }

        [JsonPropertyName("relative_path_64")]
        public string RelativePath64 { get; set; }

        [JsonPropertyName("relative_path")]
        public string RelativePath { get; set; }
    }
    internal partial class InstallLanguage {
        [JsonPropertyName("config")]
        public Config Config { get; set; }
    }

    internal partial class Config {
        [JsonPropertyName("install")]
        public Install[] Install { get; set; }
    }

    internal partial class Install {
        [JsonPropertyName("add_remove_programs_key")]
        public AddRemoveProgramsKey AddRemoveProgramsKey { get; set; }
    }

    internal partial class AddRemoveProgramsKey {
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }
    }
}