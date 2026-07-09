using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Pruebas de la lógica de rendimiento: estimación de ETA y formateo del tiempo restante.
/// </summary>
public sealed class ThroughputTests
{
    // ── Eta ──────────────────────────────────────────────────────

    [Fact]
    public void Eta_NormalCase_ReturnsRemainingOverSpeed()
        => Assert.Equal(TimeSpan.FromSeconds(10), Throughput.Eta(1000, 100));

    [Fact]
    public void Eta_ZeroSpeed_ReturnsNull()
        => Assert.Null(Throughput.Eta(1000, 0));

    [Fact]
    public void Eta_NegativeRemaining_ReturnsNull()
        => Assert.Null(Throughput.Eta(-1, 100));

    // ── FormatEta ────────────────────────────────────────────────

    [Fact]
    public void FormatEta_Null_ReturnsEmpty()
        => Assert.Equal("", Throughput.FormatEta(null));

    [Fact]
    public void FormatEta_UnderOneHour_FormatsMinutesSeconds()
        => Assert.Equal("01:05", Throughput.FormatEta(TimeSpan.FromSeconds(65)));

    [Fact]
    public void FormatEta_OverOneHour_IncludesHours()
        => Assert.Equal("1:01:01", Throughput.FormatEta(TimeSpan.FromSeconds(3661)));
}
