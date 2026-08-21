using System.Text;
using Xunit;

namespace WingetUSoft.Tests;

public class AppSettingsTests : IDisposable
{
    private readonly string _testDataDirectory;

    public AppSettingsTests()
    {
        _testDataDirectory = Path.Combine(Path.GetTempPath(), "WingetUSoft.Tests", Guid.NewGuid().ToString("N"));
        AppSettings.DataDirectoryPath = _testDataDirectory;
    }

    public void Dispose()
    {
        AppSettings.DataDirectoryPath = AppSettings.DefaultDataDirectoryPath;

        if (File.Exists(_testDataDirectory))
            File.Delete(_testDataDirectory);

        if (Directory.Exists(_testDataDirectory))
            Directory.Delete(_testDataDirectory, recursive: true);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsSettings()
    {
        var settings = new AppSettings
        {
            SilentMode = false,
            RunUpdatesAsAdministrator = true,
            AutoCheckIntervalMinutes = 60,
            DarkMode = true,
            LogToFile = false,
            ExcludedIds = ["VideoLAN.VLC"]
        };
        settings.AddHistory(new HistoryEntry
        {
            Date = new DateTime(2026, 3, 28, 10, 30, 0),
            PackageName = "VLC",
            PackageId = "VideoLAN.VLC",
            FromVersion = "3.0.20",
            ToVersion = "3.0.21",
            Success = true
        });

        Assert.True(settings.Save());

        var loaded = AppSettings.Load();

        Assert.False(loaded.SilentMode);
        Assert.True(loaded.RunUpdatesAsAdministrator);
        Assert.Equal(60, loaded.AutoCheckIntervalMinutes);
        Assert.True(loaded.DarkMode);
        Assert.False(loaded.LogToFile);
        Assert.Equal(new[] { "VideoLAN.VLC" }, loaded.ExcludedIds);
        Assert.Single(loaded.History);
        Assert.Equal("VLC", loaded.History[0].PackageName);
        Assert.Null(loaded.LastLoadError);
    }

    [Fact]
    public void Load_InvalidJson_ReturnsDefaultsAndCreatesBackup()
    {
        Directory.CreateDirectory(AppSettings.DataDirectoryPath);
        File.WriteAllText(AppSettings.SettingsFilePath, "{ invalid json", Encoding.UTF8);

        var loaded = AppSettings.Load();

        Assert.True(loaded.SilentMode);
        Assert.Empty(loaded.ExcludedIds);
        Assert.NotNull(loaded.LastLoadError);
        Assert.False(string.IsNullOrWhiteSpace(loaded.LastLoadError.Text));
        Assert.Single(Directory.GetFiles(AppSettings.DataDirectoryPath, "settings.invalid.*.json"));
    }

    [Fact]
    public void Save_WhenDataDirectoryIsAFile_ReturnsFalseAndExposesError()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_testDataDirectory)!);
        File.WriteAllText(_testDataDirectory, "occupied", Encoding.UTF8);

        var settings = new AppSettings();
        bool saved = settings.Save();

        Assert.False(saved);
        Assert.NotNull(settings.LastSaveError);
        Assert.False(string.IsNullOrWhiteSpace(settings.LastSaveError.Text));
    }

    // ── Escritura atómica (T0-02) ───────────────────────────────────────────
    //
    // settings.json guarda el historial (hasta 500 entradas), las exclusiones, las versiones omitidas,
    // el idioma y el tema. Un JSON truncado hace que Load() restaure los valores por defecto, así que
    // una escritura no atómica es pérdida de datos, no una molestia.

    [Fact]
    public void Save_Successful_LeavesNoTemporaryFile()
    {
        var settings = new AppSettings { AutoCheckIntervalMinutes = 30 };

        Assert.True(settings.Save());

        Assert.Empty(Directory.GetFiles(AppSettings.DataDirectoryPath, "*.tmp"));
        Assert.True(File.Exists(AppSettings.SettingsFilePath));
    }

    [Fact]
    public void Save_OverAnExistingFile_KeepsThePreviousVersionAsBackup()
    {
        var first = new AppSettings { AutoCheckIntervalMinutes = 30 };
        Assert.True(first.Save());

        var second = new AppSettings { AutoCheckIntervalMinutes = 120 };
        Assert.True(second.Save());

        Assert.Equal(120, AppSettings.Load().AutoCheckIntervalMinutes);

        string backup = AppSettings.SettingsFilePath + ".bak";
        Assert.True(File.Exists(backup), "File.Replace debe dejar copia del archivo anterior.");
        Assert.Contains("\"AutoCheckIntervalMinutes\": 30", File.ReadAllText(backup));
    }

    /// <summary>
    /// El escenario que motiva la tarea: el guardado falla a mitad. Antes esto dejaba un JSON truncado;
    /// ahora el archivo definitivo ni se toca hasta que el temporal está completo.
    /// </summary>
    [Fact]
    public void Save_WhenTheTargetIsLocked_LeavesThePreviousSettingsIntact()
    {
        var original = new AppSettings { AutoCheckIntervalMinutes = 60, ExcludedIds = ["VideoLAN.VLC"] };
        Assert.True(original.Save());
        string before = File.ReadAllText(AppSettings.SettingsFilePath);

        // Un tercero mantiene el archivo abierto en exclusiva: la sustitución no puede completarse.
        using (File.Open(AppSettings.SettingsFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var updated = new AppSettings { AutoCheckIntervalMinutes = 120 };

            Assert.False(updated.Save());
            Assert.NotNull(updated.LastSaveError);
            Assert.False(string.IsNullOrWhiteSpace(updated.LastSaveError.Text));
        }

        Assert.Equal(before, File.ReadAllText(AppSettings.SettingsFilePath));
        Assert.Equal(60, AppSettings.Load().AutoCheckIntervalMinutes);
        Assert.Empty(Directory.GetFiles(AppSettings.DataDirectoryPath, "*.tmp"));
    }
}
