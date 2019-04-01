using System.Collections.Generic;

namespace Rock.TypeScript
{
    /// <summary>
    /// Contains the results of a request to compile a TypeScript file into JavaScript.
    /// </summary>
    public class CompiledModule
    {
        /// <summary>
        /// Gets or sets the name of the file that was compiled.
        /// </summary>
        /// <value>
        /// The name of the file that was compiled.
        /// </value>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the JavaScript source code.
        /// </summary>
        /// <value>
        /// The JavaScript source code.
        /// </value>
        public string SourceCode { get; set; }

        /// <summary>
        /// Gets or sets the source map.
        /// </summary>
        /// <value>
        /// The source map.
        /// </value>
        public string SourceMap { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CompiledModule"/> was compiled successfully.
        /// </summary>
        /// <value>
        ///   <c>true</c> if success; otherwise, <c>false</c>.
        /// </value>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the messages that were generated during compilation.
        /// </summary>
        /// <value>
        /// The messages that were generated during compilation.
        /// </value>
        public IEnumerable<string> Messages { get; set; }
    }
}
