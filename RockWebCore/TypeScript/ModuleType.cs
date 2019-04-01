namespace Rock.TypeScript
{
    /// <summary>
    /// Defines the various module systems that the generated JavaScript code can
    /// be designed to use.
    /// </summary>
    public enum ModuleType
    {
        None = 0,
        CommonJS = 1,
        AMD = 2,
        UMD = 3,
        System = 4,
        ES2015 = 5
    }
}
