using System.Buffers;

namespace BlogSite;

public class Router
{
    private Dictionary<string, string> routesMap = new();
    private string? _notFoundPage;
    
    public async Task Route(HttpContext context, CancellationToken cancellationToken)
    {
        var url = context.Request.Path.Value;
        
        context.Response.ContentType = "text/html";
        if (url != null && routesMap.TryGetValue(url, out var page))
        {
            context.Response.StatusCode = 200;
            await context.Response.SendFileAsync(page, cancellationToken);
        }
        else NotFoundResponse(context);
    }

    public void RegisterPage(string url, string pagePath)
    {
        switch (url[0])
        {
            case '@':
            case '/': break;
            default: url = $"/{url}"; break;
        }
        
        switch (url)
        {
            case "@404": _notFoundPage = pagePath; break;
            default: routesMap[url] = pagePath; break;
        }
    }
    
    private void NotFoundResponse(HttpContext context)
    {
        context.Response.StatusCode = 404;
        if (_notFoundPage != null) context.Response.SendFileAsync(_notFoundPage);
    }
}