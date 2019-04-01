using RockWeb.TypeScript;

namespace Microsoft.AspNetCore.Builder
{
    public static class DynamicTypeScriptCompilerMiddlewareBuilderExtensions
    {
        /// <summary>
        /// Uses the Dynamic TypeScript Compiler middleware.
        /// </summary>
        /// <param name="app">The application.</param>
        public static void UseDynamicTypeScriptCompiler( this IApplicationBuilder app )
        {
            app.UseMiddleware<DynamicTypeScriptCompilerMiddleware>();
        }
    }
}
