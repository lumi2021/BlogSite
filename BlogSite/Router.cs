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
            await context.Response.SendFileAsync(page, cancellationToken);
            context.Response.StatusCode = 200;
        }
        else NotFoundResponse(context);
    }

    private void NotFoundResponse(HttpContext context)
    {
        context.Response.StatusCode = 404;
        if (_notFoundPage != null) context.Response.SendFileAsync(_notFoundPage);
    }
}