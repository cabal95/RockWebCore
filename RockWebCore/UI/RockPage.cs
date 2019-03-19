using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using AngleSharp.Dom;
using AngleSharp.Html.Dom;

using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Rock;
using Rock.Security;
using Rock.Web.Cache;

using RockWebCore.Html.Elements;
using RockWebCore.Html.Results;

namespace RockWebCore.UI
{
    public class RockPage : ILavaSafe, IRockPage
    {
        #region Private Fields

        private IHtmlDocument _layoutDocument;

        private Rock.Web.Cache.PageCache _pageCache;

        private readonly List<IElement> _headerElements = new List<IElement>();

        private Dictionary<string, RockZoneElement> _zones = new Dictionary<string, RockZoneElement>();

        private Dictionary<string, IElement> _startupScripts = new Dictionary<string, IElement>();

        #endregion

        #region Protected Fields


        #endregion

        #region Public Properties

        public int PageId => _pageCache.Id;

        public LayoutCache Layout => _pageCache.Layout;

        public SiteCache Site => Layout.Site;

        public HttpContext Context { get; set; }

        /// <summary>
        /// Publicly gets and privately sets the currently logged in user.
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.UserLogin"/> of the currently logged in user.
        /// </value>
        public Rock.Model.UserLogin CurrentUser
        {
            get
            {
                if ( _currentUser != null )
                {
                    return _currentUser;
                }

                _currentUser = Context.GetCurrentUser();

                if ( _currentUser == null )
                {
                    _currentUser = Rock.Model.UserLoginService.GetCurrentUser();
                    if ( _currentUser != null )
                    {
                        Context.SetCurrentUser( _currentUser );
                    }
                }

                if ( _currentUser != null && _currentUser.Person != null && _currentPerson == null )
                {
                    CurrentPerson = _currentUser.Person;
                }

                return _currentUser;
            }

            private set
            {
                _currentUser = value;

                Context.SetCurrentUser( _currentUser );

                CurrentPerson = _currentUser?.Person;
            }
        }
        private Rock.Model.UserLogin _currentUser;

        /// <summary>
        /// Publicly gets the current <see cref="Rock.Model.Person"/>.  This is either the currently logged in user, or if the user
        /// has not logged in, it may also be an impersonated person determined from using the encrypted
        /// person key.
        /// </summary>
        /// <value>
        /// A <see cref="Rock.Model.Person"/> representing the currently logged in person or impersonated person.
        /// </value>
        public Rock.Model.Person CurrentPerson
        {
            get
            {
                if ( _currentPerson == null )
                {
                    _currentPerson = Context.GetCurrentPerson();
                }

                return _currentPerson;
            }

            private set
            {
                _currentPerson = value;
                Context.SetCurrentPerson( _currentPerson );
            }
        }
        private Rock.Model.Person _currentPerson;

        /// <summary>
        /// The Person ID of the currently logged in user.  Returns null if there is not a user logged in
        /// </summary>
        /// <value>
        /// A <see cref="System.Int32" /> representing the PersonId of the <see cref="Rock.Model.Person"/>
        /// who is logged in as the current user. If a user is not logged in.
        /// </value>
        public int? CurrentPersonId => CurrentPerson?.Id;

        /// <summary>
        /// Gets the current person alias.
        /// </summary>
        /// <value>
        /// The current person alias.
        /// </value>
        public Rock.Model.PersonAlias CurrentPersonAlias => CurrentPerson?.PrimaryAlias;

        /// <summary>
        /// Gets the current person alias identifier.
        /// </summary>
        /// <value>
        /// The current person alias identifier.
        /// </value>
        public int? CurrentPersonAliasId => CurrentPersonAlias?.Id;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RockPage"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <exception cref="System.Exception">Page not found</exception>
        public RockPage( int id, HttpContext context )
        {
            _pageCache = Rock.Web.Cache.PageCache.Get( id );

            if ( _pageCache == null )
            {
                throw new System.Exception( "Page not found" );
            }

            Context = context;

            _layoutDocument = Layout.GetHtmlDocumentAsync( this ).Result;
        }

        #endregion

        #region Support Methods for Blocks

        /// <summary>
        /// Resolves the rock URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        public string ResolveRockUrl( string url )
        {
            string themeUrl = url;
            if ( url.StartsWith( "~~" ) )
            {
                themeUrl = "~/Themes/" + Layout.Site.Theme + ( url.Length > 2 ? url.Substring( 2 ) : string.Empty );
            }

            return themeUrl.StartsWith( "~" ) ? themeUrl.Substring( 1 ) : themeUrl;
        }

