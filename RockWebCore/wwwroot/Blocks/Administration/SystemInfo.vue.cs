using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Transactions;
using Rock.VersionInfo;
using Rock.Web.Cache;
using RockWebCore.UI;

namespace RockWebCore.BlockTypes
{
    [LegacyBlock( "~/Blocks/Administration/SystemInfo.ascx" )]
    public class SystemInfo : VueBlock
    {
        public override async Task RenderAsync( TextWriter writer )
        {
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            string processStartTime = "-";

            if ( currentProcess != null && currentProcess.StartTime != null )
            {
                processStartTime = currentProcess.StartTime.ToString( "G" ) + " " + DateTime.Now.ToString( "zzz" );
            }

            var transactionQueueStats = RockQueue.TransactionQueue.ToList().GroupBy( a => a.GetType().Name ).ToList().Select( a => new { Name = a.Key, Count = a.Count() } );

            await RegisterVueApp( writer, "Blocks/Administration/SystemInfo.vue", new
            {
                BlockId,
                RockVersion = string.Format( "{0} <small>({1})</small>", VersionInfo.GetRockProductVersionFullName(), VersionInfo.GetRockProductVersionNumber() ),
                ClientCulture = System.Globalization.CultureInfo.CurrentCulture.ToString(),
                Database = GetDbInfo(),
                SystemDateTime = DateTime.Now.ToString( "G" ) + " " + DateTime.Now.ToString( "zzz" ),
                RockTime = Rock.RockDateTime.Now.ToString( "G" ) + " " + Rock.RockDateTime.OrgTimeZoneInfo.BaseUtcOffset.ToString(),
                ProcessStartTime = processStartTime,
                LastMigrations = GetLastMigrationData(),
                ExecLocation = Assembly.GetExecutingAssembly().Location,
                TransactionQueue = transactionQueueStats.Select( a => string.Format( "{0}: {1}", a.Name, a.Count ) ).ToList().AsDelimited( "<br/>" ),
                CacheOverview = string.Empty,
                CacheObjects = GetCacheInfo(),
                Routes = GetRoutesInfo(),
                Threads = GetThreadInfo(),
                FalseCacheHits = string.Empty,
            } );
        }

        #region Methods

