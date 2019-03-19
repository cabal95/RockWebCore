namespace RockWebCore.BlockTypes
{
    public class LegacyBlockAttribute : System.Attribute
    {
        public string Path { get; }

        public LegacyBlockAttribute( string path )
        {
            Path = path;
        }
    }
}
