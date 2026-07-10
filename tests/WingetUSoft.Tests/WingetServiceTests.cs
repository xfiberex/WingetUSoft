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
            "consultar actualizaciones",
            42,
            "",
            "Error interno\r\nSe agotó el tiempo de espera\r\n");

        Assert.Contains("consultar actualizaciones", message);
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
