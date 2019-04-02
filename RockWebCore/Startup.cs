using System;

using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Rock.Rest;
using Rock.Web.Cache;

namespace RockWebCore
{

    public class Startup
    {
        public IConfiguration Configuration { get; private set; }

        public IHostingEnvironment Environment { get; private set; }

        public Startup( IConfiguration configuration, IHostingEnvironment environment )
        {
            Configuration = configuration;
            Environment = environment;
        }

        public void ConfigureServices( IServiceCollection services )
        {
            services.AddAuthentication( CookieAuthenticationDefaults.AuthenticationScheme )
                .AddCookie( o =>
                {
                    o.LoginPath = "/api/Auth/Login";
                    o.Cookie.Name = ".ROCK";
                } );

            services.AddMvc( o => o.EnableEndpointRouting = false )
                .SetCompatibilityVersion( CompatibilityVersion.Version_2_2 )
                .AddJsonOptions( options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Unspecified;
                } );

            services.AddBundling()
                .UseDefaults( Environment )
                .UseNUglify()
                .AddLess()
                .UseTimestampVersioning();

            services.AddOData();

            services.AddHttpContextAccessor();
            services.TryAddSingleton<IRockContextFactory, DefaultRockContextFactory>();
            services.TryAddScoped<RockRequestContext, RockRequestContext>();
            services.TryAddSingleton<Rock.TypeScript.ITypeScriptCompiler, Rock.TypeScript.JurassicTypeScriptCompiler>();

            services.AddLogging( config =>
            {
                config.ClearProviders();
            } );
        }

        public void Configure( IApplicationBuilder app, IHostingEnvironment env )
        {
            if ( env.IsDevelopment() )
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDynamicTypeScriptCompiler();

            //
            // Legacy support to HttpContext.Current.
            //
            System.Web.HttpContext.Configure( app.ApplicationServices.GetRequiredService<IHttpContextAccessor>() );

            app.UseRockBundles();
            app.UseStaticFiles();

            //
            // Provides a single RockRequestContext to each request.
            //
            app.UseMiddleware<RockRequestContextMiddleware>();

            app.UseAuthentication();

            //
            // Handles API key => user translation.
            //
            app.UseMiddleware<Rock.Rest.Filters.ApiKeyMiddleware>();

            //
            // Temporary hack until legacy parts switch to new RockRequestContext.
            //
            app.Use( async ( context, next ) =>
            {
                RockRequestContext.Current.SetLegacyValues();
                await next();
            } );

            //
            // Temporary, since we don't have a way to clear cache without restarting, clear
            // cache before each reqeust.
            //
            app.Use( async ( context, next ) =>
            {
                if ( context.Request.Query.ContainsKey( "clearcache" ) )
                {
                    RockCache.ClearAllCachedItems();
                }

                await next();
            } );

            var odataBuilder = new ODataConventionModelBuilder( app.ApplicationServices );

            app.UseMvc( routeBuilder =>
            {
                routeBuilder.EnableDependencyInjection();
            } );

            app.UseRockRouting();
            app.UseRockApi();

            ConfigureDotLiquid();

            //new Rock.Data.RockContext().Database.Migrate();
        }

        /// <summary>
        /// Configures the dot liquid Lava template engine.
        /// </summary>
        protected void ConfigureDotLiquid()
        {
            DotLiquid.Template.NamingConvention = new DotLiquid.NamingConventions.CSharpNamingConvention();
            DotLiquid.Template.FileSystem = new LavaFileSystem();
            DotLiquid.Template.RegisterSafeType( typeof( Enum ), o => o.ToString() );
            DotLiquid.Template.RegisterSafeType( typeof( DBNull ), o => null );
            DotLiquid.Template.RegisterFilter( typeof( Rock.Lava.RockFilters ) );
        }
    }
}
