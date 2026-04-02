namespace BlogSite.Assets;

public class DynamicPage(
    string route, DynamicPage? parent,
    string markup,
    string[] styles,
    string[] scripts
) : Asset(route)
{
    public readonly DynamicPage? Parent = parent;
    
    public readonly string DomPath = markup;
    public readonly string[] Stylesheets = styles;
    public readonly string[] Scripts = scripts;
    
    public override string ToString() => $"'{Route}'";
}
