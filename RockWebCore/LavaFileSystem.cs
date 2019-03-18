using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using DotLiquid;
using DotLiquid.Exceptions;
using DotLiquid.FileSystems;
using Rock;
using Rock.Web.Cache;

namespace RockWebCore
{
    public class LavaFileSystem : IFileSystem
    {
        public string Root { get; set; }

        public LavaFileSystem() { }

        public string ReadTemplateFile( Context context, string templateName )
        {
            string templatePath = ( string ) context[templateName];

            // Try to find exact file specified
            var file = new FileInfo( FullPath( templatePath ) );
            if ( file.Exists )
            {
                return File.ReadAllText( file.FullName );
            }

            // If requested template file does not include an extension
            if ( string.IsNullOrWhiteSpace( file.Extension ) )
            {
                // Try to find file with .lava extension
                string filePath = file.FullName + ".lava";
                if ( File.Exists( filePath ) )
                {
                    return File.ReadAllText( filePath );
                }

                // Try to find file with .liquid extension
                filePath = file.FullName + ".liquid";
                if ( File.Exists( filePath ) )
                {
                    return File.ReadAllText( filePath );
                }

                // If file still not found, try prefixing filename with an underscore
                if ( !file.Name.StartsWith( "_" ) )
                {
                    filePath = Path.Combine( file.DirectoryName, string.Format( "_{0}.lava", file.Name ) );
                    if ( File.Exists( filePath ) )
                    {
                        return File.ReadAllText( filePath );
                    }
                    filePath = Path.Combine( file.DirectoryName, string.Format( "_{0}.liquid", file.Name ) );
                    if ( File.Exists( filePath ) )
                    {
                        return File.ReadAllText( filePath );
                    }
                }
            }

            throw new FileSystemException( "LavaFileSystem Template Not Found", templatePath );

        }

        public string FullPath( string templatePath )
        {
            if ( templatePath != null && HttpContext.Current != null )
            {
                if ( templatePath.StartsWith( "~~" ) &&
                    HttpContext.Current.Items != null &&
                    HttpContext.Current.Items.ContainsKey( "Rock:PageId" ) )
                {
                    var rockPage = PageCache.Get( HttpContext.Current.Items["Rock:PageId"].ToString().AsInteger() );
                    if ( rockPage != null &&
                        rockPage.Layout != null &&
                        rockPage.Layout.Site != null )
                    {
                        templatePath = "~/Themes/" + rockPage.Layout.Site.Theme + ( templatePath.Length > 2 ? templatePath.Substring( 2 ) : string.Empty );
                    }
                }
            }

            if ( templatePath == null )
            {
                throw new FileSystemException( "LavaFileSystem Illegal Template Name", templatePath );
            }

            return templatePath.Replace( "~/", "wwwroot/" );
        }

    }
}
