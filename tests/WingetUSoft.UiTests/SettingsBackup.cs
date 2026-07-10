namespace WingetUSoft.UiTests;

/// <summary>
/// Copia y restaura <c>%AppData%\WingetUSoft\settings.json</c> y <c>history.log</c> -- la app es
/// unpackaged y no tiene almacenamiento aislado, así que sin esto las pruebas dejarían idioma/opciones/
/// exclusiones e historial de operaciones de prueba mezclados con el uso real del usuario.
/// </summary>
public sealed class SettingsBackup
{
    private readonly string _settingsPath;
    private readonly string _historyPath;
    private readonly byte[]? _settingsContent;
    private readonly byte[]? _historyContent;

    private SettingsBackup(string settingsPath, string historyPath, byte[]? settingsContent, byte[]? historyContent)
    {
        _settingsPath = settingsPath;
        _historyPath = historyPath;
        _settingsContent = settingsContent;
        _historyContent = historyContent;
    }

    public static SettingsBackup Capture()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WingetUSoft");
        var settingsPath = Path.Combine(dir, "settings.json");
        var historyPath = Path.Combine(dir, "history.log");

        return new SettingsBackup(
            settingsPath,
            historyPath,
            File.Exists(settingsPath) ? File.ReadAllBytes(settingsPath) : null,
            File.Exists(historyPath) ? File.ReadAllBytes(historyPath) : null);
    }

    public void Restore()
    {
        Restore(_settingsPath, _settingsContent);
        Restore(_historyPath, _historyContent);
    }

    private static void Restore(string path, byte[]? original)
    {
        try
        {
            if (original is null)
            {
                if (File.Exists(path)) File.Delete(path);
            }
            else
            {
                File.WriteAllBytes(path, original);
            }
        }
        catch { /* mejor esfuerzo: no tapar el resultado real de las pruebas por esto */ }
    }
}