        /// <summary>
        /// Adds the script link.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="fingerprint">if set to <c>true</c> [fingerprint].</param>
        public void AddScriptLink( string url, bool fingerprint = true )
        {
            if ( _headerElements.Any( c => c.GetAttribute( "src" ) == url ) )
            {
                return;
            }

            var el = _layoutDocument.CreateElement( "script" );
            el.SetAttribute( "src", url );
            _headerElements.Add( el );
        }

        /// <summary>
        /// Adds the meta tag.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="content">The content.</param>
        public void AddMetaTag( string name, string content )
        {
            var el = new HtmlElement( null, "meta" );
            el.SetAttribute( "name", name );
            el.SetAttribute( "content", content );

            _headerElements.Add( el );
        }

        /// <summary>
        /// Adds the icon link.
        /// </summary>
        /// <param name="binaryFileId">The binary file identifier.</param>
        /// <param name="size">The size.</param>
        /// <param name="rel">The relative.</param>
        public void AddIconLink( int binaryFileId, int size, string rel = "apple-touch-icon-precomposed" )
        {
            var el = new HtmlElement( null, "link" );
            el.SetAttribute( "rel", rel );
            el.SetAttribute( "sizes", $"{size}x{size}" );
            el.SetAttribute( "href", $"/GetImage.ashx?id={binaryFileId}&width={size}&height={size}&mode=crop&format=png" );

            _headerElements.Add( el );
        }

        /// <summary>
        /// Adds the CSS link.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="fingerprint">if set to <c>true</c> [fingerprint].</param>
        public void AddCSSLink( string url, bool fingerprint = true )
        {
            if ( _headerElements.Any( c => c.GetAttribute( "href" ) == url ) )
            {
                return;
            }

            var el = new HtmlElement( null, "link" );
            el.SetAttribute( "rel", "stylesheet" );
            el.SetAttribute( "href", url );

            _headerElements.Add( el );
        }

        /// <summary>
        /// Registers the startup script.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="key">The key.</param>
        /// <param name="script">The script.</param>
        public void RegisterStartupScript( Type type, string key, string script )
        {
            if ( _startupScripts.ContainsKey( $"{type.FullName}-{key}" ) )
            {
                return;
            }

            var el = _layoutDocument.CreateElement( "script" );
            el.TextContent = script;

            _startupScripts.Add( $"{type.FullName}-{key}", el );
        }

        #endregion

        #region Admin Features

