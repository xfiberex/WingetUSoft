namespace WingetUSoft;

public enum HistoryStatusFilter { All, Success, Failed }

/// <summary>
/// Filtrado del historial de actualizaciones por texto (nombre/Id) y estado. Lógica pura y
/// testeable (sin UI ni efectos colaterales).
/// </summary>
internal static class HistoryFilter
{
    public static List<HistoryEntry> Apply(
        IEnumerable<HistoryEntry> entries, string? searchText, HistoryStatusFilter status)
    {
        IEnumerable<HistoryEntry> filtered = status switch
        {
            HistoryStatusFilter.Success => entries.Where(e => e.Success),
            HistoryStatusFilter.Failed  => entries.Where(e => !e.Success),
            _                           => entries,
        };

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            string term = searchText.Trim();
            filtered = filtered.Where(e =>
                e.PackageName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.PackageId.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return [.. filtered];
    }
}
