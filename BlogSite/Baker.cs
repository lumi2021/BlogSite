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

        foreach (var page in pageStack)
        {
            foreach (var i in page.Stylesheets) stylesheets.Add(i);
            foreach (var i in page.Scripts) scripts.Add(i);
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
                link.SetAttribute("rel", Path.Combine(url, i.TrimStart('/')));
                link.SetAttribute("type", "text/css");
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
}
