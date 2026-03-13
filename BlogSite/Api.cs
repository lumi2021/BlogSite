using System.Text.Json;

namespace BlogSite;

public static class Api
{
    private static DirectoryInfo? _cacheDir;
    private static Configuration? _config;
        
    public static Router Router { get; private set; }  = null!;
    public static Baker Baker { get; private set; } = null!;
    public static DirectoryInfo CacheDirectory => _cacheDir ?? throw new InvalidOperationException();
    public static Configuration Configuration => _config ?? throw new InvalidOperationException();
    public static string ConfigurationPath => Path.GetFullPath("./config.json");


    public static void LoadConfiguration()
    {
        _config = JsonSerializer.Deserialize(File.ReadAllText(Api.ConfigurationPath),
            ConfigJsonContext.Default.Configuration) ?? throw new NotImplementedException();
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
    }
}
