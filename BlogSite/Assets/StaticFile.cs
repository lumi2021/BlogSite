namespace BlogSite.Assets;

public class StaticFile(string route, string file) : Asset(route)
{
    public readonly string FilePath = Path.GetFullPath(file);
    public override string ToString() => $"'{Route}' -> static '{file}'";
}
