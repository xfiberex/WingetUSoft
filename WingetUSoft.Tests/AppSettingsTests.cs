using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WingetUSoft;

namespace WingetUSoft.Tests;

[TestClass]
public class AppSettingsTests
{
    private string _testDataDirectory = null!;

    [TestInitialize]
    public void Initialize()
    {
        _testDataDirectory = Path.Combine(Path.GetTempPath(), "WingetUSoft.Tests", Guid.NewGuid().ToString("N"));
        AppSettings.DataDirectoryPath = _testDataDirectory;
    }

    [TestCleanup]
    public void Cleanup()
    {
        AppSettings.DataDirectoryPath = AppSettings.DefaultDataDirectoryPath;

        if (File.Exists(_testDataDirectory))
            File.Delete(_testDataDirectory);

        if (Directory.Exists(_testDataDirectory))
            Directory.Delete(_testDataDirectory, recursive: true);
    }

    [TestMethod]
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

        Assert.IsTrue(settings.Save());

        var loaded = AppSettings.Load();

        Assert.IsFalse(loaded.SilentMode);
        Assert.IsTrue(loaded.RunUpdatesAsAdministrator);
        Assert.AreEqual(60, loaded.AutoCheckIntervalMinutes);
        Assert.IsTrue(loaded.DarkMode);
        Assert.IsFalse(loaded.LogToFile);
        CollectionAssert.AreEqual(new[] { "VideoLAN.VLC" }, loaded.ExcludedIds);
        Assert.AreEqual(1, loaded.History.Count);
        Assert.AreEqual("VLC", loaded.History[0].PackageName);
        Assert.IsTrue(string.IsNullOrWhiteSpace(loaded.LastLoadError));
    }

    [TestMethod]
    public void Load_InvalidJson_ReturnsDefaultsAndCreatesBackup()
    {
        Directory.CreateDirectory(AppSettings.DataDirectoryPath);
        File.WriteAllText(AppSettings.SettingsFilePath, "{ invalid json", Encoding.UTF8);

        var loaded = AppSettings.Load();

        Assert.IsTrue(loaded.SilentMode);
        Assert.AreEqual(0, loaded.ExcludedIds.Count);
        Assert.IsFalse(string.IsNullOrWhiteSpace(loaded.LastLoadError));
        Assert.AreEqual(1, Directory.GetFiles(AppSettings.DataDirectoryPath, "settings.invalid.*.json").Length);
    }

    [TestMethod]
    public void Save_WhenDataDirectoryIsAFile_ReturnsFalseAndExposesError()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_testDataDirectory)!);
        File.WriteAllText(_testDataDirectory, "occupied", Encoding.UTF8);

        var settings = new AppSettings();
        bool saved = settings.Save();

        Assert.IsFalse(saved);
        Assert.IsFalse(string.IsNullOrWhiteSpace(settings.LastSaveError));
    }
}