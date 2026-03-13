using System.Collections;
using AngleSharp;
using AngleSharp.Dom;
using IConfiguration = AngleSharp.IConfiguration;

namespace BlogSite;

public class Baker
{
    private readonly IBrowsingContext _angleContext;
    private IDocument _globalDom = null!;
    private string[] _globalStyles = [];
    private string[] _globalScripts = [];
    
    public Baker()
    {
        var config = AngleSharp.Configuration.Default;
        _angleContext = BrowsingContext.New(config);
    }
    
    public void CompileAllPages()
    {
        var config = Api.Configuration;
        LoadGlobalPageCacheAsync(config, CancellationToken.None).GetAwaiter().GetResult();
        
        {
            var p = Console.GetCursorPosition();
            int done = 0, total = config.GenericRoutes.Count;

            foreach (var (route, source) in config.GenericRoutes)
            {
                Console.SetCursorPosition(p.Left, p.Top);
                Console.Write(new string(' ', Console.WindowWidth) + "\r");
                Console.WriteLine($"Compiling generic pages [{done}/{total}]");
                CompileGenericPageAsync(source, route, CancellationToken.None).GetAwaiter().GetResult();
                done++;
            }
            
            Console.SetCursorPosition(p.Left, p.Top);
            Console.Write(new string(' ', Console.WindowWidth) + "\r");
            Console.WriteLine($"Compiling generic pages [DONE]");
        }
        {
            foreach (var (id, section) in config.Sections)
            {
                var p = Console.GetCursorPosition();
                int done = 0, total = 0;

                if (!Directory.Exists(section.Path)) {
                    Console.WriteLine($"Section `{id}` points to `{Path.GetFullPath(section.Path)}`, but the directory does not exist. Ignoring.");
                    continue;
                }
                var sectionEntries = Directory.GetFileSystemEntries(section.Path)
                    .Where(e => !e.StartsWith('_')).ToArray();
                total = sectionEntries.Length;
                
                foreach (var i in sectionEntries)
                {
                    Console.SetCursorPosition(p.Left, p.Top);
                    Console.Write(new string(' ', Console.WindowWidth) + "\r");
                    Console.WriteLine($"Compiling {id} [{done}/{total}]");

                    CompileMarkdownPageAsync(section, i, CancellationToken.None).Wait();
                    done++;
                }
            
                Console.SetCursorPosition(p.Left, p.Top);
                Console.Write(new string(' ', Console.WindowWidth) + "\r");
                Console.WriteLine($"Compiling {id} [DONE]");
            }
        }
    }

    private async Task LoadGlobalPageCacheAsync(Configuration config, CancellationToken cancellationToken)
    {
        if (config.Global == null) throw new NotImplementedException();
            
        if (!Directory.Exists(config.Global))
            throw new FileNotFoundException($"Provided global page's directory '{config.Global}' does not exist.");
            
        if (!File.Exists(Path.Combine(config.Global, "index.html")))
            throw new FileNotFoundException(
                $"Provided global page's DOM file '{config.Global}/index.html' does not exist.\n" +
                $"Make sure that the file name is correct!");

        var (dom, styles, scripts) = EnumerateSourceFiles(config, config.Global);
        
       _globalDom = await _angleContext.OpenAsync(r => r.Content(File.ReadAllText(dom)), cancellationToken);
       _globalStyles = styles.Select(e => CreateSimpleRoute(e, "text/css")).ToArray();
       _globalScripts = scripts.Select(e => CreateSimpleRoute(e, "text/js")).ToArray();
       
    }

    public async Task CompileGenericPageAsync(string source, string route, CancellationToken cancellationToken)
    {
        var (domPath, styles, scripts) = EnumerateSourceFiles(Api.Configuration, source);
        
        var domRoot = _globalDom!.DocumentElement.Clone();
        var newDoc = _angleContext.OpenNewAsync(null, cancellationToken).GetAwaiter().GetResult();
        newDoc.ReplaceChild(domRoot, newDoc.DocumentElement);

        var pageDoc = await _angleContext.OpenAsync(r
            => r.Content(File.ReadAllText(domPath)), cancellationToken);
        var pageStyles = styles.Select(e => CreateSimpleRoute(e, "text/css")).ToArray();
        var pageScripts = scripts.Select(e => CreateSimpleRoute(e, "text/js")).ToArray();
        
        { // preprocessor

            // appending stylesheets
            var headTag = newDoc.QuerySelector("head");
            if (headTag != null)
            {
                // Appending stylesheets
                foreach (var i in (IEnumerable<string>)[.. _globalStyles, .. pageStyles])
                {
                    var l = newDoc.CreateElement("link");
                    l.SetAttribute("rel", "stylesheet");
                    l.SetAttribute("href", i);
                    headTag.AppendChild(l);
                }
                
                // Appending scripts
                foreach (var i in (IEnumerable<string>)[.. _globalScripts, .. pageScripts])
                {
                    var l = newDoc.CreateElement("script");
                    l.SetAttribute("src", i);
                    l.SetAttribute("defer", "");
                    headTag.AppendChild(l);
                }
            }

            // replacing content
            var contentTags = newDoc.QuerySelectorAll("content");
            foreach (var placeholder in contentTags)
            {
                var frag = newDoc.CreateDocumentFragment();
                foreach (var child in pageDoc.Body.Children.ToArray())
                {
                    child.Remove();
                    frag.AppendChild(child);
                }
                placeholder.Replace(frag);
            }
        }

        var filePath = Path.Combine(
            Api.CacheDirectory.FullName,
            $"{route.TrimStart(Path.DirectorySeparatorChar).Replace('/', '.')}.index.g.html");
        
        CreateDomRoute(newDoc, domPath, route);
    }
    public async Task CompileMarkdownPageAsync(Section section, string source, CancellationToken cancellationToken)
    {
        
    }

    private (string dom, string[] styles, string[] scripts) EnumerateSourceFiles(Configuration config, string path)
    {
        var domFiles = Directory.EnumerateFiles(path, config.FileQuery.Dom!).ToArray();
        var styleFiles = Directory.EnumerateFiles(path, config.FileQuery.Style!);
        var scriptFiles = Directory.EnumerateFiles(path, config.FileQuery.Script!);

        return domFiles.Length switch
        {
            0 => throw new Exception($"Expected file matching '{Path.Combine(path, config.FileQuery.Dom!)}' but no file found"),
            > 1 => throw new Exception($"More than one files matching '{Path.Combine(path, config.FileQuery.Dom!)}'!"),
            _ => (domFiles[0], styleFiles.ToArray(), scriptFiles.ToArray())
        };
    }

    private void CreateDomRoute(IDocument dom, string pathSrc, string route)
    {
        var fileName = (Path.IsPathRooted(pathSrc) ? pathSrc : pathSrc[2..])
            .Replace(Path.DirectorySeparatorChar, '.')
            .Replace(Path.AltDirectorySeparatorChar, '.');
        fileName = Path.Combine(Api.CacheDirectory.FullName, fileName);
        
        File.WriteAllText(fileName, dom.ToHtml());
        Api.Router.RegisterPage(route, new Router.StaticFileResult(fileName, "text/html"));
    }
    private string CreateSimpleRoute(string pathSrc, string mimeType)
    {
        var rawRoute = (Path.IsPathRooted(pathSrc) ? pathSrc : pathSrc[2..]);
        Api.Router.RegisterPage(rawRoute, new Router.StaticFileResult(pathSrc, mimeType));
        return rawRoute;
    }
}
