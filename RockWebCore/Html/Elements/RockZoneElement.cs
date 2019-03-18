using System.IO;

using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace RockWebCore.Html.Elements
{
    public class RockZoneElement : HtmlElement
    {
        public string Name => GetAttribute( "name" );

        public RockZoneElement( Document document, string prefix = null )
            : base( document, "rock:zone", prefix )
        {
        }

        public override void ToHtml( TextWriter writer, IMarkupFormatter formatter )
        {
            writer.Write( $"<div id=\"zone-{Name.ToLowerInvariant().Replace( " ", "" )}\" class=\"zone-instance\"><div class=\"zone-content\">" );

            foreach ( var child in Children )
            {
                child.ToHtml( writer, formatter );
            }

            writer.Write( "</div></div>" );
        }
    }
}
