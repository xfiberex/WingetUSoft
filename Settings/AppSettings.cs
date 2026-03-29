using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WingetUSoft;

public sealed class AppSettings
{
    private const int MaxHistoryEntries = 500;

    internal static string DefaultDataDirectoryPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WingetUSoft");

    internal static string DataDirectoryPath { get; set; } = DefaultDataDirectoryPath;

    internal static string SettingsFilePath => Path.Combine(DataDirectoryPath, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public bool SilentMode { get; set; } = true;
    public bool RunUpdatesAsAdministrator { get; set; } = false;
    public List<string> ExcludedIds { get; set; } = [];
    public List<HistoryEntry> History { get; set; } = [];
    public int AutoCheckIntervalMinutes { get; set; } = 0;

    /// <summary>0 = System, 1 = Light, 2 = Dark. Stored as int for JSON simplicity.</summary>
    public int ThemeMode { get; set; } = 0;

    /// <summary>Legacy field — kept for JSON back-compat. Migrated to ThemeMode on load.</summary>
    public bool DarkMode
    {
        get => ThemeMode == 2;
        set { if (value && ThemeMode == 0) ThemeMode = 2; }
    }

    public bool LogToFile { get; set; } = true;

    [JsonIgnore]
    public string? LastLoadError { get; private set; }

    [JsonIgnore]
    public string? LastSaveError { get; private set; }

    public static string LogDirectory => Path.Combine(DataDirectoryPath, "logs");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
                return new AppSettings();

            string json = File.ReadAllText(SettingsFilePath);
            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json);

            if (settings is not null)
            {
                settings.LastLoadError = null;
                settings.LastSaveError = null;
                return settings;
            }

            TryBackupUnreadableSettingsFile();
            return CreateDefaultsWithLoadError(
                $"El archivo de configuración '{SettingsFilePath}' es inválido o está vacío. Se usarán los valores predeterminados.");
        }
        catch (Exception ex)
        {
            if (ex is JsonException or NotSupportedException)
                TryBackupUnreadableSettingsFile();

            return CreateDefaultsWithLoadError(
                $"No se pudo cargar la configuración desde '{SettingsFilePath}': {ex.Message}");
        }
    }

    public bool Save()
    {
        LastSaveError = null;

        try
        {
            Directory.CreateDirectory(DataDirectoryPath);
            File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(this, JsonOptions));
            return true;
        }
        catch (Exception ex)
        {
            LastSaveError = $"No se pudo guardar la configuración en '{SettingsFilePath}': {ex.Message}";
            Trace.TraceError(LastSaveError);
            return false;
        }
    }

    public void AddHistory(HistoryEntry entry)
    {
        History.Insert(0, entry);
        if (History.Count > MaxHistoryEntries)
            History.RemoveRange(MaxHistoryEntries, History.Count - MaxHistoryEntries);
    }

    private static AppSettings CreateDefaultsWithLoadError(string message)
    {
        Trace.TraceError(message);
        return new AppSettings { LastLoadError = message };
    }

    private static void TryBackupUnreadableSettingsFile()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
                return;

            Directory.CreateDirectory(DataDirectoryPath);
            string backupPath = Path.Combine(
                DataDirectoryPath,
                $"settings.invalid.{DateTime.Now:yyyyMMddHHmmssfff}.json");

            File.Copy(SettingsFilePath, backupPath, overwrite: false);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"No se pudo crear una copia del archivo de configuración inválido: {ex.Message}");
        }
    }
}
