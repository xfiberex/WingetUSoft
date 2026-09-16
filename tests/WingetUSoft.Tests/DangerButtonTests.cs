using System.Globalization;
using System.Xml.Linq;
using Windows.UI;
using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Botón de peligro de Desinstalar y Eliminar (F-05), comprobado sobre su diccionario y el XAML de las ventanas.
/// </summary>
public sealed class DangerButtonTests
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    private const string DictionarySource = "ms-appx:///UI/DangerButtonResources.xaml";

    /// <summary>Reposo, hover y pulsado llegan al 4,5:1 de WCAG AA en los dos temas.</summary>
    /// <remarks>
    /// Hasta F-05, blanco sobre <c>#D55C4C</c> (oscuro, reposo) daba 3,84:1 y sobre <c>#E06858</c> (hover) 3,34:1,
    /// y el estado pulsado no se definía.
    /// </remarks>
    [Theory]
    [InlineData("Light", "")]
    [InlineData("Light", "PointerOver")]
    [InlineData("Light", "Pressed")]
    [InlineData("Dark", "")]
    [InlineData("Dark", "PointerOver")]
    [InlineData("Dark", "Pressed")]
    public void EveryState_MeetsWcagAa(string theme, string state)
    {
        var brushes = ThemeBrushes(theme);

        Color background = brushes["ButtonBackground" + state];
        Color foreground = brushes["ButtonForeground" + state];
        double ratio = LogPalette.ContrastRatio(foreground, background);

        Assert.True(ratio >= 4.5, $"{theme} {(state.Length == 0 ? "reposo" : state)}: {ratio:F2}:1.");
    }

    /// <summary>
    /// En alto contraste el botón usa los colores del sistema y no un rojo fijo, que ignoraría la paleta del usuario.
    /// </summary>
    [Fact]
    public void HighContrast_UsesSystemColors()
    {
        var highContrast = ThemeDictionary("HighContrast");

        var keys = highContrast.Elements().Select(e => (string?)e.Attribute(Xaml + "Key")).ToList();
        Assert.Contains("ButtonBackground", keys);
        Assert.Contains("ButtonBackgroundPressed", keys);
        Assert.All(highContrast.Elements(), e =>
        {
            Assert.Equal("StaticResource", e.Name.LocalName);
            Assert.StartsWith("SystemColor", (string?)e.Attribute("ResourceKey"), StringComparison.Ordinal);
        });
    }

    /// <summary>
    /// Las dos acciones destructivas comparten el diccionario, llevan la papelera (no dependen solo del color)
    /// y su botón vecino ya no lleva el acento, que con un acento rojo en Windows se confundía con el peligro.
    /// </summary>
    [Theory]
    [InlineData("UninstallWindow.xaml", "btnUninstall", "btnRefresh")]
    [InlineData("CleanupWindow.xaml", "btnEliminar", "btnEscanear")]
    public void DestructiveAction_IsMarkedBeyondColor_AndStandsAlone(string file, string dangerButton, string neighbour)
    {
        var doc = XDocument.Load(Path.Combine(UiDirectory(), file));
        XElement Button(string name) => doc.Descendants(Presentation + "Button").Single(b => (string?)b.Attribute(Xaml + "Name") == name);

        var danger = Button(dangerButton);
        Assert.Contains(danger.Descendants(Presentation + "ResourceDictionary"), d => (string?)d.Attribute("Source") == DictionarySource);
        Assert.Contains(danger.Descendants(Presentation + "FontIcon"), i => (string?)i.Attribute("Glyph") == "");

        Assert.Null(Button(neighbour).Attribute("Style"));
    }

    /// <summary>Ninguna ventana vuelve a cablear colores sueltos: van en diccionarios con variante por tema.</summary>
    [Fact]
    public void NoWindow_DeclaresLiteralBrushColors()
    {
        var offenders = Directory.EnumerateFiles(UiDirectory(), "*.xaml")
            .Select(f => (File: Path.GetFileName(f), Doc: XDocument.Load(f)))
            .Where(x => x.Doc.Root!.Name == Presentation + "Window")
            .SelectMany(x => x.Doc.Descendants(Presentation + "SolidColorBrush")
                .Where(b => b.Attribute("Color") is { } color && !color.Value.StartsWith('{'))
                .Select(b => $"{x.File}: {(string?)b.Attribute(Xaml + "Key")} = {(string?)b.Attribute("Color")}"))
            .ToList();

        Assert.True(offenders.Count == 0, "Colores literales en ventanas: " + string.Join(", ", offenders));
    }

    private static Dictionary<string, Color> ThemeBrushes(string theme) =>
        ThemeDictionary(theme).Elements(Presentation + "SolidColorBrush")
            .ToDictionary(b => (string)b.Attribute(Xaml + "Key")!, b => ParseColor((string)b.Attribute("Color")!));

    private static XElement ThemeDictionary(string theme) =>
        XDocument.Load(Path.Combine(UiDirectory(), "DangerButtonResources.xaml")).Root!
            .Element(Presentation + "ResourceDictionary.ThemeDictionaries")!
            .Elements(Presentation + "ResourceDictionary")
            .Single(d => (string?)d.Attribute(Xaml + "Key") == theme);

    private static Color ParseColor(string argb)
    {
        uint value = uint.Parse(argb.TrimStart('#'), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return Color.FromArgb((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value);
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
