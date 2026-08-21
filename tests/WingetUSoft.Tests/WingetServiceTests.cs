using Xunit;

namespace WingetUSoft.Tests;

public class WingetServiceTests
{
    // ── ParseUpgradeOutput ──────────────────────────────────────────────────

    [Fact]
    public void ParseUpgradeOutput_EnglishOutput_ReturnsCorrectPackages()
    {
        string output =
            "Name                             Id                         Version    Available  Source\r\n" +
            "-----------------------------------------------------------------------------------------------\r\n" +
            "Microsoft OneDrive               Microsoft.OneDrive         26.035.0   26.040.0   winget\r\n" +
            "TeamViewer                       TeamViewer.TeamViewer      15.75.5    15.76.3    winget\r\n" +
            "2 upgrades available.\r\n";

        var packages = WingetService.ParseUpgradeOutput(output);

        Assert.Equal(2, packages.Count);
        Assert.Equal("Microsoft OneDrive", packages[0].Name);
        Assert.Equal("Microsoft.OneDrive", packages[0].Id);
        Assert.Equal("winget", packages[0].Source);
        Assert.Equal("TeamViewer.TeamViewer", packages[1].Id);
    }

    [Fact]
    public void ParseUpgradeOutput_SpanishOutput_ReturnsCorrectPackages()
    {
        string output =
            "Nombre                           Id                         Versión    Disponible Origen\r\n" +
            "-----------------------------------------------------------------------------------------------\r\n" +
            "MongoDB Compass                  MongoDB.Compass.Full       1.49.1     1.49.4.0   winget\r\n" +
            "1 actualización disponible.\r\n";

        var packages = WingetService.ParseUpgradeOutput(output);

        Assert.Single(packages);
        Assert.Equal("MongoDB.Compass.Full", packages[0].Id);
        Assert.Equal("1.49.1", packages[0].Version.Trim());
    }

    [Fact]
    public void ParseUpgradeOutput_SummaryLineFiltered_NotIncludedAsPackage()
    {
        string output =
            "Name                  Id              Version  Available  Source\r\n" +
            "-------------------------------------------------------------------\r\n" +
            "VLC                   VideoLAN.VLC    3.0.18   3.0.20     winget\r\n" +
            "1 upgrades available.\r\n";

        var packages = WingetService.ParseUpgradeOutput(output);

        Assert.True(packages.Count == 1, "Summary line must not be parsed as a package.");
        Assert.Equal("VideoLAN.VLC", packages[0].Id);
    }

    [Fact]
    public void ParseUpgradeOutput_NoUpdates_ReturnsEmpty()
    {
        string output = "No se encontraron actualizaciones disponibles.\r\n";

        var packages = WingetService.ParseUpgradeOutput(output);

        Assert.Empty(packages);
    }

    [Fact]
    public void ParseUpgradeOutput_UnknownVersion_ParsedCorrectly()
    {
        string output =
            "Name              Id                  Version        Available  Source\r\n" +
            "------------------------------------------------------------------------\r\n" +
            "Driver Booster 13 IObit.DriverBooster  < 13.3.0.229   13.3.0.229 winget\r\n" +
            "1 upgrades available.\r\n";

        var packages = WingetService.ParseUpgradeOutput(output);

        Assert.Single(packages);
        Assert.StartsWith("<", packages[0].Version.TrimStart());
    }

    [Fact]
    public void ParseUpgradeOutput_PackageNameStartsWithDigit_IsNotFilteredOut()
    {
        string output =
            "Name                             Id                         Version    Available  Source\r\n" +
            "-----------------------------------------------------------------------------------------------\r\n" +
            "7-Zip                            7zip.7zip                 24.08      24.09      winget\r\n";

        var packages = WingetService.ParseUpgradeOutput(output);

        Assert.Single(packages);
        Assert.Equal("7-Zip", packages[0].Name);
        Assert.Equal("7zip.7zip", packages[0].Id);
    }

