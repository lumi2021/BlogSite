using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlogSite;

public static class Api
{
    private static DirectoryInfo? _cacheDir;
    private static Configuration? _config;
        
    public static Router Router { get; private set; }  = null!;
    public static Baker Baker { get; private set; } = null!;
    public static AssetsManager Assets { get; private set; } = null!;
    
    public static DirectoryInfo CacheDirectory => _cacheDir ?? throw new InvalidOperationException();
    public static Configuration Configuration => _config ?? throw new InvalidOperationException();
    public static string ConfigurationPath => Path.GetFullPath("./config.json");


    public static void LoadConfiguration()
    {
        _config = JsonSerializer.Deserialize(File.ReadAllText(Api.ConfigurationPath),
            GenericJsonConverter.Default.Configuration) ?? throw new NotImplementedException();

        List<RouteNode> bakedNodes = [.._config.RawRoutesData.Select(LoadRoutesRecursive)];
        _config.RawRoutesData = null!;
        _config.Routes = [..bakedNodes];
        
        return;
        RouteNode LoadRoutesRecursive(GenericRouteNode node)
        {
            if (node.Auto != null)
            {
                var n = new AutoRouteNode()
                {
                    Path = node.Auto,
                    Mode = node.AutoMode ?? AutoRouteMode.List,
                };
                
                return n;
            }
            else
            {
                var n = new StaticRouteNode();

                if (node.Path == null && !node.Stat.HasValue) throw new Exception("Route node has no path");
                if (node is { Path: not null, Stat: not null }) throw new Exception("Route node cannot have a path and a status value at the same time");
                if (node is { Stat: not null, Subroutes.Length: > 0 }) throw new Exception("Status route cannot have subroutes"); 
                
                n.Path = node.Path?.Trim('/');
                n.Status = node.Stat;
                n.PathMatching = node.PathMatching ?? RoutePathMatching.Default;
                n.source = node.Source ?? throw new Exception("Route node is not associated with a page");
                n.Default = node.Default;

                if (node.Subroutes is not { Length: > 0 }) return n;
                
                Dictionary<string, RouteNode> namedSubRoutes = [];
                Dictionary<int, RouteNode> statusSubRoutes = [];
                foreach (var i in node.Subroutes)
                {
                    var routeNode = LoadRoutesRecursive(i);
                    routeNode.Parent = n;
                    switch (routeNode)
                    {
                        case AutoRouteNode autoRoute: namedSubRoutes.Add("*", routeNode); break;
                        case StaticRouteNode {Path: not null } @s: namedSubRoutes.Add(s.Path, routeNode); break;
                        case StaticRouteNode {Status: not null } @s: statusSubRoutes.Add(s.Status.Value, routeNode); break;
                    }
                }
                namedSubRoutes.TrimExcess();
                statusSubRoutes.TrimExcess();
                    
                n.NamedSubroutes = namedSubRoutes;
                n.StatusSubroutes = statusSubRoutes;

                return n;
            }
        }
    }
    public static void CreateCacheDir()
    {
        Console.WriteLine($"Configuration file loaded");

        DirectoryInfo cacheDir;
        if (!Configuration.UseSystemTmp)
        {
            if (Directory.Exists(".local-cache")) Directory.Delete(".local-cache", true);
            _cacheDir = Directory.CreateDirectory(".local-cache");
        }
        else _cacheDir = Directory.CreateTempSubdirectory(".local-api-cache-");
    }
    
    public static void Setup()
    {
        if (_cacheDir == null || _config == null) throw new ArgumentNullException();

        Router = new Router();
        Baker = new Baker();
        Assets = new AssetsManager();
    }
}
