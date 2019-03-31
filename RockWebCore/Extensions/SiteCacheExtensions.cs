using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;

using Rock.Model;
using Rock.Web.Cache;

namespace RockWebCore
{
    public static class SiteCacheExtensions
    {
        /// <summary>
        /// Gets the return URL to be used during a redirect.
        /// </summary>
        /// <returns></returns>
        private static string GetReturnUrl()
        {
            var context = RockRequestContext.Current;

            var returnUrl = context.PageParameter( "returnUrl" );

            if ( string.IsNullOrWhiteSpace( returnUrl ) )
            {
                returnUrl = Uri.EscapeDataString( PersonToken.RemoveRockMagicToken( context.RawUrl ) );
            }

            return returnUrl;
        }

        /// <summary>
        /// Redirects to default page.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <returns></returns>
        public static IActionResult RedirectToDefaultPageResult( this SiteCache site )
        {
            return new RedirectResult( site.DefaultPageReference.BuildUrl(), false );
        }

        /// <summary>
        /// Redirects to login page.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <param name="includeReturnUrl">if set to <c>true</c> [include return URL].</param>
        /// <returns></returns>
        public static IActionResult RedirectToLoginPageResult( this SiteCache site, bool includeReturnUrl )
        {
            var pageReference = site.LoginPageReference;

            if ( includeReturnUrl )
            {
                pageReference.Parameters = new Dictionary<string, string>
                {
                    { "returnurl", GetReturnUrl() }
                };
            }

            return new RedirectResult( pageReference.BuildUrl(), false );
        }

        /// <summary>
        /// Redirects to change password page.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <param name="isChangePasswordRequired">if set to <c>true</c> [is change password required].</param>
        /// <param name="includeReturnUrl">if set to <c>true</c> [include return URL].</param>
        /// <returns></returns>
        public static IActionResult RedirectToChangePasswordPageResult( this SiteCache site, bool isChangePasswordRequired, bool includeReturnUrl )
        {
            var pageReference = site.ChangePasswordPageReference;

            var parms = new Dictionary<string, string>();

            if ( isChangePasswordRequired )
            {
                parms.Add( "ChangeRequired", "True" );
            }

            if ( includeReturnUrl )
            {
                parms.Add( "ReturnUrl", GetReturnUrl() );
            }

            pageReference.Parameters = parms;

            return new RedirectResult( pageReference.BuildUrl(), false );
        }

        /// <summary>
        /// Redirects to communication page.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <returns></returns>
        public static IActionResult RedirectToCommunicationPageResult( this SiteCache site )
        {
            return new RedirectResult( site.CommunicationPageReference.BuildUrl(), false );
        }

        /// <summary>
        /// Redirects to registration page.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <returns></returns>
        public static IActionResult RedirectToRegistrationPageResult( this SiteCache site )
        {
            return new RedirectResult( site.RegistrationPageReference.BuildUrl(), false );
        }
    }
}
