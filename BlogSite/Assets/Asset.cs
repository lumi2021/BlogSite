namespace BlogSite.Assets;

public abstract class Asset(string route) : IDisposable
{
    public readonly string Route = route;
    
    public void Dispose()
    {
        DisposeImpl(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void DisposeImpl(bool byGc) { }
}
