using System.Xml.Linq;
using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Acciones rápidas de la ventana principal (F-23): una sola acción de consulta y un único acento por ventana.
/// </summary>
/// <remarks>
/// Hasta F-23, «Consultar actualizaciones» y «Consultar con desconocidas» eran dos botones hermanos para la misma
/// acción, y el acento se quedaba en el primero también con la tabla llena, cuando lo que el usuario viene a hacer
/// ya es actualizar.
/// </remarks>
public sealed class QuickActionsTests
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void CheckingForUpdates_IsOneActionWithItsVariantInTheFlyout()
    {
        var main = MainWindowXaml();

        var split = main.Descendants(Presentation + "SplitButton")
            .SingleOrDefault(e => (string?)e.Attribute(Xaml + "Name") == "btnConsultar");

        Assert.True(split is not null, "btnConsultar debería ser un SplitButton.");
        Assert.Equal(2, split!.Descendants(Presentation + "RadioMenuFlyoutItem").Count());
        Assert.DoesNotContain(main.Descendants(), e => (string?)e.Attribute(Xaml + "Name") == "btnConsultarDesconocidas");
    }

    /// <summary>Como mucho un botón con acento por ventana, y en la principal lo decide el código.</summary>
    [Fact]
    public void NoView_ShowsMoreThanOneAccentButton()
    {
        var offenders = Views()
            .Select(v => (v.File, Count: v.Doc.Descendants()
                .Count(e => ((string?)e.Attribute("Style"))?.Contains("AccentButtonStyle", StringComparison.Ordinal) == true)))
            .Where(x => x.Count > 1)
            .Select(x => $"{x.File}: {x.Count}")
            .ToList();

        Assert.True(offenders.Count == 0, "Ventanas con más de un botón de acento: " + string.Join(", ", offenders));
    }

    /// <summary>En la principal el acento se mueve según el estado, así que no va escrito en el XAML.</summary>
    [Fact]
    public void MainWindow_LeavesTheAccentToTheCode()
    {
        Assert.DoesNotContain(MainWindowXaml().Descendants(),
            e => ((string?)e.Attribute("Style"))?.Contains("AccentButtonStyle", StringComparison.Ordinal) == true);

        string code = File.ReadAllText(Path.Combine(UiDirectory(), "MainWindow.xaml.cs"));
        Assert.Contains("AccentButtonStyle", code, StringComparison.Ordinal);
        Assert.Contains("DefaultButtonStyle", code, StringComparison.Ordinal);
    }

    private static XDocument MainWindowXaml() => XDocument.Load(Path.Combine(UiDirectory(), "MainWindow.xaml"));

    private static IEnumerable<(string File, XDocument Doc)> Views() =>
        Directory.EnumerateFiles(UiDirectory(), "*.xaml")
            .Select(f => (Path.GetFileName(f), XDocument.Load(f)));

    private static string UiDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WingetUSoft.slnx")))
            dir = dir.Parent;

        Assert.True(dir is not null, "No se encontró la raíz del repositorio (WingetUSoft.slnx) desde " + AppContext.BaseDirectory);
        return Path.Combine(dir!.FullName, "src", "WingetUSoft", "UI");
    }
}
