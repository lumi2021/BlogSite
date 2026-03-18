using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using BlogSite.Assets;
using DynamicExpresso;
using DynamicExpresso.Exceptions;

namespace BlogSite.Preprocessor;

public static class HtmlPreprocessor
{

    public static async Task<IDocument> BakePageTemplates(
        string url,
        DynamicPage globalTemplateAsset, IDocument globalTemplate,
        DynamicPage pageTemplateAsset, IDocument pageTemplate,
        CancellationToken cancellationToken)
    {
        var config = Api.Configuration;
        var interpreter = new Interpreter();
        
        interpreter.SetVariable("dateTime", DateTime.Now);
        interpreter.SetVariable("url", url);
        foreach (var i in config.GlobalVariables)
            interpreter.SetVariable(i.Key, i.Value);
        
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
        
        if (document.Body != null) AnalyzeElement(document, document.Body!, interpreter);
        if (pageTemplate.Body != null) AnalyzeElement(pageTemplate, pageTemplate.Body, interpreter);

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
    
    private static void AnalyzeElement(IDocument document, IElement element, Interpreter interpreter)
    {
        foreach (var child in element.Children) AnalyzeElement(document, child, interpreter);
        
        Console.WriteLine($"{element} {element.TagName}");
        switch (element)
        {
            case IHtmlButtonElement @buttonElement when buttonElement.HasAttribute("href"):
            {
                var newAElement = document.CreateElement("a");
                foreach (var attr in buttonElement.Attributes) newAElement.SetAttribute(attr.Name, attr.Value);
                if (!newAElement.HasAttribute("draggable")) newAElement.SetAttribute("draggable", "false");
                if (!newAElement.ClassList.Contains("button")) newAElement.ClassList.Add("button");
                while (buttonElement.FirstChild != null) newAElement.AppendChild(buttonElement.FirstChild);
                buttonElement.Replace(newAElement);
            } break;

            case IHtmlUnknownElement { TagName: "FMT" } @fmtElement:
            {
                var expression = fmtElement.TextContent;
                try
                {
                    var result = interpreter.Eval(expression);
                    var newSpan = document.CreateElement("span");
                    foreach (var i in fmtElement.Attributes) newSpan.SetAttribute(i.Name, i.Value);
                    newSpan.TextContent = result.ToString() ?? "";
                    fmtElement.Replace(newSpan);
                }
                catch (Exception e)
                {
                    var fmterrElement = document.CreateElement("span");
                    fmterrElement.TextContent = fmtElement.TextContent;
                    fmterrElement.ClassList.Add("____fmterr____");
                    fmterrElement.SetAttribute("title", e.Message);
                    fmtElement.Replace(fmterrElement);
                }
            } break;

            case IHtmlUnknownElement { TagName: "ICON" } @iconElement:
            {
                var spanElement = document.CreateElement("span");
                spanElement.ClassList.Add(["icon", ..iconElement.ClassList]);
                iconElement.Replace(spanElement);
            }break;
            
            case IHtmlUnknownElement @unk: Console.WriteLine($"Unknown element {unk} {unk.TagName}"); break;
            default: Console.WriteLine($"Unknown element {element}"); break;
        }
    }
}
