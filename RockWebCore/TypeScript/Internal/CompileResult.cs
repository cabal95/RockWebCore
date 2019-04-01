using System.Collections.Generic;

namespace Rock.TypeScript.Internal
{
    /// <summary>
    /// The results from the transpileModule call.
    /// </summary>
    internal class CompileResult
    {
        /// <summary>
        /// Gets or sets the output text.
        /// </summary>
        /// <value>
        /// The output text.
        /// </value>
        public string OutputText { get; set; }

        /// <summary>
        /// Gets or sets the source map text.
        /// </summary>
        /// <value>
        /// The source map text.
        /// </value>
        public string SourceMapText { get; set; }

        /// <summary>
        /// Gets or sets the diagnostics messages.
        /// </summary>
        /// <value>
        /// The diagnostics messages.
        /// </value>
        public List<Diagnostic> Diagnostics { get; set; }
    }
}
