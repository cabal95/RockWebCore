using RockWebCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    public static class RockRouterMiddlewareBuilderExtensions
    {
        /// <summary>
        /// Uses the rock routing middleware to handle incoming requests.
        /// </summary>
        /// <param name="app">The application.</param>
        public static void UseRockRouting( this IApplicationBuilder app )
        {
            app.UseMiddleware<RockRouterMiddleware>( app );
        }
    }
}
