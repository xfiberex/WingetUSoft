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

    /// <summary>
    /// Cada buscador y filtro toma su nombre accesible de la etiqueta que se ve a su lado (T2-08 y F-15).
    /// </summary>
    /// <remarks>
    /// T2-08 lo hizo en la ventana principal y las otras tres se quedaron fuera: sus controles se anunciaban con
    /// el placeholder o con el valor elegido. La lista es explícita a propósito: un control nuevo sin etiqueta
    /// no se detecta solo, pero uno de estos que pierda el <c>LabeledBy</c>, o cuya etiqueta cambie de nombre, sí.
    /// </remarks>
    [Theory]
    [InlineData("MainWindow.xaml", "cmbFuente", "txtFuenteLabel")]
    [InlineData("MainWindow.xaml", "btnFiltroExcluidos", "txtExcluidosLabel")]
    [InlineData("MainWindow.xaml", "txtBuscar", "txtBuscarLabel")]
    [InlineData("HistoryWindow.xaml", "txtBuscar", "txtBuscarLabel")]
    [InlineData("HistoryWindow.xaml", "btnFiltroEstado", "txtEstadoLabel")]
    [InlineData("UninstallWindow.xaml", "txtBuscar", "txtBuscarLabel")]
    [InlineData("SearchWindow.xaml", "txtBuscar", "txtBuscarLabel")]
    public void SearchAndFilterControls_AreLabeledByTheirVisibleLabel(string file, string controlName, string labelName)
    {
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        var doc = XDocument.Load(UiXamlFiles().Single(f => Path.GetFileName(f) == file));
        XElement Named(string name) => doc.Descendants().Single(e => (string?)e.Attribute(xaml + "Name") == name);

        Assert.Equal(Presentation + "TextBlock", Named(labelName).Name);
        Assert.Equal(
            "{Binding ElementName=" + labelName + "}",
            (string?)Named(controlName).Attribute("AutomationProperties.LabeledBy"));
    }

    /// <summary>
    /// F-16: en Configuración, toda opción es una <c>SettingsCard</c> con su título y su descripción.
    /// </summary>
    /// <remarks>
    /// Hasta F-16 convivían cuatro patrones: radios con subtítulo, <c>ComboBox</c> con <c>Header</c>, <c>CheckBox</c> con
    /// el texto al lado e interruptores con un <c>TextBlock</c> suelto. Se prohíben los tres controles que traían su
    /// propia etiqueta, y se exige que cada control de opción cuelgue de una fila que lo nombre.
    /// </remarks>
    [Fact]
    public void SettingsWindow_UsesASingleRowPattern()
    {
        XNamespace local = "using:WingetUSoft";
        var doc = XDocument.Load(UiXamlFiles().Single(f => Path.GetFileName(f) == "SettingsWindow.xaml"));

        Assert.Empty(doc.Descendants(Presentation + "RadioButtons"));
        Assert.Empty(doc.Descendants(Presentation + "CheckBox"));
        Assert.DoesNotContain(doc.Descendants(Presentation + "ComboBox"), c => c.Attribute("Header") is not null);

        var options = doc.Descendants()
            .Where(e => e.Name == Presentation + "ComboBox" || e.Name == Presentation + "ToggleSwitch")
            .ToList();
        Assert.True(options.Count >= 7, $"Solo se encontraron {options.Count} controles de opción.");
        Assert.All(options, o => Assert.Equal(local + "SettingsCard", o.Parent!.Name));

        var cards = doc.Descendants(local + "SettingsCard").ToList();
        Assert.All(cards, c => Assert.NotNull(c.Attribute("Header")));
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
