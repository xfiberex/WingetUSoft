using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Etiquetas accesibles de las filas de Historial y Desinstalar (F-02).
/// </summary>
/// <remarks>
/// Mismo enfoque que <see cref="CleanupItemLabelsTests"/>: lo que se fija aquí es la composición de la
/// etiqueta. Que la plantilla la enlace lo exige <see cref="XamlAccessibilityTests"/>, y que llegue al árbol
/// de UI Automation se comprobó conduciendo la app real.
/// </remarks>
public sealed class RowLabelTests
{
    private static HistoryEntry Entry(string fromVersion = "1.0", bool success = true) => new()
    {
        Date = new DateTime(2026, 9, 14, 10, 30, 0),
        PackageName = "Un Programa",
        PackageId = "Editor.UnPrograma",
        FromVersion = fromVersion,
        ToVersion = "2.0",
        Success = success,
    };

    [Fact]
    public void HistoryRow_CarriesDateNameVersionsAndStatus()
    {
        var row = new HistoryEntryViewModel(Entry());

        Assert.Contains(row.DateDisplay, row.RowLabel, StringComparison.Ordinal);
        Assert.Contains("Un Programa", row.RowLabel, StringComparison.Ordinal);
        Assert.Contains("1.0", row.RowLabel, StringComparison.Ordinal);
        Assert.Contains("2.0", row.RowLabel, StringComparison.Ordinal);
        Assert.Contains(row.StatusDisplay, row.RowLabel, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(HistoryEntryViewModel), row.RowLabel, StringComparison.Ordinal);
    }

    /// <summary>
    /// Una instalación desde Buscar no tiene versión de origen. La etiqueta no puede decir «de  a 2.0» con
    /// un hueco: usa su propia forma.
    /// </summary>
    [Fact]
    public void HistoryRow_WithoutFromVersion_UsesTheInstallWording()
    {
        var row = new HistoryEntryViewModel(Entry(fromVersion: ""));
        int lang = (int)L.Current;

        Assert.Equal(
            string.Format(L.Map["history.rowAccessibleInstall"][lang],
                row.DateDisplay, row.PackageName, row.ToVersion, row.StatusDisplay),
            row.RowLabel);
    }

    [Fact]
    public void HistoryRow_FailedAndSucceeded_AreAnnouncedDifferently()
    {
        Assert.NotEqual(
            new HistoryEntryViewModel(Entry(success: true)).RowLabel,
            new HistoryEntryViewModel(Entry(success: false)).RowLabel);
    }

    [Fact]
    public void InstalledPackageRow_CarriesNameVersionAndSource()
    {
        var row = new InstalledPackageViewModel(new WingetPackage
        {
            Name = "Un Programa", Id = "Editor.UnPrograma", Version = "3.1", Source = "winget",
        });
        int lang = (int)L.Current;

        Assert.Equal(string.Format(L.Map["uninstall.rowAccessible"][lang], "Un Programa", "3.1", "winget"), row.RowLabel);
        Assert.DoesNotContain(nameof(WingetPackage), row.RowLabel, StringComparison.Ordinal);
    }

    /// <summary>
    /// La mayoría de lo instalado no viene de winget (entradas de «Agregar o quitar programas»): sin origen,
    /// la etiqueta no termina en «origen » vacío.
    /// </summary>
    [Fact]
    public void InstalledPackageRow_WithoutSource_OmitsTheSource()
    {
        var row = new InstalledPackageViewModel(new WingetPackage { Name = "Un Programa", Id = "X", Version = "3.1" });
        int lang = (int)L.Current;

        Assert.Equal(string.Format(L.Map["uninstall.rowAccessibleNoSource"][lang], "Un Programa", "3.1"), row.RowLabel);
    }
}
