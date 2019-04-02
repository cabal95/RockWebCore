using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RockWebCore.BlockTypes;
using RockWebCore.DynamicRun;

namespace RockWebCore
{
    public static class BlockCacheExtensions
    {
        private static AssemblyCollector AssemblyCollector = new AssemblyCollector( "wwwroot/Blocks", "*.cs", true );

        public static IBlockType GetMappedBlockType( this Rock.Web.Cache.BlockCache block, IServiceProvider serviceProvider )
        {
            var blockPath = block.BlockType.Path;

            //
            // Check dynamic compilation.
            //
            var type = AssemblyCollector.GetExportedTypesAsync().Result
                .Where( t => typeof( IBlockType ).IsAssignableFrom( t ) )
                .Where( t => t.GetCustomAttribute<LegacyBlockAttribute>()?.Path == blockPath )
                .SingleOrDefault();

            if ( type == null )
            {
                //
                // Check against internal types.
                //
                type = typeof( BlockCacheExtensions ).Assembly.GetTypes()
                    .Where( t => typeof( IBlockType ).IsAssignableFrom( t ) )
                    .Where( t => t.GetCustomAttribute<LegacyBlockAttribute>()?.Path == blockPath )
                    .SingleOrDefault();
            }

            if ( type != null )
            {
                return ( IBlockType ) ActivatorUtilities.CreateInstance( serviceProvider, type );
            }

            return new UnsupportedBlockType();
        }
    }
}
