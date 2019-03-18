using System;
using System.IO;
using System.Threading.Tasks;

using Karambolo.AspNetCore.Bundling;

using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Rock.Rest;
using RockWebCore.UI;

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
        }

        public void Configure( IApplicationBuilder app, IHostingEnvironment env )
        {
            if ( env.IsDevelopment() )
            {
                app.UseDeveloperExceptionPage();
            }

            var bundlingOptions = new BundlingOptions
            {
                RequestPath = "/Scripts/Bundles"
            };
            app.UseBundling( bundlingOptions, bundles =>
            {
                bundles.AddJs( "/RockJQueryLatest.js" )
                    .Include( "/Scripts/jquery-3.3.1.js" )
                    .Include( "/Scripts/jquery-migrate-3.0.0.min.js" );

                bundles.AddJs( "/RockLibs.js" )
                .Include( "/Scripts/jquery-ui-1.10.4.custom.min.js" )
                .Include( "/Scripts/bootstrap.min.js" )
                .Include( "/Scripts/bootstrap-timepicker.js" )
                .Include( "/Scripts/bootstrap-datepicker.js" )
                .Include( "/Scripts/bootstrap-limit.js" )
                .Include( "/Scripts/bootstrap-modalmanager.js" )
                .Include( "/Scripts/bootstrap-modal.js" )
                .Include( "/Scripts/bootbox.min.js" )
                .Include( "/Scripts/chosen.jquery.min.js" )
                .Include( "/Scripts/typeahead.min.js" )
                .Include( "/Scripts/jquery.fileupload.js" )
                .Include( "/Scripts/jquery.stickytableheaders.js" )
                .Include( "/Scripts/iscroll.js" )
                .Include( "/Scripts/jcrop.min.js" )
                .Include( "/Scripts/ResizeSensor.js" )
                .Include( "/Scripts/ion.rangeSlider/js/ion-rangeSlider/ion.rangeSlider.min.js" )
                .Include( "/Scripts/Rock/Extensions/*.js" );

                bundles.AddJs( "/RockUi.js" )
                    .Include( "/Scripts/Rock/coreListeners.js" )
                    .Include( "/Scripts/Rock/dialogs.js" )
                    .Include( "/Scripts/Rock/settings.js" )
                    .Include( "/Scripts/Rock/utility.js" )
                    .Include( "/Scripts/Rock/Controls/*.js" )
                    .Include( "/Scripts/Rock/reportingInclude.js" );

                bundles.AddJs( "/RockValidation.js" )
                    .Include( "/Scripts/Rock/Validate/*.js" );

                // Creating a separate "Admin" bundle specifically for JS functionality that needs
                // to be included for administrative users
                bundles.AddJs( "/RockAdmin.js" )
                    .Include( "/Scripts/Rock/Admin/*.js" );
            } );

            DotLiquid.Template.NamingConvention = new DotLiquid.NamingConventions.CSharpNamingConvention();
            DotLiquid.Template.FileSystem = new LavaFileSystem();
            DotLiquid.Template.RegisterSafeType( typeof( Enum ), o => o.ToString() );
            DotLiquid.Template.RegisterSafeType( typeof( DBNull ), o => null );
            DotLiquid.Template.RegisterFilter( typeof( Rock.Lava.RockFilters ) );

            System.Web.HttpContext.Configure( app.ApplicationServices.GetRequiredService<IHttpContextAccessor>() );

            app.UseAuthentication();
            app.UseMiddleware<Rock.Rest.Filters.ApiKeyMiddleware>();

            app.Use( async ( context, next ) =>
            {
                if ( !string.IsNullOrEmpty( context.User?.Identity?.Name ) )
                {
                    context.Items["CurrentPerson"] = new Rock.Model.UserLoginService( new Rock.Data.RockContext() ).GetByUserName( context.User.Identity.Name ).Person;
                }

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
                    Rock.Web.Cache.RockCache.ClearAllCachedItems();
                }

                await next();
            } );

            var odataBuilder = new ODataConventionModelBuilder( app.ApplicationServices );

            app.UseMvc( routeBuilder =>
            {
                routeBuilder.EnableDependencyInjection();
            } );

            app.UseMiddleware<RockRouterMiddleware>( app );

            app.UseRockApi();

            app.UseStaticFiles();

            new Rock.Data.RockContext().Database.Migrate();
        }
    }

    public class MyApi : ControllerBase
    {
        [Microsoft.AspNetCore.Mvc.Route( "/page/{pageId}" )]
        public async Task<IActionResult> GetPage( int pageId )
        {
            HttpContext.Items["Rock:PageId"] = pageId;

            var rockPage = new RockPage( pageId, HttpContext );

            var ms = new MemoryStream();
            await rockPage.RenderAsync( ms );
            ms.Position = 0;

            return new FileStreamResult( ms, "text/html" );
        }
    }
}
