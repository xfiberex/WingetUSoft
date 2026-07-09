using Xunit;

namespace WingetUSoft.Tests;

public sealed class HistoryFilterTests
{
    private static List<HistoryEntry> SampleEntries() =>
    [
        new HistoryEntry { PackageName = "VLC", PackageId = "VideoLAN.VLC", Success = true },
        new HistoryEntry { PackageName = "7-Zip", PackageId = "7zip.7zip", Success = false },
        new HistoryEntry { PackageName = "Mozilla Firefox", PackageId = "Mozilla.Firefox", Success = true },
    ];

    [Fact]
    public void Apply_NoFilters_ReturnsAllEntries()
    {
        var result = HistoryFilter.Apply(SampleEntries(), null, HistoryStatusFilter.All);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Apply_StatusSuccess_ReturnsOnlySuccessful()
    {
        var result = HistoryFilter.Apply(SampleEntries(), null, HistoryStatusFilter.Success);
        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.True(e.Success));
    }

    [Fact]
    public void Apply_StatusFailed_ReturnsOnlyFailed()
    {
        var result = HistoryFilter.Apply(SampleEntries(), null, HistoryStatusFilter.Failed);
        Assert.Single(result);
        Assert.Equal("7-Zip", result[0].PackageName);
    }

    [Fact]
    public void Apply_SearchByName_IsCaseInsensitive()
    {
        var result = HistoryFilter.Apply(SampleEntries(), "vlc", HistoryStatusFilter.All);
        Assert.Single(result);
        Assert.Equal("VideoLAN.VLC", result[0].PackageId);
    }

    [Fact]
    public void Apply_SearchById_MatchesPartial()
    {
        var result = HistoryFilter.Apply(SampleEntries(), "Mozilla", HistoryStatusFilter.All);
        Assert.Single(result);
        Assert.Equal("Mozilla Firefox", result[0].PackageName);
    }

    [Fact]
    public void Apply_SearchAndStatusCombined_IntersectsBoth()
    {
        var result = HistoryFilter.Apply(SampleEntries(), "7-Zip", HistoryStatusFilter.Success);
        Assert.Empty(result); // 7-Zip failed, so combined with Success filter yields nothing
    }

    [Fact]
    public void Apply_BlankSearch_IsIgnored()
    {
        var result = HistoryFilter.Apply(SampleEntries(), "   ", HistoryStatusFilter.All);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Apply_NoMatches_ReturnsEmpty()
    {
        var result = HistoryFilter.Apply(SampleEntries(), "NonExistentPackage", HistoryStatusFilter.All);
        Assert.Empty(result);
    }
}