    [Fact]
    public void ParseUpgradeOutput_CustomHeaderLabels_ParsesUsingColumnLayout()
    {
        string output =
            "Paquete                          Identificador              Actual     Nueva      Repositorio\r\n" +
            "------------------------------------------------------------------------------------------------\r\n" +
            "Mozilla Firefox                  Mozilla.Firefox            136.0      137.0      winget\r\n";

        var packages = WingetService.ParseUpgradeOutput(output);

        Assert.Single(packages);
        Assert.Equal("Mozilla Firefox", packages[0].Name);
        Assert.Equal("Mozilla.Firefox", packages[0].Id);
        Assert.Equal("137.0", packages[0].Available);
        Assert.Equal("winget", packages[0].Source);
    }

    [Fact]
    public void ParseUpgradeOutput_EmptyString_ReturnsEmpty()
    {
        var packages = WingetService.ParseUpgradeOutput(string.Empty);
        Assert.Empty(packages);
    }

    [Fact]
    public void BuildWingetCommandErrorMessage_UsesLastMeaningfulLine()
    {
        string message = WingetService.BuildWingetCommandErrorMessage(
            "winget.actionCheckUpdates",
            42,
            "",
            "Error interno\r\nSe agotó el tiempo de espera\r\n");

        // El verbo llega como clave, no como texto: el mensaje se compone entero en el idioma activo.
        Assert.Contains(L.T("winget.actionCheckUpdates"), message);
        Assert.DoesNotContain("winget.action", message);
        Assert.Contains("Se agotó el tiempo de espera", message);
        Assert.Contains("42", message);
    }

    [Fact]
    public void GetFailureReason_UserCancelledElevation_ReturnsFriendlyMessage()
    {
        var result = new UpgradeResult
        {
            Success = false,
            ExitCode = 1223,
            UserCancelled = true
        };

        Assert.Equal(
            "Se canceló la elevación de permisos. La actualización no se inició.",
            result.GetFailureReason());
    }

    [Fact]
    public void GetFailureReason_HashMismatch_ReturnsFriendlyMessage()
    {
        var result = new UpgradeResult
        {
            Success = false,
            ExitCode = 1,
            ErrorOutput = "Hash mismatch detected for installer"
        };

        Assert.Contains("hash", result.GetFailureReason());
    }

    [Fact]
    public void GetFailureReason_NetworkError_ReturnsFriendlyMessage()
    {
        var result = new UpgradeResult
        {
            Success = false,
            ExitCode = 1,
            ErrorOutput = "network connection failed"
        };

        Assert.Contains("red", result.GetFailureReason());
    }

    /// <summary>
    /// Un código de winget, **sin nada de texto**, tiene que bastar para clasificar el fallo.
    /// </summary>
    /// <remarks>
    /// Es el escenario de cualquier Windows que no esté en español o inglés: winget traduce su salida,
    /// así que el texto no dice nada que la app sepa leer, pero el código es el mismo en todas partes.
    /// Antes de T1-02 todos estos casos caían en el «última línea con sentido» y el usuario veía la
    /// línea cruda de winget.
    /// </remarks>
    [Theory]
    [InlineData("0x8A15002B", "reason.noApplicableUpdate")]
    [InlineData("0x8A150010", "reason.noApplicableInstaller")]
    [InlineData("0x8A150011", "reason.hashMismatch")]
    [InlineData("0x8A150019", "reason.needsAdmin")]
    [InlineData("0x8A15003A", "reason.blocked")]
    [InlineData("0x8A15010F", "reason.blocked")]
    [InlineData("0x8A150101", "reason.currentlyRunning")]
    [InlineData("0x8A150102", "reason.currentlyRunning")]
    [InlineData("0x8A150014", "reason.notFound")]
    [InlineData("0x8A150008", "reason.networkError")]
    [InlineData("0x8A150086", "reason.networkError")]
    [InlineData("0x8A150107", "reason.networkError")]
    public void GetFailureReason_WingetErrorCode_ClassifiesWithoutAnyText(string code, string expectedKey)
    {
        var result = new UpgradeResult
        {
            Success = false,
            ExitCode = 1,
            ErrorOutput = code
        };

        Assert.Equal(L.T(expectedKey), result.GetFailureReason());
    }

