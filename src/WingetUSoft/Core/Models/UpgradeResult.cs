using System.Text.RegularExpressions;

namespace WingetUSoft;

public sealed class UpgradeResult
{
    public bool Success { get; init; }
    public int ExitCode { get; init; }
    public bool UserCancelled { get; init; }
    public string Output { get; init; } = "";
    public string ErrorOutput { get; init; } = "";

    /// <summary>
    /// ¿Aparece <paramref name="word"/> como palabra completa en <paramref name="text"/>?
    /// </summary>
    /// <remarks>
    /// Un <c>Contains</c> a secas convertía cualquier fallo que mencionara <c>requi‑red</c>,
    /// <c>sha‑red</c>, <c>expi‑red</c> o <c>configu‑red</c> en un «Error de red»: al usuario
    /// se le manda a revisar su conexión por un archivo que falta. Con el límite de palabra, «red» solo casa
    /// cuando el texto realmente dice «red».
    /// </remarks>
    private static bool HasWord(string text, string word) =>
        Regex.IsMatch(text, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase);

    public string GetFailureReason()
    {
        if (Success) return "";

        string combined = $"{Output}\n{ErrorOutput}";

        if (UserCancelled || ExitCode == 1223
            || HasWord(combined, "canceled by the user")
            || HasWord(combined, "cancelado por el usuario"))
            return L.T("reason.userCancelled");

        if (combined.Contains("0x8A150011") || HasWord(combined, "No applicable update"))
            return L.T("reason.noApplicableUpdate");

        if (combined.Contains("0x8A150014") || HasWord(combined, "No applicable installer"))
            return L.T("reason.noApplicableInstaller");

        if (HasWord(combined, "hash") && HasWord(combined, "mismatch"))
            return L.T("reason.hashMismatch");

        if (HasWord(combined, "administrator") || HasWord(combined, "administrador"))
            return L.T("reason.needsAdmin");

        if (HasWord(combined, "blocked") || HasWord(combined, "bloqueado"))
            return L.T("reason.blocked");

        if (HasWord(combined, "currently running") || HasWord(combined, "en ejecución"))
            return L.T("reason.currentlyRunning");

        if (HasWord(combined, "not found") || HasWord(combined, "no se encontró"))
            return L.T("reason.notFound");

        if (HasWord(combined, "network") || HasWord(combined, "red"))
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
