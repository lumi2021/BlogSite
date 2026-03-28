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
        var url = context.Request.Path.Value?.Trim('/') ?? throw new NoNullAllowedException("URL value was null");
        var config = Api.Configuration;
        
        var urlTokens = new Queue<string>(url.Split('/'));

        List<DynamicPage> _last404 = [];

        try
        {
            StaticRouteNode? currentRoute = (StaticRouteNode)config.Routes
                .First(e => e is StaticRouteNode @st && st.Path == urlTokens.Dequeue());
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
                else throw new NotFoundRouterException();
            }

            while (currentRoute.NamedSubroutes.TryGetValue("", out var subRoute))
            {
                currentRoute = (StaticRouteNode)subRoute;
                _pages.Add((DynamicPage)subRoute.Asset);
            }

            var docHtml = await Api.Baker.BakeDynamicPageAsync(url, [.. _pages], cancellationToken);
            await context.Response.WriteAsync(docHtml, cancellationToken);
        }
        catch (NotFoundRouterException e)
        {
            context.Response.StatusCode = 404;
        }
        catch (RouterException e)
        {
            Console.WriteLine(e);
        }
    }
}
