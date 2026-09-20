using System.Xml.Linq;
using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Más espacio para los datos (F-17): sin tarjetas de cabecera, registros plegables y órdenes con icono y etiqueta.
/// </summary>
/// <remarks>
/// A 1180×820, las cabeceras de columna de la ventana principal empezaban al 59 % del alto y la tabla enseñaba tres
/// filas: una tarjeta repetía el título de la ventana, otra rotulaba «Acciones rápidas» y el registro reservaba un
/// tercio del alto aunque estuviera vacío. El alto real lo mide <c>DensityUiTests</c> en la app.
/// </remarks>
public sealed class DensityTests
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly XNamespace Local = "using:WingetUSoft";

    private static readonly string[] Windows =
        ["MainWindow", "SearchWindow", "UninstallWindow", "CleanupWindow", "HistoryWindow", "SettingsWindow"];

    /// <summary>El título ya lo dice la barra de título: ninguna ventana lo repite en grande.</summary>
    [Fact]
    public void NoWindow_RepeatsItsTitleInAHeaderCard()
    {
        var offenders = Windows
            .SelectMany(w => Load(w).Descendants()
                .Where(e => ((string?)e.Attribute("Style"))?.Contains("TitleTextBlockStyle", StringComparison.Ordinal) == true
                         || ((string?)e.Attribute("Style"))?.Contains("HeaderCardStyle", StringComparison.Ordinal) == true)
                .Select(e => $"{w}: {(string?)e.Attribute(Xaml + "Name") ?? e.Name.LocalName}"))
            .ToList();

        Assert.True(offenders.Count == 0, "Cabeceras que repiten el título: " + string.Join(", ", offenders));
        Assert.DoesNotContain("HeaderCardStyle", File.ReadAllText(Path.Combine(UiDirectory(), "Styles.xaml")), StringComparison.Ordinal);
    }

    /// <summary>
    /// Cada registro vive en un <c>Expander</c> que empieza plegado, su fila es <c>Auto</c> y la ventana lo engancha a
    /// <c>LogExpander</c>, que la pasa a <c>*</c> al desplegarlo.
    /// </summary>
    [Theory]
    [InlineData("MainWindow")]
    [InlineData("SearchWindow")]
    [InlineData("UninstallWindow")]
    [InlineData("CleanupWindow")]
    public void ActivityLog_IsCollapsibleAndStartsCollapsed(string window)
    {
        var doc = Load(window);
        var log = doc.Descendants(Local + "ActivityLog").Single();

        var expander = log.Parent;
        Assert.Equal(Presentation + "Expander", expander!.Name);
        Assert.Equal("expLog", (string?)expander.Attribute(Xaml + "Name"));
        Assert.NotEqual("True", (string?)expander.Attribute("IsExpanded"));

        var row = doc.Descendants(Presentation + "RowDefinition").Single(r => (string?)r.Attribute(Xaml + "Name") == "rowLog");
        Assert.Equal("Auto", (string?)row.Attribute("Height"));

        Assert.Contains("LogExpander.Attach(expLog, rowLog, activityLog", Code(window), StringComparison.Ordinal);
    }

    /// <summary>
    /// El registro se despliega solo al empezar la operación que cambia el equipo en cada ventana; una consulta o una
    /// carga no lo abren, porque su resultado está en la lista.
    /// </summary>
    [Theory]
    [InlineData("MainWindow.Batch.cs")]      // lote de actualizaciones
    [InlineData("MainWindow.Menus.cs")]      // importar paquetes
    [InlineData("SearchWindow.xaml.cs")]     // instalar
    [InlineData("UninstallWindow.xaml.cs")]  // desinstalar
    [InlineData("CleanupWindow.xaml.cs")]    // borrar residuos
    public void ActivityLog_OpensWhenAnOperationStarts(string file)
    {
        Assert.Contains("LogExpander.Reveal(expLog)", File.ReadAllText(Path.Combine(UiDirectory(), file)), StringComparison.Ordinal);
    }

    /// <summary>Un error escrito en un registro plegado pasaría desapercibido: lo despliega.</summary>
    [Fact]
    public void AnError_OpensTheLog()
    {
        string code = File.ReadAllText(Path.Combine(UiDirectory(), "LogExpander.cs"));

        Assert.Contains("log.LineAppended +=", code, StringComparison.Ordinal);
        Assert.Contains("LogLineKind.Error", code, StringComparison.Ordinal);
        Assert.Contains("LineAppended?.Invoke(kind)", File.ReadAllText(Path.Combine(UiDirectory(), "ActivityLog.xaml.cs")), StringComparison.Ordinal);
    }

    /// <summary>
    /// Las órdenes de la ventana principal llevan icono y etiqueta. Con ese contenido WinUI ya no deduce el nombre
    /// accesible, así que el código rotula siempre texto y nombre juntos (<c>CommandLabel</c>).
    /// </summary>
    [Theory]
    [InlineData("btnConsultar", "txtConsultarLabel")]
    [InlineData("btnActualizarSeleccionados", "txtActualizarSeleccionadosLabel")]
    [InlineData("btnActualizarTodo", "txtActualizarTodoLabel")]
    [InlineData("btnCancelar", "txtCancelarLabel")]
    [InlineData("btnHerramientas", "txtHerramientasLabel")]
    [InlineData("btnAyuda", "txtAyudaLabel")]
    public void MainCommands_HaveIconAndLabel_AndAnAccessibleName(string button, string label)
    {
        var element = Load("MainWindow").Descendants().Single(e => (string?)e.Attribute(Xaml + "Name") == button);

        Assert.NotEmpty(element.Descendants(Presentation + "FontIcon"));
        Assert.Contains(element.Descendants(Presentation + "TextBlock"), t => (string?)t.Attribute(Xaml + "Name") == label);
        Assert.Null(element.Attribute("Content"));

        string code = string.Concat(Directory.EnumerateFiles(UiDirectory(), "MainWindow*.cs").Select(File.ReadAllText));
        Assert.Contains($"CommandLabel.Set({button}, {label},", code, StringComparison.Ordinal);
        Assert.DoesNotContain($"{button}.Content =", code, StringComparison.Ordinal);
    }

    private static XDocument Load(string window) => XDocument.Load(Path.Combine(UiDirectory(), window + ".xaml"));

    private static string Code(string window) => File.ReadAllText(Path.Combine(UiDirectory(), window + ".xaml.cs"));

    private static string UiDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WingetUSoft.slnx")))
            dir = dir.Parent;

        Assert.True(dir is not null, "No se encontró la raíz del repositorio (WingetUSoft.slnx) desde " + AppContext.BaseDirectory);
        return Path.Combine(dir!.FullName, "src", "WingetUSoft", "UI");
    }
}
