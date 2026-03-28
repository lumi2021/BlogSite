namespace BlogSite.Assets;

public class DynamicPage(
    string route, DynamicPage? parent,
    string markup,
    Dictionary<string, string> styles,
    Dictionary<string, string> scripts
) : Asset(route)
{
    public readonly DynamicPage? Parent = parent;
    
    public readonly string DomPath = markup;
    public readonly Dictionary<string, string> Stylesheets = styles;
    public readonly Dictionary<string, string> Scripts = scripts;
    
    public override string ToString() => $"'{Route}'";
}
