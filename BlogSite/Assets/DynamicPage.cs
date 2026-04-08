namespace BlogSite.Assets;

public class DynamicPage(
    string route, DynamicPage? parent,
    string directory,
    string template,
    string[] stylesheets,
    string[] scripts
) : Asset(route)
{
    public readonly DynamicPage? Parent = parent;

    public readonly string DirPath = directory;
    public readonly string Template = template;
    public readonly string[] Stylesheets = stylesheets;
    public readonly string[] Scripts = scripts;
    
    public override string ToString() => $"'{Route}' -> '{DirPath}'";
}
