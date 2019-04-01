namespace Rock.TypeScript
{
    public interface ITypeScriptCompiler
    {
        /// <summary>
        /// Gets the default compiler options.
        /// </summary>
        /// <returns>A new CompileOptions instance that contains the default options.</returns>
        CompileOptions GetDefaultOptions();

        /// <summary>
        /// Compiles the given TypeScript source into pure JavaScript.
        /// </summary>
        /// <param name="source">The typescript source code to be compiled.</param>
        /// <returns>The results of the compilation.</returns>
        CompiledModule Compile( string source );

        /// <summary>
        /// Compiles the given TypeScript source into pure JavaScript.
        /// </summary>
        /// <param name="source">The typescript source code to be compiled.</param>
        /// <param name="options">The options to use when compiling.</param>
        /// <returns>The results of the compilation.</returns>
        CompiledModule Compile( string source, CompileOptions options );

        /// <summary>
        /// Compiles the given TypeScript source into pure JavaScript.
        /// </summary>
        /// <param name="source">The typescript source code to be compiled.</param>
        /// <param name="fileName">The filename that represents this source code.</param>
        /// <param name="options">The options to use when compiling.</param>
        /// <returns>The results of the compilation.</returns>
        CompiledModule Compile( string source, string fileName, CompileOptions options );

        /// <summary>
        /// Compiles the TypeScript file into a JavaScript module.
        /// </summary>
        /// <param name="filePath">The file path that contains the TypeScript code.</param>
        /// <returns>The results of the compilation.</returns>
        CompiledModule CompileFile( string filePath );

        /// <summary>
        /// Compiles the TypeScript file into a JavaScript module.
        /// </summary>
        /// <param name="filePath">The file path that contains the TypeScript code.</param>
        /// <param name="options">The options to use when compiling.</param>
        /// <returns>The results of the compilation.</returns>
        CompiledModule CompileFile( string filePath, CompileOptions options );
    }
}
