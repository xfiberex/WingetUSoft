namespace WingetUSoft;

public sealed class UpgradeResult
{
    public bool Success { get; init; }
    public int ExitCode { get; init; }
    public bool UserCancelled { get; init; }
    public string Output { get; init; } = "";
    public string ErrorOutput { get; init; } = "";

    public string GetFailureReason()
    {
        if (Success) return "";

        string combined = $"{Output}\n{ErrorOutput}";

        if (UserCancelled || ExitCode == 1223
            || combined.Contains("canceled by the user", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("cancelado por el usuario", StringComparison.OrdinalIgnoreCase))
            return L.T("reason.userCancelled");

        if (combined.Contains("0x8A150011") || combined.Contains("No applicable update", StringComparison.OrdinalIgnoreCase))
            return L.T("reason.noApplicableUpdate");

        if (combined.Contains("0x8A150014") || combined.Contains("No applicable installer", StringComparison.OrdinalIgnoreCase))
            return L.T("reason.noApplicableInstaller");

        if (combined.Contains("hash", StringComparison.OrdinalIgnoreCase) && combined.Contains("mismatch", StringComparison.OrdinalIgnoreCase))
            return L.T("reason.hashMismatch");

        if (combined.Contains("administrator", StringComparison.OrdinalIgnoreCase) || combined.Contains("administrador", StringComparison.OrdinalIgnoreCase))
            return L.T("reason.needsAdmin");

        if (combined.Contains("blocked", StringComparison.OrdinalIgnoreCase) || combined.Contains("bloqueado", StringComparison.OrdinalIgnoreCase))
            return L.T("reason.blocked");

        if (combined.Contains("currently running", StringComparison.OrdinalIgnoreCase) || combined.Contains("en ejecución", StringComparison.OrdinalIgnoreCase))
            return L.T("reason.currentlyRunning");

        if (combined.Contains("not found", StringComparison.OrdinalIgnoreCase) || combined.Contains("no se encontró", StringComparison.OrdinalIgnoreCase))
            return L.T("reason.notFound");

        if (combined.Contains("network", StringComparison.OrdinalIgnoreCase) || combined.Contains("red", StringComparison.OrdinalIgnoreCase))
            return L.T("reason.networkError");

        // Fallback: extract the last meaningful line
        string[] lines = combined.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            string line = lines[i].Trim();
            if (line.Length > 5 && !line.StartsWith("--"))
                return line;
        }

        return L.T("reason.unknownError", ExitCode);
    }
}
