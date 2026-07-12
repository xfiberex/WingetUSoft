using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Verifica que <c>ParsePackageInfo</c> entiende la salida de <c>winget show</c> en cualquier idioma.
/// winget rotula sus campos en el idioma de Windows y no admite forzar el inglés, así que un parser
/// que solo buscara "Description:"/"Homepage:" dejaba el panel de detalle vacío fuera del inglés.
/// Las muestras son salida real de winget v1.29.280.
/// </summary>
public sealed class ParsePackageInfoTests
{
    private const string SpanishOutput = """
        Encontrado Git [Git.Git]
        Versión: 2.55.0.2
        Editor: The Git Development Community
        Dirección URL del editor: https://gitforwindows.org/
        Dirección URL de soporte del editor: https://github.com/git-for-windows/git/issues
        Moniker: git
        Descripción:
          Git is a free and open source distributed version control system.
          Git for Windows focuses on offering a lightweight, native set of tools.
        Página principal: https://gitforwindows.org/
        Licencia: GPL-2.0
        Dirección URL de la licencia: https://github.com/git-for-windows/build-extra/blob/HEAD/LICENSE.txt
        Notas de la versión:
          Changes since Git for Windows v2.55.0
        Dirección URL de notas de la versión: https://github.com/git-for-windows/git/releases/tag/v2.55.0.windows.2
        Instalador:
          Tipo de instalador: inno
          Dirección URL del instalador: https://github.com/git-for-windows/git/releases/download/Git-2.55.0.2.exe
        """;

    private const string EnglishOutput = """
        Found Git [Git.Git]
        Version: 2.55.0.2
        Publisher: The Git Development Community
        Publisher Url: https://gitforwindows.org/
        Description:
          Git is a free and open source distributed version control system.
        Homepage: https://gitforwindows.org/
        License: GPL-2.0
        Release Notes Url: https://github.com/git-for-windows/git/releases/tag/v2.55.0.windows.2
        Installer:
          Installer Type: inno
        """;

    [Fact]
    public void ParsePackageInfo_Spanish_ReadsDescriptionAndUrls()
    {
        var info = WingetService.ParsePackageInfo(SpanishOutput);

        Assert.StartsWith("Git is a free and open source", info.Description);
        Assert.Contains("lightweight, native set of tools", info.Description);   // el bloque se une
        Assert.Equal("https://gitforwindows.org/", info.Homepage);
        Assert.Equal("https://github.com/git-for-windows/git/releases/tag/v2.55.0.windows.2", info.ReleaseNotesUrl);
    }

    [Fact]
    public void ParsePackageInfo_English_ReadsDescriptionAndUrls()
    {
        var info = WingetService.ParsePackageInfo(EnglishOutput);

        Assert.StartsWith("Git is a free and open source", info.Description);
        Assert.Equal("https://gitforwindows.org/", info.Homepage);
        Assert.Equal("https://github.com/git-for-windows/git/releases/tag/v2.55.0.windows.2", info.ReleaseNotesUrl);
    }

    [Fact]
    public void ParsePackageInfo_Spanish_DoesNotConfusePublisherUrlWithHomepage()
    {
        // "Dirección URL del editor" aparece ANTES que "Página principal" y también es una URL:
        // el parser no debe quedarse con la primera URL que vea.
        var info = WingetService.ParsePackageInfo(SpanishOutput);

        Assert.DoesNotContain("issues", info.Homepage);
        Assert.Equal("https://gitforwindows.org/", info.Homepage);
    }

    [Theory]
    // Una etiqueta por idioma, con sus rarezas: espacio duro en francés, dos puntos de ancho
    // completo en chino tradicional y ningún signo en coreano.
    [InlineData("Startseite: https://ejemplo.dev/")]                      // alemán
    [InlineData("Page d’accueil : https://ejemplo.dev/")]            // francés
    [InlineData("Home page: https://ejemplo.dev/")]                       // italiano
    [InlineData("ホーム ページ: https://ejemplo.dev/")]   // japonés
    [InlineData("홈페이지: https://ejemplo.dev/")]        // coreano (sin ':' en la etiqueta)
    [InlineData("Página inicial: https://ejemplo.dev/")]             // portugués
    [InlineData("首頁：https://ejemplo.dev/")]                // chino tradicional
    public void ParsePackageInfo_EveryTranslatedLanguage_ReadsHomepage(string homepageLine)
    {
        var info = WingetService.ParsePackageInfo($"Found X [X.Y]\n{homepageLine}\n");

        Assert.Equal("https://ejemplo.dev/", info.Homepage);
    }

    [Fact]
    public void ParsePackageInfo_IgnoresNonHttpUrls()
    {
        var info = WingetService.ParsePackageInfo("Homepage: ftp://ejemplo.dev/\n");

        Assert.Equal("", info.Homepage);
    }

    [Fact]
    public void ParsePackageInfo_EmptyOutput_ReturnsEmptyInfo()
    {
        var info = WingetService.ParsePackageInfo("");

        Assert.Equal("", info.Description);
        Assert.Equal("", info.Homepage);
        Assert.Equal("", info.ReleaseNotesUrl);
    }
}
