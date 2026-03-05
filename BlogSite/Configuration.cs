using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BlogSite;

public class Configuration
{
    [JsonPropertyName("generic-routes")]
    public Dictionary<string, string> GenericRoutes { get; set; } = [];
    
    [JsonPropertyName("sections")]
    public Dictionary<string, Section> Sections { get; set; } = [];
    
    [JsonPropertyName("global")]
    public string? Global { get; set; }
}

public record Section
{
    [JsonPropertyName("path")]
    public string Path { get; set; }
}

[JsonSerializable(typeof(Configuration))]
[JsonSerializable(typeof(Section))]
internal partial class ConfigJsonContext : JsonSerializerContext
{
}
