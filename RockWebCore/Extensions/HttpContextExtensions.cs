using Microsoft.AspNetCore.Http;

using Rock;
using Rock.Model;

namespace RockWebCore
{
    public static class HttpContextExtensions
    {
        public static void SetCurrentPerson( this HttpContext httpContext, Person person )
        {
            httpContext.Items.AddOrReplace( "CurrentPerson", person );
        }

        public static Person GetCurrentPerson( this HttpContext httpContext )
        {
            if ( httpContext.Items.ContainsKey( "CurrentPerson" ) )
            {
                return ( Person ) httpContext.Items["CurrentPerson"];
            }

            return null;
        }

        public static void SetCurrentUser( this HttpContext httpContext, UserLogin user)
        {
            httpContext.Items.AddOrReplace( "CurrentUser", user );
        }

        public static UserLogin GetCurrentUser( this HttpContext httpContext )
        {
            if ( httpContext.Items.ContainsKey( "CurrentUser" ) )
            {
                return ( UserLogin ) httpContext.Items["CurrentUser"];
            }

            return null;
        }
    }
}
