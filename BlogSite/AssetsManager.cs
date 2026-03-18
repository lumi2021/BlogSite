using BlogSite.Assets;
using BlogSite.Exceptions;

namespace BlogSite;

public class AssetsManager
{
    // maps file/directory -> asset
    private Dictionary<string, Asset> _assetsPool = [];
    private Dictionary<string, Dictionary<string, Asset>> _sections = [];
    
    public void InvalidateAllAssets()
    {
        
    }
    public void LoadAllAssets()
    {
        var config = Api.Configuration;
        
        { // Loads global page
            if (config.Global == null) throw new NotImplementedException();
            
            if (!Directory.Exists(config.Global))
                throw new FileNotFoundException($"Provided global page's directory '{config.Global}' does not exist.");
            
            var (dom, styles, scripts) = EnumerateDirectoryContents(config, config.Global);

            var stylesList = new List<Asset>();
            var scriptsList = new List<Asset>();

            foreach (var i in styles)
            {
                var ass = new StaticFile(PathToRoute(i, config.Global, "/global"), i);
                stylesList.Add(ass);
                _assetsPool.Add(i, ass);
            }

            foreach (var i in scripts)
            {
                var ass = new StaticFile(PathToRoute(i, config.Global), i);
                scriptsList.Add(ass);
                _assetsPool.Add(i, ass);
            }

            var domAsset = new DynamicPage(PathToRoute(dom, config.Global),
                dom, [.. stylesList], [.. scriptsList]);
            _ = Api.Baker.UpdateGlobalTemplateAsync(domAsset, CancellationToken.None);
        }
        
        { // loads general routes
            var p = Console.GetCursorPosition();
            int done = 0, total = config.GenericRoutes.Count;

            foreach (var (route, source) in config.GenericRoutes)
            {
                Console.SetCursorPosition(p.Left, p.Top);
                Console.Write(new string(' ', Console.WindowWidth) + "\r");
                Console.WriteLine($"Evaluating generic pages [{done}/{total}]");
                try
                {
                    var (dom, styles, scripts) = EnumerateDirectoryContents(config, source);
                    var stylesList = new List<Asset>();
                    var scriptsList = new List<Asset>();

                    foreach (var i in styles)
                    {
                        var ass = new StaticFile(PathToRoute(i, source, route), i);
                        stylesList.Add(ass);
                        _assetsPool.Add(i, ass);
                    }
                    foreach (var i in scripts)
                    {
                        var ass = new StaticFile(PathToRoute(i, source, route), i);
                        scriptsList.Add(ass);
                        _assetsPool.Add(i, ass);
                    }
                    _assetsPool.Add(dom, new DynamicPage(route, dom, [.. stylesList], [.. scriptsList]));
                }
                // TODO proper error handling
                catch (NoDomException e) { Console.WriteLine("Error: {e}"); }
                catch (TooMuchDomException e) { Console.WriteLine("Error: {e}"); }
                done++;
            }
            
            Console.SetCursorPosition(p.Left, p.Top);
            Console.Write(new string(' ', Console.WindowWidth) + "\r");
            Console.WriteLine($"Evaluating generic pages [DONE]");
        }
        
        { // loads sections
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
                    Console.WriteLine($"Evaluating {id} entries in {section} [{done}/{total}]");

                    // TODO
                    done++;
                }
            
                Console.SetCursorPosition(p.Left, p.Top);
                Console.Write(new string(' ', Console.WindowWidth) + "\r");
                Console.WriteLine($"Evaluating {id} entries in {section} [DONE]");
            }
        }

        foreach (var i in _assetsPool) Api.Router.RegisterAsset(i.Value);
    }
    
    
    public static (string dom, string[] styles, string[] scripts) EnumerateDirectoryContents(Configuration config, string path)
    {
        var domFiles = config.FileQuery.Dom!.SelectMany(e => Directory.EnumerateFiles(path, e)).ToArray();
        var styleFiles = config.FileQuery.Style!.SelectMany(e => Directory.EnumerateFiles(path, e)).ToArray();
        var scriptFiles = config.FileQuery.Script!.SelectMany(e => Directory.EnumerateFiles(path, e)).ToArray();

        return domFiles.Length switch
        {
            0 => throw new NoDomException(config.FileQuery.Dom!, path),
            > 1 => throw new TooMuchDomException(config.FileQuery.Dom!, path, domFiles),
            _ => (domFiles[0], styleFiles, scriptFiles)
        };
    }
    private static string PathToRoute(string file, string dir, string route = "/")
    {
        var fileDir = Path.GetFullPath(Path.GetDirectoryName(file) ?? "");
        var relative = Path.GetRelativePath(dir, fileDir);

        if (relative == ".") relative = "";

        var combine = Path.Combine(route, relative, Path.GetFileName(file));
        
        return combine
                .Replace("@", "")
#if WINDOWS
            .Replace(Path.DirectorySeparatorChar, '/')
#endif
            ;
    }
}
