using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Rock.Model;
using Rock.Web.Cache;

namespace RockWebCore
{
    public interface IRockPage
    {
        int PageId { get; }

        RockRequestContext Context { get; }

        LayoutCache Layout { get; }
    
        SiteCache Site { get; }

        Person CurrentPerson { get; }

        string ResolveRockUrl( string url );

        void AddScriptLink( string url, bool fingerprint = true );

        void AddMetaTag( string name, string content );

        void AddCSSLink( string url, bool fingerprint = true );

        void RegisterStartupScript( Type type, string key, string script );

        Task<IActionResult> RenderAsync();
    }
}
