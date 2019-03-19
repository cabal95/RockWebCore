using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RockWebCore.DynamicRun
{
    public class AssemblyCollector
    {
        #region Properties

        /// <summary>
        /// Gets or sets the compiled assembly.
        /// </summary>
        /// <value>
        /// The compiled assembly.
        /// </value>
        protected Assembly CompiledAssembly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether we need to recompile.
        /// </summary>
        /// <value>
        ///   <c>true</c> if we need to recompile; otherwise, <c>false</c>.
        /// </value>
        protected bool NeedRecompile { get; set; }

        /// <summary>
        /// Gets the file system watchers.
        /// </summary>
        /// <value>
        /// The file system watchers.
        /// </value>
        protected List<FileSystemWatcher> Watchers { get; } = new List<FileSystemWatcher>();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyCollector"/> class.
        /// </summary>
        public AssemblyCollector()
        {
            NeedRecompile = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyCollector"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="pattern">The pattern.</param>
        /// <param name="includeSubfolders">if set to <c>true</c> [include subfolders].</param>
        public AssemblyCollector( string path, string pattern, bool includeSubfolders )
            : this()
        {
            AddAssemblyPath( path, pattern, includeSubfolders );
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds the path to the list of watched folders.
        /// </summary>
        /// <param name="path">The path to be watched.</param>
        /// <param name="pattern">The file name pattern to be watched.</param>
        /// <param name="includeSubfolders">if set to <c>true</c> [include subfolders].</param>
        public void AddAssemblyPath( string path, string pattern, bool includeSubfolders )
        {
            var watcher = new FileSystemWatcher( path, pattern )
            {
                IncludeSubdirectories = includeSubfolders
            };

            watcher.Changed += watcher_Changed;
            watcher.Created += watcher_Changed;
            watcher.Deleted += watcher_Changed;
            watcher.Renamed += watcher_Renamed;

            Watchers.Add( watcher );

            watcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Gets the assembly.
        /// </summary>
        /// <returns></returns>
        public async Task<Assembly> GetAssemblyAsync()
        {
            if ( NeedRecompile )
            {
                var compiler = new Compiler();

                var files = Watchers.SelectMany( w => Directory.EnumerateFiles( w.Path, w.Filter, w.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly ) );

                NeedRecompile = false;

                var success = await compiler.CompileAsync( files );

                if ( success )
                {
                    CompiledAssembly = compiler.Assembly;
                }
            }

            return CompiledAssembly;
        }

        /// <summary>
        /// Gets the types exported by the assembly.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Type>> GetExportedTypesAsync()
        {
            var assembly = await GetAssemblyAsync();

            return assembly.GetExportedTypes();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the Renamed event of the watcher control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RenamedEventArgs"/> instance containing the event data.</param>
        private void watcher_Renamed( object sender, RenamedEventArgs e )
        {
            CompiledAssembly = null;
            NeedRecompile = true;
        }

        /// <summary>
        /// Handles the Changed event of the watcher control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FileSystemEventArgs"/> instance containing the event data.</param>
        private void watcher_Changed( object sender, FileSystemEventArgs e )
        {
            CompiledAssembly = null;
            NeedRecompile = true;
        }

        #endregion
    }
}
