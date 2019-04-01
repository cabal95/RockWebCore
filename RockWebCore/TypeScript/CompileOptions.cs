using Newtonsoft.Json;

namespace Rock.TypeScript
{
    [JsonObject( ItemNullValueHandling = NullValueHandling.Ignore )]
    public class CompileOptions
    {
        [JsonProperty( "target" )]
        public Target Target { get; set; }

        [JsonProperty( "module" )]
        public ModuleType Module { get; set; }

        [JsonProperty( "sourceMap" )]
        public bool? SourceMap { get; set; }

        [JsonProperty( "removeComments" )]
        public bool? RemoveComments { get; set; }

        [JsonProperty( "isolatedModules" )]
        public bool? IsolatedModules { get; set; }
    }
}
