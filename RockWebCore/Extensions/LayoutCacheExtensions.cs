using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

using Rock.Web.Cache;
using Rock.Web.UI;

using RockWebCore.Html;

namespace RockWebCore
{
    public static class LayoutCacheExtensions
    {
        /// <summary>
        /// Gets the HTML document for the layout.
        /// </summary>
        /// <param name="layout">The layout whose Html Document is to be retrieved.</param>
        /// <param name="rockPage">The rock page associated with the layout. TODO: Maybe should be changed to a mergeFields dictionary?</param>
        /// <returns>An IHtmlDocument that contains the DOM.</returns>
        public static async Task<IHtmlDocument> GetHtmlDocumentAsync( this LayoutCache layout, RockWebCore.UI.RockPage rockPage )
        {
            var mergeFields = new Dictionary<string, object>
            {
                { "CurrentPage", rockPage }
            };

            var themeFilename = $"wwwroot/Themes/{layout.Site.Theme}/Layouts/Master.lava";
            var layoutContent = await File.ReadAllTextAsync( themeFilename );

            var resolveTask = layoutContent.ResolveMergeFieldsAsync( mergeFields );

            //
            // Generate custom configuration to use our own Html Element Factory.
            //
            var configuration = Configuration.Default
                .Without<IElementFactory<Document, HtmlElement>>()
                .With( new RockHtmlElementFactory() );
            var context = BrowsingContext.New( configuration );

            return await new HtmlParser( new HtmlParserOptions(), context ).ParseDocumentAsync( await resolveTask );
        }
    }
}
