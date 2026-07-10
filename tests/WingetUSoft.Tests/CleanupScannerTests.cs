using Xunit;

namespace WingetUSoft.Tests;

public class CleanupScannerTests
{
    // Uses a name suffix unlikely to collide with real installed software.
    private const string TestSuffix = "_WUSoftScanTest";

    [Fact]
    public async Task ScanAsync_EmptyPackageList_ReturnsEmpty()
    {
        var results = await CleanupScanner.ScanAsync([]);
        Assert.Empty(results);
    }

    [Fact]
    public async Task ScanAsync_CancellationRequested_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CleanupScanner.ScanAsync([new WingetPackage { Name = "Foo", Id = "Pub.Foo" }], cts.Token));
    }

    [Fact]
    public async Task ScanAsync_ExistingDirectoryMatchingPackageName_IsFound()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dirName = $"TestApp{TestSuffix}";
        string testDir = Path.Combine(localAppData, dirName);
        Directory.CreateDirectory(testDir);

        try
        {
            var packages = new[]
            {
                new WingetPackage { Name = dirName, Id = $"Publisher.{dirName}" }
            };

            var results = await CleanupScanner.ScanAsync(packages);

            Assert.True(
                results.Any(r => string.Equals(r.Path, testDir, StringComparison.OrdinalIgnoreCase)),
                $"Expected to find '{testDir}' in scan results.");
        }
        finally
        {
            Directory.Delete(testDir, recursive: false);
        }
    }

    [Fact]
    public async Task ScanAsync_NonExistentPaths_ReturnsEmpty()
    {
        var packages = new[]
        {
            new WingetPackage
            {
                Name = $"NonExistentApp{TestSuffix}",
                Id   = $"Nobody.NonExistentApp{TestSuffix}"
            }
        };

        var results = await CleanupScanner.ScanAsync(packages);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ScanAsync_SamePathFromMultiplePackages_ReportedOnce()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dirName = $"SharedApp{TestSuffix}";
        string testDir = Path.Combine(localAppData, dirName);
        Directory.CreateDirectory(testDir);

        try
        {
            // Two packages whose name/id both resolve to the same candidate path.
            var packages = new[]
            {
                new WingetPackage { Name = dirName, Id = $"Publisher.{dirName}" },
                new WingetPackage { Name = dirName, Id = $"OtherPub.{dirName}" }
            };

            var results = await CleanupScanner.ScanAsync(packages);

            int matches = results.Count(r =>
                string.Equals(r.Path, testDir, StringComparison.OrdinalIgnoreCase));

            Assert.True(matches == 1, "The same path should only appear once even if multiple packages resolve to it.");
        }
        finally
        {
            Directory.Delete(testDir, recursive: false);
        }
    }

    [Fact]
    public async Task ScanAsync_FoundItem_IsNotSelectedByDefault()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dirName = $"DefaultSelTest{TestSuffix}";
        string testDir = Path.Combine(localAppData, dirName);
        Directory.CreateDirectory(testDir);

        try
        {
            var packages = new[] { new WingetPackage { Name = dirName, Id = $"Pub.{dirName}" } };
            var results = await CleanupScanner.ScanAsync(packages);

            var item = results.FirstOrDefault(r =>
                string.Equals(r.Path, testDir, StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(item);
            Assert.False(item.IsSelected, "Cleanup items must NOT be pre-selected to avoid accidental deletion.");
        }
        finally
        {
            Directory.Delete(testDir, recursive: false);
        }
    }
}
