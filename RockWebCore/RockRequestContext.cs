using System;
using System.Linq;
using System.Threading;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Rock.Data;
using Rock.Model;

namespace RockWebCore
{
    public class RockRequestContext : IDisposable
    {
        #region Access to Current

        private static AsyncLocal<RockRequestContext> _rockRequestContextCurrent = new AsyncLocal<RockRequestContext>();

        /// <summary>
        /// Gets or sets the current context.
        /// </summary>
        /// <value>
        /// The current context.
        /// </value>
        public static RockRequestContext Current
        {
            get
            {
                return _rockRequestContextCurrent.Value;
            }
            set
            {
                _rockRequestContextCurrent.Value = value;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the HTTP context associated with this rock request. If there is no actual Htttp request
        /// in progress then this will return null.
        /// </summary>
        /// <value>
        /// The HTTP context.
        /// </value>
        public HttpContext HttpContext { get; private set; }

        /// <summary>
        /// Gets the currently authenticated person.
        /// </summary>
        /// <value>
        /// The currently authenticated person.
        /// </value>
        public Person CurrentPerson
        {
            get
            {
                return _currentPerson.Value;
            }
            private set
            {
                _currentPerson = new Lazy<Person>( value );
            }
        }
        private Lazy<Person> _currentPerson;

        /// <summary>
        /// Gets the currently authenticated user.
        /// </summary>
        /// <value>
        /// The currently authenticated user.
        /// </value>
        public UserLogin CurrentUser
        {
            get
            {
                return _currentUser.Value;
            }
            private set
            {
                _currentUser = new Lazy<UserLogin>( value );
            }
        }
        private Lazy<UserLogin> _currentUser;

        /// <summary>
        /// Gets the current page.
        /// </summary>
        /// <value>
        /// The current page.
        /// </value>
        public IRockPage CurrentPage { get; set; }

        /// <summary>
        /// The rock context factory that we will use to generate new Rock Contexts.
        /// </summary>
        private readonly IRockContextFactory _rockContextFactory;

        /// <summary>
        /// The rock context used internally by this object.
        /// </summary>
        private readonly RockContext _rockContext;

        #endregion

        #region Virtual Properties

        /// <summary>
        /// Gets the request full URL including scheme and query string.
        /// </summary>
        /// <value>
        /// The request full URL including scheme and query string.
        /// </value>
        public virtual string RawUrl
        {
            get
            {
                if ( HttpContext == null )
                {
                    return string.Empty;
                }

                var request = HttpContext.Request;

                return $"{request.Scheme}://{request.Host}{RequestPathAndQuery}";
            }
        }

        /// <summary>
        /// Gets the request path and query string.
        /// </summary>
        /// <value>
        /// The request path and query string.
        /// </value>
        public virtual string RequestPathAndQuery
        {
            get
            {
                if ( HttpContext == null )
                {
                    return string.Empty;
                }

                var request = HttpContext.Request;

                if ( string.IsNullOrEmpty( request.PathBase ) && string.IsNullOrEmpty( request.Path ) )
                {
                    return "/";
                }

                return request.PathBase + request.Path + request.QueryString.Value;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RockRequestContext"/> class.
        /// </summary>
        public RockRequestContext()
            : this( null, null )
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RockRequestContext"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="rockContextFactory">The rock context factory.</param>
        public RockRequestContext( IHttpContextAccessor httpContextAccessor, IRockContextFactory rockContextFactory )
            : this( httpContextAccessor, rockContextFactory, null, null, null )
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RockRequestContext"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="rockContextFactory">The rock context factory.</param>
        /// <param name="rockPage">The rock page.</param>
        /// <param name="currentUser">The current user.</param>
        /// <param name="currentPerson">The current person.</param>
        public RockRequestContext( IHttpContextAccessor httpContextAccessor, IRockContextFactory rockContextFactory, IRockPage rockPage, UserLogin currentUser, Person currentPerson )
        {
            HttpContext = httpContextAccessor?.HttpContext;
            _rockContextFactory = rockContextFactory ?? new DefaultRockContextFactory();
            _rockContext = GetRockContext();

            //
            // Set the current user either to the explicit user or the one found
            // in the HttpContext.
            //
            if ( currentUser != null )
            {
                CurrentUser = currentUser;
            }
            else
            {
                _currentUser = new Lazy<UserLogin>( () =>
                {
                    UserLogin user = null;
                    string username = HttpContext?.User?.Identity?.Name;

                    if ( !string.IsNullOrEmpty( username ) && !username.StartsWith( "rckipid=" ) )
                    {
                        user = new UserLoginService( _rockContext ).GetByUserName( username );
                    }

                    return user;
                } );
            }

            //
            // Set the current person either to the explicit person or the one found
            // in the HttpContext.
            //
            if ( currentPerson != null )
            {
                CurrentPerson = currentPerson;
            }
            else
            {
                _currentPerson = new Lazy<Person>( () =>
                {
                    string username = HttpContext?.User?.Identity?.Name;
                    string rckipid = null;

                    if ( !string.IsNullOrEmpty( username ) && username.StartsWith( "rckipid=" ) )
                    {
                        rckipid = username.Substring( 8 );
                    }
                    else if ( HttpContext?.Request?.Query?.ContainsKey( "rckipid" ) ?? false )
                    {
                        rckipid = HttpContext.Request.Query["rckipid"];
                    }

                    if ( rckipid != null )
                    {
                        var impersonatedPerson = new PersonService( _rockContext ).GetByImpersonationToken( rckipid, false, rockPage?.PageId );

                        if ( impersonatedPerson != null )
                        {
                            return impersonatedPerson;
                        }
                    }

                    return CurrentUser?.Person;
                } );
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            //
            // Explicitely unreference properties so we don't end up with looped references.
            //
            HttpContext = null;
            CurrentUser = null;
            CurrentPage = null;
        }

        #endregion

        #region Methods

        public void SetLegacyValues()
        {
            //
            // This is a dirty, dirty hack until other parts of Rock are updated to
            // use the RockRequestContext. With this in place we lose the Lazy<>
            // functionality so we are always loading these even if we don't need them.
            //
            HttpContext.SetCurrentUser( CurrentUser );
            HttpContext.SetCurrentPerson( CurrentPerson );
        }

        /// <summary>
        /// Gets a new rock context.
        /// </summary>
        /// <returns></returns>
        public RockContext GetRockContext()
        {
            return _rockContextFactory.GetRockContext();
        }

        /// <summary>
        /// Checks various page parameters and returns the match.
        /// </summary>
        /// <param name="name">The key whose value is wanted.</param>
        /// <returns></returns>
        public virtual string PageParameter( string name, string defaultValue = "" )
        {
            if ( string.IsNullOrWhiteSpace( name ) )
            {
                return defaultValue;
            }

            if ( HttpContext != null )
            {
                var routeData = HttpContext.GetRouteData();
                var lowerName = name.ToLowerInvariant();
                string lowerKey;

                //
                // Check route data for the parameter.
                //
                lowerKey = routeData.Values.Keys.FirstOrDefault( k => k.ToLowerInvariant() == lowerName );
                if ( lowerKey != null )
                {
                    return routeData.Values[lowerKey].ToString();
                }

                //
                // Check the query string for the parameter.
                //
                lowerKey = HttpContext.Request.Query.Keys.FirstOrDefault( k => k.ToLowerInvariant() == name.ToLowerInvariant() );
                if ( lowerKey != null )
                {
                    return HttpContext.Request.Query[lowerKey];
                }
            }

            return defaultValue;
        }

        #endregion
    }
}
