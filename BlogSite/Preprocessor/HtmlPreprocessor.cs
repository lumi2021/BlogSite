using AngleSharp;
using AngleSharp.Dom;
using BlogSite.Assets;

namespace BlogSite.Preprocessor;

public static class HtmlPreprocessor
{

    public static async Task<IDocument> BakePageTemplates(
        DynamicPage globalTemplateAsset, IDocument globalTemplate,
        DynamicPage pageTemplateAsset, IDocument pageTemplate,
        CancellationToken cancellationToken)
    {
        var config = Api.Configuration;
        
        var document = await globalTemplate.Context.OpenNewAsync(
            new Uri(config.PageHostUrl!, pageTemplateAsset.Route).ToString(), cancellationToken);
        foreach (var i in document.Children.ToArray()) document.RemoveChild(i);
        
        if (globalTemplate.Doctype != null!)
        {
            var dt = globalTemplate.Doctype;
            var newDoctype = document.Implementation.CreateDocumentType(
                dt.Name, dt.PublicIdentifier, dt.SystemIdentifier);
            document.InsertBefore(newDoctype, document.FirstChild);
        }

        var head = document.Head ?? document.AppendChild(document.CreateElement("head"));
        // Appending styles
        foreach (var i in (IEnumerable<Asset>)[.. globalTemplateAsset.Stylesheets, .. pageTemplateAsset.Stylesheets])
        {
            var l = document.CreateElement("link");
            l.SetAttribute("rel", "stylesheet");
            l.SetAttribute("href", new Uri(config.PageHostUrl!, i.Route).ToString());
            head.AppendChild(l);
        }
        
        // Appending scripts
        foreach (var i in (IEnumerable<Asset>)[.. globalTemplateAsset.Scripts, .. pageTemplateAsset.Scripts])
        {
            var l = document.CreateElement("script");
            l.SetAttribute("src", new Uri(config.PageHostUrl!, i.Route).ToString());
            l.SetAttribute("defer", null);
            head.AppendChild(l);
        }
        
        var importedHtml = document.Import(globalTemplate.DocumentElement!);
        document.AppendChild(importedHtml);
        
        if (pageTemplate.Body != null) AnalyzeElement(pageTemplate, pageTemplate.Body);

        var body = document.Body ?? (IElement)document.AppendChild(document.CreateElement("body"));
        var contentElement = body.QuerySelector("content");

        if (contentElement != null && pageTemplate.Body is { ChildNodes: {} @nodes, ChildNodes.Length: > 0 })
        {
            List<INode> toReplace = [];
            toReplace.AddRange(nodes.OfType<IElement>().Select(i => document.Import(i)));
            contentElement.Replace([.. toReplace]);
        }
        
        return document;
    }
    
    public static void AnalyzeElement(IDocument document, IElement element)
    {
        foreach (var child in element.Children) AnalyzeElement(document, child);
        
        switch (element)
        {
            default: Console.WriteLine($"Unknown: {element}"); break;
        }
    }
}