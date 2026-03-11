using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BlogSite;

public class Configuration
{
    [JsonPropertyName("use-system-tmp")]
    public bool UseSystemTmp { get; set; }
    
    [JsonPropertyName("file-query")]
    public FileQuery FileQuery { get; set; } = new FileQuery
    {
        Dom = "*.html",
        Style = "*.css",
        Script = "*.js"
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
    public string? Dom { get; set; }
    public string? Style { get; set; }
    public string? Script { get; set; }
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
