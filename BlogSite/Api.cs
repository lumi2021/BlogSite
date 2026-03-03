namespace BlogSite;

public static class Api
{
    private static DirectoryInfo? _cacheDir;
    private static Configuration? _config;
    
    public static DirectoryInfo CacheDirectory => _cacheDir ?? throw new InvalidOperationException();
    public static Configuration Configuration => _config ?? throw new InvalidOperationException();
    
    public static void Setup(DirectoryInfo cache, Configuration configuration)
    {
        _cacheDir = cache ?? throw new ArgumentNullException();
        _config = configuration ?? throw new ArgumentNullException();
    }
}
