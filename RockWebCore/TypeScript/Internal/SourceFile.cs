namespace Rock.TypeScript.Internal
{
    /// <summary>
    /// Defines the source file identifying information about a diagnostic message.
    /// </summary>
    internal class SourceFile
    {
        /// <summary>
        /// Gets or sets the text of the file.
        /// </summary>
        /// <value>
        /// The text of the file.
        /// </value>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        /// <value>
        /// The name of the file.
        /// </value>
        public string FileName { get; set; }
    }
}
