using System.Text.RegularExpressions;

namespace WingetUSoft.UiTests;

/// <summary>
/// El respaldo que protege los datos reales del usuario durante los UI tests (F-25).
/// </summary>
/// <remarks>
/// Sin <c>[Collection(AppCollection.Name)]</c> a propósito: estas pruebas trabajan sobre un directorio
/// temporal y no lanzan la app, así que corren sin escritorio interactivo.
/// </remarks>
public sealed class SettingsBackupTests : IDisposable
{
    private readonly string _dataDirectory =
        Path.Combine(Path.GetTempPath(), "WingetUSoft.UiTests", Guid.NewGuid().ToString("N"));

    public SettingsBackupTests() => Directory.CreateDirectory(_dataDirectory);

    public void Dispose()
    {
        if (Directory.Exists(_dataDirectory))
            Directory.Delete(_dataDirectory, recursive: true);
    }

    private string DataPath(params string[] parts) => Path.Combine([_dataDirectory, .. parts]);

    [Fact]
    public void Restore_LeavesTheDataDirectoryByteIdentical()
    {
        File.WriteAllText(DataPath("settings.json"), """{ "Language": "es", "History": [] }""");
        File.WriteAllText(DataPath("settings.json.bak"), """{ "Language": "es" }""");
        Directory.CreateDirectory(DataPath("logs"));
        File.WriteAllText(DataPath("logs", "2026-09-01.log"), "línea original\n");
        byte[] settings = File.ReadAllBytes(DataPath("settings.json"));
        byte[] bak = File.ReadAllBytes(DataPath("settings.json.bak"));
        byte[] log = File.ReadAllBytes(DataPath("logs", "2026-09-01.log"));

        var backup = SettingsBackup.Capture(_dataDirectory);

        // Lo que hace una suite de UI tests: guardar ajustes (que rota el .bak), escribir en el registro
        // de un día anterior y crear el del día.
        File.WriteAllText(DataPath("settings.json"), """{ "Language": "en" }""");
        File.Delete(DataPath("settings.json.bak"));
        File.AppendAllText(DataPath("logs", "2026-09-01.log"), "línea de la suite\n");
        File.WriteAllText(DataPath("logs", "2026-09-16.log"), "registro nuevo\n");

        backup.Restore();

        Assert.Equal(settings, File.ReadAllBytes(DataPath("settings.json")));
        Assert.Equal(bak, File.ReadAllBytes(DataPath("settings.json.bak")));
        Assert.Equal(log, File.ReadAllBytes(DataPath("logs", "2026-09-01.log")));
        Assert.False(File.Exists(DataPath("logs", "2026-09-16.log")));
    }

    [Fact]
    public void Restore_DeletesWhatDidNotExistAtCapture()
    {
        File.WriteAllText(DataPath("settings.json"), "{}");

        var backup = SettingsBackup.Capture(_dataDirectory);

        File.WriteAllText(DataPath("settings.json.bak"), "{}");
        Directory.CreateDirectory(DataPath("logs"));
        File.WriteAllText(DataPath("logs", "2026-09-16.log"), "registro nuevo\n");

        backup.Restore();

        Assert.False(File.Exists(DataPath("settings.json.bak")));
        Assert.False(Directory.Exists(DataPath("logs")));
    }

    /// <summary>
    /// El fallo que motivó F-25 fue un respaldo que no restauraba nada y no se quejaba. Si un archivo no
    /// se puede reponer, la suite tiene que enterarse.
    /// </summary>
    [Fact]
    public void Restore_Throws_WhenAFileCannotBeRestored()
    {
        File.WriteAllText(DataPath("settings.json"), """{ "Language": "es" }""");
        var backup = SettingsBackup.Capture(_dataDirectory);
        File.WriteAllText(DataPath("settings.json"), """{ "Language": "en" }""");

        using (new FileStream(DataPath("settings.json"), FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var ex = Assert.Throws<InvalidOperationException>(backup.Restore);
            Assert.Contains("settings.json", ex.Message);
        }
    }

    /// <summary>
    /// Este proyecto no referencia el ensamblado de la app, así que la ruta de datos está repetida en
    /// <see cref="SettingsBackup"/>. Si la app la cambia, este test obliga a cambiar también el respaldo:
    /// es exactamente la divergencia (Roaming frente a Local) que dejó sin proteger los datos reales.
    /// </summary>
    [Fact]
    public void DefaultDataDirectory_IsTheOneTheAppUses()
    {
        string repoRoot = AppFixture.FindRepoRoot(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("No se encontró la raíz del repositorio.");
        string source = File.ReadAllText(Path.Combine(repoRoot, "src", "WingetUSoft", "Settings", "AppSettings.cs"));

        var match = Regex.Match(
            source,
            @"DefaultDataDirectoryPath\s*\{\s*get;\s*\}\s*=\s*Path\.Combine\(\s*" +
            @"Environment\.GetFolderPath\(\s*Environment\.SpecialFolder\.(?<folder>\w+)\s*\)\s*,\s*""(?<name>[^""]+)""\s*\)");
        Assert.True(match.Success, "No se reconoce la declaración de AppSettings.DefaultDataDirectoryPath.");

        string appDirectory = Path.Combine(
            Environment.GetFolderPath(Enum.Parse<Environment.SpecialFolder>(match.Groups["folder"].Value)),
            match.Groups["name"].Value);

        Assert.Equal(appDirectory, SettingsBackup.DefaultDataDirectory);
    }
}
