using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.TypeScript;
using Rock.Web.Cache;

using RockWebCore.UI;

namespace RockWeb.TypeScript
{
    public class DynamicTypeScriptCompilerMiddleware
    {
        #region Private Fields

        private readonly ITypeScriptCompiler _compiler;
        private readonly RequestDelegate _next;

        private readonly Dictionary<string, CachedContent> _cachedFiles = new Dictionary<string, CachedContent>();

        private readonly int _cacheTime = 3600;

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

        /// <summary>
        /// Invokes the asynchronous handler.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public async Task InvokeAsync( HttpContext context )
        {
            var path = context.Request.Path.Value;
            System.IO.FileInfo fileInfo = null;

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
                if ( context.Request.Headers.ContainsKey( "If-None-Match" ) && context.Request.Headers["If-None-Match"] == _cachedFiles[path].ETag )
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
