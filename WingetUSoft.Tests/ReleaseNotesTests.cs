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
}
