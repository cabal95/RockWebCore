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
            var script = $@"require(['VueLoader!{path}'], function (app) {{
    new app.default('vueapp_{BlockId}', {options});
}});";

            RockPage.RegisterStartupScript( GetType(), $"VueApp-{BlockId}", script );

            await writer.WriteLineAsync( $"<div id=\"vueapp_{BlockId}\"></div>" );
        }
    }
}
