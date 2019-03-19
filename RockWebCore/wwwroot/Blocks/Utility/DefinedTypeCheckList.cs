using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace RockWebCore.BlockTypes
{
    [LegacyBlock( "~/Blocks/Utility/DefinedTypeCheckList.ascx" )]
    public class DefinedTypeCheckList : RockBlockBase
    {
        public override async Task PreRenderAsync()
        {
            await base.PreRenderAsync();

            RockPage.AddScriptLink( "https://cdnjs.cloudflare.com/ajax/libs/vue/2.6.9/vue.min.js", false );
        }

        protected List<Item> GetItemList()
        {
            var attributeKey = GetAttributeValue( "AttributeKey" );

            // Should selected items be displayed
            bool hideCheckedItems = GetAttributeValue( "HideCheckedItems" ).AsBoolean();

            Guid guid = Guid.Empty;
            if ( Guid.TryParse( GetAttributeValue( "DefinedType" ), out guid ) )
            {
                var definedType = DefinedTypeCache.Get( guid );
                if ( definedType != null )
                {
                    // Get the values
                    var values = definedType.DefinedValues.OrderBy( v => v.Order ).ToList();

                    // Find all the unselected values
                    var selectedValues = new List<int>();
                    foreach ( var value in values )
                    {
                        bool selected = false;
                        if ( bool.TryParse( value.GetAttributeValue( attributeKey ), out selected ) && selected )
                        {
                            selectedValues.Add( value.Id );
                        }
                    }

                    var displayValues = hideCheckedItems ?
                        values.Where( v => !selectedValues.Contains( v.Id ) ) : values;

                    var items = displayValues
                        .Select( v => new Item
                        {
                            Id = v.Id,
                            Value = v.Value,
                            Description = v.Description,
                            Selected = selectedValues.Contains( v.Id )
                        } ).ToList();

                    return items;
                }
            }

            return new List<Item>();
        }

        protected virtual async Task RegisterVueApp( TextWriter writer, string path, object dataParameters )
        {
            var appId = $"vueapp_{BlockId}";
            var importRegex = new System.Text.RegularExpressions.Regex( "import +([^ ]+) +from +'([^']+)'" );
            var script = File.ReadAllText( path + ".js" );

            script = importRegex.Replace( script, ( match ) =>
            {
                return $"var {match.Groups[1].Value} = {ImportComponent( match.Groups[2].Value )};";
            } );

            script = script.Replace( "$$data$$", dataParameters.ToJson() ).Replace( "$$id$$", appId );

            RockPage.RegisterStartupScript( GetType(), $"VueApp-{BlockId}", script );

            await writer.WriteLineAsync( File.ReadAllText( path ).Replace( "$$id$$", appId ) );
        }

        protected virtual string ImportComponent( string path )
        {
            var js = File.ReadAllText( $"{path}.js" );
            var templateLines = File.ReadAllLines( path );

            var template = string.Join( " +\n", templateLines.Select( s => s.ToJson() ) );

            return js.Replace( "$$template$$", template ).Trim().TrimEnd( ';' );
        }

        public override async Task RenderAsync( TextWriter writer )
        {
            // Should content be hidden when empty list
            bool hideBlockWhenEmpty = GetAttributeValue( "HideBlockWhenEmpty" ).AsBoolean();

            var items = GetItemList();

            if ( !items.Any() && hideBlockWhenEmpty )
            {
                return;
            }

            await RegisterVueApp( writer, "wwwroot/Blocks/Utility/DefinedTypeCheckList.vue", new
            {
                BlockId = BlockId,
                Items = items,
                Title = GetAttributeValue( "ChecklistTitle" ),
                Description = GetAttributeValue( "ChecklistDescription" )
            } );
        }

        [ActionName( "SetSelected" )]
        public async Task<IActionResult> SetSelected( int id, bool state )
        {
            var definedTypeGuid = GetAttributeValue( "DefinedType" ).AsGuid();
            var attributeKey = GetAttributeValue( "AttributeKey" );

            using ( var rockContext = new RockContext() )
            {
                var definedValueService = new DefinedValueService( rockContext );

                var definedValue = await definedValueService.Queryable()
                    .Where( v => v.DefinedType.Guid == definedTypeGuid )
                    .Where( v => v.Id == id )
                    .SingleOrDefaultAsync();

                if ( definedValue == null )
                {
                    return new NotFoundResult();
                }

                definedValue.LoadAttributes( rockContext );

                if ( definedValue.GetAttributeValue( attributeKey ).AsBoolean() != state )
                {
                    definedValue.SetAttributeValue( attributeKey, state.ToString() );
                    definedValue.SaveAttributeValues( rockContext );
                }
            }

            return new OkResult();
        }

        protected class Item
        {
            public int Id { get; set; }

            public string Value { get; set; }

            public string Description { get; set; }

            public bool Selected { get; set; }
        }
    }
}
