using Microsoft.VisualStudio.TestTools.UnitTesting;
using WingetUSoft;

namespace WingetUSoft.Tests;

[TestClass]
public class WingetServiceTests
{
    // ── ParseUpgradeOutput ──────────────────────────────────────────────────

    [TestMethod]
    public void ParseUpgradeOutput_EnglishOutput_ReturnsCorrectPackages()
    {
        string output =
            "Name                             Id                         Version    Available  Source\r\n" +
            "-----------------------------------------------------------------------------------------------\r\n" +
            "Microsoft OneDrive               Microsoft.OneDrive         26.035.0   26.040.0   winget\r\n" +
            "TeamViewer                       TeamViewer.TeamViewer      15.75.5    15.76.3    winget\r\n" +
            "2 upgrades available.\r\n";

        var packages = WingetService.ParseUpgradeOutput(output);

        Assert.AreEqual(2, packages.Count);
        Assert.AreEqual("Microsoft OneDrive", packages[0].Name);
        Assert.AreEqual("Microsoft.OneDrive", packages[0].Id);
        Assert.AreEqual("winget", packages[0].Source);
        Assert.AreEqual("TeamViewer.TeamViewer", packages[1].Id);
    }

    [TestMethod]
    public void ParseUpgradeOutput_SpanishOutput_ReturnsCorrectPackages()
    {
        string output =
            "Nombre                           Id                         Versión    Disponible Origen\r\n" +
            "-----------------------------------------------------------------------------------------------\r\n" +
            "MongoDB Compass                  MongoDB.Compass.Full       1.49.1     1.49.4.0   winget\r\n" +
            "1 actualización disponible.\r\n";

        var packages = WingetService.ParseUpgradeOutput(output);

        Assert.AreEqual(1, packages.Count);
        Assert.AreEqual("MongoDB.Compass.Full", packages[0].Id);
        Assert.AreEqual("1.49.1", packages[0].Version.Trim());
    }

    [TestMethod]
    public void ParseUpgradeOutput_SummaryLineFiltered_NotIncludedAsPackage()
    {
        string output =
            "Name                  Id              Version  Available  Source\r\n" +
            "-------------------------------------------------------------------\r\n" +
            "VLC                   VideoLAN.VLC    3.0.18   3.0.20     winget\r\n" +
            "1 upgrades available.\r\n";

        var packages = WingetService.ParseUpgradeOutput(output);

        Assert.AreEqual(1, packages.Count, "Summary line must not be parsed as a package.");
        Assert.AreEqual("VideoLAN.VLC", packages[0].Id);
    }

    [TestMethod]
    public void ParseUpgradeOutput_NoUpdates_ReturnsEmpty()
    {
        string output = "No se encontraron actualizaciones disponibles.\r\n";

        var packages = WingetService.ParseUpgradeOutput(output);

        Assert.AreEqual(0, packages.Count);
    }

    [TestMethod]
    public void ParseUpgradeOutput_UnknownVersion_ParsedCorrectly()
    {
        string output =
            "Name              Id                  Version        Available  Source\r\n" +
            "------------------------------------------------------------------------\r\n" +
            "Driver Booster 13 IObit.DriverBooster  < 13.3.0.229   13.3.0.229 winget\r\n" +
            "1 upgrades available.\r\n";

        var packages = WingetService.ParseUpgradeOutput(output);

        Assert.AreEqual(1, packages.Count);
        StringAssert.StartsWith(packages[0].Version.TrimStart(), "<");
    }

    [TestMethod]
    public void ParseUpgradeOutput_PackageNameStartsWithDigit_IsNotFilteredOut()
    {
        string output =
            "Name                             Id                         Version    Available  Source\r\n" +
            "-----------------------------------------------------------------------------------------------\r\n" +
            "7-Zip                            7zip.7zip                 24.08      24.09      winget\r\n";

        var packages = WingetService.ParseUpgradeOutput(output);

        Assert.AreEqual(1, packages.Count);
        Assert.AreEqual("7-Zip", packages[0].Name);
        Assert.AreEqual("7zip.7zip", packages[0].Id);
    }

    [TestMethod]
    public void ParseUpgradeOutput_CustomHeaderLabels_ParsesUsingColumnLayout()
    {
        string output =
            "Paquete                          Identificador              Actual     Nueva      Repositorio\r\n" +
            "------------------------------------------------------------------------------------------------\r\n" +
            "Mozilla Firefox                  Mozilla.Firefox            136.0      137.0      winget\r\n";

        var packages = WingetService.ParseUpgradeOutput(output);

        Assert.AreEqual(1, packages.Count);
        Assert.AreEqual("Mozilla Firefox", packages[0].Name);
        Assert.AreEqual("Mozilla.Firefox", packages[0].Id);
        Assert.AreEqual("137.0", packages[0].Available);
        Assert.AreEqual("winget", packages[0].Source);
    }

    [TestMethod]
    public void ParseUpgradeOutput_EmptyString_ReturnsEmpty()
    {
        var packages = WingetService.ParseUpgradeOutput(string.Empty);
        Assert.AreEqual(0, packages.Count);
    }

    [TestMethod]
    public void BuildUpgradeArguments_SilentMode_ContainsExpectedFlags()
    {
        string arguments = WingetService.BuildUpgradeArguments("VideoLAN.VLC", silent: true);

        StringAssert.Contains(arguments, "upgrade --id \"VideoLAN.VLC\"");
        StringAssert.Contains(arguments, "--accept-source-agreements");
        StringAssert.Contains(arguments, "--accept-package-agreements");
        StringAssert.Contains(arguments, "--silent");
    }

