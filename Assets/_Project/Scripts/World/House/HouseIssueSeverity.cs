namespace CinliMahzen.World
{
    /// <summary>How bad a <see cref="HouseIssue"/> is. Int values are stable.</summary>
    public enum HouseIssueSeverity
    {
        /// <summary>The house breaks the marker contract; round setup may fail.</summary>
        Error = 0,

        /// <summary>Allowed for now (e.g. the cellar is not built yet) or merely suspicious.</summary>
        Warning = 1
    }
}
