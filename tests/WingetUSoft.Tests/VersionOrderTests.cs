using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Verifica <see cref="VersionOrder"/>, que existe porque ordenar versiones como texto daba
/// resultados falsos: "1.10.0" quedaba antes que "1.9.0" al comparar carácter a carácter.
/// </summary>
public sealed class VersionOrderTests
{
    [Theory]
    // El caso que motivó el comparador: numéricamente 10 > 9, aunque como texto '1' < '9'.
    [InlineData("1.9.0", "1.10.0")]
    [InlineData("2.9", "2.10")]
    [InlineData("1.2.3", "1.2.10")]
    // Distinto número de segmentos: el más corto es el menor.
    [InlineData("1.2", "1.2.1")]
    // Casos reales vistos en la lista de actualizaciones.
    [InlineData("2.54.0", "2.55.0.2")]
    [InlineData("13.735.2.6250", "13.743.0.6256")]
    [InlineData("21.0.7.6", "21.0.11.10")]
    public void Compare_OrdersEarlierVersionFirst(string earlier, string later)
    {
        Assert.True(VersionOrder.Compare(earlier, later) < 0, $"'{earlier}' debería ir antes que '{later}'");
        Assert.True(VersionOrder.Compare(later, earlier) > 0, $"'{later}' debería ir después que '{earlier}'");
    }

    [Theory]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("", "")]
    [InlineData("Unknown", "unknown")]
    public void Compare_EquivalentVersions_ReturnsZero(string a, string b)
        => Assert.Equal(0, VersionOrder.Compare(a, b));

    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData(null)]
    public void Compare_UnknownSortsLast(string? unknown)
    {
        Assert.True(VersionOrder.Compare(unknown, "1.0.0") > 0);
        Assert.True(VersionOrder.Compare("1.0.0", unknown) < 0);
    }

    [Fact]
    public void Compare_IgnoresLessThanPrefixWhenComparingNumbers()
    {
        // winget escribe "< 13.5.0.359" cuando solo conoce una cota superior: el número sigue mandando.
        Assert.True(VersionOrder.Compare("< 13.5.0.359", "13.6.0") < 0);
        Assert.True(VersionOrder.Compare("< 13.5.0.359", "13.4.0") > 0);
    }

    [Fact]
    public void Compare_BoundedVersionPrecedesExactSameNumber()
    {
        // Con el mismo número, la cota "< 1.2.3" no es la versión exacta 1.2.3: va antes.
        Assert.True(VersionOrder.Compare("< 1.2.3", "1.2.3") < 0);
        Assert.True(VersionOrder.Compare("1.2.3", "< 1.2.3") > 0);
    }

    [Fact]
    public void Compare_PrereleasePrecedesFinal()
    {
        Assert.True(VersionOrder.Compare("1.2.3-beta", "1.2.3") < 0);
    }

    [Fact]
    public void Comparer_SortsAscendingWithUnknownLast()
    {
        string[] versions = ["1.10.0", "Unknown", "1.9.0", "1.2", "1.2.1"];

        var sorted = versions.OrderBy(v => v, VersionOrder.Comparer).ToArray();

        Assert.Equal(["1.2", "1.2.1", "1.9.0", "1.10.0", "Unknown"], sorted);
    }
}
