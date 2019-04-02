using System;
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
    public class JurassicTypeScriptCompiler : ITypeScriptCompiler
    {
        #region Static Fields

        private static readonly Queue<CompileRequest> _sharedQueue = new Queue<CompileRequest>();

        private static readonly AutoResetEvent _sharedQueueEvent = new AutoResetEvent( false );

        private static Thread _workerThread;

        private static readonly object _workerThreadLock = new object();

        #endregion

        #region Methods

        /// <summary>
        /// Ensures the worker thread running.
        /// </summary>
        private static void EnsureWorkerThreadRunning()
        {
            lock ( _workerThreadLock )
            {
                if ( _workerThread != null && _workerThread.IsAlive )
                {
                    return;
                }

                //
                // Default stack sizes:
                // 32-bit 1MB
                // 64-bit 4MB
                // IIS 32-bit 256 KB
                // IIS 64-bit 512 KB
                //
                // 2MB seems to be enough for our testing, we're going to double that to 4MB just to be sure.
                //
                _workerThread = new Thread( QueueThread, 4 * 1024 * 1024 );

                _workerThread.Start();
            }
        }

        /// <summary>
        /// Background thread to handle TypeScript compilation.
        /// </summary>
        private static void QueueThread()
        {
            try
            {
                //
                // Initialize the engine.
                //
                var engine = new ScriptEngine();
                InitializeEngine( engine );

                while ( true )
                {
                    //
                    // Wait for the next compile request to come in.
                    //
                    if ( !_sharedQueue.TryDequeue( out var request ) )
                    {
                        _sharedQueueEvent.WaitOne();
                        continue;
                    }

                    try
                    {
                        //
                        // Execute the compile function and then cast it to our CLR type.
                        //
                        var jsobject = engine.CallGlobalFunction<ObjectInstance>( "tsCompile", request.SourceCode, request.FileName, JsonConvert.SerializeObject( request.Options ) );
                        var result = jsobject.Cast<CompileResult>();

                        //
                        // Create the compiled module object.
                        //
                        var module = new CompiledModule
                        {
                            FileName = request.FileName,
                            Success = !result.Diagnostics.Any( d => d.Category == Category.Error ),
                            Messages = result.Diagnostics.Select( d => $"{d.Code}:{d.MessageText}" )
                        };

                        //
                        // If the compile worked, set the code and map.
                        //
                        if ( module.Success )
                        {
                            module.SourceCode = result.OutputText;
                            module.SourceMap = result.SourceMapText;
                        }

                        request.CompiledModule = module;
                    }
                    catch ( Exception ex )
                    {
                        //
                        // Generate an error with the exception message.
                        //
                        request.CompiledModule = new CompiledModule
                        {
                            FileName = request.FileName,
                            Success = false,
                            Messages = new[] { ex.Message }
                        };
                    }
                    finally
                    {
                        //
                        // Tell the requestor that their compile is finished.
                        //
                        request.WaitHandle.Set();
                    }
                }
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.WriteLine( ex.Message );
            }
        }

        /// <summary>
        /// Initializes the engine.
        /// </summary>
        /// <param name="engine">The engine.</param>
        public static void InitializeEngine( ScriptEngine engine )
        {
            var streamName = typeof( JurassicTypeScriptCompiler ).Assembly.GetManifestResourceNames().Single( n => n.EndsWith( ".TypeScript.typescript.js" ) );
            var tsStream = typeof( JurassicTypeScriptCompiler ).Assembly.GetManifestResourceStream( streamName );
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
        public static string ReadStream( Stream stream )
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
            //
            // Generate the compilation request.
            //
            var request = new CompileRequest
            {
                SourceCode = source,
                FileName = fileName,
                Options = options
            };

            //
            // Make sure the queue is running and submit the new request to the queue.
            //
            EnsureWorkerThreadRunning();
            _sharedQueue.Enqueue( request );
            _sharedQueueEvent.Set();

            //
            // Wait for up to 30 seconds for the compilation to complete.
            //
            if ( !request.WaitHandle.WaitOne( 30000 ) )
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

        #region Support Classes

        private class CompileRequest
        {
            public ManualResetEvent WaitHandle { get; } = new ManualResetEvent( false );

            public string SourceCode { get; set; }

            public string FileName { get; set; }

            public CompileOptions Options { get; set; }

            public CompiledModule CompiledModule { get; set; }
        }

        #endregion
    }
}
