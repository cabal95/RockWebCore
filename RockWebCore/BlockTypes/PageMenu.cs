using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotLiquid;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace RockWebCore.BlockTypes
{
    public class PageMenu : RockBlockBase
    {
        private static readonly string ROOT_PAGE = "RootPage";
        private static readonly string NUM_LEVELS = "NumberofLevels";

        public override Task PreRenderAsync()
        {
            if ( GetAttributeValue( "CSSFile" ).Trim() != string.Empty )
            {
                RockPage.AddCSSLink( ResolveRockUrl( GetAttributeValue( "CSSFile" ) ), false );
            }

            return Task.CompletedTask;
        }

        public override async Task RenderAsync( TextWriter writer )
        {
            try
            {
                PageCache currentPage = PageCache.Get( RockPage.PageId );
                PageCache rootPage = null;

                var pageRouteValuePair = GetAttributeValue( ROOT_PAGE ).SplitDelimitedValues( false ).AsGuidOrNullList();
                if ( pageRouteValuePair.Any() && pageRouteValuePair[0].HasValue && !pageRouteValuePair[0].Value.IsEmpty() )
                {
                    rootPage = PageCache.Get( pageRouteValuePair[0].Value );
                }

                // If a root page was not found, use current page
                if ( rootPage == null )
                {
                    rootPage = currentPage;
                }

                int levelsDeep = Convert.ToInt32( GetAttributeValue( NUM_LEVELS ) );

                Dictionary<string, string> pageParameters = null;
                //if ( GetAttributeValue( "IncludeCurrentParameters" ).AsBoolean() )
                //{
                //    pageParameters = CurrentPageReference.Parameters;
                //}

                NameValueCollection queryString = null;
                //if ( GetAttributeValue( "IncludeCurrentQueryString" ).AsBoolean() )
                //{
                //    queryString = CurrentPageReference.QueryString;
                //}

                // Get list of pages in current page's hierarchy
                var pageHeirarchy = new List<int>();
                if ( currentPage != null )
                {
                    pageHeirarchy = currentPage.GetPageHierarchy().Select( p => p.Id ).ToList();
                }

                // Add context to merge fields
                //var contextEntityTypes = RockPage.GetContextEntityTypes();
                //var contextObjects = new Dictionary<string, object>();
                //foreach ( var conextEntityType in contextEntityTypes )
                //{
                //    var contextObject = RockPage.GetCurrentContext( conextEntityType );
                //    contextObjects.Add( conextEntityType.FriendlyName, contextObject );
                //}

                var pageProperties = new Dictionary<string, object>();
                pageProperties.Add( "CurrentPerson", CurrentPerson );
                //pageProperties.Add( "Context", contextObjects );
                pageProperties.Add( "Site", GetSiteProperties( RockPage.Site ) );
                pageProperties.Add( "IncludePageList", GetIncludePageList() );
                pageProperties.Add( "CurrentPage", this.PageCache );

                using ( var rockContext = new RockContext() )
                {
                    pageProperties.Add( "Page", rootPage.GetMenuProperties( levelsDeep, CurrentPerson, rockContext, pageHeirarchy, pageParameters, queryString ) );
                }

                var lavaTemplate = GetTemplate();

                // Apply Enabled Lava Commands
                var enabledCommands = GetAttributeValue( "EnabledLavaCommands" );
                lavaTemplate.Registers.AddOrReplace( "EnabledCommands", enabledCommands );

                string content = lavaTemplate.Render( Hash.FromDictionary( pageProperties ) );

                // check for errors
                if ( content.Contains( "error" ) )
                {
                    content = "<div class='alert alert-warning'><h4>Warning</h4>" + content + "</div>";
                }

                await writer.WriteAsync( content );

            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex );
                StringBuilder errorMessage = new StringBuilder();
                errorMessage.Append( "<div class='alert alert-warning'>" );
                errorMessage.Append( "An error has occurred while generating the page menu. Error details:" );
                errorMessage.Append( ex.Message );
                errorMessage.Append( "</div>" );

                await writer.WriteAsync( errorMessage.ToString() );
            }
        }

        #region Methods

        private string CacheKey()
        {
            return string.Format( "Rock:PageMenu:{0}", BlockId );
        }

        private Template GetTemplate()
        {
            var cacheTemplate = LavaTemplateCache.Get( CacheKey(), GetAttributeValue( "Template" ) );
            return cacheTemplate != null ? cacheTemplate.Template : null;
        }

        /// <summary>
        /// Gets the site *PageId properties.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <returns>A dictionary of various page ids for the site.</returns>
        private Dictionary<string, object> GetSiteProperties( SiteCache site )
        {
            var properties = new Dictionary<string, object>();
            properties.Add( "DefaultPageId", site.DefaultPageId );
            properties.Add( "LoginPageId", site.LoginPageId );
            properties.Add( "PageNotFoundPageId", site.PageNotFoundPageId );
            properties.Add( "CommunicationPageId", site.CommunicationPageId );
            properties.Add( "RegistrationPageId ", site.RegistrationPageId );
            properties.Add( "MobilePageId", site.MobilePageId );
            return properties;
        }

        /// <summary>
        /// Gets the include page list as a dictionary to be included in the Lava.
        /// </summary>
        /// <returns>A dictionary of Titles with their Links.</returns>
        private Dictionary<string, object> GetIncludePageList()
        {
            var properties = new Dictionary<string, object>();

            var navPagesString = GetAttributeValue( "IncludePageList" );

            if ( !string.IsNullOrWhiteSpace( navPagesString ) )
            {
                navPagesString = navPagesString.TrimEnd( '|' );
                var navPages = navPagesString.Split( '|' )
                                .Select( s => s.Split( '^' ) )
                                .Select( p => new { Title = p[0], Link = p[1] } );

                StringBuilder sbPageMarkup = new StringBuilder();
                foreach ( var page in navPages )
                {
                    properties.Add( page.Title, RockPage.ResolveRockUrl( page.Link ) );
                }
            }
            return properties;
        }

        #endregion
    }
}
