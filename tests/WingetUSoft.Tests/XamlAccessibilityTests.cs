using System.Xml.Linq;
using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Comprobaciones de accesibilidad sobre el XAML de las ventanas, sin arrancar la app.
/// </summary>
/// <remarks>
/// Leen los <c>.xaml</c> como XML en vez de conducir la app porque así cubren también las ventanas que los
/// UI tests no pueden abrir sin efectos: Limpieza solo aparece tras desinstalar un programa de verdad, y
/// Desinstalar lanza <c>winget list</c> al abrirse (los UI tests no dependen de winget ni de la red).
/// </remarks>
public sealed class XamlAccessibilityTests
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    /// <summary>
    /// Toda plantilla de fila de un <c>ListView</c> nombra la fila para UI Automation (F-02).
    /// </summary>
    /// <remarks>
    /// Sin <c>AutomationProperties.Name</c> en la raíz de la plantilla, el <c>ListViewItem</c> hereda el
    /// <c>ToString()</c> del objeto y un lector de pantalla anuncia el nombre del tipo .NET. Pasó cuatro veces:
    /// en la tabla principal y en Búsqueda (Tier E), en Limpieza (T1-09) y, comprobado en la app real el
    /// 2026-09-14, en Historial (<c>WingetUSoft.HistoryEntryViewModel</c>) y en Desinstalar
    /// (<c>WingetUSoft.WingetPackage</c>). Cada vez se arregló la ventana en la que se vio; esto lo exige en
    /// todas, incluidas las que se escriban en el futuro.
    /// </remarks>
    [Fact]
    public void EveryListRowTemplate_GivesTheRowAnAccessibleName()
    {
        var templates = UiXamlFiles()
            .SelectMany(file => XDocument.Load(file).Descendants(Presentation + "ListView.ItemTemplate")
                .Select(itemTemplate => (File: Path.GetFileName(file), Root: RowRoot(itemTemplate))))
            .ToList();

        // Principal, Búsqueda, Desinstalar, Historial y Limpieza: si el escaneo encuentra menos, no está
        // probando lo que dice.
        Assert.True(templates.Count >= 5, $"Solo se encontraron {templates.Count} plantillas de fila.");

        var unnamed = templates
            .Where(t => t.Root?.Attribute("AutomationProperties.Name") is null)
            .Select(t => t.File)
            .ToList();

        Assert.True(
            unnamed.Count == 0,
            "Plantillas de fila sin AutomationProperties.Name (el lector anunciaría el tipo .NET): "
                + string.Join(", ", unnamed));
    }

    /// <summary>Primer elemento dentro del <c>DataTemplate</c>: el que representa la fila.</summary>
    private static XElement? RowRoot(XElement itemTemplate) =>
        itemTemplate.Element(Presentation + "DataTemplate")?.Elements().FirstOrDefault();

    private static IEnumerable<string> UiXamlFiles()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WingetUSoft.slnx")))
            dir = dir.Parent;

        Assert.True(dir is not null, "No se encontró la raíz del repositorio (WingetUSoft.slnx) desde " + AppContext.BaseDirectory);

        string ui = Path.Combine(dir!.FullName, "src", "WingetUSoft", "UI");
        Assert.True(Directory.Exists(ui), "No existe el directorio de vistas: " + ui);
        return Directory.EnumerateFiles(ui, "*.xaml");
    }
}
