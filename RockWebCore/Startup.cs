using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Rock.Rest;

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
                .UseTimestampVersioning()
                .EnableMinification();

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

            } );

            app.UseMiddleware<RockRouterMiddleware>( app );

            app.UseAuthentication();
            app.UseMiddleware<Rock.Rest.Filters.ApiKeyMiddleware>();

            var odataBuilder = new ODataConventionModelBuilder( app.ApplicationServices );

            app.UseMvc( routeBuilder =>
            {
                routeBuilder.EnableDependencyInjection();
            } );

            System.Web.HttpContext.Configure( app.ApplicationServices.GetRequiredService<IHttpContextAccessor>() );

            app.UseRockApi();

            new Rock.Data.RockContext().Database.Migrate();

            app.UseStaticFiles();
        }
    }

    public class RockPage : ILavaSafe
    {
        public int Id { get; set; }

        public RockLayout Layout { get; private set; }

        public string HeaderContent { get; set; }

        public IReadOnlyDictionary<string, string> ZoneContent => new ReadOnlyDictionary<string, string>( _zoneContent );
        private Dictionary<string, string> _zoneContent;

        public IReadOnlyList<RockBlockBase> Blocks
        {
            get
            {
                return new ReadOnlyCollection<RockBlockBase>( _blocks );
            }
        }
        private readonly List<RockBlockBase> _blocks = new List<RockBlockBase>();


        public RockPage( int id )
        {
            Id = id;
            if ( id == 2 )
            {
                Layout = new RockLayout( "Splash" );
            }
            else
            {
                Layout = new RockLayout( "Full Width", "Master" );
            }
        }

        public void AddBlock( RockBlockBase block )
        {
            _blocks.Add( block );
        }

        /// <summary>
        /// Calls the pre-render events on each block of the page.
        /// </summary>
        /// <returns>A task that can be awaited.</returns>
        protected virtual async Task PreRenderAsync()
        {
            var preRenderTasks = _blocks.Select( b => b.PreRenderAsync() );

            await Task.WhenAll( preRenderTasks );
        }

        /// <summary>
        /// Renders the contents of each block and populates the ZoneContent property.
        /// </summary>
        /// <returns>A task that can be awaited.</returns>
        protected async Task RenderAsync()
        {
            var content = new List<KeyValuePair<string, TextWriter>>();

            var renderTasks = _blocks.Select( b =>
            {
                var writer = new StringWriter();

                content.Add( new KeyValuePair<string, TextWriter>( b.Zone, writer ) );

                return b.RenderAsync( writer );
            } );

            await Task.WhenAll( renderTasks );

            _zoneContent = content.GroupBy( kv => kv.Key )
                .ToDictionary( g => g.Key, g => string.Join( "\n", g.Select( w => w.Value.ToString() ) ) );
        }

        /// <summary>
        /// Renders the page into the stream.
        /// </summary>
        /// <param name="stream">The stream to write the page contents to.</param>
        /// <returns>A task that can be awaited.</returns>
        public async Task RenderAsync( Stream stream )
        {
            await PreRenderAsync();

            await RenderAsync();

            var mergeFields = new Dictionary<string, object>
            {
                { "CurrentPage", this }
            };

            using ( var sw = new StreamWriter( stream, System.Text.Encoding.UTF8, 4096, true ) )
            {
                var layoutContent = await File.ReadAllTextAsync( Layout.RelativePath );

                var content = await layoutContent.ResolveMergeFieldsAsync( mergeFields );

                await sw.WriteAsync( content );
            }
        }
    }

    public class RockLayout : ILavaSafe
    {
        public string RelativePath
        {
            get
            {
                return $"wwwroot/Themes/{ThemeName}/Layouts/{FileName}.lava";
            }
        }

        public string FileName { get; protected set; }

        public string Name { get; protected set; }

        public string ThemeName { get; protected set; }

        public RockLayout( string layoutName )
        {
            Name = layoutName;
            FileName = layoutName.Replace( " ", string.Empty );
            ThemeName = "Rock";
        }

        public RockLayout( string layoutName, string fileName )
            : this( layoutName )
        {
            FileName = fileName;
        }
    }

    public class RockBlockBase
    {
        public string Zone { get; set; }

        public virtual Task PreRenderAsync()
        {
            return Task.CompletedTask;
        }

        public virtual async Task RenderAsync( TextWriter writer )
        {
            await writer.WriteLineAsync( "<p>Hello world!</p>" );
        }
    }

    public class MyApi : ControllerBase
    {
        [Route( "/page/{pageId}" )]
        public async Task<IActionResult> GetPage( int pageId )
        {
            var rockPage = new RockPage( pageId );

            rockPage.AddBlock( new RockBlockBase
            {
                Zone = "Main"
            } );

            var ms = new MemoryStream();
            await rockPage.RenderAsync( ms );
            ms.Position = 0;

            return new FileStreamResult( ms, "text/html" );
        }
    }
}