    /// <summary>El código también llega como código de salida del proceso, sin aparecer en el texto.</summary>
    [Fact]
    public void GetFailureReason_WingetErrorCodeAsExitCode_ClassifiesWithoutAnyText()
    {
        var result = new UpgradeResult
        {
            Success = false,
            ExitCode = unchecked((int)0x8A15002B),
            Output = "",
            ErrorOutput = ""
        };

        Assert.Equal(L.T("reason.noApplicableUpdate"), result.GetFailureReason());
    }

    /// <summary>
    /// <c>0x8A150011</c> es «el hash del instalador no coincide con el manifiesto», no «no hay
    /// actualización aplicable»: la tabla de la app lo tenía cambiado y presentaba un fallo de
    /// integridad —el caso que más importa distinguir— como un tranquilizador «ya estás al día».
    /// Igual con <c>0x8A150014</c>, que es «no se encontró el paquete», no «ningún instalador aplicable».
    /// </summary>
    [Fact]
    public void GetFailureReason_MisassignedCodes_MapToTheirRealMeaning()
    {
        var hashMismatch = new UpgradeResult { Success = false, ExitCode = 1, ErrorOutput = "0x8A150011" };
        Assert.Equal(L.T("reason.hashMismatch"), hashMismatch.GetFailureReason());
        Assert.NotEqual(L.T("reason.noApplicableUpdate"), hashMismatch.GetFailureReason());

        var notFound = new UpgradeResult { Success = false, ExitCode = 1, ErrorOutput = "0x8A150014" };
        Assert.Equal(L.T("reason.notFound"), notFound.GetFailureReason());
        Assert.NotEqual(L.T("reason.noApplicableInstaller"), notFound.GetFailureReason());
    }

    /// <summary>El código gana al texto: winget imprime ambos y el código es el dato fiable.</summary>
    [Fact]
    public void GetFailureReason_CodeWins_OverTranslatedText()
    {
        var result = new UpgradeResult
        {
            Success = false,
            ExitCode = 1,
            // Texto en un idioma que la app no sabe leer, con el código al final: así es la salida real.
            ErrorOutput = "Impossible de trouver un programme d'installation applicable 0x8A150010"
        };

        Assert.Equal(L.T("reason.noApplicableInstaller"), result.GetFailureReason());
    }

    [Fact]
    public void GetFailureReason_Success_ReturnsEmpty()
    {
        var result = new UpgradeResult { Success = true, ExitCode = 0 };
        Assert.Equal("", result.GetFailureReason());
    }

    [Fact]
    public void GetFailureReason_UnknownError_IncludesExitCode()
    {
        var result = new UpgradeResult { Success = false, ExitCode = 42, Output = "", ErrorOutput = "" };
        Assert.Contains("42", result.GetFailureReason());
    }

    /// <summary>
    /// La clasificación buscaba «red» con <c>Contains</c>, así que casaba dentro de <c>required</c>,
    /// <c>shared</c>, <c>expired</c>, <c>configured</c> o <c>registered</c>. Un fallo real como
    /// «A required file is missing» se le presentaba al usuario como «Error de red», mandándolo a revisar
    /// su conexión a internet por un archivo que falta.
    /// </summary>
    [Theory]
    [InlineData("A required file is missing")]
    [InlineData("The shared component could not be registered")]
    [InlineData("The installer certificate has expired")]
    [InlineData("No sources are configured")]
    public void GetFailureReason_WordsThatMerelyContainRed_AreNotNetworkErrors(string errorOutput)
    {
        var result = new UpgradeResult { Success = false, ExitCode = 1, ErrorOutput = errorOutput };

        Assert.NotEqual(L.T("reason.networkError"), result.GetFailureReason());
    }

    /// <summary>Y el usuario ve el fallo de verdad, no uno inventado.</summary>
    [Fact]
    public void GetFailureReason_MissingRequiredFile_ShowsTheRealMessage()
    {
        var result = new UpgradeResult { Success = false, ExitCode = 1, ErrorOutput = "A required file is missing" };

        Assert.Contains("required file", result.GetFailureReason());
    }

