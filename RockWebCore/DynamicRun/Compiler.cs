using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace RockWebCore.DynamicRun
{
    public class Compiler
    {
        #region Properties

        /// <summary>
        /// Gets the errors during compilation.
        /// </summary>
        /// <value>
        /// The errors during compilation.
        /// </value>
        public string[] Errors { get; private set; }

        /// <summary>
        /// Gets the warnings during compilation.
        /// </summary>
        /// <value>
        /// The warnings during compilation.
        /// </value>
        public string[] Warnings { get; private set; }

        /// <summary>
        /// Gets the informatives during compilation.
        /// </summary>
        /// <value>
        /// The informatives during compilation.
        /// </value>
        public string[] Informatives { get; private set; }

        /// <summary>
        /// Gets the assembly that was compiled.
        /// </summary>
        /// <value>
        /// The assembly that was compiled.
        /// </value>
        public Assembly Assembly { get; private set; }

        /// <summary>
        /// Gets or sets the context that the assembly will be loaded into.
        /// </summary>
        /// <value>
        /// The context that the assembly will be loaded into.
        /// </value>
        public AssemblyLoadContext Context { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Compiler"/> class.
        /// </summary>
        public Compiler()
            : this( AssemblyLoadContext.Default )
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Compiler"/> class.
        /// </summary>
        /// <param name="context">The assembly load context.</param>
        public Compiler( AssemblyLoadContext context )
        {
            Errors = new string[0];
            Warnings = new string[0];
            Informatives = new string[0];

            Context = context;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Compiles the specified files into an assembly.
        /// </summary>
        /// <param name="files">The files to be compiled.</param>
        /// <returns><c>true</c> if the compilation was successful, <c>false</c> otherwise.</returns>
        public async Task<bool> CompileAsync( IEnumerable<string> files )
        {
            var sourceCodeTasks = files.Select( f => File.ReadAllTextAsync( f ) );

            var sourceCodes = await Task.WhenAll( sourceCodeTasks );

            using ( var peStream = new MemoryStream() )
            {
                var result = GenerateCode( sourceCodes ).Emit( peStream );

                Errors = result.Diagnostics.Where( d => d.Severity == DiagnosticSeverity.Error ).Select( d => $"{d.Id}: {d.GetMessage()}" ).ToArray();
                Warnings = result.Diagnostics.Where( d => d.Severity == DiagnosticSeverity.Warning ).Select( d => $"{d.Id}: {d.GetMessage()}" ).ToArray();
                Informatives = result.Diagnostics.Where( d => d.Severity == DiagnosticSeverity.Info ).Select( d => $"{d.Id}: {d.GetMessage()}" ).ToArray();

                if ( result.Success )
                {
                    peStream.Seek( 0, SeekOrigin.Begin );

                    Assembly = Context.LoadFromStream( peStream );
                }
                else
                {
                    Assembly = null;
                    var exceptions = Errors.Select( e => new Exception( e ) ).ToList();
                    throw new AggregateException( exceptions );
                }

                return result.Success;
            }
        }

        /// <summary>
        /// Generates the code.
        /// </summary>
        /// <param name="sourceCodes">The source codes.</param>
        /// <returns></returns>
        private static CSharpCompilation GenerateCode( string[] sourceCodes )
        {
            var syntaxOptions = CSharpParseOptions.Default.WithLanguageVersion( LanguageVersion.Latest );
            var compileOptions = new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default );

            var syntaxTrees = sourceCodes.Select( c => SyntaxFactory.ParseSyntaxTree( SourceText.From( c ), syntaxOptions ) );

            var trustedAssembliesPaths = ( ( string ) AppContext.GetData( "TRUSTED_PLATFORM_ASSEMBLIES" ) ).Split( Path.PathSeparator );
            var neededAssemblies = new[]
            {
                "System.Runtime",
                "netstandard",
                "System.Private.CoreLib",
                "System.Private.Uri",
                "System.Collections",
                "System.Data.Common",
                "System.IO.FileSystem",
                "System.Linq",
                "System.Linq.Expressions",
                "System.Linq.Queryable",
                "System.Runtime.Extensions",
                "System.Text.RegularExpressions"
            };

            //
            // Add in system assemblies that for whatever reason are not in our directory.
            //
            var references = trustedAssembliesPaths
//                .Where( p => neededAssemblies.Contains( Path.GetFileNameWithoutExtension( p ) ) )
                .Select( p => ( MetadataReference ) MetadataReference.CreateFromFile( p ) )
                .ToList();

            //
            // Add ourselves so it can reference anything in the executing assembly.
            //
            references.Add( MetadataReference.CreateFromFile( Assembly.GetExecutingAssembly().Location ) );

            //
            // Add in any additional assemblies in our folder.
            //
            var refPath = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );
            var refFiles = Directory.GetFiles( refPath, "*.dll" );
            foreach ( var refFile in refFiles )
            {
                if ( !references.Any( r => Path.GetFileName( r.Display ) == Path.GetFileName( refFile ) ) )
                {
                    references.Add( ( MetadataReference ) MetadataReference.CreateFromFile( refFile ) );
                }
            }

            //
            // Compile the files.
            //
            return CSharpCompilation.Create( $"DynamicRun_{Guid.NewGuid()}.dll",
                syntaxTrees,
                references: references,
                options: compileOptions );
        }

        #endregion
    }
}
