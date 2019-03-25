using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace RockWebCore
{
    /// <summary>
    /// Ensures that the `RockRequestContext.Current` value is correct
    /// </summary>
    public class RockRequestContextMiddleware
    {
        RequestDelegate _next;

        public RockRequestContextMiddleware( RequestDelegate next )
        {
            _next = next;
        }

        public async Task InvokeAsync( HttpContext context, RockRequestContext rockRequestContext )
        {
            RockRequestContext.Current = rockRequestContext;

            await _next( context );
        }
    }
}
