using System.IO;
using System.Threading.Tasks;

using Rock.Web.Cache;

namespace RockWebCore
{
    public interface IBlockType
    {
        int BlockId { get; }

        IRockPage RockPage { get; set; }

        PageCache PageCache { get; set; }

        BlockCache Block { get; set; }

        Task PreRenderAsync();

        Task RenderAsync( TextWriter writer );
    }
}
