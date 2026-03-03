using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BlogSite;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public class Configuration
{
    public Dictionary<string, string> Routes = [];
    public Dictionary<string, string> Error = [];
    public string? Globals;
}

[JsonSerializable(typeof(Configuration))]
internal partial class ConfigJsonContext : JsonSerializerContext
{
}
