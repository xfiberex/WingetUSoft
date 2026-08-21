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

    /// <summary>
    /// Códigos de error de winget → motivo que se le enseña al usuario. **Esta tabla es la vía principal
    /// de clasificación**; el texto solo se mira si ninguna entrada casa.
    /// </summary>
    /// <remarks>
    /// La cadena de <c>if</c> que había aquí clasificaba por texto y solo reconocía español e inglés,
    /// justo la trampa que el proyecto ya documenta en <see cref="WingetTable"/> y resolvió en
    /// <c>WingetShowLabels</c>: <b>winget traduce su salida</b> al idioma de Windows. En un Windows en
    /// francés, italiano, alemán o portugués no casaba ninguna rama y todo acababa en el <i>fallback</i>
    /// de «última línea con sentido»: el usuario veía la línea cruda de winget.
    ///
    /// Los códigos no se traducen, así que se comprueban primero, y por partida doble: contra el
    /// <see cref="ExitCode"/> del proceso y contra el texto, porque winget imprime el código en su
    /// salida incluso cuando el proceso termina con otro valor (p. ej. en los lotes).
    ///
    /// Las asignaciones salen de la tabla oficial de <c>winget-cli</c>
    /// (<c>doc/windows/package-manager/winget/returnCodes.md</c>). **Dos de las que había estaban mal:**
    /// <c>0x8A150011</c> no es «no hay actualización aplicable» sino «el hash del instalador no coincide»
    /// —un fallo de integridad que se le presentaba al usuario como un «ya estás al día»— y
    /// <c>0x8A150014</c> no es «ningún instalador aplicable» sino «no se encontró el paquete».
    /// </remarks>
    private static readonly (int Code, string ReasonKey)[] WingetErrorCodes =
    [
        (unchecked((int)0x8A15002B), "reason.noApplicableUpdate"),     // No applicable update found
        (unchecked((int)0x8A150010), "reason.noApplicableInstaller"),  // None of the installers are applicable
        (unchecked((int)0x8A150011), "reason.hashMismatch"),           // Installer hash does not match the manifest
        (unchecked((int)0x8A150019), "reason.needsAdmin"),             // Command requires administrator privileges
        (unchecked((int)0x8A15003A), "reason.blocked"),                // Blocked by Group Policy
        (unchecked((int)0x8A15010F), "reason.blocked"),                // Blocked by organization policy
        (unchecked((int)0x8A150101), "reason.currentlyRunning"),       // Application is currently running
        (unchecked((int)0x8A150102), "reason.currentlyRunning"),       // Another installation already in progress
        (unchecked((int)0x8A150014), "reason.notFound"),               // No packages found
        (unchecked((int)0x8A150008), "reason.networkError"),           // Downloading installer failed
        (unchecked((int)0x8A150086), "reason.networkError"),           // Downloaded zero byte installer
        (unchecked((int)0x8A150107), "reason.networkError"),           // Requires internet connectivity
    ];

    /// <summary>El código tal y como winget lo imprime en su salida: <c>0x8A15002B</c>.</summary>
    private static string ToHex(int code) => "0x" + code.ToString("X8");

    public string GetFailureReason()
    {
        if (Success) return "";

        string combined = $"{Output}\n{ErrorOutput}";

        // 1223 = ERROR_CANCELLED (el usuario dijo que no al UAC). Va antes que nada: no es un fallo.
        if (UserCancelled || ExitCode == 1223
            || HasWord(combined, "canceled by the user")
            || HasWord(combined, "cancelado por el usuario"))
            return L.T("reason.userCancelled");

        // 1) Por código: funciona en cualquier idioma de Windows.
        foreach ((int code, string reasonKey) in WingetErrorCodes)
            if (ExitCode == code || combined.Contains(ToHex(code), StringComparison.OrdinalIgnoreCase))
                return L.T(reasonKey);

        // 2) Por texto: último recurso, y solo cubre español e inglés. Sigue haciendo falta cuando el
        //    fallo viene del instalador del paquete, que trae sus propios códigos y su propio texto.
        if (HasWord(combined, "hash") && HasWord(combined, "mismatch"))
            return L.T("reason.hashMismatch");

        if (HasWord(combined, "administrator") || HasWord(combined, "administrador"))
            return L.T("reason.needsAdmin");

        if (HasWord(combined, "blocked") || HasWord(combined, "bloqueado"))
            return L.T("reason.blocked");

        if (HasWord(combined, "currently running") || HasWord(combined, "en ejecución"))
            return L.T("reason.currentlyRunning");

        if (HasWord(combined, "No applicable update"))
            return L.T("reason.noApplicableUpdate");

        if (HasWord(combined, "No applicable installer"))
            return L.T("reason.noApplicableInstaller");

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
