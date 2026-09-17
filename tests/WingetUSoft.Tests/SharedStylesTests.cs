using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Estilos compartidos de <c>UI/Styles.xaml</c> (F-14), comprobados sobre el XAML.
/// </summary>
/// <remarks>
/// Hasta F-14 el bloque de tarjeta se repetía 30 veces con cinco rellenos distintos, los radios iban como
/// literales y los espaciados mezclaban 3, 6 y 10 px. Estos tests no miden el aspecto (eso lo hacen las capturas),
/// sino que impiden que la repetición vuelva a entrar.
/// </remarks>
public sealed class SharedStylesTests
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>Los dos diccionarios que definen estilos a propósito: el compartido y el del botón de peligro.</summary>
    private static readonly string[] StyleDictionaries = ["Styles.xaml", "DangerButtonResources.xaml"];

    [Fact]
    public void App_MergesTheSharedStyles()
    {
        var app = XDocument.Load(Path.Combine(UiDirectory(), "..", "App.xaml"));

        Assert.Contains(app.Descendants(Presentation + "ResourceDictionary"),
            d => (string?)d.Attribute("Source") == "ms-appx:///UI/Styles.xaml");
    }

    /// <summary>Ninguna ventana repite el bloque de tarjeta: lo trae <c>CardStyle</c> o una de sus variantes.</summary>
    [Fact]
    public void NoWindow_RepeatsTheCardBlock()
    {
        var inline = Views()
            .SelectMany(v => v.Doc.Descendants(Presentation + "Border")
                .Where(b => ((string?)b.Attribute("Background"))?.Contains("CardBackgroundFillColor", StringComparison.Ordinal) == true)
                .Select(b => $"{v.File}:{LineOf(b)}"))
            .ToList();

        var cards = Views().Sum(v => v.Doc.Descendants(Presentation + "Border")
            .Count(b => ((string?)b.Attribute("Style"))?.EndsWith("CardStyle}", StringComparison.Ordinal) == true));

        Assert.True(inline.Count == 0, "Tarjetas con el bloque repetido en vez de CardStyle: " + string.Join(", ", inline));
        Assert.True(cards >= 30, $"Solo {cards} tarjetas usan CardStyle: el escaneo no está viendo las ventanas.");
    }

    /// <summary>Los radios salen de <c>OverlayCornerRadius</c> y <c>ControlCornerRadius</c>, no de un 8 o un 4 escritos a mano.</summary>
    [Fact]
    public void NoView_UsesLiteralCornerRadius()
    {
        var literals = Views()
            .SelectMany(v => v.Doc.Descendants()
                .Where(e => e.Attribute("CornerRadius") is { } radius && !radius.Value.StartsWith('{'))
                .Select(e => $"{v.File}:{LineOf(e)} CornerRadius=\"{(string?)e.Attribute("CornerRadius")}\""))
            .ToList();

        Assert.True(literals.Count == 0, "CornerRadius literales: " + string.Join(", ", literals));
    }

    /// <summary>Márgenes, rellenos y separaciones en la rampa de 4 px de Fluent.</summary>
    [Fact]
    public void Spacing_StaysOnTheFourPixelRamp()
    {
        var spacing = new Regex(@"^-?\d+(\s*,\s*-?\d+)*$");
        string[] properties = ["Margin", "Padding", "Spacing", "HorizontalSpacing", "VerticalSpacing"];

        var offRamp = Views(includeStyleDictionaries: true)
            .SelectMany(v => v.Doc.Descendants().SelectMany(e => properties
                .Select(p => (Element: e, Property: p, Value: (string?)e.Attribute(p)))
                .Concat(e.Name == Presentation + "Setter"
                    ? [(Element: e, Property: (string?)e.Attribute("Property") ?? "", Value: (string?)e.Attribute("Value"))]
                    : [])
                .Where(x => properties.Contains(x.Property) && x.Value is not null && spacing.IsMatch(x.Value))
                .Where(x => x.Value!.Split(',').Select(n => int.Parse(n.Trim(), System.Globalization.CultureInfo.InvariantCulture)).Any(n => n % 4 != 0))
                .Select(x => $"{v.File}:{LineOf(x.Element)} {x.Property}=\"{x.Value}\"")))
            .ToList();

        Assert.True(offRamp.Count == 0, "Espaciados fuera de la rampa de 4 px: " + string.Join(", ", offRamp));
    }

    private static int LineOf(XElement element) => ((System.Xml.IXmlLineInfo)element).LineNumber;

    private static IEnumerable<(string File, XDocument Doc)> Views(bool includeStyleDictionaries = false) =>
        Directory.EnumerateFiles(UiDirectory(), "*.xaml")
            .Where(f => includeStyleDictionaries || !StyleDictionaries.Contains(Path.GetFileName(f)))
            .Select(f => (Path.GetFileName(f), XDocument.Load(f, LoadOptions.SetLineInfo)));

    private static string UiDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WingetUSoft.slnx")))
            dir = dir.Parent;

        Assert.True(dir is not null, "No se encontró la raíz del repositorio (WingetUSoft.slnx) desde " + AppContext.BaseDirectory);
        return Path.Combine(dir!.FullName, "src", "WingetUSoft", "UI");
    }
}