        protected void GenerateAdminFooter( bool canAdministrateBlockOnPage = false )
        {
            //bool canAdministratePage = _pageCache.IsAuthorized( Rock.Security.Authorization.ADMINISTRATE, CurrentPerson );
            //bool canEditPage = _pageCache.IsAuthorized( Rock.Security.Authorization.EDIT, CurrentPerson );
            bool canAdministratePage = true;
            bool canEditPage = true;
            int pageId = 1;

            if ( true /*_pageCache.IncludeAdminFooter && ( canAdministratePage || canAdministrateBlockOnPage || canEditPage )*/ )
            {
                // Add the page admin script
                AddScriptLink( ResolveRockUrl( "~/Scripts/Bundles/RockAdmin.js" ), false );

                var adminFooter = _layoutDocument.CreateElement( "div" );
                adminFooter.SetAttribute( "id", "cms-admin-footer" );
                _layoutDocument.Body.AppendChild( adminFooter );

                //phLoadStats = new PlaceHolder();
                //adminFooter.Controls.Add( phLoadStats );

                //// If the current user is Impersonated by another user, show a link on the admin bar to login back in as the original user
                //var impersonatedByUser = Session["ImpersonatedByUser"] as UserLogin;
                //var currentUserIsImpersonated = ( HttpContext.Current?.User?.Identity?.Name ?? string.Empty ).StartsWith( "rckipid=" );
                //if ( canAdministratePage && currentUserIsImpersonated && impersonatedByUser != null )
                //{
                //    HtmlGenericControl impersonatedByUserDiv = new HtmlGenericControl( "span" );
                //    impersonatedByUserDiv.AddCssClass( "label label-default margin-l-md" );
                //    _btnRestoreImpersonatedByUser = new LinkButton();
                //    _btnRestoreImpersonatedByUser.ID = "_btnRestoreImpersonatedByUser";
                //    //_btnRestoreImpersonatedByUser.CssClass = "btn";
                //    _btnRestoreImpersonatedByUser.Visible = impersonatedByUser != null;
                //    _btnRestoreImpersonatedByUser.Click += _btnRestoreImpersonatedByUser_Click;
                //    _btnRestoreImpersonatedByUser.Text = $"<i class='fa-fw fa fa-unlock'></i> " + $"Restore { impersonatedByUser?.Person?.ToString()}";
                //    impersonatedByUserDiv.Controls.Add( _btnRestoreImpersonatedByUser );
                //    adminFooter.Controls.Add( impersonatedByUserDiv );
                //}

                var buttonBar = _layoutDocument.CreateElement( "div" );
                adminFooter.AppendChild( buttonBar );
                buttonBar.SetAttribute( "class", "button-bar" );

                // RockBlock Config
                if ( canAdministratePage || canAdministrateBlockOnPage )
                {
                    var aBlockConfig = JavascriptIconLinkElement( "Block Configuration", "btn block-config", "fa fa-th-large", "Rock.admin.pageAdmin.showBlockConfig()" );
                    buttonBar.AppendChild( aBlockConfig );
                }

                if ( canEditPage || canAdministratePage )
                {
                    // RockPage Properties
                    var aPageProperties = ModalIconLinkElement( "Page Properties", "btn properties", "fa fa-cog", ResolveRockUrl( $"~/PageProperties/{pageId}?t=Page Properties" ) );
                    aPageProperties.SetAttribute( "id", "aPageProperties" );
                    buttonBar.AppendChild( aPageProperties );
                }

                if ( canAdministratePage )
                {
                    // Child Pages
                    var aChildPages = ModalIconLinkElement( "Child Pages", "btn page-child-pages", "fa fa-sitemap", ResolveRockUrl( $"~/pages/{pageId}?t=Child Pages&pb=&sb=Done" ) );
                    aChildPages.SetAttribute( "id", "aChildPages" );
                    buttonBar.AppendChild( aChildPages );

                    // RockPage Zones
                    var aPageZones = JavascriptIconLinkElement( "Page Zones", "btn page-zones", "fa fa-columns", "Rock.admin.pageAdmin.showPageZones()" );
                    buttonBar.AppendChild( aPageZones );

                    // RockPage Security
                    //var pageEntityTypeId = EntityTypeCache.Get( typeof( Rock.Model.Page ) ).Id;
                    var pageEntityTypeId = 42;
                    var aPageSecurity = ModalIconLinkElement( "Page Security", "btn page-security", "fa fa-lock", ResolveRockUrl( $"~/Secure/{pageEntityTypeId}/{pageId}?t=Page Security&pb=&sb=Done" ) );
                    buttonBar.AppendChild( aPageSecurity );

                    // ShorLink Properties
                    //HtmlGenericControl aShortLink = new HtmlGenericControl( "a" );
                    //buttonBar.Controls.Add( aShortLink );
                    //aShortLink.ID = "aShortLink";
                    //aShortLink.ClientIDMode = System.Web.UI.ClientIDMode.Static;
                    //aShortLink.Attributes.Add( "class", "btn properties" );
                    //aShortLink.Attributes.Add( "href", "javascript: Rock.controls.modal.show($(this), '" +
                    //    ResolveUrl( string.Format( "~/ShortLink/{0}?t=Shortened Link&url={1}", _pageCache.Id, Server.UrlEncode( HttpContext.Current.Request.Url.AbsoluteUri.ToString() ) ) )
                    //    + "')" );
                    //aShortLink.Attributes.Add( "Title", "Add Short Link" );
                    //HtmlGenericControl iShortLink = new HtmlGenericControl( "i" );
                    //aShortLink.Controls.Add( iShortLink );
                    //iShortLink.Attributes.Add( "class", "fa fa-link" );

                    // System Info
                    var aSystemInfo = ModalIconLinkElement( "Rock Information", "btn system-info", "fa fa-info-circle", ResolveRockUrl( "~/SystemInfo?t=System Information&pb=&sb=Done" ) );
                    buttonBar.AppendChild( aSystemInfo );
                }
            }
        }

        protected IElement JavascriptIconLinkElement( string title, string linkClass, string iconClass, string script )
        {
            var el = _layoutDocument.CreateElement( "a" );

            el.SetAttribute( "title", title );
            el.SetAttribute( "class", linkClass );
            el.SetAttribute( "href", $"javascript: {script}" );

            var icon = _layoutDocument.CreateElement( "i" );
            icon.SetAttribute( "class", iconClass );

            el.AppendChild( icon );

            return el;
        }

        protected IElement ModalIconLinkElement( string title, string linkClass, string iconClass, string url )
        {
            return JavascriptIconLinkElement( title, linkClass, iconClass, $"Rock.controls.modal.show($(this), '{url}')" );
        }

