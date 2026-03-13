namespace BlogSite;

public partial class Router
{
    public class DynamicPageResult(string pagePath) : RouterResult
    {
        public readonly string PagePath = Path.GetFullPath(pagePath);
        public string? Cached = null;
        public override string ToString() => $"Dynamic '{PagePath}'" + (Cached != null ? " (cached)" : "");
    }
}
