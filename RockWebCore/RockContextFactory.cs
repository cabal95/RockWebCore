using Rock.Data;

namespace RockWebCore
{
    public interface IRockContextFactory
    {
        RockContext GetRockContext();
    }

    public class DefaultRockContextFactory : IRockContextFactory
    {
        public RockContext GetRockContext()
        {
            return new RockContext();
        }
    }
}
