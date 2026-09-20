using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// El botón «Apoyar el proyecto» de Acerca de.
/// </summary>
/// <remarks>
/// Apoyar es voluntario: el enlace vive en Acerca de, junto al repositorio, y no aparece en ningún flujo de
/// la aplicación. El test fija lo único que se puede romper sin que nadie lo note —que el botón siga estando,
/// que su texto esté traducido y que el enlace sea HTTPS— porque pulsarlo abre el navegador y eso no se
/// puede comprobar en un test.
/// </remarks>
public sealed class SupportLinkTests
{
    private static string AboutDialog => File.ReadAllText(
        Path.Combine(RepoRoot(), "src", "WingetUSoft", "UI", "AboutDialog.xaml.cs"));

    [Fact]
    public void AboutDialog_OffersTheSupportButton()
    {
        string code = AboutDialog;

        Assert.Contains("PrimaryButtonText = L.T(\"about.donate\")", code, StringComparison.Ordinal);
        Assert.Contains("SecondaryButtonText = L.T(\"about.github\")", code, StringComparison.Ordinal);
        // El acento y el Intro siguen en Cerrar: apoyar no es la acción principal del diálogo (F-12).
        Assert.Contains("DefaultButton       = ContentDialogButton.Close;", code, StringComparison.Ordinal);
    }

    [Fact]
    public void TheDonationLink_IsHttps()
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            AboutDialog, @"DonateUrl\s*=\s*""(?<url>[^""]*)""");

        Assert.True(match.Success, "No se encontró DonateUrl en AboutDialog.xaml.cs.");
        string url = match.Groups["url"].Value;
        Assert.StartsWith("https://", url, StringComparison.Ordinal);
    }

    /// <summary>Traducido a los cinco idiomas, como cualquier otro texto visible.</summary>
    [Theory]
    [InlineData(AppLang.Es, "Apoyar el proyecto")]
    [InlineData(AppLang.En, "Support the project")]
    [InlineData(AppLang.Pt, "Apoiar o projeto")]
    [InlineData(AppLang.Fr, "Soutenir le projet")]
    [InlineData(AppLang.It, "Sostieni il progetto")]
    public void TheSupportLabel_IsTranslated(AppLang language, string expected)
        => Assert.Equal(expected, L.Map["about.donate"][(int)language]);

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WingetUSoft.slnx")))
            dir = dir.Parent;

        Assert.True(dir is not null, "No se encontró la raíz del repositorio (WingetUSoft.slnx).");
        return dir!.FullName;
    }
}
