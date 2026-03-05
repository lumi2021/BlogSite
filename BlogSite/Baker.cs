using AngleSharp;
using AngleSharp.Dom;

namespace BlogSite;

public class Baker
{
    public void CompileAllPages()
    {
        var config = Api.Configuration;

        IBrowsingContext angleContext;
        IDocument globalDom;

        {
            if (config.Global == null) throw new NotImplementedException();
            if (!File.Exists(config.Global)) throw new FileNotFoundException($"file '{config.Global}' does not exist.");
            var fileContent = File.ReadAllText(config.Global);
            
            var angleConfig = AngleSharp.Configuration.Default;
            angleContext = BrowsingContext.New(angleConfig);
            globalDom = angleContext.OpenAsync(r => r.Content(fileContent))
                .GetAwaiter().GetResult();
        }
        
        {
            var p = Console.GetCursorPosition();
            int done = 0, total = config.GenericRoutes.Count;

            foreach (var (route, source) in config.GenericRoutes)
            {
                Console.SetCursorPosition(p.Left, p.Top);
                Console.Write(new string(' ', Console.WindowWidth) + "\r");
                Console.WriteLine($"Compiling generic pages [{done}/{total}]");

                var domRoot = globalDom.DocumentElement.Clone();
                var newDoc = angleContext.OpenNewAsync().GetAwaiter().GetResult();
                newDoc.ReplaceChild(domRoot, newDoc.DocumentElement);

                var pageDoc = angleContext
                    .OpenAsync(r => r.Content(File.ReadAllText(source)))
                    .GetAwaiter().GetResult();

                IElement pageBody;
                if (pageDoc.Body!.Children.Length > 1)
                {
                    pageBody = pageDoc.CreateElement("div");
                    foreach (var i in pageDoc.Body.Children)
                    {
                        i.Parent!.RemoveChild(i);
                        pageBody.AppendChild(i);
                    }
                }
                else pageBody = pageDoc.Body;
                
                var contentTags = newDoc.QuerySelectorAll("content");
                foreach (var i in contentTags)
                    i.Parent!.ReplaceChild(pageBody, i);

                var filePath = Path.Combine(
                    Api.CacheDirectory.FullName,
                    $"{route.TrimStart(Path.DirectorySeparatorChar).Replace('/', '.')}.index.g.html");
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                File.WriteAllText(filePath, newDoc.ToHtml());
                
                CompileGenericPageAsync(source, route, CancellationToken.None).Wait();
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

    public async Task CompileGenericPageAsync(string source, string route, CancellationToken cancellationToken)
    {
        
    }
    public async Task CompileMarkdownPageAsync(Section section, string source, CancellationToken cancellationToken)
    {
        
    }
    
}