        /// <summary>
        /// Queries the MigrationHistory and the PluginMigration tables and returns 
        /// the name (MigrationId) of the last core migration that was run and a table
        /// listing the last plugin assembly's migration name and number that was run. 
        /// </summary>
        /// <returns>An HTML fragment of the MigrationId of the last core migration and a table of the
        /// last plugin migrations.</returns>
        private string GetLastMigrationData()
        {
            StringBuilder sb = new StringBuilder();

            var result = DbService.ExecuteScaler( "SELECT TOP 1 [MigrationId] FROM [__EFMigrationsHistory] ORDER BY [MigrationId] DESC ", CommandType.Text, null );
            if ( result != null )
            {
                sb.AppendFormat( "Last Core Migration: {0}{1}", ( string ) result, Environment.NewLine );
            }

            var tableResult = DbService.GetDataTable( @"
    WITH summary AS 
    (
        SELECT p.[PluginAssemblyName], p.MigrationName, p.[MigrationNumber], ROW_NUMBER() 
            OVER( PARTITION BY p.[PluginAssemblyName] ORDER BY p.[MigrationNumber] DESC ) AS section
        FROM [PluginMigration] p
    )
    SELECT s.[PluginAssemblyName], s.MigrationName, s.[MigrationNumber]
    FROM summary s
    WHERE s.section = 1", System.Data.CommandType.Text, null );

            if ( tableResult != null )
            {
                sb.AppendFormat( "<table class='table table-condensed'>" );
                sb.Append( "<tr><th>Plugin Assembly</th><th>Migration Name</th><th>Number</th></tr>" );
                foreach ( DataRow row in tableResult.Rows )
                {
                    sb.AppendFormat( "<tr><td>{0}</td><td>{1}</td><td>{2}</td></tr>", row[0].ToString(), row[1].ToString(), row[2].ToString() );
                }
                sb.AppendFormat( "</table>" );
            }

            return sb.ToString();
        }

        private string GetCacheInfo()
        {
            StringBuilder sb = new StringBuilder();

            var cacheStats = RockCache.GetAllStatistics();
            foreach ( CacheItemStatistics cacheItemStat in cacheStats.OrderBy( s => s.Name ) )
            {
                foreach ( CacheHandleStatistics cacheHandleStat in cacheItemStat.HandleStats )
                {
                    var stats = new List<string>();
                    cacheHandleStat.Stats.ForEach( s => stats.Add( string.Format( "{0}: {1:N0}", s.CounterType.ConvertToString(), s.Count ) ) );
                    sb.AppendFormat( "<p><strong>{0}:</strong><br/>{1}</p>{2}", cacheItemStat.Name, stats.AsDelimited( ", " ), Environment.NewLine );
                }
            }

            return sb.ToString();
        }

        private string GetRoutesInfo()
        {
#if false
            var pageService = new PageService( new RockContext() );

            var routes = new SortedDictionary<string, System.Web.Routing.Route>();
            foreach ( System.Web.Routing.Route route in System.Web.Routing.RouteTable.Routes.OfType<System.Web.Routing.Route>() )
            {
                if ( !routes.ContainsKey( route.Url ) )
                    routes.Add( route.Url, route );
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat( "<table class='table table-condensed'>" );
            sb.Append( "<tr><th>Route</th><th>Pages</th></tr>" );
            foreach ( var routeItem in routes )
            {
                //sb.AppendFormat( "{0}<br />", routeItem.Key );
                var pages = pageService.GetListByIds( routeItem.Value.PageIds() );

                sb.AppendFormat( "<tr><td>{0}</td><td>{1}</td></tr>", routeItem.Key, string.Join( "<br />", pages.Select( n => n.InternalName + " (" + n.Id.ToString() + ")" ).ToArray() ) );
            }

            sb.AppendFormat( "</table>" );
            return sb.ToString();
#endif
            return "Not Supported";
        }

        /// <summary>
        /// Gets thread pool details such as the number of threads in use and the maximum number of threads.
        /// </summary>
        /// <returns></returns>
        private string GetThreadInfo()
        {
            int maxWorkerThreads = 0;
            int maxIoThreads = 0;
            int availWorkerThreads = 0;
            int availIoThreads = 0;

            ThreadPool.GetMaxThreads( out maxWorkerThreads, out maxIoThreads );
            ThreadPool.GetAvailableThreads( out availWorkerThreads, out availIoThreads );
            var workerThreadsInUse = maxWorkerThreads - availWorkerThreads;
            var pctUse = ( ( float ) workerThreadsInUse / ( float ) maxWorkerThreads );
            string badgeType = string.Empty;
            if ( pctUse > .1 )
            {
                if ( pctUse < .3 )
                {
                    badgeType = "badge badge-warning";
                }
                else
                {
                    badgeType = "badge badge-danger";
                }
            }

            return string.Format( "<span class='{0}'>{1}</span> out of {2} worker threads in use ({3}%)", badgeType, workerThreadsInUse, maxWorkerThreads, ( int ) Math.Ceiling( pctUse * 100 ) );
        }

        private string GetDbInfo()
        {
            StringBuilder databaseResults = new StringBuilder();
            string _catalog = string.Empty;

            var csBuilder = new System.Data.SqlClient.SqlConnectionStringBuilder( ConfigurationManager.ConnectionStrings["RockContext"].ConnectionString );
            object dataSource, catalog = string.Empty;
            if ( csBuilder.TryGetValue( "data source", out dataSource ) && csBuilder.TryGetValue( "initial catalog", out catalog ) )
            {
                _catalog = catalog.ToString();
                databaseResults.Append( string.Format( "Name: {0} <br /> Server: {1}", catalog, dataSource ) );
            }

            try
            {
                // get database version
                var reader = DbService.GetDataReader( "SELECT SERVERPROPERTY('productversion'), @@Version ", System.Data.CommandType.Text, null );
                if ( reader != null )
                {
                    string version = "";
                    string versionInfo = "";

                    while ( reader.Read() )
                    {
                        version = reader[0].ToString();
                        versionInfo = reader[1].ToString();
                    }

                    databaseResults.Append( string.Format( "<br />Database Version: {0}", versionInfo ) );
                }

                try
                {
                    // get database size
                    reader = DbService.GetDataReader( "sp_helpdb '" + catalog.ToStringSafe().Replace( "'", "''" ) + "'", System.Data.CommandType.Text, null );
                    if ( reader != null )
                    {
                        // get second data table
                        reader.NextResult();
                        reader.Read();

                        string size = reader.GetValue( 5 ).ToString();
                        if ( size != "Unlimited" )
                        {
                            if ( size.Contains( "KB" ) )
                            {
                                size = size.Replace( " KB", "" );
                                int sizeInKB = Int32.Parse( size );

                                int sizeInMB = sizeInKB / 1024;

                                databaseResults.AppendFormat( "<br />Database Size: {0}", sizeInMB );
                            }
                        }
                        else
                        {
                            databaseResults.Append( "<br />Database Size: Unlimited" );
                        }
                    }
                }
                catch
                {
                    databaseResults.AppendFormat( "<br />Database Size: unable to determine" );
                }

                try
                {
                    // get database snapshot isolation details
                    reader = DbService.GetDataReader( string.Format( "SELECT [snapshot_isolation_state], [is_read_committed_snapshot_on] FROM sys.databases WHERE [name] = '{0}'", _catalog ), System.Data.CommandType.Text, null );
                    if ( reader != null )
                    {
                        bool isAllowSnapshotIsolation = false;
                        bool isReadCommittedSnapshopOn = true;

                        while ( reader.Read() )
                        {
                            isAllowSnapshotIsolation = reader[0].ToStringSafe().AsBoolean();
                            isReadCommittedSnapshopOn = reader[1].ToString().AsBoolean();
                        }

                        databaseResults.AppendFormat( "<br />Allow Snapshot Isolation: {0}<br />Is Read Committed Snapshot On: {1}<br />", isAllowSnapshotIsolation.ToYesNo(), isReadCommittedSnapshopOn.ToYesNo() );
                    }
                }
                catch { }
            }
            catch ( Exception ex )
            {
                databaseResults.AppendFormat( "Unable to read database system information: {0}", ex.Message );
            }

            return databaseResults.ToString();
        }

        // method from Rick Strahl http://weblog.west-wind.com/posts/2006/Oct/08/Recycling-an-ASPNET-Application-from-within
        private bool RestartWebApplication()
        {
            return false;
        }

        #endregion

        #region Actions

        /// <summary>
        /// Used to manually flush the cache.
        /// </summary>
        [ActionName( "ClearCache" )]
        public IActionResult btnClearCache_Click()
        {
            var msgs = RockCache.ClearAllCachedItems();

            // Flush today's Check-in Codes
            Rock.Model.AttendanceCodeService.FlushTodaysCodes();

#if false
            string webAppPath = Server.MapPath( "~" );

            // Check for any unregistered entity types, field types, and block types
            EntityTypeService.RegisterEntityTypes( webAppPath );
            FieldTypeService.RegisterFieldTypes( webAppPath );
            BlockTypeService.RegisterBlockTypes( webAppPath, Page, false );
            msgs.Add( "EntityTypes, FieldTypes, BlockTypes have been re-registered" );

            // Delete all cached files
            try
            {
                var dirInfo = new DirectoryInfo( Path.Combine( webAppPath, "App_Data/Cache" ) );
                foreach ( var childDir in dirInfo.GetDirectories() )
                {
                    childDir.Delete( true );
                }
                foreach ( var file in dirInfo.GetFiles().Where( f => f.Name != ".gitignore" ) )
                {
                    file.Delete();
                }
                msgs.Add( "Cached files have been deleted" );
            }
            catch ( Exception ex )
            {
                nbMessage.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Warning;
                nbMessage.Visible = true;
                nbMessage.Text = "The following error occurred when attempting to delete cached files: " + ex.Message;
                return;
            }

            nbMessage.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Success;
            nbMessage.Visible = true;
            nbMessage.Title = "Clear Cache";
            nbMessage.Text = string.Format( "<p>{0}</p>", msgs.AsDelimited( "<br />" ) );
#endif

            return new OkObjectResult( new {
                Error = false,
                Messages = msgs
            } );
        }

        #endregion
    }
}
