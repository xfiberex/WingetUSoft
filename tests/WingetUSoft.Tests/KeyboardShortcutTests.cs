using System.Xml.Linq;
using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Atajos de la ventana principal (F-21), comprobados sobre el XAML y el código.
/// </summary>
/// <remarks>
/// Hasta F-21 los cuatro aceleradores colgaban de <c>Content</c>: su tooltip saltaba al pasar el ratón por cualquier
/// parte de la ventana, así que se ocultó y los atajos se listaron en una línea fija de la cabecera. Colgados del control
/// de su acción, el tooltip solo aparece sobre ese control y la línea sobra.
/// </remarks>
public sealed class KeyboardShortcutTests
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    [Theory]
    [InlineData("btnConsultar", "F5", null)]
    [InlineData("btnCancelar", "Escape", null)]
    [InlineData("chkSelectAll", "A", "Control")]
    [InlineData("txtBuscar", "F", "Control")]
    [InlineData("lvPackages", "Delete", null)]
    public void MainWindow_DeclaresEachShortcutOnTheControlItTriggers(string control, string key, string? modifiers)
    {
        var owner = MainWindowXaml().Descendants()
            .Single(e => (string?)e.Attribute(Xaml + "Name") == control);

        var accelerators = owner.Elements()
            .Where(e => e.Name.LocalName.EndsWith(".KeyboardAccelerators", StringComparison.Ordinal))
            .SelectMany(e => e.Elements(Presentation + "KeyboardAccelerator"))
            .ToList();

        Assert.Contains(accelerators, a => (string?)a.Attribute("Key") == key && (string?)a.Attribute("Modifiers") == modifiers);
    }

    [Fact]
    public void NoAccelerator_HangsFromTheWholeWindow()
    {
        var offenders = Directory.EnumerateFiles(UiDirectory(), "*.cs")
            .Where(f => File.ReadAllText(f).Contains("Content.KeyboardAccelerator", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();

        Assert.True(offenders.Count == 0, "Aceleradores colgados de Content: " + string.Join(", ", offenders));
    }

    /// <summary>La línea fija de atajos de la cabecera se retiró: cada atajo se descubre en su control.</summary>
    [Fact]
    public void Header_NoLongerListsTheShortcuts()
    {
        Assert.DoesNotContain(MainWindowXaml().Descendants(), e => (string?)e.Attribute(Xaml + "Name") == "txtShortcuts");
        Assert.False(L.Map.ContainsKey("header.shortcuts"), "La clave header.shortcuts debería haberse retirado.");
    }

    private static XDocument MainWindowXaml() => XDocument.Load(Path.Combine(UiDirectory(), "MainWindow.xaml"));

    private static string UiDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WingetUSoft.slnx")))
            dir = dir.Parent;

        Assert.True(dir is not null, "No se encontró la raíz del repositorio (WingetUSoft.slnx) desde " + AppContext.BaseDirectory);
        return Path.Combine(dir!.FullName, "src", "WingetUSoft", "UI");
    }
}
