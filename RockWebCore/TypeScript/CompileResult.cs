using System.Collections.Generic;

namespace Rock.TypeScript
{
    public class CompileResult
    {
        public string OutputText { get; set; }

        public List<Diagnostic> Diagnostics { get; set; }
    }
}
