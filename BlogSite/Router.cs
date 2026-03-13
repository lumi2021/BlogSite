namespace BlogSite;

public partial class Router
{
    private readonly Dictionary<string, RouterResult> _routesMap = new();
    private RouterResult? _notFoundPage;
    
    public async Task Route(HttpContext context, CancellationToken cancellationToken)
    {
        var url = context.Request.Path.Value;
        if (url != null && _routesMap.TryGetValue(url, out var page))
        {
            context.Response.StatusCode = 200;
            await SolveRouterResult(page, context, cancellationToken);
        }
        else await NotFoundResponse(context, cancellationToken);
    }

    public void RegisterPage(string url, RouterResult result)
    {
        switch (url[0])
        {
            case '@':
            case '/': break;
            default: url = $"/{url}"; break;
        }
        
        switch (url)
        {
            case "@404": _notFoundPage = result; break;
            default: _routesMap[url] = result; break;
        }
    }
    public void InvalidateAll() => _routesMap.Clear();
    
    private async Task NotFoundResponse(HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = 404;
        if (_notFoundPage != null) await SolveRouterResult(_notFoundPage, context, cancellationToken);
    }

    private async Task SolveRouterResult(RouterResult result, HttpContext context, CancellationToken cancellationToken)
    {
        switch (result)
        {
            case StaticFileResult staticFileResult:
                context.Response.ContentType = staticFileResult.MimeType;
                await context.Response.SendFileAsync(staticFileResult.FilePath, cancellationToken);
                return;
                
            default: throw new NotImplementedException();
        }
    }
}
