using System;
using System.IO;

using Jurassic;

namespace TestApp
{
    class Program
    {
        static void Main( string[] args )
        {
            var compiler = new TypeScriptCompiler();

            var js = compiler.Compile( @"import { m } from 'mod';
let t = m.something + 1;" );

            Console.WriteLine( js );
        }


    }

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
function tsCompile( source )
{
    var js = ts.transpileModule(source, {
        compilerOptions: { module: ts.ModuleKind.AMD },
        reportDiagnostics: true
    });

    return js;
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
        /// Compiles the given typescript source into pure JavaScript.
        /// </summary>
        /// <param name="source">The typescript source code to be compiled.</param>
        /// <returns>A JavaScript string.</returns>
        public virtual string Compile( string source )
        {
            InitializeEngineIfNeeded();

            lock ( this )
            {
                var result = ( Jurassic.Library.ObjectInstance ) JSEngine.CallGlobalFunction( "tsCompile", new[] { source } );

                return result.GetPropertyValue( "outputText" ).ToString();
            }
        }

        #endregion
    }
}
