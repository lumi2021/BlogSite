using System.Collections;
using System.Text.RegularExpressions;
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
        var interpreter = new InterpreterContext();
        
        interpreter.Set("dateTime", DateTime.Now);
        interpreter.Set("url", url);
        foreach (var i in config.GlobalVariables)
            interpreter.Set(i.Key, i.Value);
        
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
        // foreach (var i in (IEnumerable<Asset>)[.. globalTemplateAsset.Stylesheets, .. pageTemplateAsset.Stylesheets])
        // {
        //     var l = document.CreateElement("link");
        //     l.SetAttribute("rel", "stylesheet");
        //     l.SetAttribute("href", new Uri(config.PageHostUrl!, i.Route).ToString());
        //     head.AppendChild(l);
        // }
        //
        // // Appending scripts
        // foreach (var i in (IEnumerable<Asset>)[.. globalTemplateAsset.Scripts, .. pageTemplateAsset.Scripts])
        // {
        //     var l = document.CreateElement("script");
        //     l.SetAttribute("src", new Uri(config.PageHostUrl!, i.Route).ToString());
        //     l.SetAttribute("defer", null);
        //     head.AppendChild(l);
        // }
        
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
    
    private static void AnalyzeElement(IDocument document, IElement element, InterpreterContext interpreter)
    {
        if (element.HasAttribute("for"))
        {
            var input = element.GetAttribute("for")!;
            element.RemoveAttribute("for");
            
            var parent = element.Parent!;
            element.Remove();
            
            try
            {
                string variable;
                string? index = null;
                string collection;

                
                var match = Regex.Match(input, @"(\w+)\s*,\s*(\w+)\s+in\s+(\w+)");
                if (match.Success)
                {
                    variable = match.Groups[1].Value;
                    index = match.Groups[2].Value;
                    collection = match.Groups[3].Value;
                    goto valid;
                }
                
                match = Regex.Match(input, @"(\w+)\s+in\s+(\w+)");
                if (match.Success)
                {
                    variable = match.Groups[1].Value;
                    collection = match.Groups[2].Value;
                    goto valid;
                }
                
                throw new Exception("Invalid for expression syntax");
                
                valid:
                var enumerable = (IEnumerable<object>)interpreter.Eval(collection);

                foreach (var (i, v) in enumerable.Index())
                {
                    interpreter.Push();
                    if (index != null) interpreter.Set(index, i);
                    interpreter.Set(variable, v);

                    var newElement = element.Clone();
                    AnalyzeElement(document, (IElement)newElement, interpreter);
                    parent.AppendChild(newElement);
                    
                    interpreter.Pop();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return;
        }
        
        // Global attribute analysis
        if (element.HasAttribute("if"))
        {
            try
            {
                var input = element.GetAttribute("if")!;
                var result = interpreter.Eval(input);
                if (result is not true)
                {
                    element.Remove();
                    return;
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }
        if (element.HasAttribute("center"))
        {
            element.RemoveAttribute("center");
            element.ClassList.Add("center");
        }

        // Tag-specific analysis
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
        }
        
        // Children analysis
        foreach (var child in element.Children)
            AnalyzeElement(document, child, interpreter);
    }
    
    class InterpreterContext
    {
        private readonly Stack<Dictionary<string, object>> _scopes = new([[]]);
        private Interpreter? _interpreter;

        public void Set(string name, object value)
        {
            _scopes.Peek()[name] = value;
            _interpreter = null;
        }
        public void Remove(string name)
        {
            _scopes.Peek().Remove(name);
            _interpreter = null;
        }
        public void Clear()
        {
            _scopes.Clear();
            _interpreter = null;
        }
        
        public void Push() => _scopes.Push([]);
        public void Pop()
        {
            if (_scopes.Count == 1) throw new IndexOutOfRangeException();
            var s = _scopes.Pop();
            if (s.Count > 0) _interpreter = null;
        }
        
        public object Eval(string expr)
        {
            var interpreter = _interpreter ?? NewInterpreterInstance();
            return interpreter.Eval(expr);
        }

        private Interpreter NewInterpreterInstance()
        {
            var interpreter = new Interpreter();
            foreach (var i in _scopes) 
                foreach (var j in i)
                    interpreter.SetVariable(j.Key, j.Value);

            _interpreter = interpreter;
            return interpreter;
        }
    }
}
