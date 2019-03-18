using System.Linq;

using AngleSharp.Dom;
using AngleSharp.Html.Dom;

using RockWebCore.Html.Elements;

namespace RockWebCore.Html
{
    public class RockHtmlElementFactory : IElementFactory<Document, HtmlElement>
    {
        private readonly IElementFactory<Document, HtmlElement> _baseFactory;

        public RockHtmlElementFactory()
        {
            _baseFactory = ( IElementFactory<Document, HtmlElement> ) AngleSharp.Configuration.Default.Services
                .Single( s => s.GetType().Name == "HtmlElementFactory" );
        }

        public HtmlElement Create( Document document, string localName, string prefix = null )
        {
            if ( localName == "rock:zone" )
            {
                return new RockZoneElement( document, prefix );
            }
            else if ( localName == "rock:block" )
            {
                return new RockBlockElement( document, prefix );
            }

            return _baseFactory.Create( document, localName, prefix );
        }
    }
}