        #endregion

        #region Render Methods

        /// <summary>
        /// Finds the zones declared in the theme template.
        /// </summary>
        protected void FindZones()
        {
            _zones = _layoutDocument.GetElementsByTagName( "rock:zone" )
                .Cast<RockZoneElement>()
                .ToDictionary( z => z.Name.Replace( " ", string.Empty ), z => z );
        }

        /// <summary>
        /// Gets the zone for block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        protected RockZoneElement GetZoneForBlock( BlockCache block )
        {
            if ( _zones.ContainsKey( block.Zone ) )
            {
                return _zones[block.Zone];
            }
            else if ( _zones.ContainsKey( "Main" ) )
            {
                return _zones["Main"];
            }
            else
            {
                return _zones.Last().Value;
            }
        }

        /// <summary>
        /// Finalizes the content of the page with any data that is waiting for final insertion.
        /// </summary>
        protected void FinalizePageContent()
        {
            var head = _layoutDocument.GetElementsByTagName( "head" ).Single();
            head.AppendNodes( _headerElements.ToArray() );

            var body = _layoutDocument.GetElementsByTagName( "body" ).Single();
            body.AppendNodes( _startupScripts.Values.ToArray() );
        }

        /// <summary>
        /// Calls the pre-render events on each block of the page.
        /// </summary>
        /// <returns>A task that can be awaited.</returns>
        protected virtual async Task PreRenderAsync()
        {
            var preRenderTasks = new List<Task>();

            //
            // Create the block wrappers for each block and add them to the DOM.
            //
            _pageCache.Blocks.ForEach( b =>
            {
                var zone = GetZoneForBlock( b );
                var blockWrapper = ( RockBlockElement ) _layoutDocument.CreateElement( "rock:block" );
                blockWrapper.Block = b.GetMappedBlockType();
                blockWrapper.Block.Block = b;
                blockWrapper.Block.RockPage = this;
                blockWrapper.Block.PageCache = _pageCache;

                zone.AppendChild( blockWrapper );
                preRenderTasks.Add( blockWrapper.Block.PreRenderAsync() );
            } );

            //
            // Wait for all the pre-render tasks to finish.
            //
            await Task.WhenAll( preRenderTasks );
        }

        /// <summary>
        /// Renders the contents of each block and populates the ZoneContent property.
        /// </summary>
        /// <returns>A task that can be awaited.</returns>
        protected async Task RenderInternalAsync()
        {
            var content = new List<KeyValuePair<RockBlockElement, TextWriter>>();

            //
            // Get all the block HTML elements.
            //
            var wrappers = _zones.Values.SelectMany( z => z.Children ).Cast<RockBlockElement>().ToList();

            //
            // Start the blocks doing their final rendering, each into it's own TextWriter.
            //
            var renderTasks = wrappers.Select( b =>
            {
                var writer = new StringWriter();

                content.Add( new KeyValuePair<RockBlockElement, TextWriter>( b, writer ) );

                return b.Block.RenderAsync( writer );
            } );

            //
            // Wait for all the rendering to finish.
            //
            await Task.WhenAll( renderTasks );

            //
            // Stuff the rendered content into the HTML element.
            //
            content.ForEach( c => c.Key.Content = c.Value.ToString() );
        }

        /// <summary>
        /// Renders the page into the stream.
        /// </summary>
        /// <param name="stream">The stream to write the page contents to.</param>
        /// <returns>A task that can be awaited.</returns>
        public async Task<IActionResult> RenderAsync()
        {
            if ( !_pageCache.IsAuthorized( Authorization.VIEW, CurrentPerson ) )
            {
                if ( CurrentPerson != null )
                {
                    return new ForbidResult();
                }
                else
                {
                    string url = Context.Request.Path + Context.Request.QueryString.Value;

                    return new RedirectResult( $"/page/{Site.LoginPageId}?returnUrl={Uri.EscapeDataString(url)}" );
                }
            }

            FindZones();

            var preRenderTask = PreRenderAsync();

            AddScriptLink( ResolveRockUrl( "~/Scripts/Bundles/RockLibs.js" ) );
            AddScriptLink( ResolveRockUrl( "~/Scripts/Bundles/RockUi.js" ) );
            //AddScriptLink( ResolveRockUrl( "~/Scripts/Bundles/RockValidation.js" ) );

            await preRenderTask;

            GenerateAdminFooter();

            await RenderInternalAsync();

            FinalizePageContent();

            return new HtmlObjectResult( _layoutDocument.DocumentElement.OuterHtml );
        }

        #endregion
    }
}
