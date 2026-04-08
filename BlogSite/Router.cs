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

        var fullUrl = Path.Combine(config.PageHostUrl?.ToString() ?? "/", url);
        var dirPath = Path.GetDirectoryName(url)?.TrimEnd('/') ?? "";
        var urlLastPart = url[(url.LastIndexOf('/')+1)..];
        
        var urlTokens = new Queue<string>(dirPath.Split('/'));

        List<DynamicPage> _last404 = [];

        try
        {
            var currentRoute = (StaticRouteNode?)config.Routes
                .FirstOrDefault(e => e is StaticRouteNode @st && st.Path == urlTokens.Dequeue());
            if (currentRoute == null) throw new NotFoundRouterException();
            List<DynamicPage> _pages = [(DynamicPage)currentRoute.Asset];
            
            while (urlTokens.Count > 0)
            {
                var token = urlTokens.Dequeue();
                if (currentRoute.StatusSubroutes.TryGetValue(404, out var subroute))
                    _last404 = [.._pages, (DynamicPage)subroute.Asset];

                if (currentRoute.NamedSubroutes.TryGetValue(token, out var subRoute))
                {
                    currentRoute = (StaticRouteNode)subRoute;
                    _pages.Add((DynamicPage)subRoute.Asset);
                }
                else
                {
                    urlLastPart = Path.Combine(token, string.Join('/', urlTokens), urlLastPart);
                    break;
                }
            }
            
            if (string.IsNullOrEmpty(urlLastPart))
            {
                while (currentRoute.NamedSubroutes.TryGetValue("", out var subRoute))
                {
                    currentRoute = (StaticRouteNode)subRoute;
                    _pages.Add((DynamicPage)subRoute.Asset);
                }
                
                var docHtml = await Api.Baker.BakeDynamicPageAsync(fullUrl, [.. _pages], cancellationToken);
                await context.Response.WriteAsync(docHtml, cancellationToken);
            }
            else
            {
                var dot0 = urlLastPart.IndexOf('.'); if (dot0 == -1) throw new NotFoundRouterException();
                var dot1 = urlLastPart.LastIndexOf('.'); if (dot1 == -1 || dot1 - dot0 < 1) throw new NotFoundRouterException();
                
                var fileName = urlLastPart[0 .. dot0];
                var level = int.Parse(urlLastPart[(dot0+1) .. dot1]);
                var extension = urlLastPart[dot1..];

                var i = 0;
                while (i < level && currentRoute.NamedSubroutes.TryGetValue("", out var subRoute))
                {
                    currentRoute = (StaticRouteNode)subRoute;
                    _pages.Add((DynamicPage)subRoute.Asset);
                    i++;
                }
                if (i != level) throw new NotFoundRouterException();

                var page = _pages[^1];
                var pageDir = page.DirPath;

                var fname = fileName + extension;
                if (!config.FileQuery.RecursiveSearch && Path.GetDirectoryName(fname) != null)
                    throw new ForbiddenRouterException();
                
                var fullPath = Path.Combine(pageDir, fname); ;
                if (!File.Exists(fullPath)) throw new NotFoundRouterException();
                
                (bool mustAlwaysUpdate, context.Response.ContentType) = extension switch
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
