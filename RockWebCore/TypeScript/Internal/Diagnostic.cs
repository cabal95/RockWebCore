namespace Rock.TypeScript.Internal
{
    /// <summary>
    /// Defines the Diagnostic message data returned by the transpileModule function.
    /// </summary>
    internal class Diagnostic
    {
        /// <summary>
        /// Gets or sets the source file reference.
        /// </summary>
        /// <value>
        /// The source file reference.
        /// </value>
        public SourceFile File { get; set; }

        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        /// <value>
        /// The category.
        /// </value>
        public Category Category { get; set; }

        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <value>
        /// The code.
        /// </value>
        public int Code { get; set; }

        /// <summary>
        /// Gets or sets the starting position of the related code from the start of the file.
        /// </summary>
        /// <value>
        /// The starting position of the related code from the start of the file.
        /// </value>
        public int Start { get; set; }

        /// <summary>
        /// Gets or sets the length of the related code.
        /// </summary>
        /// <value>
        /// The length of the related code.
        /// </value>
        public int Length { get; set; }

        /// <summary>
        /// Gets or sets the message text.
        /// </summary>
        /// <value>
        /// The message text.
        /// </value>
        public string MessageText { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [reports unnecessary].
        /// </summary>
        /// <value>
        ///   <c>true</c> if it's not necessary to report; otherwise, <c>false</c>.
        /// </value>
        public bool ReportsUnnecessary { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{File.FileName}:{Category}:{Code}:{Start}:{Length}:{MessageText}";
        }
    }
}
