using System.Text;
using BlogSite.Assets;

namespace BlogSite;

public partial class Router
{
    private readonly Dictionary<string, Asset> _routesMap = new();
    private Asset? _notFoundPage;
    
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

    public void RegisterAsset(Asset asset)
    {
        var url = asset.Route;
        switch (url[0])
        {
            case '@':
            case '/': break;
            default: url = $"/{url}"; break;
        }
        
        switch (url)
        {
            case "@404": _notFoundPage = asset; break;
            default: _routesMap[url] = asset; break;
        }
    }
    public void InvalidateAll() => _routesMap.Clear();
    
    private async Task NotFoundResponse(HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = 404;
        if (_notFoundPage != null) await SolveRouterResult(_notFoundPage, context, cancellationToken);
    }

    private async Task SolveRouterResult(Asset result, HttpContext context, CancellationToken cancellationToken)
    {
        switch (result)
        {
            case DynamicPage dynamicPage:
                context.Response.ContentType = "text/html";
                var pageContent = await Api.Baker.BakeDynamicPageAsync(
                    context.Request.Host + context.Request.Path,
                    dynamicPage, cancellationToken);
                await context.Response.WriteAsync(pageContent, new UTF8Encoding(), cancellationToken);
                return;
            
            case StaticFile staticFile:
                context.Response.ContentType = Path.GetExtension(staticFile.FilePath) switch
                {
                    ".html" => "text/html",
                    ".css" => "text/css",
                    ".js" => "text/javascript",
                    _ => "text/plain"
                };
                await context.Response.SendFileAsync(staticFile.FilePath, cancellationToken);
                return;
                
            default: throw new NotImplementedException();
        }
    }
}
