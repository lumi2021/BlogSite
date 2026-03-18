using System.Diagnostics;
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
    
    [JsonPropertyName("global-variables"), JsonConverter(typeof(VariableDictionaryConverter))]
    public Dictionary<string, object> GlobalVariables { get; set; } = [];
    
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
public class VariableDictionaryConverter : JsonConverter<Dictionary<string, object>>
{
    public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Dictionary<string, object> dict = [];
        var anytypeConverter = new AnyObjectConverter();
        
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            var key = reader.GetString()!;
            reader.Read();
            var value = anytypeConverter.Read(ref reader, typeof(object), options);
            dict.Add(key, value);
        }

        return dict;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
    {
        throw new UnreachableException();
    }
}
public class AnyObjectConverter : JsonConverter<object>
{
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString()!,
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.StartArray => ParseArray(ref reader, typeToConvert, options),
            JsonTokenType.StartObject => ParseObject(ref reader, typeToConvert, options),
            _ => throw new JsonException("Unexpected token")
        };
    }

    private object[] ParseArray(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = new List<object>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) break;
            switch (reader.TokenType)
            {
                case JsonTokenType.String: list.Add(reader.GetString()!); break;
                case JsonTokenType.Number: list.Add(reader.GetDouble()); break;
                case JsonTokenType.True: list.Add(true); break;
                case JsonTokenType.False: list.Add(false); break;
                case JsonTokenType.StartArray: list.Add(ParseArray(ref reader, typeToConvert, options)); break;
                case JsonTokenType.StartObject: list.Add(ParseObject(ref reader, typeToConvert, options)); break;

                default: throw new JsonException("Unexpected token");
            }
        }
        return [.. list];
    }
    private Dictionary<string, object> ParseObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options) =>
        WriteAnytypeValue(writer, value, options);
    private void WriteAnytypeValue(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case String @v: writer.WriteStringValue(v); break;
            case Double @v: writer.WriteNumberValue(v); break;
            case Boolean @v: writer.WriteBooleanValue(v); break;
            
            case IEnumerable<object> @v:
                writer.WriteStartArray();
                foreach (var i in v) WriteAnytypeValue(writer, i, options);
                writer.WriteEndArray();
                break;
            
            case Dictionary<string, object> @v:
                throw new NotImplementedException();
                break;
        }
    }
}
