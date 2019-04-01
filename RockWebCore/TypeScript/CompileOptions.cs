using Newtonsoft.Json;

namespace Rock.TypeScript
{
    /// <summary>
    /// Defines the options used while compiling a TypeScript file.
    /// </summary>
    [JsonObject( ItemNullValueHandling = NullValueHandling.Ignore )]
    public class CompileOptions
    {
        /// <summary>
        /// Gets or sets the target JavaScript language version.
        /// </summary>
        /// <value>
        /// The target JavaScript language version.
        /// </value>
        [JsonProperty( "target" )]
        public LanguageVersion Target { get; set; }

        /// <summary>
        /// Gets or sets the type of module system to use when generating code.
        /// </summary>
        /// <value>
        /// The type of module system to use when generating code.
        /// </value>
        [JsonProperty( "module" )]
        public ModuleType Module { get; set; }

        /// <summary>
        /// Gets or sets the option to generate the source map.
        /// </summary>
        /// <value>
        /// The option to generate the source map.
        /// </value>
        [JsonProperty( "sourceMap" )]
        public bool? SourceMap { get; set; }

        /// <summary>
        /// Gets or sets the option to remove comments from the generated output.
        /// </summary>
        /// <value>
        /// The option to remove comments from the generated output.
        /// </value>
        [JsonProperty( "removeComments" )]
        public bool? RemoveComments { get; set; }
    }
}
