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
    /// Con Mica (F-11) la tarjeta del registro ya no es un color fijo: su fondo translúcido deja pasar el tinte
    /// del fondo de escritorio. La paleta tiene que aguantar esa variación, no solo el valor nominal.
    /// </summary>
    /// <remarks>
    /// Medido en pantalla el 2026-09-17 con Mica activo, la tarjeta se movió entre <c>#FBFBFB</c> y
    /// <c>#FDFBFB</c> en claro y entre <c>#2B2B2B</c> y <c>#2D2A2A</c> en oscuro, y el margen de página, hasta
    /// <c>#F9F0F0</c> / <c>#231F1F</c>. Se exige el 4,5:1 contra un fondo varios niveles más desfavorable que lo
    /// medido. El color que menos margen tiene es el aviso en claro, que deja de cumplir por debajo de
    /// <c>#EDEDED</c>.
    /// </remarks>
    [Theory]
    [InlineData(false, 0xF4)]
    [InlineData(true, 0x33)]
    public void EveryLogColor_KeepsWcagAa_WhenMicaTintsTheCard(bool darkTheme, byte worstCardGray)
    {
        Color worstCard = Color.FromArgb(255, worstCardGray, worstCardGray, worstCardGray);

        var offenders = Enum.GetValues<LogLineKind>()
            .Select(kind => (kind, ratio: LogPalette.ContrastRatio(LogPalette.For(kind, darkTheme), worstCard)))
            .Where(x => x.ratio < WcagAaNormalText)
            .Select(x => $"{x.kind}: {x.ratio:F2}:1")
            .ToList();

        Assert.True(offenders.Count == 0,
            $"Sobre una tarjeta #{worstCardGray:X2}{worstCardGray:X2}{worstCardGray:X2} no llegan al {WcagAaNormalText}:1: "
                + string.Join(" | ", offenders));
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

    /// <summary>
    /// Los tests de arriba miden <see cref="LogPalette"/>… pero solo protegen a quien la use.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Hasta la auditoría del 2026-08-20 esa distinción no era teórica: <c>CleanupWindow</c> y
    /// <c>UninstallWindow</c> **no** usaban <c>LogPalette</c>. Conservaban los RGB cableados que el
    /// Tier C #4 había retirado de <c>MainWindow</c> (#387A4D verde, #BA4636 rojo), que sobre la
    /// tarjeta oscura miden 2,74:1 y 2,71:1 — por debajo del 4,5:1 de WCAG AA. Todos los tests de
    /// contraste pasaban en verde mientras dos de las cuatro ventanas incumplían, y el README llegó a
    /// afirmar que el contraste estaba «comprobado por tests».
    /// </para>
    /// <para>
    /// Por eso este test no mide colores: lee el código fuente y comprueba **quién** decide el color.
    /// </para>
    /// <para>
    /// <b>Desde T2-05 el alcance cambió, no se relajó.</b> Antes exigía que las cuatro ventanas con
    /// registro usaran <c>LogPalette</c>; ahora el registro es un único control compartido
    /// (<c>ActivityLog</c>), así que lo que se exige es que **siga siendo uno solo** y que ningún
    /// archivo de UI vuelva a pintar líneas de registro por su cuenta. Si alguien reintroduce un
    /// registro por ventana con colores cableados, este test lo ve igual que antes.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheActivityLog_IsTheOnlyPlaceThatColorsLogLines()
    {
        string uiDirectory = Path.Combine(FindRepositoryRoot(), "src", "WingetUSoft", "UI");
        Assert.True(Directory.Exists(uiDirectory), "No existe el directorio de UI: " + uiDirectory);

        var offenders = new List<string>();
        var logRenderers = new List<string>();

        foreach (string file in Directory.EnumerateFiles(uiDirectory, "*.cs", SearchOption.AllDirectories))
        {
            string code = File.ReadAllText(file);
            string name = Path.GetFileName(file);

            // Un archivo "pinta el registro" si toca el RichTextBlock del log o resuelve colores por tipo
            // de línea. Cualquiera de las dos cosas basta para que le apliquen las reglas de abajo.
            bool rendersLog = code.Contains("rtbLog", StringComparison.Ordinal)
                           || code.Contains("LogPalette.For", StringComparison.Ordinal);
            if (!rendersLog) continue;

            logRenderers.Add(name);

            if (code.Contains("Color.FromArgb", StringComparison.Ordinal))
                offenders.Add($"{name}: cablea un color con Color.FromArgb en vez de usar LogPalette");

            // Un recurso de nivel de aplicación NO sigue el RequestedTheme forzado por elemento, así que
            // con "Claro" sobre un Windows oscuro devolvía el color del tema contrario.
            if (code.Contains("Application.Current.Resources", StringComparison.Ordinal))
                offenders.Add($"{name}: lee un pincel de Application.Current.Resources (ignora el tema por elemento)");

            if (!code.Contains("LogPalette.", StringComparison.Ordinal))
                offenders.Add($"{name}: pinta el registro pero no usa LogPalette");
        }

        Assert.True(offenders.Count == 0,
            "Los colores del registro deben salir de LogPalette (es lo único que los tests de contraste "
                + "cubren):\n  " + string.Join("\n  ", offenders));

        // La duplicación era la causa mecánica del bug original: cuatro copias, y la corrección se
        // aplicó a una. Que haya exactamente una es lo que impide que vuelva a pasar.
        Assert.True(
            logRenderers.Count == 1 && logRenderers[0] == "ActivityLog.xaml.cs",
            "El registro debe pintarse en un único sitio (ActivityLog.xaml.cs). Encontrado en: "
                + string.Join(", ", logRenderers));
    }

    /// <summary>
    /// Sube hasta la raíz del repositorio (la que tiene <c>WingetUSoft.slnx</c>). Falla con un mensaje
    /// claro si no la encuentra: un test que "pasa" porque no pudo leer el código no probaría nada.
    /// </summary>
    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WingetUSoft.slnx")))
            dir = dir.Parent;

        Assert.True(dir is not null,
            "No se encontró la raíz del repositorio (WingetUSoft.slnx) desde " + AppContext.BaseDirectory);
        return dir!.FullName;
    }

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
