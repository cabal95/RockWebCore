using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Fluid;
using Fluid.Ast;
using Fluid.Tags;
using Fluid.Values;

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using RockWebCore.UI;

namespace RockWebCore
{
    public interface ILavaSafe
    {
    }

    public class LavaSafeAccessor : IMemberAccessor
    {
        public object Get( object obj, string name, TemplateContext ctx )
        {
            var mi = obj.GetType().GetProperty( name );
            if ( mi == null || !mi.CanRead )
            {
                return null;
            }

            return mi.GetValue( obj );
        }
    }

    public class RockMemberAccessStrategy : IMemberAccessStrategy
    {
        private readonly MemberAccessStrategy baseMemberAccessStrategy = new MemberAccessStrategy();

        public IMemberAccessor GetAccessor( Type type, string name )
        {
            var accessor = baseMemberAccessStrategy.GetAccessor( type, name );
            if ( accessor != null )
            {
                return accessor;
            }

            if ( typeof( ILavaSafe ).IsAssignableFrom( type ) || typeof( Rock.Lava.ILiquidizable ).IsAssignableFrom( type ) )
            {
                return new LavaSafeAccessor();
            }

            return null;
        }

        public void Register( Type type, string name, IMemberAccessor getter )
        {
            baseMemberAccessStrategy.Register( type, name, getter );
        }
    }

    public class RockLavaFileProvider : IFileProvider
    {
        IFileProvider _baseProvider;

        public RockLavaFileProvider()
        {
            string root = Path.Join( Directory.GetCurrentDirectory(), "wwwroot" );
            _baseProvider = new PhysicalFileProvider( root );
        }

        public IDirectoryContents GetDirectoryContents( string subpath )
        {
            return _baseProvider.GetDirectoryContents( subpath );
        }

        public IFileInfo GetFileInfo( string subpath )
        {
            //
            // Strip off the .liquid at the end of the path.
            //
            if ( subpath.EndsWith( ".liquid" ) )
            {
                subpath = subpath.Substring( 0, subpath.Length - 7 );
            }

            if ( !subpath.EndsWith( ".lava" ) )
            {
                subpath += ".lava";
            }

            if ( subpath.StartsWith( "~" ) )
            {
                subpath = subpath.Substring( 1 );
            }

            return _baseProvider.GetFileInfo( subpath );
        }

        public IChangeToken Watch( string filter )
        {
            return _baseProvider.Watch( filter );
        }
    }

    public class MasterPage : ExpressionBlock
    {
        public override async ValueTask<Completion> WriteToAsync( TextWriter writer, TextEncoder encoder, TemplateContext context, Expression expression, List<Statement> statements )
        {
            using ( var subWriter = new StringWriter() )
            {
                var completion = Completion.Normal;

                for ( int i = 0; i < statements.Count; i++ )
                {
                    var statement = statements[i];

                    completion = await statement.WriteToAsync( subWriter, encoder, context );

                    if ( completion != Completion.Normal )
                    {
                        break;
                    }
                }

                context.EnterChildScope();
                context.SetValue( "ChildLayout", subWriter.ToString() );

                //
                // Find the lava file to use as the master page.
                //
                var relativePath = ( await expression.EvaluateAsync( context ) ).ToStringValue();
                if ( !relativePath.EndsWith( ".lava", StringComparison.OrdinalIgnoreCase ) )
                {
                    relativePath += ".lava";
                }

                if ( relativePath.StartsWith( "~~" ) )
                {
                    var rockPage = context.GetValue( "CurrentPage" ).ToObjectValue() as RockPage;

                    if ( rockPage != null )
                    {
                        relativePath = $"~/Themes/{rockPage.Site.Theme}" + relativePath.Substring( 2 );
                    }
                }

                //
                // Check if the file exists.
                //
                var fileProvider = context.FileProvider ?? TemplateContext.GlobalFileProvider;
                var fileInfo = fileProvider.GetFileInfo( relativePath );

                if ( !fileInfo.Exists )
                {
                    throw new FileNotFoundException( relativePath );
                }

                using ( var stream = fileInfo.CreateReadStream() )
                {
                    using ( var streamReader = new StreamReader( stream ) )
                    {
                        string partialTemplate = await streamReader.ReadToEndAsync();
                        var parser = context.ParserFactory != null ? context.ParserFactory.CreateParser() : FluidTemplate.Factory.CreateParser();

                        if ( parser.TryParse( partialTemplate, true, out var masterStatements, out var errors ) )
                        {
                            var template = context.TemplateFactory != null ? context.TemplateFactory() : new FluidTemplate();

                            template.Statements = masterStatements;

                            await template.RenderAsync( writer, encoder, context );
                        }
                        else
                        {
                            throw new Exception( String.Join( Environment.NewLine, errors ) );
                        }
                    }
                }

                context.ReleaseScope();
            }

            return Completion.Normal;
        }
    }

    public class RockFluidTemplate : BaseFluidTemplate<RockFluidTemplate>
    {
        static RockFluidTemplate()
        {
            Factory.RegisterBlock<MasterPage>( "masterpage" );
        }
    }

    public static class LavaExtensions
    {
        static LavaExtensions()
        {
            TemplateContext.GlobalFilters.AddFilter( "ResolveRockUrl", ResolveRockUrl );
            TemplateContext.GlobalFilters.AddFilter( "ReadFile", ReadFile );
            TemplateContext.GlobalFilters.AddFilter( "Replace", Fluid.Filters.StringFilters.Replace );

            TemplateContext.GlobalMemberAccessStrategy = new RockMemberAccessStrategy();
            TemplateContext.GlobalFileProvider = new RockLavaFileProvider();
        }

        public static string ResolveMergeFields( this string s )
        {
            return ResolveMergeFields( s, new Dictionary<string, object>() );
        }

        public static string ResolveMergeFields( this string s, Dictionary<string, object> mergeFields )
        {
            var template = RockFluidTemplate.Parse( s );
            var context = new TemplateContext();

            foreach ( var f in mergeFields )
            {
                context.SetValue( f.Key, f.Value );
            }

            return template.Render( context );
        }

        public static async Task<string> ResolveMergeFieldsAsync( this string s, Dictionary<string, object> mergeFields )
        {
            var template = RockFluidTemplate.Parse( s );
            var context = new TemplateContext();

            foreach ( var f in mergeFields )
            {
                context.SetValue( f.Key, f.Value );
            }

            return await template.RenderAsync( context );
        }

        #region Filters

        public static FluidValue ResolveRockUrl( FluidValue input, FilterArguments arguments, TemplateContext context )
        {
            var url = input.ToStringValue().Replace( "~~", "/Themes/Rock" ).Replace( "~", "" );

            return FluidValue.Create( url != string.Empty ? url : "/" );
        }

        public static FluidValue ReadFile( FluidValue input, FilterArguments arguments, TemplateContext context )
        {
            var path = input.ToStringValue().Replace( "~~", "wwwroot/Themes/Rock" ).Replace( "~", "wwwroot" );

            return FluidValue.Create( File.ReadAllText( path ) );
        }

        #endregion
    }
}
