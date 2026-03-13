using Microsoft.AspNetCore.Http.Connections;

namespace BlogSite;

#if DEBUG
public static class PwdWatcher
{
    private static FileSystemWatcher _watcher;
    
    public static void Init()
    {
        _watcher = new FileSystemWatcher();

        _watcher.Path = Path.GetFullPath("./");
        _watcher.IncludeSubdirectories = true;

        _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite;

        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Deleted += OnChanged;
        _watcher.Renamed += OnRenamed;

        _watcher.EnableRaisingEvents = true;

        Console.WriteLine($"Now watching directory '{_watcher.Path}'");
    }
    private static void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.StartsWith(Api.CacheDirectory.FullName)) return;
        if (e.FullPath == Api.ConfigurationPath)
        {
            Api.LoadConfiguration();
            Api.Router.InvalidateAll();
            Api.Baker.CompileAllPages();
        }
        
        Api.Router.InvalidateAll();
        Api.Baker.CompileAllPages();
    }
    private static void OnRenamed(object sender, RenamedEventArgs e)
    {
        // uhhh nothing to worry here i guess
    }
}
#endif
