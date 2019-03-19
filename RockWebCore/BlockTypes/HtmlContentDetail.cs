using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Security;

namespace RockWebCore.BlockTypes
{
    [LegacyBlock( "~/Blocks/Cms/HtmlContentDetail.ascx" )]
    public class HtmlContentDetail : RockBlockBase
    {
        public override async Task RenderAsync( TextWriter writer )
        {
            string entityValue = EntityValue();
            string html = string.Empty;

            int cacheDuration = GetAttributeValue( "CacheDuration" ).AsInteger();
            string cacheTags = GetAttributeValue( "CacheTags" ) ?? string.Empty;
            string cachedContent = null;

            // only load from the cache if a cacheDuration was specified
            if ( cacheDuration > 0 )
            {
                cachedContent = HtmlContentService.GetCachedContent( this.BlockId, entityValue );
            }

            // if content not cached load it from DB
            if ( cachedContent == null )
            {
                using ( var rockContext = new RockContext() )
                {
                    var htmlContentService = new HtmlContentService( rockContext );
                    HtmlContent content = htmlContentService.GetActiveContent( this.BlockId, entityValue );

                    if ( content != null )
                    {
                        if ( content.Content.HasMergeFields() )
                        {
                            //var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                            var mergeFields = new Dictionary<string, object>();
                            mergeFields.Add( "CurrentPage", this.PageCache );

                            if ( CurrentPerson != null )
                            {
                                // TODO: When support for "Person" is not supported anymore (should use "CurrentPerson" instead), remove this line
                                mergeFields.AddOrIgnore( "Person", CurrentPerson );
                            }

                            //mergeFields.Add( "CurrentBrowser", this.RockPage.BrowserClient );

                            mergeFields.Add( "RockVersion", Rock.VersionInfo.VersionInfo.GetRockProductVersionNumber() );
                            mergeFields.Add( "CurrentPersonCanEdit", IsUserAuthorized( Authorization.EDIT ) );
                            mergeFields.Add( "CurrentPersonCanAdministrate", IsUserAuthorized( Authorization.ADMINISTRATE ) );

                            html = content.Content.ResolveMergeFields( mergeFields, GetAttributeValue( "EnabledLavaCommands" ) );
                        }
                        else
                        {
                            html = content.Content;
                        }
                    }
                    else
                    {
                        html = string.Empty;
                    }
                }

                // Resolve any dynamic url references
                string appRoot = ResolveRockUrl( "~/" );
                string themeRoot = ResolveRockUrl( "~~/" );
                html = html.Replace( "~~/", themeRoot ).Replace( "~/", appRoot );

                // cache content
                if ( cacheDuration > 0 )
                {
                    HtmlContentService.AddCachedContent( this.BlockId, entityValue, html, cacheDuration, cacheTags );
                }
            }
            else
            {
                html = cachedContent;
            }

            // add content to the content window
            await writer.WriteLineAsync( html );
        }


        /// <summary>
        /// Entities the value.
        /// </summary>
        /// <returns></returns>
        private string EntityValue()
        {
            string entityValue = string.Empty;

            string contextParameter = GetAttributeValue( "ContextParameter" );
            if ( !string.IsNullOrEmpty( contextParameter ) )
            {
                entityValue = string.Format( "{0}={1}", contextParameter, PageParameter( contextParameter ) ?? string.Empty );
            }

            string contextName = GetAttributeValue( "ContextName" );
            if ( !string.IsNullOrEmpty( contextName ) )
            {
                entityValue += "&ContextName=" + contextName;
            }

            return entityValue;
        }
    }
}
