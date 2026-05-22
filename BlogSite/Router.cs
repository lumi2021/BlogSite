using System.Collections;
using System.Data;
using System.Text;
using BlogSite.Assets;
using BlogSite.Exceptions;

namespace BlogSite;

public partial class Router
{
    public async Task Route(HttpContext context, CancellationToken cancellationToken)
    {
        var url = context.Request.Path.Value ?? throw new NoNullAllowedException("URL value was null");
        var config = Api.Configuration;

        var fullUrl = new Uri(new Uri(config.PageHostUrl?.ToString() ?? "/"), url).ToString();
        var dirPath = Path.GetDirectoryName(url)?.TrimEnd('/') ?? "";
        var urlLastPart = url[(url.LastIndexOf('/')+1)..];
        
        var urlTokens = new Queue<string>(dirPath.Split('/'));

        List<DynamicPage> _last404 = [];

        try
        {
            var firstToken = urlTokens.Dequeue();
            RouteNode? currentRoute = (StaticRouteNode?)config.Routes
                .FirstOrDefault(e => e is StaticRouteNode @st && st.Path == firstToken);
            
            if (currentRoute == null) throw new NotFoundRouterException();
            List<DynamicPage> _pages = [(DynamicPage)currentRoute.Asset];
            
            while (urlTokens.Count > 0)
            {
                var token = urlTokens.Dequeue();
                switch (currentRoute)
                {
                    case StaticRouteNode @st:
                    {
                        if (st.StatusSubroutes.TryGetValue(404, out var subroute))
                            _last404 = [.._pages, (DynamicPage)subroute.Asset];

                        if (st.NamedSubroutes.TryGetValue(token, out var subRoute))
                        {
                            currentRoute = subRoute;
                            _pages.Add((DynamicPage)subRoute.Asset);
                        }
                        else
                        {
                            urlLastPart = Path.Combine(token, string.Join('/', urlTokens), urlLastPart);
                            goto hardBreak;
                        }
                    
                    } break;
                    case AutoRouteNode @at:
                    {
                        
                    } break;
                }
                
            }
            hardBreak:
            var staticRoute = (StaticRouteNode)currentRoute;
            
            
            if (string.IsNullOrEmpty(urlLastPart))
            {
                while (staticRoute.NamedSubroutes.TryGetValue("", out var subRoute))
                {
                    staticRoute = (StaticRouteNode)subRoute;
                    _pages.Add((DynamicPage)subRoute.Asset);
                }
                
                var docHtml = await Api.Baker.BakeDynamicPageAsync(fullUrl, [.. _pages], cancellationToken);
                await context.Response.WriteAsync(docHtml, cancellationToken);
            }
            else
            {
                var page = _pages[^1];
                var pageDir = page.DirPath;
                
                var fileName = urlLastPart;
                if (!config.FileQuery.RecursiveSearch && Path.GetDirectoryName(fileName) != null)
                    throw new ForbiddenRouterException();
                
                var fullPath = Path.Combine(pageDir, fileName); ;
                if (!File.Exists(fullPath)) throw new NotFoundRouterException();
                
                (var mustAlwaysUpdate, context.Response.ContentType) = Path.GetExtension(fileName) switch
                {
                    ".css" => (true, "text/css"),
                    ".js" => (true, "text/js"),
                    
                    // images
                    ".png" => (false, "image/png"),
                    ".gif" => (false, "image/gif"),
                    ".jpg" => (false, "image/jpg"),
                    ".jpeg" => (false, "image/jpeg"),
                    ".webp" => (false, "image/webp"),
                    
                    _ => (true, "text/plain"),
                };
                context.Response.StatusCode = 200;
                if (!mustAlwaysUpdate) context.Response.Headers.CacheControl = "public,max-age=86400";
                await context.Response.Body.WriteAsync(await File.ReadAllBytesAsync(fullPath, cancellationToken), cancellationToken);
            }
        }
        catch (NotFoundRouterException e)
        {
            var u = url;
            context.Response.StatusCode = 404;
        }
        catch (ForbiddenRouterException e)
        {
            var u = url;
            context.Response.StatusCode = 403;
        }
        catch (RouterException e)
        {
            Console.WriteLine(e);
        }
    }
}