    /// <summary>El contrapunto: un fallo de red de verdad sí se sigue clasificando como tal.</summary>
    [Theory]
    [InlineData("network connection failed")]
    [InlineData("Error de red al descargar el instalador")]
    [InlineData("Se perdió la conexión de red.")]
    public void GetFailureReason_ActualNetworkFailures_AreStillClassified(string errorOutput)
    {
        var result = new UpgradeResult { Success = false, ExitCode = 1, ErrorOutput = errorOutput };

        Assert.Equal(L.T("reason.networkError"), result.GetFailureReason());
    }

    // ── ParseProgressLine ───────────────────────────────────────────────────

    [Fact]
    public void ParseProgressLine_MBFormat_ReturnsCorrectValues()
    {
        string line = "  ████████████  45.3 MB / 200.0 MB  8.5 MB/s";

        var info = WingetService.ParseProgressLine(line);

        Assert.NotNull(info);
        long expectedDownloaded = (long)(45.3 * 1_048_576);
        long expectedTotal = (long)(200.0 * 1_048_576);
        Assert.InRange(info.DownloadedBytes, expectedDownloaded - 1024, expectedDownloaded + 1024);
        Assert.InRange(info.TotalBytes, expectedTotal - 1024, expectedTotal + 1024);
        Assert.True(info.SpeedBytesPerSecond > 0);
    }

    [Fact]
    public void ParseProgressLine_KBFormat_ReturnsCorrectValues()
    {
        string line = "  500 KB / 1024 KB";

        var info = WingetService.ParseProgressLine(line);

        Assert.NotNull(info);
        Assert.Equal(500L * 1024, info.DownloadedBytes);
        Assert.Equal(1024L * 1024, info.TotalBytes);
        Assert.Equal(0.0, info.SpeedBytesPerSecond);
    }

    [Fact]
    public void ParseProgressLine_CommaDecimalSeparator_ParsedCorrectly()
    {
        string line = "  12,5 MB / 100,0 MB  2,3 MB/s";

        var info = WingetService.ParseProgressLine(line);

        Assert.NotNull(info);
        long expectedDownloaded = (long)(12.5 * 1_048_576);
        Assert.InRange(info.DownloadedBytes, expectedDownloaded - 1024, expectedDownloaded + 1024);
    }

    [Fact]
    public void ParseProgressLine_NonProgressLine_ReturnsNull()
    {
        Assert.Null(WingetService.ParseProgressLine("Descargando https://example.com/installer.exe"));
        Assert.Null(WingetService.ParseProgressLine("Instalando paquete..."));
        Assert.Null(WingetService.ParseProgressLine(string.Empty));
    }

    [Fact]
    public void ParseProgressLine_GBFormat_ReturnsCorrectValues()
    {
        string line = "  1.2 GB / 4.0 GB";

        var info = WingetService.ParseProgressLine(line);

        Assert.NotNull(info);
        long expectedDownloaded = (long)(1.2 * 1_073_741_824L);
        long expectedTotal = (long)(4.0 * 1_073_741_824L);
        Assert.InRange(info.DownloadedBytes, expectedDownloaded - 1_048_576, expectedDownloaded + 1_048_576);
        Assert.InRange(info.TotalBytes, expectedTotal - 1_048_576, expectedTotal + 1_048_576);
    }

    [Fact]
    public void DelimitedTextExporter_FormatField_FormulaIsNeutralized()
    {
        string formatted = DelimitedTextExporter.FormatField("=SUM(A1:A2)");

        Assert.Equal("\"'=SUM(A1:A2)\"", formatted);
    }

    [Fact]
    public void DelimitedTextExporter_FormatField_QuotesAndLineBreaksAreEscaped()
    {
        string formatted = DelimitedTextExporter.FormatField("valor \"peligroso\"\r\nsegundo");

        Assert.Equal("\"valor \"\"peligroso\"\" segundo\"", formatted);
    }
}
