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
            var script = $@"require.config({{baseUrl:'/'}});require(['{path.Substring( 8 )}'], function (app) {{
    new app.default('bid_{BlockId}', {Newtonsoft.Json.JsonConvert.SerializeObject( dataParameters )});
}});";

            RockPage.RegisterStartupScript( GetType(), $"VueApp-{BlockId}", script );

            writer.WriteLine( $"<div id=\"bid_{BlockId}\">" );
            await writer.WriteLineAsync( await File.ReadAllTextAsync( path + ".vue" ) );
            writer.WriteLine( "</div>" );
        }

        public override async Task RenderAsync( TextWriter writer )
        {
            RockPage.AddScriptLink( "https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js", false );
            // Should content be hidden when empty list
            bool hideBlockWhenEmpty = GetAttributeValue( "HideBlockWhenEmpty" ).AsBoolean();

            var items = GetItemList();

            if ( !items.Any() && hideBlockWhenEmpty )
            {
                return;
            }

            await RegisterVueApp( writer, "wwwroot/Blocks/Utility/DefinedTypeCheckList", new
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
