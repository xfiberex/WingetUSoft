using Windows.UI;
using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Los colores del registro de actividad son los únicos de la app que no salen de un ThemeResource de
/// Windows: los elige <see cref="LogPalette"/> a mano, y a mano se pueden elegir mal. Hasta v1.5.0 el
/// mismo juego de RGB servía para claro y oscuro, y sobre la tarjeta oscura el verde de los aciertos
/// y el rojo de los fallos quedaban ilegibles.
///
/// Estos tests miden el contraste real contra el fondo de cada tema y exigen el 4.5:1 de WCAG AA, así
/// que si alguien retoca un color y se pasa de claro (u oscuro), el build lo caza aquí.
/// </summary>
public sealed class LogPaletteTests
{
    private const double WcagAaNormalText = 4.5;

    // Los métodos de test son públicos y LogLineKind es internal, así que el enum no puede asomar en
    // sus firmas (CS0051). Se recorre por dentro, que además da un único fallo con TODOS los colores
    // que no llegan al mínimo en vez de uno suelto por caso.
    [Fact]
    public void EveryLogColor_MeetsWcagAaContrast_AgainstItsCardBackground()
    {
        var offenders = new List<string>();

        foreach (LogLineKind kind in Enum.GetValues<LogLineKind>())
        {
            foreach (bool darkTheme in (bool[])[false, true])
            {
                double ratio = LogPalette.ContrastRatio(
                    LogPalette.For(kind, darkTheme), LogPalette.Background(darkTheme));

                if (ratio < WcagAaNormalText)
                    offenders.Add($"{kind} en tema {(darkTheme ? "oscuro" : "claro")}: {ratio:F2}:1");
            }
        }

        Assert.True(offenders.Count == 0,
            $"WCAG AA exige {WcagAaNormalText}:1 para texto normal. No llegan: {string.Join(" | ", offenders)}");
    }

    /// <summary>
    /// El punto de tener una paleta por tema: los tipos con significado NO pueden pintarse igual en
    /// claro que en oscuro. Si alguien "simplifica" volviendo a un solo color por tipo, esto lo caza.
    /// </summary>
    [Fact]
    public void SignificantKinds_UseADifferentColorPerTheme() =>
        Assert.All(
            (LogLineKind[])[LogLineKind.Success, LogLineKind.Error, LogLineKind.Warning],
            kind => Assert.NotEqual(LogPalette.For(kind, darkTheme: false), LogPalette.For(kind, darkTheme: true)));

    /// <summary>Un acierto y un fallo no pueden confundirse en el mismo tema.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SuccessAndError_AreDistinguishable(bool darkTheme) =>
        Assert.NotEqual(LogPalette.For(LogLineKind.Success, darkTheme), LogPalette.For(LogLineKind.Error, darkTheme));

    [Fact]
    public void ContrastRatio_MatchesTheWcagReferenceValues()
    {
        Color black = Color.FromArgb(255, 0, 0, 0);
        Color white = Color.FromArgb(255, 255, 255, 255);

        // Los dos extremos que fija la propia norma: 21:1 negro sobre blanco, 1:1 un color consigo mismo.
        Assert.Equal(21.0, LogPalette.ContrastRatio(black, white), precision: 2);
        Assert.Equal(1.0, LogPalette.ContrastRatio(white, white), precision: 2);
    }
}
