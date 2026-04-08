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

    public static async Task BakePageTemplates(
        string url,
        IDocument document,
        DynamicPage[] templates,
        IBrowsingContext angleContext,
        CancellationToken cancellationToken)
    {
        var config = Api.Configuration;
        var interpreter = new InterpreterContext();
        
        interpreter.Set("dateTime", DateTime.Now);
        interpreter.Set("url", url);
        foreach (var i in config.GlobalVariables) interpreter.Set(i.Key, i.Value);

        Stack<string> styles = [];
        Stack<string> scripts = [];

        INode[] lastElements = [];
        for (var i = templates.Length - 1; i >= 0; i--)
        {
            var template = await angleContext.OpenAsync(req
                => req.Content(File.ReadAllText(templates[i].Template)), cancellationToken);
            if (template.Body == null) continue;
            
            var content = lastElements;
            var ctx = new AnalyzingContext(url, templates[i], i);
            
            AnalyzeElement(ctx, document, template.Body, content, interpreter);
            lastElements = document.Import(template.Body).ChildNodes.ToArray();
        }

        if (lastElements != null!) document.Body!.Append(lastElements);
        
        var head = document.Head ?? document.AppendChild(document.CreateElement("head"));
        // Appending styles
        while (styles.Count > 0) 
        {
            var j = styles.Pop();
            var l = document.CreateElement("link");
            l.SetAttribute("rel", "stylesheet");
            l.SetAttribute("href", new Uri(config.PageHostUrl!, j).ToString());
            head.AppendChild(l);
        }
        
        // Appending scripts
        while (scripts.Count > 0) 
        {
            var j = styles.Pop();
            var l = document.CreateElement("script");
            l.SetAttribute("src", new Uri(config.PageHostUrl!, j).ToString());
            l.SetAttribute("defer", null);
            head.AppendChild(l);
        }
    }
    
    private static void AnalyzeElement(
        AnalyzingContext ctx,
        IDocument document,
        IElement element,
        INode[] content,
        InterpreterContext interpreter)
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
                    AnalyzeElement(ctx, document, (IElement)newElement, content, interpreter);
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
        
        if (element.HasAttribute("href"))
            element.SetAttribute("href", Baker.FixLink(element.GetAttribute("href")!, ctx.Page, ctx.level));
        else if (element.HasAttribute("src"))
            element.SetAttribute("src", Baker.FixLink(element.GetAttribute("src")!, ctx.Page, ctx.level));
        
        // Tag-specific analysis
        switch (element)
        {
            case IHtmlImageElement @imgElement when !imgElement.HasAttribute("alt"):
            {
                var src = imgElement.GetAttribute("href");
                imgElement.SetAttribute("alt", src == null ? "No src provided" : "Cannot load '{src}'");
            } break;
            
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

            case IHtmlUnknownElement { TagName: "CONTENT" } @contentElement:
            {
                contentElement.Replace(content);
            } break;
            
            case IHtmlUnknownElement @unk: Console.WriteLine($"Unknown element {unk} {unk.TagName}"); break;
        }
        
        // Children analysis
        foreach (var child in element.Children) AnalyzeElement(ctx, document, child, content, interpreter);
    }
    
    class AnalyzingContext(string route, DynamicPage page, int level)
    {
        public readonly string Route = route;
        public readonly DynamicPage Page = page;
        public readonly int level = level;
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
