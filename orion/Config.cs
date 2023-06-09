using System.Text.Json;
using System.Text.Json.Serialization;
namespace Ptk;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Config))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}

public class Config
{
    [JsonPropertyName("apps")]
    public List<App>? Apps { get; set; }

    [JsonPropertyName("winePrefix")]
    public string? WinePrefix { get; set; }

    [JsonPropertyName("brewPath")]
    public string? BrewPath { get; set; }

    public static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/Preferences/orion/config.json");

    private static readonly JsonSerializerOptions Settings = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true,
        TypeInfoResolver = SourceGenerationContext.Default,
        Converters = {
            new PlatformConverter()
        },
    };
    public static Config? Load() => File.Exists(FilePath) ? FromJson(File.ReadAllText(FilePath)) : null;
    public static Config? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonSerializer.Deserialize(json, typeof(Config), Settings) as Config;
    }

    public void Save()
    {
        if (!Directory.Exists(Path.GetDirectoryName(FilePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        }
        File.WriteAllText(FilePath, ToString());
    }

    public override string ToString() => JsonSerializer.Serialize(this, typeof(Config), Settings);

    internal class PlatformConverter : JsonConverter<AppPlatform>
    {
        public override bool CanConvert(Type t) => t == typeof(AppPlatform);

        public override AppPlatform Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return value switch
            {
                "none" => AppPlatform.None,
                "steam" => AppPlatform.Steam,
                _ => throw new Exception("Cannot unmarshal type Platform"),
            };
        }

        public override void Write(Utf8JsonWriter writer, AppPlatform value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case AppPlatform.None:
                    writer.WriteStringValue("none");
                    // JsonSerializer.Serialize(writer, "none", options);
                    return;
                case AppPlatform.Steam:
                    writer.WriteStringValue("steam");
                    return;
            }
            throw new Exception("Cannot marshal type Platform");
        }
    }
}