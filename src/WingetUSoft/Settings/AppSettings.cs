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

    /// <summary>
    /// Versiones descartadas con "Omitir esta versión": Id del paquete → versión omitida. El paquete
    /// reaparece solo cuando winget ofrece una versión distinta (ver <see cref="SkippedVersions"/>).
    /// No confundir con <see cref="ExcludedIds"/>, que excluye el paquete entero y para siempre.
    /// </summary>
    public Dictionary<string, string> SkippedVersions { get; set; } = [];

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

    public bool MinimizeToTray { get; set; } = false;
    public bool ShowNotifications { get; set; } = true;

    /// <summary>Código ISO del idioma ("es"/"en"/"pt"/"fr"/"it"). Null = sin elegir todavía (se detecta el del sistema en el primer arranque).</summary>
    public string? Language { get; set; }

    /// <summary>Última versión de la app cuyas novedades ya se mostraron (diálogo "Novedades…"). Null = nunca se mostró.</summary>
    public string? LastVersionSeen { get; set; }

    /// <summary>
    /// Error de carga pendiente de mostrar, **sin traducir**: <see cref="Load"/> corre antes de que se
    /// fije el idioma (es uno de los ajustes que lee), así que el texto se resuelve al presentarlo.
    /// </summary>
    [JsonIgnore]
    public DeferredMessage? LastLoadError { get; private set; }

    /// <summary>Error del último <see cref="Save"/>, sin traducir (ver <see cref="LastLoadError"/>).</summary>
    [JsonIgnore]
    public DeferredMessage? LastSaveError { get; private set; }

    /// <summary>True si estos ajustes se cargaron desde un settings.json ya existente (uso previo de la app), no desde los valores por defecto.</summary>
    [JsonIgnore]
    public bool LoadedFromFile { get; private set; }

    public static string LogDirectory => Path.Combine(DataDirectoryPath, "logs");

    /// <summary>Días que se conservan los registros diarios antes de purgarlos.</summary>
    internal const int LogRetentionDays = 30;

    /// <summary>
    /// Borra los registros diarios con más de <see cref="LogRetentionDays"/> días.
    /// </summary>
    /// <remarks>
    /// Se escribe un <c>.log</c> por día, con <c>LogToFile = true</c> de fábrica y sin límite ninguno:
    /// tal cual, la carpeta crece para siempre en el equipo del usuario. La purga va al arrancar porque
    /// es el único momento en que se sabe que nadie está escribiendo, y nunca debe impedir que la app
    /// abra: cualquier fallo aquí se traga a propósito, es mantenimiento, no funcionalidad.
    ///
    /// Se filtra por **nombre** (<c>aaaa-mm-dd.log</c>) y no por fecha del sistema de archivos: copiar
    /// o restaurar la carpeta reescribe las fechas de los archivos, y entonces se borraría lo que no
    /// toca o se conservaría lo que ya sobra.
    /// </remarks>
    /// <returns>Cuántos archivos se borraron.</returns>
    internal static int PurgeOldLogs(int retentionDays = LogRetentionDays)
    {
        int deleted = 0;

        try
        {
            if (!Directory.Exists(LogDirectory)) return 0;

            // "Los últimos 30 días" incluye hoy, así que el corte es hace 29 días y quedan 30 archivos.
            // Con -retentionDays se conservaban 31: el día del borde sobrevivía de propina.
            DateTime oldestKept = DateTime.Today.AddDays(-(retentionDays - 1));

            foreach (string file in Directory.EnumerateFiles(LogDirectory, "*.log"))
            {
                if (!DateTime.TryParseExact(
                        Path.GetFileNameWithoutExtension(file), "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out DateTime day))
                    continue;   // no lo escribimos nosotros: no se toca

                if (day >= oldestKept) continue;

                try { File.Delete(file); deleted++; }
                catch (IOException) { }                    // en uso: ya caerá en el próximo arranque
                catch (UnauthorizedAccessException) { }
            }
        }
        catch (Exception ex)
        {
            CrashLog.WriteDiagnostic(nameof(PurgeOldLogs), ex.Message);
        }

        return deleted;
    }

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
                settings.LoadedFromFile = true;
                return settings;
            }

            TryBackupUnreadableSettingsFile();
            return CreateDefaultsWithLoadError(
                new DeferredMessage("settings.invalidFile", SettingsFilePath));
        }
        catch (Exception ex)
        {
            if (ex is JsonException or NotSupportedException)
                TryBackupUnreadableSettingsFile();

            return CreateDefaultsWithLoadError(
                new DeferredMessage("settings.loadFailed", SettingsFilePath, ex.Message));
        }
    }

    /// <summary>Ruta temporal sobre la que se escribe antes de sustituir el archivo definitivo.</summary>
    private static string TempSettingsFilePath => SettingsFilePath + ".tmp";

    /// <summary>Copia que <see cref="File.Replace(string, string, string)"/> deja del archivo anterior.</summary>
    private static string BackupSettingsFilePath => SettingsFilePath + ".bak";

    /// <summary>
    /// Persiste la configuración de forma **atómica**: se escribe en un temporal del mismo volumen y
    /// solo después se sustituye el archivo definitivo.
    /// </summary>
    /// <remarks>
    /// Hasta aquí se hacía un <c>File.WriteAllText</c> directo sobre <c>settings.json</c>, que trunca
    /// el archivo antes de escribirlo: cualquier interrupción a mitad (corte de luz, cierre forzado,
    /// disco lleno) dejaba un JSON parcial. Y a un JSON parcial <see cref="Load"/> responde haciendo
    /// copia y **restaurando los valores por defecto**, así que el usuario perdía de golpe el historial
    /// (hasta <see cref="MaxHistoryEntries"/> entradas), la lista de exclusiones, las versiones
    /// omitidas, el idioma y el tema. La ventana era real: se guarda en cada exclusión, cada omisión,
    /// cada actualización con éxito y en el propio arranque.
    ///
    /// Con <see cref="File.Replace(string, string, string)"/> —atómico en NTFS— el archivo definitivo
    /// es siempre uno completo: o el anterior, o el nuevo. Además deja gratis una copia del anterior
    /// en <c>settings.json.bak</c>.
    /// </remarks>
    /// <returns><c>true</c> si los ajustes quedaron en disco; si no, <see cref="LastSaveError"/> explica por qué.</returns>
    public bool Save()
    {
        LastSaveError = null;

        try
        {
            Directory.CreateDirectory(DataDirectoryPath);
            File.WriteAllText(TempSettingsFilePath, JsonSerializer.Serialize(this, JsonOptions));

            if (File.Exists(SettingsFilePath))
                File.Replace(TempSettingsFilePath, SettingsFilePath, BackupSettingsFilePath, ignoreMetadataErrors: true);
            else
                File.Move(TempSettingsFilePath, SettingsFilePath, overwrite: true);

            return true;
        }
        catch (Exception ex)
        {
            LastSaveError = new DeferredMessage("settings.saveFailed", SettingsFilePath, ex.Message);
            CrashLog.WriteDiagnostic(nameof(Save), ex.Message);
            return false;
        }
        finally
        {
            // Tras un guardado correcto el temporal ya no existe (lo consumió Replace/Move). Si algo
            // falló, sí queda, y dejarlo ahí solo confundiría: el archivo bueno es settings.json.
            TryDeleteLeftoverTempFile();
        }
    }

    private static void TryDeleteLeftoverTempFile()
    {
        try
        {
            if (File.Exists(TempSettingsFilePath))
                File.Delete(TempSettingsFilePath);
        }
        catch (Exception ex)
        {
            CrashLog.WriteDiagnostic(nameof(TryDeleteLeftoverTempFile), ex.Message);
        }
    }

    public void AddHistory(HistoryEntry entry)
    {
        History.Insert(0, entry);
        if (History.Count > MaxHistoryEntries)
            History.RemoveRange(MaxHistoryEntries, History.Count - MaxHistoryEntries);
    }

    private static AppSettings CreateDefaultsWithLoadError(DeferredMessage message)
    {
        // El texto va sin traducir a propósito: esto lo lee quien depura, no el usuario, y aquí el
        // idioma todavía no está fijado (Load corre antes que L.Set — ver DeferredMessage).
        CrashLog.WriteDiagnostic(nameof(Load), message.Key);
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
            CrashLog.WriteDiagnostic(nameof(TryBackupUnreadableSettingsFile), ex.Message);
        }
    }
}
