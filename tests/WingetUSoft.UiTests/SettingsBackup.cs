namespace WingetUSoft.UiTests;

/// <summary>
/// Copia y restaura el directorio de datos de la app —<c>settings.json</c>, su <c>settings.json.bak</c> y
/// la carpeta <c>logs</c>— alrededor de la suite de UI tests.
/// </summary>
/// <remarks>
/// <para>
/// La app es unpackaged y no tiene almacenamiento aislado: los UI tests corren contra el mismo
/// <c>settings.json</c> que la instalación real del usuario, con su historial, sus exclusiones y su idioma.
/// Varios tests pulsan «Guardar» en Configuración, así que sin esto sus cambios se quedarían en la app de
/// verdad.
/// </para>
/// <para>
/// Hasta F-25 este respaldo apuntaba a <c>%AppData%</c> (Roaming), pero la app escribe en
/// <c>%LocalAppData%</c>: no encontraba nada que copiar, <see cref="Restore"/> no reponía nada y la suite
/// modificaba los datos reales sin que ningún test fallara. Por eso ahora <see cref="Restore"/> comprueba
/// el resultado y lanza si algo no quedó idéntico: un respaldo que falla en silencio es peor que ninguno.
/// </para>
/// </remarks>
public sealed class SettingsBackup
{
    private const string LogsFolder = "logs";

    /// <summary>Archivos sueltos del directorio de datos que la suite puede tocar.</summary>
    private static readonly string[] TrackedFiles = ["settings.json", "settings.json.bak"];

    private readonly string _dataDirectory;

    /// <summary>Contenido original por nombre de archivo; <c>null</c> = no existía y debe seguir sin existir.</summary>
    private readonly Dictionary<string, byte[]?> _files;

    /// <summary>Contenido original de cada registro diario; <c>null</c> = la carpeta no existía.</summary>
    private readonly Dictionary<string, byte[]>? _logs;

    private SettingsBackup(string dataDirectory, Dictionary<string, byte[]?> files, Dictionary<string, byte[]>? logs)
    {
        _dataDirectory = dataDirectory;
        _files = files;
        _logs = logs;
    }

    /// <summary>
    /// El mismo directorio que usa la app (<c>AppSettings.DefaultDataDirectoryPath</c>). Este proyecto no
    /// referencia el ensamblado de la app —la conduce como proceso—, así que la ruta se repite aquí y
    /// <c>SettingsBackupTests</c> comprueba que no diverja del código de la app.
    /// </summary>
    public static string DefaultDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WingetUSoft");

    public static SettingsBackup Capture() => Capture(DefaultDataDirectory);

    public static SettingsBackup Capture(string dataDirectory)
    {
        var files = TrackedFiles.ToDictionary(
            name => name,
            name => ReadIfExists(Path.Combine(dataDirectory, name)));

        string logsDirectory = Path.Combine(dataDirectory, LogsFolder);
        Dictionary<string, byte[]>? logs = Directory.Exists(logsDirectory)
            ? Directory.EnumerateFiles(logsDirectory).ToDictionary(path => Path.GetFileName(path), File.ReadAllBytes)
            : null;

        return new SettingsBackup(dataDirectory, files, logs);
    }

    /// <summary>
    /// Deja el directorio de datos exactamente como estaba al capturar y lo verifica.
    /// </summary>
    /// <exception cref="InvalidOperationException">Algún archivo no quedó idéntico al original.</exception>
    public void Restore()
    {
        foreach (var (name, original) in _files)
            TryRestoreFile(Path.Combine(_dataDirectory, name), original);

        RestoreLogs();

        var mismatches = FindMismatches();
        if (mismatches.Count > 0)
        {
            throw new InvalidOperationException(
                "Los UI tests no dejaron intactos los datos reales de WingetUSoft en " +
                $"'{_dataDirectory}': {string.Join(", ", mismatches)}.");
        }
    }

    private void RestoreLogs()
    {
        string logsDirectory = Path.Combine(_dataDirectory, LogsFolder);

        if (_logs is null)
        {
            try
            {
                if (Directory.Exists(logsDirectory))
                    Retry(() => Directory.Delete(logsDirectory, recursive: true));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return;
        }

        Directory.CreateDirectory(logsDirectory);

        // Lo que la app creó durante la suite (el registro del día, si no existía) sobra; lo que purgó o
        // añadió a un registro previo se repone con su contenido original.
        foreach (string path in Directory.EnumerateFiles(logsDirectory))
        {
            if (!_logs.ContainsKey(Path.GetFileName(path)))
                TryRestoreFile(path, original: null);
        }

        foreach (var (name, original) in _logs)
            TryRestoreFile(Path.Combine(logsDirectory, name), original);
    }

    private List<string> FindMismatches()
    {
        var mismatches = new List<string>();

        foreach (var (name, original) in _files)
        {
            if (!SameContent(Path.Combine(_dataDirectory, name), original))
                mismatches.Add(name);
        }

        string logsDirectory = Path.Combine(_dataDirectory, LogsFolder);
        if (_logs is null)
        {
            if (Directory.Exists(logsDirectory))
                mismatches.Add($"{LogsFolder}/ (no existía)");
            return mismatches;
        }

        var current = Directory.Exists(logsDirectory)
            ? Directory.EnumerateFiles(logsDirectory).Select(path => Path.GetFileName(path)).ToHashSet()
            : [];

        foreach (string extra in current.Where(name => !_logs.ContainsKey(name)))
            mismatches.Add($"{LogsFolder}/{extra} (no existía)");

        foreach (var (name, original) in _logs)
        {
            if (!SameContent(Path.Combine(logsDirectory, name), original))
                mismatches.Add($"{LogsFolder}/{name}");
        }

        return mismatches;
    }

    private static bool SameContent(string path, byte[]? original)
    {
        if (original is null)
            return !File.Exists(path);

        // Un archivo que no se puede leer (la app aún lo tiene abierto) cuenta como no restaurado: así el
        // error nombra el archivo en vez de salir como una IOException sin contexto.
        try
        {
            return File.Exists(path)
                && File.ReadAllBytes(path).AsSpan().SequenceEqual(original);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static byte[]? ReadIfExists(string path) => File.Exists(path) ? File.ReadAllBytes(path) : null;

    /// <summary>
    /// Mejor esfuerzo con reintentos: si algo falla —p. ej. la app aún no soltó el archivo—, no se lanza
    /// aquí; lo detecta la verificación de <see cref="Restore"/>, que lo informa con el nombre del archivo.
    /// </summary>
    private static void TryRestoreFile(string path, byte[]? original)
    {
        try
        {
            Retry(() =>
            {
                if (original is null)
                {
                    if (File.Exists(path)) File.Delete(path);
                }
                else
                {
                    File.WriteAllBytes(path, original);
                }
            });
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void Retry(Action action)
    {
        const int attempts = 10;
        for (int i = 1; ; i++)
        {
            try
            {
                action();
                return;
            }
            catch (Exception ex) when (i < attempts && ex is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(200);
            }
        }
    }
}
