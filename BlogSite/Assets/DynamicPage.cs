namespace BlogSite.Assets;

public class DynamicPage(string route, string dom, Asset[] styles, Asset[] scripts) : Asset(route)
{
    public readonly string DomPath = Path.GetFullPath(dom);
    public readonly Asset[] Stylesheets = styles;
    public readonly Asset[] Scripts = scripts;
    
    public string Directory => Path.GetDirectoryName(DomPath) ?? string.Empty;
    public override string ToString() => $"'{Route}' -> dynamic '{DomPath}'";
}
