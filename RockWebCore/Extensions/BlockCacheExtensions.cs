using RockWebCore.BlockTypes;

namespace RockWebCore
{
    public static class BlockCacheExtensions
    {
        public static IBlockType GetMappedBlockType( this Rock.Web.Cache.BlockCache block )
        {
            var blockPath = block.BlockType.Path;

            if ( blockPath == "~/Blocks/Cms/HtmlContentDetail.ascx" )
            {
                return new HtmlContentDetail();
            }
            else if ( blockPath == "~/Blocks/Cms/PageMenu.ascx" )
            {
                return new PageMenu();
            }
            else if ( blockPath == "~/Blocks/Utility/DefinedTypeCheckList.ascx" )
            {
                return new DefinedTypeCheckList();
            }
            else
            {
                return new UnsupportedBlockType();
            }
        }
    }
}
