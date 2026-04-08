using System.Collections;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Html.Dom;
using BlogSite.Assets;
using BlogSite.Preprocessor;
using IConfiguration = AngleSharp.IConfiguration;

namespace BlogSite;

public partial class Baker
{
    private readonly IBrowsingContext _angleContext;
    private DynamicPage _globalTemplateAsset = null!;
    private IDocument _globalTemplateDom = null!;
    private TaskCompletionSource _globalLoaded = new();
    
    public Baker()
    {
        var config = AngleSharp.Configuration.Default;
        _angleContext = BrowsingContext.New(config);
    }
    
    public async Task<string> BakeDynamicPageAsync(string url, DynamicPage[] pageStack, CancellationToken cancellationToken)
    {
        var document = await _angleContext.OpenNewAsync(null, cancellationToken);
        var stylesheets = new HashSet<string>();
        var scripts = new HashSet<string>();

        foreach (var (level, page) in pageStack.Index())
        {
            foreach (var i in page.Stylesheets) stylesheets.Add(ManglePath(i, level));
            foreach (var i in page.Scripts) scripts.Add(ManglePath(i, level));
        }
        
        if (document.Doctype == null!)
        {
            document.InsertBefore(
                document.Implementation.CreateDocumentType("html", null!, null!),
                document.FirstChild);
        }

        var html = document.DocumentElement ?? (IHtmlElement)document.AppendChild(document.CreateElement("html"));
        var head = document.Head ?? (IHtmlElement)html.AppendChild(document.CreateElement("head"));
        {
            var charset = document.CreateElement("meta");
            charset.SetAttribute("charset", "utf-8");
            head.AppendChild(charset);
                
            var viewport = document.CreateElement("meta");
            charset.SetAttribute("name", "viewport");
            charset.SetAttribute("content", "width=device-width, initial-scale=1");
            head.AppendChild(viewport);

            foreach (var i in stylesheets)
            {
                var link =  document.CreateElement("link");
                link.SetAttribute("rel", "stylesheet");
                link.SetAttribute("href", Path.Combine(url, i.TrimStart('/')));
                head.AppendChild(link);
            }
            
            foreach (var i in scripts)
            {
                var script = document.CreateElement("script");
                script.SetAttribute("type", "text/javascript");
                script.SetAttribute("src", Path.Combine(url, i.TrimStart('/')));
                script.SetAttribute("defer", null);
                head.AppendChild(script);
            }
        }

        var body = document.Body ?? (IHtmlElement)html.AppendChild(document.CreateElement("body"));
        body.InnerHtml = string.Empty;
        
        await HtmlPreprocessor.BakePageTemplates(url, document, [.. pageStack], _angleContext, cancellationToken);
        return document.ToHtml(new PrettyMarkupFormatter());
    }

    public static string ManglePath(string path, int level)
    {
        string? dir = Path.GetDirectoryName(path);
        string filename = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);

        var newName = $"{filename}.{level}{extension}";
        return dir != null ? Path.Combine(dir, newName) : newName;
    }
    public static string FixLink(string path, DynamicPage page, int level)
    {
        if (path.StartsWith("https://") || path.StartsWith("http://")) return path;

        var config = Api.Configuration;
        var pathTrimmed = path.TrimEnd('/');

        return Path.Exists(Path.Combine(page.DirPath, pathTrimmed))
            ? new Uri(config.PageHostUrl ?? throw new NullReferenceException(), Path.Combine(page.Route, ManglePath(path, level))).ToString()
            : new Uri(config.PageHostUrl ?? throw new NullReferenceException(), Path.Combine(page.Route, path)).ToString();
    }
}
