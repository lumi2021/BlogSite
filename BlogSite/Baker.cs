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

    public async Task UpdateGlobalTemplateAsync(DynamicPage globalTemplate, CancellationToken cancellationToken)
    {
        _globalLoaded.SetResult();
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _globalLoaded = tcs;

        var doc = await File.ReadAllTextAsync(globalTemplate.DomPath, cancellationToken);
        var globalTemplateDom = await _angleContext.OpenAsync(req => req.Content(doc), cancellationToken);

        _globalTemplateAsset = globalTemplate;
        _globalTemplateDom = await _angleContext.OpenNewAsync(null, cancellationToken);
        var document = _globalTemplateDom;

        try
        {
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
                head.AppendChild(charset);
            }

            var body = document.Body ?? (IHtmlElement)html.AppendChild(document.CreateElement("body"));

            body.InnerHtml = string.Empty;
            if (globalTemplateDom.Body != null)
            {
                foreach (var node in globalTemplateDom.Body.ChildNodes)
                {
                    var imported = document.Import(node);
                    document.Body!.AppendChild(imported);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally { _globalLoaded.SetResult(); }
    }
    
    public async Task<string> BakeDynamicPageAsync(string url, DynamicPage page, CancellationToken cancellationToken)
    {
        await _globalLoaded.Task;
        
        var doc = await File.ReadAllTextAsync(page.DomPath, cancellationToken);
        var pageTemplate = await _angleContext.OpenAsync(req => req.Content(doc), cancellationToken);
        
        var result = await HtmlPreprocessor.BakePageTemplates(url,
            _globalTemplateAsset, _globalTemplateDom, page, pageTemplate, cancellationToken);
        return result.ToHtml(new PrettyMarkupFormatter());
    }
}
