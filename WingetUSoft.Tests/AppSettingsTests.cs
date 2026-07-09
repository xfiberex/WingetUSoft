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
        Assert.True(string.IsNullOrWhiteSpace(loaded.LastLoadError));
    }

    [Fact]
    public void Load_InvalidJson_ReturnsDefaultsAndCreatesBackup()
    {
        Directory.CreateDirectory(AppSettings.DataDirectoryPath);
        File.WriteAllText(AppSettings.SettingsFilePath, "{ invalid json", Encoding.UTF8);

        var loaded = AppSettings.Load();

        Assert.True(loaded.SilentMode);
        Assert.Empty(loaded.ExcludedIds);
        Assert.False(string.IsNullOrWhiteSpace(loaded.LastLoadError));
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
        Assert.False(string.IsNullOrWhiteSpace(settings.LastSaveError));
    }
}
