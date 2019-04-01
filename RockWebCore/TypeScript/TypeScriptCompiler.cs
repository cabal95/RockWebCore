using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Jurassic;
using Jurassic.Library;

using Newtonsoft.Json;

using Rock.TypeScript.Internal;

namespace Rock.TypeScript
{
    /// <summary>
    /// Implements the native TypeScript compiler that is written in JavaScript.
    /// The ScriptEngine cannot move between threads, so create one background thread
    /// to handle compilation.
    /// </summary>
    /// <seealso cref="Rock.TypeScript.ITypeScriptCompiler" />
    public class TypeScriptCompiler : ITypeScriptCompiler
    {
        #region Static Fields

        private static readonly Queue<CompileRequest> _sharedQueue = new Queue<CompileRequest>();

        private static readonly AutoResetEvent _sharedQueueEvent = new AutoResetEvent( false );

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the <see cref="TypeScriptCompiler"/> class.
        /// </summary>
        static TypeScriptCompiler()
        {
            new Thread( QueueThread ).Start();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Background thread to handle TypeScript compilation.
        /// </summary>
        private static void QueueThread()
        {
            var engine = new ScriptEngine();
            InitializeEngine( engine );

            while ( true )
            {
                if ( !_sharedQueue.TryDequeue( out var request ) )
                {
                    _sharedQueueEvent.WaitOne();
                    continue;
                }

                System.Diagnostics.Debug.WriteLine( "Compiling..." );
                var jsobject = ( ObjectInstance ) engine.CallGlobalFunction( "tsCompile", new[] { request.SourceCode, request.FileName, JsonConvert.SerializeObject( request.Options ) } );

                var result = jsobject.Cast<CompileResult>();

                var module = new CompiledModule
                {
                    FileName = request.FileName,
                    Success = !result.Diagnostics.Any( d => d.Category == Category.Error ),
                    Messages = result.Diagnostics.Select( d => $"{d.Code}:{d.MessageText}" )
                };

                if ( module.Success )
                {
                    module.SourceCode = result.OutputText;
                    module.SourceMap = result.SourceMapText;
                }
                System.Diagnostics.Debug.WriteLine( "Done..." );

                request.CompiledModule = module;
                request.WaitHandle.Set();
            }
        }

        /// <summary>
        /// Initializes the engine.
        /// </summary>
        /// <param name="engine">The engine.</param>
        private static void InitializeEngine( ScriptEngine engine )
        {
            var streamName = typeof( TypeScriptCompiler ).Assembly.GetManifestResourceNames().Single( n => n.EndsWith( ".TypeScript.typescript.js" ) );
            var tsStream = typeof( TypeScriptCompiler ).Assembly.GetManifestResourceStream( streamName );
            var tsScript = ReadStream( tsStream );

            engine.Execute( tsScript );

            string compilerHelper = @"
function tsCompile(source, fileName, options)
{
    return ts.transpileModule(source, {
        fileName: fileName,
        compilerOptions: JSON.parse(options),
        reportDiagnostics: true
    });
}
";
            engine.Execute( compilerHelper );
        }

        /// <summary>
        /// Reads the stream as text and returns a plain string with it's contents.
        /// </summary>
        /// <param name="stream">The stream to be read.</param>
        /// <returns>The plain text string.</returns>
        protected static string ReadStream( Stream stream )
        {
            using ( var sourceReader = new StreamReader( stream ) )
            {
                return sourceReader.ReadToEnd();
            }
        }

        /// <summary>
        /// Gets the default compiler options.
        /// </summary>
        /// <returns>A new CompileOptions instance that contains the default options.</returns>
        public virtual CompileOptions GetDefaultOptions()
        {
            return new CompileOptions
            {
                Target = LanguageVersion.ES5,
                Module = ModuleType.AMD,
                SourceMap = true
            };
        }

        /// <summary>
        /// Compiles the given TypeScript source into pure JavaScript.
        /// </summary>
        /// <param name="source">The typescript source code to be compiled.</param>
        /// <returns>The results of the compilation.</returns>
        public virtual CompiledModule Compile( string source )
        {
            return Compile( source, GetDefaultOptions() );
        }

        /// <summary>
        /// Compiles the given TypeScript source into pure JavaScript.
        /// </summary>
        /// <param name="source">The typescript source code to be compiled.</param>
        /// <param name="options">The options to use when compiling.</param>
        /// <returns>The results of the compilation.</returns>
        public virtual CompiledModule Compile( string source, CompileOptions options )
        {
            return Compile( source, "module.ts", options );
        }

        /// <summary>
        /// Compiles the given TypeScript source into pure JavaScript.
        /// </summary>
        /// <param name="source">The typescript source code to be compiled.</param>
        /// <param name="fileName">The filename that represents this source code.</param>
        /// <param name="options">The options to use when compiling.</param>
        /// <returns>The results of the compilation.</returns>
        public virtual CompiledModule Compile( string source, string fileName, CompileOptions options )
        {
            var request = new CompileRequest
            {
                SourceCode = source,
                FileName = fileName,
                Options = options
            };

            _sharedQueue.Enqueue( request );
            _sharedQueueEvent.Set();

            if ( !request.WaitHandle.WaitOne( 15000 ) )
            {
                return new CompiledModule
                {
                    FileName = fileName,
                    Success = false,
                    Messages = new[] { "Timeout waiting for compilation to complete." }
                };
            }

            return request.CompiledModule;
        }

        /// <summary>
        /// Compiles the TypeScript file into a JavaScript module.
        /// </summary>
        /// <param name="filePath">The file path that contains the TypeScript code.</param>
        /// <returns>The results of the compilation.</returns>
        public virtual CompiledModule CompileFile( string filePath )
        {
            return CompileFile( filePath, GetDefaultOptions() );
        }

        /// <summary>
        /// Compiles the TypeScript file into a JavaScript module.
        /// </summary>
        /// <param name="filePath">The file path that contains the TypeScript code.</param>
        /// <param name="options">The options to use when compiling.</param>
        /// <returns>The results of the compilation.</returns>
        public virtual CompiledModule CompileFile( string filePath, CompileOptions options )
        {
            string tsSource = File.ReadAllText( filePath );
            var filename = Path.GetFileName( filePath );

            return Compile( tsSource, filename, options );
        }

        #endregion

        private class CompileRequest
        {
            public ManualResetEvent WaitHandle { get; } = new ManualResetEvent( false );

            public string SourceCode { get; set; }

            public string FileName { get; set; }

            public CompileOptions Options { get; set; }

            public CompiledModule CompiledModule { get; set; }
        }
    }
}
