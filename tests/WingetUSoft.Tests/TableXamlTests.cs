using System.Xml.Linq;
using Windows.UI;
using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Las tablas de la app (un <c>ListView</c> con cabecera de columnas propia), comprobadas sobre su XAML.
/// </summary>
/// <remarks>
/// Sobre el XAML y no conduciendo la app porque las filas de casi todas las tablas dependen de winget
/// (consultar actualizaciones, listar lo instalado), y los UI tests no dependen de winget ni de la red.
/// </remarks>
public sealed class TableXamlTests
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>
    /// F-07: el <c>ListViewItem</c> trae <c>Padding="16,0,12,0"</c> por defecto y las cabeceras no lo replican.
    /// </summary>
    /// <remarks>
    /// Medido en las capturas de la app real el 2026-09-14: la primera columna quedaba 16 px a la derecha de su
    /// cabecera y el resto 12 px a la izquierda, en cuatro de las cinco tablas. La quinta (Limpieza) ya anulaba
    /// el padding, y por eso era la única alineada.
    /// </remarks>
    [Fact]
    public void EveryTable_RemovesTheDefaultItemPadding()
    {
        var tables = UiXaml()
            .SelectMany(x => x.Doc.Descendants(Presentation + "ListView")
                .Where(lv => lv.Element(Presentation + "ListView.ItemTemplate") is not null)
                .Select(lv => (x.File, ListView: lv)))
            .ToList();

        Assert.True(tables.Count >= 5, $"Solo se encontraron {tables.Count} tablas.");

        // Desde F-14 el estilo vive en Styles.xaml y cada tabla lo referencia.
        var rowContainer = XDocument.Load(Path.Combine(Path.GetDirectoryName(UiXaml().First().Path)!, "Styles.xaml"))
            .Descendants(Presentation + "Style")
            .Single(s => (string?)s.Attribute(Xaml + "Key") == "TableRowContainerStyle");
        Assert.Contains(rowContainer.Elements(Presentation + "Setter"), s =>
            (string?)s.Attribute("Property") == "Padding" && (string?)s.Attribute("Value") == "0");

        var misaligned = tables
            .Where(t => (string?)t.ListView.Attribute("ItemContainerStyle") != "{StaticResource TableRowContainerStyle}")
            .Select(t => $"{t.File} ({(string?)t.ListView.Attribute(Xaml + "Name")})")
            .ToList();

        Assert.True(
            misaligned.Count == 0,
            "Tablas que conservan el padding por defecto del ListViewItem (celdas desalineadas con su cabecera): "
                + string.Join(", ", misaligned));
    }

    /// <summary>
    /// F-06: una fila de paquete excluido u omitido se atenúa con el gris secundario del tema, no con
    /// <c>Opacity</c>.
    /// </summary>
    /// <remarks>
    /// Con <c>Opacity 0.4</c> sobre la fila entera, el texto caía a 2,50:1 en claro y 3,59:1 en oscuro, por
    /// debajo del 4,5:1 de WCAG AA que el proyecto ya exige al registro de actividad.
    /// </remarks>
    [Fact]
    public void MainTable_DimsRowsWithTheSecondaryTextStyle_NotWithOpacity()
    {
        var doc = UiXaml().Single(x => x.File == "MainWindow.xaml").Doc;

        var table = doc.Descendants(Presentation + "ListView").Single(lv => (string?)lv.Attribute(Xaml + "Name") == "lvPackages");
        var row = table.Element(Presentation + "ListView.ItemTemplate")!.Element(Presentation + "DataTemplate")!.Elements().First();

        Assert.Null(row.Attribute("Opacity"));

        var cells = row.Elements(Presentation + "TextBlock").ToList();
        Assert.Equal(5, cells.Count);
        Assert.All(cells, cell => Assert.Contains("PackageCellStyleConverter", (string?)cell.Attribute("Style") ?? ""));

        var dimmedStyle = doc.Descendants(Presentation + "Style")
            .Single(s => (string?)s.Attribute(Xaml + "Key") == "DimmedPackageCellTextStyle");
        Assert.Contains(dimmedStyle.Elements(Presentation + "Setter"), s =>
            (string?)s.Attribute("Property") == "Foreground"
            && (string?)s.Attribute("Value") == "{ThemeResource TextFillColorSecondaryBrush}");
    }

    /// <summary>
    /// F-06: el gris secundario de WinUI llega a AA sobre la tarjeta, en los dos temas.
    /// </summary>
    /// <remarks>
    /// <c>TextFillColorSecondaryBrush</c> vale <c>#9E000000</c> en claro y <c>#C5FFFFFF</c> en oscuro; se
    /// compone sobre el mismo fondo de tarjeta contra el que <see cref="LogPaletteTests"/> mide el registro.
    /// </remarks>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SecondaryText_MeetsWcagAa_OnTheCard(bool darkTheme)
    {
        Color secondary = darkTheme
            ? Color.FromArgb(0xC5, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0x9E, 0x00, 0x00, 0x00);
        Color card = LogPalette.Background(darkTheme);

        double ratio = LogPalette.ContrastRatio(Over(secondary, card), card);

        Assert.True(ratio >= 4.5, $"Texto atenuado a {ratio:F2}:1 en tema {(darkTheme ? "oscuro" : "claro")}.");
    }

    /// <summary>
    /// F-10: el panel de información del paquete no se pliega ni se despliega, así que seleccionar una fila
    /// no mueve la tabla.
    /// </summary>
    /// <remarks>
    /// Pasaba de <c>Collapsed</c> a <c>Visible</c> al seleccionar, y la tabla bajaba 36 px (medido en la app
    /// real el 2026-09-14). Se comprueban las dos formas de volver a plegarlo: en el XAML y desde el código.
    /// </remarks>
    [Fact]
    public void PackageInfoPanel_KeepsItsSpace()
    {
        var panel = UiXaml().Single(x => x.File == "MainWindow.xaml").Doc
            .Descendants().Single(e => (string?)e.Attribute(Xaml + "Name") == "panelPackageInfo");

        Assert.Null(panel.Attribute("Visibility"));
        Assert.NotNull(panel.Attribute("MinHeight"));

        string ui = Path.GetDirectoryName(UiXaml().First().Path)!;
        var toggles = Directory.EnumerateFiles(ui, "*.cs")
            .Where(f => File.ReadAllText(f).Contains("panelPackageInfo.Visibility", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();
        Assert.True(toggles.Count == 0, "Código que vuelve a plegar el panel de información: " + string.Join(", ", toggles));
    }

    /// <summary>
    /// F-01: la página mide lo mismo que su viewport (<c>Height</c>, no solo <c>MinHeight</c>), así que la tabla
    /// recibe un alto finito y desplaza por sí misma; el mínimo, por debajo del cual desplaza la página, lo
    /// calcula el código.
    /// </summary>
    /// <remarks>
    /// Con <c>MinHeight</c> atado al viewport y sin <c>Height</c>, el <c>ScrollViewer</c> medía con alto infinito:
    /// con 26 actualizaciones a 1180×820 la lista no desplazaba (vista al 100 %), la página sí (44,7 %) y el
    /// registro quedaba fuera de la ventana (medido el 2026-09-14).
    /// </remarks>
    [Fact]
    public void MainPage_FillsTheViewport_SoTheTableScrollsByItself()
    {
        var grid = UiXaml().Single(x => x.File == "MainWindow.xaml").Doc
            .Descendants(Presentation + "Grid").Single(g => (string?)g.Attribute(Xaml + "Name") == "ContentGrid");

        Assert.Equal(Presentation + "ScrollViewer", grid.Parent!.Name);
        Assert.Equal("{Binding ViewportHeight, ElementName=ContentScroller}", (string?)grid.Attribute("Height"));
        Assert.Null(grid.Attribute("MinHeight"));

        string code = File.ReadAllText(Path.Combine(Path.GetDirectoryName(UiXaml().First().Path)!, "MainWindow.xaml.cs"));
        Assert.Contains("ContentGrid.MinHeight =", code, StringComparison.Ordinal);
        Assert.Contains("TrackContentMinHeight();", code, StringComparison.Ordinal);
    }

    private static Color Over(Color foreground, Color background)
    {
        double a = foreground.A / 255.0;
        byte Mix(byte f, byte b) => (byte)Math.Round((f * a) + (b * (1 - a)));
        return Color.FromArgb(255, Mix(foreground.R, background.R), Mix(foreground.G, background.G), Mix(foreground.B, background.B));
    }

    private static IEnumerable<(string File, string Path, XDocument Doc)> UiXaml()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WingetUSoft.slnx")))
            dir = dir.Parent;

        Assert.True(dir is not null, "No se encontró la raíz del repositorio (WingetUSoft.slnx) desde " + AppContext.BaseDirectory);

        return Directory.EnumerateFiles(Path.Combine(dir!.FullName, "src", "WingetUSoft", "UI"), "*.xaml")
            .Select(f => (Path.GetFileName(f), f, XDocument.Load(f)));
    }
}
