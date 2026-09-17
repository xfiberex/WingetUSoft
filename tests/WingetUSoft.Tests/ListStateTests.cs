using System.Xml.Linq;
using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Estados de las cuatro listas de la aplicación (F-18).
/// </summary>
/// <remarks>
/// Hasta F-18 solo la tabla principal contaba en qué punto estaba, con su panel escrito a mano; Buscar,
/// Desinstalar y Limpieza dejaban un hueco en blanco y lo explicaban únicamente en la barra de estado del pie.
/// Además, la instrucción del arranque se repetía tres veces y ninguna se podía pulsar.
/// </remarks>
public sealed class ListStateTests
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace Local = "using:WingetUSoft";
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>Las cuatro ventanas con lista usan el mismo control, no un panel copiado en cada XAML.</summary>
    [Theory]
    [InlineData("MainWindow.xaml")]
    [InlineData("SearchWindow.xaml")]
    [InlineData("UninstallWindow.xaml")]
    [InlineData("CleanupWindow.xaml")]
    public void EveryListWindow_UsesTheSharedStatePanel(string file)
    {
        var doc = XDocument.Load(Path.Combine(UiDirectory(), file));

        Assert.Single(doc.Descendants(Local + "ListStatePanel"));
        Assert.DoesNotContain(doc.Descendants(Presentation + "StackPanel"),
            e => (string?)e.Attribute(Xaml + "Name") == "panelListState");
    }

    /// <summary>El estado inicial y los de error o cancelado traen la acción que los resuelve.</summary>
    [Fact]
    public void ListStates_OfferTheActionThatResolvesThem()
    {
        string main = File.ReadAllText(Path.Combine(UiDirectory(), "MainWindow.xaml.cs"));

        Assert.Contains("L.T(\"list.stateInitialBody\"),\r\n                ListStatePanel.Glyph.Sync, L.T(\"btn.checkUpdates\")",
            main, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(main, "L.T(\"btn.retry\")"));   // cancelado y error
    }

    /// <summary>La instrucción del arranque vive en un solo sitio: el panel de la tabla vacía.</summary>
    [Fact]
    public void StartupInstruction_IsNotRepeated()
    {
        Assert.False(L.Map.ContainsKey("header.detailEmpty"), "header.detailEmpty repetía la instrucción en la línea de detalle.");
        Assert.False(L.Map.ContainsKey("status.readyToStart"), "status.readyToStart la repetía en la barra de estado.");

        foreach (string file in Directory.EnumerateFiles(UiDirectory(), "*.cs"))
        {
            string code = File.ReadAllText(file);
            Assert.DoesNotContain("header.detailEmpty", code, StringComparison.Ordinal);
            Assert.DoesNotContain("status.readyToStart", code, StringComparison.Ordinal);
        }
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        for (int i = text.IndexOf(value, StringComparison.Ordinal); i >= 0;
             i = text.IndexOf(value, i + value.Length, StringComparison.Ordinal))
            count++;
        return count;
    }

    private static string UiDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WingetUSoft.slnx")))
            dir = dir.Parent;

        Assert.True(dir is not null, "No se encontró la raíz del repositorio (WingetUSoft.slnx) desde " + AppContext.BaseDirectory);
        return Path.Combine(dir!.FullName, "src", "WingetUSoft", "UI");
    }
}
