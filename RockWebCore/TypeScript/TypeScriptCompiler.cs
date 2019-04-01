using System.IO;

using Jurassic;
using Jurassic.Library;

using Newtonsoft.Json;

namespace Rock.TypeScript
{
    public class TypeScriptCompiler
    {
        #region Properties

        /// <summary>
        /// The script engine that will be handling our compile requests.
        /// </summary>
        private ScriptEngine JSEngine { get; set; }

        /// <summary>
        /// <c>true</c> if the engine has been initialized, <c>false</c> otherwise.
        /// </summary>
        protected bool IsInitialized { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Rock.TypeScript.TypeScriptCompiler"/> class.
        /// </summary>
        public TypeScriptCompiler()
        {
            JSEngine = new ScriptEngine();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the engine if it's needed, otherwise does nothing.
        /// </summary>
        protected virtual void InitializeEngineIfNeeded()
        {
            lock ( this )
            {
                if ( IsInitialized )
                {
                    return;
                }

                var tsStream = GetType().Assembly.GetManifestResourceStream( "TestApp.typescript.js" );
                var tsScript = ReadStream( tsStream );

                JSEngine.Execute( tsScript );

                string compilerHelper = @"
function tsCompile(source, options)
{
    return ts.transpileModule(source, {
        compilerOptions: JSON.parse(options),
        reportDiagnostics: true
    });
}
";
                JSEngine.Execute( compilerHelper );

                IsInitialized = true;
            }
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
        /// Compiles the given TypeScript source into pure JavaScript.
        /// </summary>
        /// <param name="source">The typescript source code to be compiled.</param>
        /// <returns>The results of the compilation.</returns>
        public virtual CompileResult Compile( string source )
        {
            var options = new CompileOptions
            {
                Target = Target.ES5,
                Module = ModuleType.AMD
            };

            return Compile( source, options );
        }

        /// <summary>
        /// Compiles the given TypeScript source into pure JavaScript.
        /// </summary>
        /// <param name="source">The typescript source code to be compiled.</param>
        /// <param name="options">The options to use when compiling.</param>
        /// <returns>The results of the compilation.</returns>
        public virtual CompileResult Compile( string source, CompileOptions options )
        {
            InitializeEngineIfNeeded();

            lock ( this )
            {
                var jsobject = ( ObjectInstance ) JSEngine.CallGlobalFunction( "tsCompile", new[] { source, JsonConvert.SerializeObject( options ) } );

                return jsobject.Cast<CompileResult>();
            }
        }

        #endregion
    }
}
