using System.Text;
using BlogSite.Assets;
using BlogSite.Exceptions;

namespace BlogSite;

public class AssetsManager
{
    private Dictionary<string, Asset> _assetPool = [];
    
    public void InvalidateAllAssets()
    {
        
    }
    public void LoadAllAssets()
    {
        var config = Api.Configuration;

        Stack<(int lvl, int i, int c, StaticRouteNode n)> nodesToIterate = [];
        StringBuilder fullPath = new("/");

        foreach (var i in config.Routes)
        {
            LoadSingleAsset("", (StaticRouteNode)i, config);
            nodesToIterate.Push((0, 1, 0, (StaticRouteNode)i));
        }
        
        while (nodesToIterate.Count > 0)
        {
            var info = nodesToIterate.Pop();
            
            if (info.c < info.n.NamedSubroutes.Count + info.n.StatusSubroutes.Count)
            {
                var i = info.c++;
                //nodesToIterate.Push(info);
                var sub = i < info.n.NamedSubroutes.Count
                    ? info.n.NamedSubroutes.ElementAt(i).Value
                    : info.n.StatusSubroutes.ElementAt(i - info.n.NamedSubroutes.Count).Value;
                
                switch (sub)
                {
                    case StaticRouteNode @static:
                    {
                        info.i = fullPath.Length;

                        if (@static.Path != null) // status code errors have no subroute
                        {
                            var lastLength = fullPath.Length;
                            fullPath.Append(@static.Path);

                            nodesToIterate.Push(info);
                            nodesToIterate.Push((info.lvl+1, lastLength, 0, @static));
                        }
                        LoadSingleAsset("", @static, config);
                    } continue;
                    
                    case AutoRouteNode @auto:
                    {
                      LoadAutoRouteAssets(fullPath.ToString(), auto, config);
                    } continue;
                }
            }
            
            fullPath.Length = info.i;
        }
    }

    public void LoadSingleAsset(string fullPath, StaticRouteNode node, Configuration config)
    {
        var parentAsset = (DynamicPage)node.Parent?.Asset!;
        var pathRoot = Path.GetFullPath(node.source!);
        var contents = EnumerateDirectoryContents(config, pathRoot);

        var structure = contents.dom;
        var stylesMaps = contents.styles.Select(i => i[pathRoot.Length..]);
        var scriptsMaps = contents.scripts.Select(i => i[pathRoot.Length..]);
        
        var asset = new DynamicPage(fullPath, parentAsset, structure, [..stylesMaps], [..scriptsMaps]);
        node.Asset = asset;
        _assetPool.Add(pathRoot, asset);
    }
    public void LoadAutoRouteAssets(string fullRootPath, AutoRouteNode node, Configuration config)
    {
        //Console.WriteLine($"fuck? {fullRootPath} {node}");
    }
    
    public static (string dom, string[] styles, string[] scripts) EnumerateDirectoryContents(Configuration config, string path)
    {
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException(path);
        
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
