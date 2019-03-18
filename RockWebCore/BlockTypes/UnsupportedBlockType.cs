using System.IO;
using System.Threading.Tasks;

namespace RockWebCore.BlockTypes
{
    public class UnsupportedBlockType : RockBlockBase
    {
        public override async Task RenderAsync( TextWriter writer )
        {
            await writer.WriteLineAsync( $"<div class='alert alert-warning'>The block {Block.Name} uses an unsupported block type.</div>" );
        }
    }
}
