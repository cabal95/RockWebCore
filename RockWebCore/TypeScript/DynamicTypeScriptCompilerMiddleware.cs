using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Http;

using Rock;
using Rock.TypeScript;

namespace RockWeb.TypeScript
{
    public class DynamicTypeScriptCompilerMiddleware
    {
        #region Private Fields

        private readonly ITypeScriptCompiler _compiler;
        private readonly RequestDelegate _next;

        private readonly Dictionary<string, CachedContent> _cachedFiles = new Dictionary<string, CachedContent>();

        private readonly int _cacheTime = 0;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicTypeScriptCompilerMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next.</param>
        /// <param name="compiler">The compiler.</param>
        public DynamicTypeScriptCompilerMiddleware( RequestDelegate next, ITypeScriptCompiler compiler )
        {
            _next = next;
            _compiler = compiler;
        }

        #endregion

        #region Middleware Methods

        private async Task ProcessVueFile( HttpContext context, string path )
        {
            var filePath = System.IO.Path.Combine( "wwwroot", path.Substring( 1 ) );
            var fileInfo = new System.IO.FileInfo( filePath );

            if ( !fileInfo.Exists )
            {
                await _next( context );
                return;
            }

            //
            // See if we have it cached.
            //
            if ( _cachedFiles.ContainsKey( path ) )
            {
                if ( context.Request.Headers.ContainsKey( "If-None-Match" ) && context.Request.Headers["If-None-Match"] == fileInfo.LastWriteTime.ToString() )
                {
                    context.Response.StatusCode = 304;
                    return;
                }
            }

            var parser = new HtmlParser();
            var dom = parser.ParseDocument( "<html><body></body></html>" );
            dom.Body.InnerHtml = System.IO.File.ReadAllText( filePath );
            var scriptNode = dom.Body.Children.Where( c => c.TagName == "SCRIPT" ).SingleOrDefault();
            if ( scriptNode.GetAttribute( "lang" ) != "ts" )
            {
                await _next( context );
                return;
            }
            var results = _compiler.Compile( scriptNode.TextContent );

            //
            // Store the cached result or inform the user that compilation failed.
            //
            if ( results.Success )
            {
                scriptNode.TextContent = results.SourceCode + "\n";
                _cachedFiles.AddOrReplace( path, new CachedContent
                {
                    Content = dom.Body.InnerHtml,
                    ETag = fileInfo.LastWriteTime.ToString()
                } );
            }
            else
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync( "Compilation failed." );
            }

            var cachedFile = _cachedFiles[path];

            //
            // Send the response.
            //
            context.Response.ContentType = "text/vue";
            context.Response.ContentLength = cachedFile.Content.Length;
            context.Response.Headers.AddOrReplace( "ETag", cachedFile.ETag );
            context.Response.Headers.AddOrReplace( "Pragma", "cache" );
            context.Response.Headers.AddOrReplace( "Cache-Control", $"max-age={_cacheTime}" );
            await context.Response.WriteAsync( cachedFile.Content );
        }

        /// <summary>
        /// Invokes the asynchronous handler.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public async Task InvokeAsync( HttpContext context )
        {
            var path = context.Request.Path.Value;
            System.IO.FileInfo fileInfo = null;

            if ( path.EndsWith( ".vue" ) )
            {
                await ProcessVueFile( context, path );
                return;
            }

            if ( !path.EndsWith( ".js" ) && !path.EndsWith( ".js.map" ) )
            {
                await _next( context );
                return;
            }

            string basePath = path.Replace( path.EndsWith( ".js" ) ? ".js" : ".js.map", string.Empty );
            var tsPath = System.IO.Path.Join( "wwwroot", basePath ) + ".ts";

            fileInfo = new System.IO.FileInfo( tsPath );
            if ( !fileInfo.Exists )
            {
                await _next( context );
                return;
            }

            //
            // See if we have it cached.
            //
            if ( _cachedFiles.ContainsKey( path ) )
            {
                if ( context.Request.Headers.ContainsKey( "If-None-Match" ) && context.Request.Headers["If-None-Match"] == fileInfo.LastWriteTime.ToString() )
                {
                    context.Response.StatusCode = 304;
                    return;
                }
            }

            var results = _compiler.CompileFile( tsPath );

            //
            // Store the cached result or inform the user that compilation failed.
            //
            if ( results.Success )
            {
                _cachedFiles.AddOrReplace( basePath + ".js", new CachedContent { Content = results.SourceCode, ETag = fileInfo.LastWriteTime.ToString() } );
                _cachedFiles.AddOrReplace( basePath + ".js.map", new CachedContent { Content = results.SourceMap, ETag = fileInfo.LastWriteTime.ToString() } );
            }
            else
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync( "Compilation failed." );
            }

            var cachedFile = _cachedFiles[path];

            //
            // Send the response.
            //
            context.Response.ContentType = "application/javascript";
            context.Response.ContentLength = cachedFile.Content.Length;
            context.Response.Headers.AddOrReplace( "ETag", cachedFile.ETag );
            context.Response.Headers.AddOrReplace( "Pragma", "cache" );
            context.Response.Headers.AddOrReplace( "Cache-Control", $"max-age={_cacheTime}" );
            await context.Response.WriteAsync( cachedFile.Content );
        }

        #endregion

        private class CachedContent
        {
            public string Content { get; set; }

            public string ETag { get; set; }
        }
    }
}
