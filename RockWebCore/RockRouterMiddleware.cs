using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

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

            RebuildRoutes();
        }

        public void RebuildRoutes()
        {
            var constraintResolver = _applicationBuilder.ApplicationServices.GetRequiredService<IInlineConstraintResolver>();
            var builder = new RouteBuilder( _applicationBuilder );
            builder.Routes.Add( new Route(
                new RouteHandler( CustomPageRoute ),
                null,
                "info",
                new RouteValueDictionary( new { Id = 33 } ),
                new RouteValueDictionary(),
                new RouteValueDictionary(),
                constraintResolver ) );

            builder.Routes.Add( new Route(
                new RouteHandler( CustomPageRoute ),
                null,
                "help",
                new RouteValueDictionary( new { Id = 12 } ),
                new RouteValueDictionary(),
                new RouteValueDictionary(),
                constraintResolver ) );

            builder.MapRoute( "/info/{id}", CustomPageRoute );

            _router = builder.Build();
        }

        public async Task InvokeAsync( HttpContext context )
        {
            var routeContext = new RouteContext( context );

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

        private Task CustomPageRoute( HttpContext context )
        {
            var routeData = context.Features.Get<IRoutingFeature>();

            return Task.CompletedTask;
        }
    }
}
