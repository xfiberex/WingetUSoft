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
            return "Se canceló la elevación de permisos. La actualización no se inició.";

        if (combined.Contains("0x8A150011") || combined.Contains("No applicable update", StringComparison.OrdinalIgnoreCase))
            return "No se encontró una actualización aplicable para este paquete.";

        if (combined.Contains("0x8A150014") || combined.Contains("No applicable installer", StringComparison.OrdinalIgnoreCase))
            return "No se encontró un instalador compatible para este sistema.";

        if (combined.Contains("hash", StringComparison.OrdinalIgnoreCase) && combined.Contains("mismatch", StringComparison.OrdinalIgnoreCase))
            return "El hash del instalador no coincide. El paquete podría haber sido modificado por el proveedor.";

        if (combined.Contains("administrator", StringComparison.OrdinalIgnoreCase) || combined.Contains("administrador", StringComparison.OrdinalIgnoreCase))
            return "Se requieren permisos de administrador adicionales para este instalador específico.";

        if (combined.Contains("blocked", StringComparison.OrdinalIgnoreCase) || combined.Contains("bloqueado", StringComparison.OrdinalIgnoreCase))
            return "La instalación fue bloqueada por una directiva del sistema.";

        if (combined.Contains("currently running", StringComparison.OrdinalIgnoreCase) || combined.Contains("en ejecución", StringComparison.OrdinalIgnoreCase))
            return "El programa está actualmente en ejecución. Ciérrelo e intente de nuevo.";

        if (combined.Contains("not found", StringComparison.OrdinalIgnoreCase) || combined.Contains("no se encontró", StringComparison.OrdinalIgnoreCase))
            return "No se encontró el paquete en los orígenes configurados.";

        if (combined.Contains("network", StringComparison.OrdinalIgnoreCase) || combined.Contains("red", StringComparison.OrdinalIgnoreCase))
            return "Error de red. Verifique su conexión a internet.";

        // Fallback: extract the last meaningful line
        string[] lines = combined.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            string line = lines[i].Trim();
            if (line.Length > 5 && !line.StartsWith("--"))
                return line;
        }

        return $"Error desconocido (código de salida: {ExitCode}).";
    }
}
