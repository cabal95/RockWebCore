namespace Rock.TypeScript
{
    public class Diagnostic
    {
        public SourceFile File { get; set; }

        public Category Category { get; set; }

        public int Code { get; set; }

        public int Start { get; set; }

        public int Length { get; set; }

        public string MessageText { get; set; }

        public bool ReportsUnnecessary { get; set; }

        public override string ToString()
        {
            return $"{File.FileName}:{Category}:{Code}:{Start}:{Length}:{MessageText}";
        }
    }
}
