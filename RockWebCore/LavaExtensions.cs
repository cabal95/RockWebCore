using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Fluid;
using Fluid.Values;

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

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

            if ( typeof( ILavaSafe ).IsAssignableFrom( type ) )
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
            subpath = subpath.Substring( 0, subpath.Length - 7 );

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
            var template = FluidTemplate.Parse( s );
            var context = new TemplateContext();

            foreach ( var f in mergeFields )
            {
                context.SetValue( f.Key, f.Value );
            }

            return template.Render( context );
        }

        public static async Task<string> ResolveMergeFieldsAsync( this string s, Dictionary<string, object> mergeFields )
        {
            var template = FluidTemplate.Parse( s );
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
            return FluidValue.Create( input.ToStringValue().Replace( "~~", "/Themes/Rock" ).Replace( "~", "" ) );
        }

        public static FluidValue ReadFile( FluidValue input, FilterArguments arguments, TemplateContext context )
        {
            var path = input.ToStringValue().Replace( "~~", "wwwroot/Themes/Rock" ).Replace( "~", "wwwroot" );

            return FluidValue.Create( File.ReadAllText( path ) );
        }

        #endregion
    }
}
