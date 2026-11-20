using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Pruebas de la conversión de notas Markdown a texto plano para el diálogo de novedades (lógica pura).
/// </summary>
public sealed class ReleaseNotesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n  \n")]
    public void ToPlainText_NullOrBlank_IsEmpty(string? input)
        => Assert.Equal("", ReleaseNotes.ToPlainText(input));

    [Fact]
    public void ToPlainText_StripsHeadingMarkers()
        => Assert.Equal("Novedades", ReleaseNotes.ToPlainText("## Novedades"));

    [Fact]
    public void ToPlainText_ConvertsBulletsToDots()
    {
        string result = ReleaseNotes.ToPlainText("- uno\n* dos\n+ tres");
        Assert.Equal("• uno\n• dos\n• tres", result);
    }

    [Fact]
    public void ToPlainText_RemovesBoldAndCodeMarkers()
        => Assert.Equal("texto importante y codigo",
            ReleaseNotes.ToPlainText("**texto** importante y `codigo`"));

    [Fact]
    public void ToPlainText_ReducesLinksToTheirText()
        => Assert.Equal("ver el repo",
            ReleaseNotes.ToPlainText("ver el [repo](https://github.com/xfiberex/WingetUSoft)"));

    [Fact]
    public void ToPlainText_CollapsesExtraBlankLines()
        => Assert.Equal("a\n\nb", ReleaseNotes.ToPlainText("a\n\n\n\nb"));

    /// <summary>
    /// Un párrafo partido en varias líneas se vuelve a unir. Hasta la crítica de diseño del 2026-11-20 cada salto
    /// llegaba al diálogo y las frases se cortaban a la mitad («fallaba / con la configuración…»).
    /// </summary>
    [Fact]
    public void ToPlainText_RejoinsWrappedParagraphs()
        => Assert.Equal("Desde la 1.8.8, actualizar fallaba con la configuración de fábrica.\n\nTus preferencias se conservan.",
            ReleaseNotes.ToPlainText("Desde la 1.8.8, actualizar fallaba\ncon la configuración de fábrica.\n\nTus preferencias se conservan."));

    /// <summary>Las líneas sangradas que continúan una viñeta se unen a ella, sin sangría suelta.</summary>
    [Fact]
    public void ToPlainText_RejoinsBulletContinuationLines()
        => Assert.Equal("• Los lotes vuelven a terminar. Con uno, winget lo actualizaba.\n• Ningún error llega en blanco.",
            ReleaseNotes.ToPlainText("- **Los lotes vuelven a terminar.** Con uno,\n  winget lo actualizaba.\n- Ningún error llega en blanco."));

    /// <summary>Un encabezado corta el párrafo anterior aunque no haya línea en blanco entre ambos.</summary>
    [Fact]
    public void ToPlainText_HeadingStartsANewBlock()
        => Assert.Equal("texto\nCorregido\n• uno", ReleaseNotes.ToPlainText("texto\n### Corregido\n- uno"));

    /// <summary>El primer encabezado se omite si repite la versión que ya rotula el diálogo.</summary>
    [Fact]
    public void ToPlainText_DropsLeadingHeadingThatRepeatsTheVersion()
        => Assert.Equal("Actualización recomendada.",
            ReleaseNotes.ToPlainText("## WingetUSoft v1.11.0\n\nActualización recomendada.", "1.11.0"));

    /// <summary>
    /// Sin versión, o con un encabezado que no la repite, el encabezado se conserva, pegado a lo que rotula: la
    /// línea en blanco que lo sigue se omite.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("1.12.0")]
    public void ToPlainText_KeepsLeadingHeadingOtherwise(string? version)
        => Assert.Equal("WingetUSoft v1.11.0\nTexto.",
            ReleaseNotes.ToPlainText("## WingetUSoft v1.11.0\n\nTexto.", version));

    /// <summary>La cursiva de un solo asterisco se quita sin tocar las viñetas que empiezan por «* ».</summary>
    [Fact]
    public void ToPlainText_RemovesItalicMarkers_ButKeepsStarBullets()
        => Assert.Equal("• Ayuda → Acerca de estrena un botón.",
            ReleaseNotes.ToPlainText("* *Ayuda → Acerca de* estrena un botón."));

    /// <summary>Los encabezados salen marcados como tales, para que el diálogo los resalte.</summary>
    [Fact]
    public void ToBlocks_MarksHeadings()
    {
        var blocks = ReleaseNotes.ToBlocks("Intro.\n\n### Corregido\n\n- uno");

        Assert.Equal(
            [new ReleaseNoteBlock("Intro.", false), new ReleaseNoteBlock("", false),
             new ReleaseNoteBlock("Corregido", true), new ReleaseNoteBlock("• uno", false)],
            blocks);
    }
}
