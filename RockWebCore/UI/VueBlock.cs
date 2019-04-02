using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using RockWebCore.BlockTypes;

namespace RockWebCore.UI
{
    public class VueBlock : RockBlockBase
    {
        protected virtual async Task RegisterVueApp( TextWriter writer, string path, object dataParameters )
        {
            var options = Newtonsoft.Json.JsonConvert.SerializeObject( dataParameters ?? new Dictionary<string, object>() );
            var script = $@"require(['{path}'], function (app) {{
    new app.default('bid_{BlockId}', {options});
}});";

            RockPage.RegisterStartupScript( GetType(), $"VueApp-{BlockId}", script );

            writer.WriteLine( $"<div id=\"bid_{BlockId}\">" );
            await writer.WriteLineAsync( await File.ReadAllTextAsync( "wwwroot/" + path ) );
            writer.WriteLine( "</div>" );
        }
    }
}
