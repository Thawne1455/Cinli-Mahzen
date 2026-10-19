namespace CinliMahzen.World
{
    /// <summary>One finding of <see cref="HouseValidator"/>.</summary>
    public sealed class HouseIssue
    {
        public HouseIssueSeverity Severity { get; }
        public string Message { get; }

        public HouseIssue(HouseIssueSeverity severity, string message)
        {
            Severity = severity;
            Message = message ?? string.Empty;
        }

        public override string ToString()
        {
            return "[" + Severity + "] " + Message;
        }
    }
}
