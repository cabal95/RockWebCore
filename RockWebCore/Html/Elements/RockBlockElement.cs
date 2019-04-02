using System.IO;

using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;

using RockWebCore.UI;

namespace RockWebCore.Html.Elements
{
    public class RockBlockElement : HtmlElement
    {
        public IBlockType Block { get; set; }

        public string Content { get; set; }

        public RockBlockElement( Document document, string prefix = null )
            : base( document, "rock:block", prefix )
        {
        }

        public override void ToHtml( TextWriter writer, IMarkupFormatter formatter )
        {
            var zone = this.ParentElement as RockZoneElement;

            writer.Write( $"<div id=\"bid_{Block.BlockId}\" data-zone-location=\"{zone.Name}\" class=\"block-instance {Block.Block.Name.ToLowerInvariant().Replace( ' ', '-' )}\"><div class=\"block-content\">" );

            writer.Write( Content );

            writer.Write( "</div></div>" );
        }
    }
}
