using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Rock;
using RockWebCore.UI;

namespace RockWebCore.BlockTypes
{
    [LegacyBlock( "~/Blocks/Security/Login.ascx" )]
    public class Login : VueBlock
    {
        protected virtual string ImportComponent( string path )
        {
            var js = File.ReadAllText( $"{path}.js" );
            var templateLines = File.ReadAllLines( path );

            var template = string.Join( " +\n", templateLines.Select( s => s.ToJson() ) );

            return js.Replace( "$$template$$", template ).Trim().TrimEnd( ';' );
        }

        public override async Task RenderAsync( TextWriter writer )
        {
            string helpUrl = string.Empty;
            string redirectUrl = Uri.UnescapeDataString( System.Web.HttpContext.Current.Request.Query["returnUrl"].FirstOrDefault() ?? string.Empty );

            if ( string.IsNullOrWhiteSpace( redirectUrl ) )
            {
                redirectUrl = GetAttributeValue( "RedirectPage" );
            }

            if ( string.IsNullOrWhiteSpace( redirectUrl ) )
            {
                redirectUrl = $"/page/{RockPage.Site.DefaultPageId}";
            }

            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "HelpPage" ) ) )
            {
//                helpUrl = LinkedPageUrl( "HelpPage" );
            }
            else
            {
                helpUrl = ResolveRockUrl( "~/ForgotUserName" );
            }

            var mergeFieldsNoAccount = new Dictionary<string, object>
            {
                { "CurrentPerson", CurrentPerson },
                { "HelpPage", helpUrl }
            };

            await RegisterVueApp( writer, "Blocks/Security/Login.vue", new
            {
                BlockId = BlockId,
                PromptMessage = GetAttributeValue( "PromptMessage" ),
                InvalidPersonTokenText = "",
                ButtonDisabled = false,
                KeepLoggedIn = false,
                NewAccountLink = "link",
                ShowNewAccount = !GetAttributeValue( "HideNewAccount" ).AsBoolean(),
                NewAccountText = GetAttributeValue( "NewAccountButtonText" ),
                Username = string.Empty,
                Password = string.Empty,
                Message = string.Empty,
                NoAccountText = GetAttributeValue( "NoAccountText" ).ResolveMergeFields( mergeFieldsNoAccount ),
                RedirectUrl = redirectUrl
            } );
        }
    }
}
