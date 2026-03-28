namespace WingetUSoft;

public sealed class HistoryEntry
{
    public DateTime Date { get; set; }
    public string PackageName { get; set; } = "";
    public string PackageId { get; set; } = "";
    public string FromVersion { get; set; } = "";
    public string ToVersion { get; set; } = "";
    public bool Success { get; set; }
}
