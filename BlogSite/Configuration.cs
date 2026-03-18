using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlogSite;

public class Configuration
{
    [JsonPropertyName("use-system-tmp")]
    public bool UseSystemTmp { get; set; }
    
    [JsonPropertyName("page-host-url")]
    public Uri? PageHostUrl { get; set; }
    
    [JsonPropertyName("file-query")]
    public FileQuery FileQuery { get; set; } = new FileQuery
    {
        Dom = ["*.html", "*.md", "*.mdx"],
        Style = ["*.css"],
        Script = ["*.js"]
    };
    
    [JsonPropertyName("generic-routes")]
    public Dictionary<string, string> GenericRoutes { get; set; } = [];
    
    [JsonPropertyName("sections")]
    public Dictionary<string, Section> Sections { get; set; } = [];
    
    [JsonPropertyName("global")]
    public string? Global { get; set; }
    [JsonPropertyName("components")]
    public string? Components { get; set; }
}

public record FileQuery
{
    [JsonConverter(typeof(StringOrArrayConverter))]
    public string[]? Dom { get; set; }
    [JsonConverter(typeof(StringOrArrayConverter))]
    public string[]? Style { get; set; }
    [JsonConverter(typeof(StringOrArrayConverter))]
    public string[]? Script { get; set; }
}

public record Section
{
    [JsonPropertyName("path")]
    public string Path { get; set; }
}

[JsonSerializable(typeof(Configuration))]
[JsonSerializable(typeof(Section))]
[JsonSerializable(typeof(FileQuery))]
internal partial class ConfigJsonContext : JsonSerializerContext
{
}

public class StringOrArrayConverter : JsonConverter<string[]>
{
    public override string[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String: return [reader.GetString()!];
            case JsonTokenType.StartArray:
            {
                var list = new List<string>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray) break;
                    list.Add(reader.GetString()!);
                }
                return [.. list];
            }
            
            default: throw new JsonException("Expected string or array.");
        }
    }

    public override void Write(Utf8JsonWriter writer, string[] value, JsonSerializerOptions options)
    {
        if (value.Length == 1)
        {
            writer.WriteStringValue(value[0]);
            return;
        }

        writer.WriteStartArray();
        foreach (var v in value) writer.WriteStringValue(v);
        writer.WriteEndArray();
    }
}
