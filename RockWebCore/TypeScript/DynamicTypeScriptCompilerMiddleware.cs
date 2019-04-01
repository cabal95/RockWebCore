using System;
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

            if ( path.EndsWith( ".js" ) )
            {
                var tsFile = path.Substring( 0, path.Length - 3 ) + ".ts";
                var tsPath = System.IO.Path.Join( "wwwroot", tsFile );
                if ( System.IO.File.Exists( tsPath ) )
                {
                    var results = _compiler.CompileFile( tsPath );
                    if ( results.Success )
                    {
                        context.Response.ContentType = "application/javascript";
                        context.Response.ContentLength = results.SourceCode.Length;
                        await context.Response.WriteAsync( results.SourceCode );
                    }
                    else
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync( "Compilation failed." );
                    }

                    return;
                }
            }

            if ( path.EndsWith( ".js.map" ) )
            {
                var tsFile = path.Substring( 0, path.Length - 7 ) + ".ts";
                var tsPath = System.IO.Path.Join( "wwwroot", tsFile );
                if ( System.IO.File.Exists( tsPath ) )
                {
                    var results = _compiler.CompileFile( tsPath );
                    if ( results.Success )
                    {
                        context.Response.ContentType = "application/json";
                        context.Response.ContentLength = results.SourceMap.Length;
                        await context.Response.WriteAsync( results.SourceMap );
                    }
                    else
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync( "Compilation failed." );
                    }

                    return;
                }
            }

            await _next( context );
        }

        #endregion
    }
}
