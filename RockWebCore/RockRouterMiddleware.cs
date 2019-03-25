using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using RockWebCore.UI;

namespace RockWebCore
{
    public class RockRouterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IApplicationBuilder _applicationBuilder;
        private IRouter _router;

        public RockRouterMiddleware( RequestDelegate next, IApplicationBuilder applicationBuilder )
        {
            _next = next;
            _applicationBuilder = applicationBuilder;
        }

        public void RebuildRoutes()
        {
            var constraintResolver = _applicationBuilder.ApplicationServices.GetRequiredService<IInlineConstraintResolver>();
            var builder = new RouteBuilder( _applicationBuilder );

            using ( var rockContext = new RockContext() )
            {
                var pageRouteService = new PageRouteService( rockContext );

                var routes = pageRouteService.Queryable()
                    .Where( r => r.Page.SiteId == 1 );

                foreach ( var route in routes )
                {
                    builder.Routes.Add( new Route(
                        new RouteHandler( CustomPageRouteAsync ),
                        null,
                        route.Route,
                        new RouteValueDictionary( new { InternalRoutePageId = route.PageId } ),
                        new RouteValueDictionary(),
                        new RouteValueDictionary(),
                        constraintResolver ) );
                }
            }

            _router = builder.Build();
        }

        public async Task InvokeAsync( HttpContext context )
        {
            var routeContext = new RouteContext( context );

            if ( _router == null )
            {
                RebuildRoutes();
            }

            await _router.RouteAsync( routeContext );

            if ( routeContext.Handler == null )
            {
                await _next( context );
            }
            else
            {
                context.Features.Set<IRoutingFeature>( new RoutingFeature { RouteData = routeContext.RouteData } );

                await routeContext.Handler( context );
            }
        }

        private async Task CustomPageRouteAsync( HttpContext context )
        {
            var routeData = context.GetRouteData();
            int pageId = ( int ) routeData.Values["InternalRoutePageId"];
            var rockRequestContext = context.RequestServices.GetRequiredService<RockRequestContext>();

            rockRequestContext.CurrentPage = new RockPage( PageCache.Get( pageId ), rockRequestContext );

            var actionContext = new ActionContext( context, routeData, new ActionDescriptor() );
            var response = await rockRequestContext.CurrentPage.RenderAsync();

            await response.ExecuteResultAsync( actionContext );
        }
    }
}