    [TestMethod]
    public void BuildWingetCommandErrorMessage_UsesLastMeaningfulLine()
    {
        string message = WingetService.BuildWingetCommandErrorMessage(
            "consultar actualizaciones",
            42,
            "",
            "Error interno\r\nSe agotó el tiempo de espera\r\n");

        StringAssert.Contains(message, "consultar actualizaciones");
        StringAssert.Contains(message, "Se agotó el tiempo de espera");
        StringAssert.Contains(message, "42");
    }

    [TestMethod]
    public void GetFailureReason_UserCancelledElevation_ReturnsFriendlyMessage()
    {
        var result = new UpgradeResult
        {
            Success = false,
            ExitCode = 1223,
            UserCancelled = true
        };

        Assert.AreEqual(
            "Se canceló la elevación de permisos. La actualización no se inició.",
            result.GetFailureReason());
    }

    [TestMethod]
    public void ParseElevatedBatchResult_ValidJson_ReturnsItemsAndCancelledFlag()
    {
        string json =
            "{" +
            "\"BatchCancelled\":true," +
            "\"Results\":[" +
            "{\"PackageId\":\"App.One\",\"Success\":true,\"ExitCode\":0,\"UserCancelled\":false,\"Output\":\"ok\",\"ErrorOutput\":\"\"}," +
            "{\"PackageId\":\"App.Two\",\"Success\":false,\"ExitCode\":5,\"UserCancelled\":false,\"Output\":\"\",\"ErrorOutput\":\"fail\"}" +
            "]}";

        var result = WingetService.ParseElevatedBatchResult(json);

        Assert.IsTrue(result.CancelledAfterCurrentPackage);
        Assert.AreEqual(2, result.Items.Count);
        Assert.AreEqual("App.One", result.Items[0].PackageId);
        Assert.IsTrue(result.Items[0].Result.Success);
        Assert.AreEqual("fail", result.Items[1].Result.ErrorOutput);
    }

    [TestMethod]
    public void ParseElevatedBatchStatus_RunningJson_ReturnsCurrentPackageInfo()
    {
        string json =
            "{" +
            "\"Phase\":\"running\"," +
            "\"CurrentIndex\":2," +
            "\"PackageId\":\"App.Two\"," +
            "\"TotalCount\":5" +
            "}";

        var status = WingetService.ParseElevatedBatchStatus(json);

        Assert.IsNotNull(status);
        Assert.AreEqual("running", status.Phase);
        Assert.AreEqual(2, status.CurrentIndex);
        Assert.AreEqual(5, status.TotalCount);
        Assert.AreEqual("App.Two", status.PackageId);
    }

    // ── ParseProgressLine ───────────────────────────────────────────────────

    [TestMethod]
    public void ParseProgressLine_MBFormat_ReturnsCorrectValues()
    {
        string line = "  ████████████  45.3 MB / 200.0 MB  8.5 MB/s";

        var info = WingetService.ParseProgressLine(line);

        Assert.IsNotNull(info);
        Assert.AreEqual((long)(45.3 * 1_048_576), info.DownloadedBytes, delta: 1024);
        Assert.AreEqual((long)(200.0 * 1_048_576), info.TotalBytes, delta: 1024);
        Assert.IsTrue(info.SpeedBytesPerSecond > 0);
    }

    [TestMethod]
    public void ParseProgressLine_KBFormat_ReturnsCorrectValues()
    {
        string line = "  500 KB / 1024 KB";

        var info = WingetService.ParseProgressLine(line);

        Assert.IsNotNull(info);
        Assert.AreEqual(500L * 1024, info.DownloadedBytes);
        Assert.AreEqual(1024L * 1024, info.TotalBytes);
        Assert.AreEqual(0.0, info.SpeedBytesPerSecond);
    }

    [TestMethod]
    public void ParseProgressLine_CommaDecimalSeparator_ParsedCorrectly()
    {
        string line = "  12,5 MB / 100,0 MB  2,3 MB/s";

        var info = WingetService.ParseProgressLine(line);

        Assert.IsNotNull(info);
        Assert.AreEqual((long)(12.5 * 1_048_576), info.DownloadedBytes, delta: 1024);
    }

    [TestMethod]
    public void ParseProgressLine_NonProgressLine_ReturnsNull()
    {
        Assert.IsNull(WingetService.ParseProgressLine("Descargando https://example.com/installer.exe"));
        Assert.IsNull(WingetService.ParseProgressLine("Instalando paquete..."));
        Assert.IsNull(WingetService.ParseProgressLine(string.Empty));
    }

    [TestMethod]
    public void ParseProgressLine_GBFormat_ReturnsCorrectValues()
    {
        string line = "  1.2 GB / 4.0 GB";

        var info = WingetService.ParseProgressLine(line);

        Assert.IsNotNull(info);
        Assert.AreEqual((long)(1.2 * 1_073_741_824L), info.DownloadedBytes, delta: 1_048_576);
        Assert.AreEqual((long)(4.0 * 1_073_741_824L), info.TotalBytes, delta: 1_048_576);
    }

    [TestMethod]
    public void DelimitedTextExporter_FormatField_FormulaIsNeutralized()
    {
        string formatted = DelimitedTextExporter.FormatField("=SUM(A1:A2)");

        Assert.AreEqual("\"'=SUM(A1:A2)\"", formatted);
    }

    [TestMethod]
    public void DelimitedTextExporter_FormatField_QuotesAndLineBreaksAreEscaped()
    {
        string formatted = DelimitedTextExporter.FormatField("valor \"peligroso\"\r\nsegundo");

        Assert.AreEqual("\"valor \"\"peligroso\"\" segundo\"", formatted);
    }
}
