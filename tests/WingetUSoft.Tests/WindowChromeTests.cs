using System.Xml.Linq;
using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// El arranque común de las ventanas (<c>WindowChrome</c>): fondo Mica (F-11) y barra de título (F-13).
/// </summary>
/// <remarks>
/// Sobre el código y el XAML: Mica y los botones de la barra de título los dibuja el sistema, fuera del árbol
/// de automatización. El resultado se mide en pantalla, y aquí se fija la regla que lo produce.
/// </remarks>
public sealed class WindowChromeTests
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    /// <summary>
    /// F-11: la raíz de cada ventana conserva el fondo sólido en el XAML, y solo se retira cuando hay Mica.
    /// </summary>
    /// <remarks>
    /// Sin la retirada, el pincel opaco tapaba Mica entero (márgenes <c>#F3F3F3</c> / <c>#202020</c> planos). Sin el
    /// pincel, Windows 10 —que no tiene Mica y la app admite— se quedaría sin fondo.
    /// </remarks>
    [Fact]
    public void Mica_ReplacesTheSolidBackground_OnlyWhereItIsSupported()
    {
        var windows = Xaml()
            .Where(x => x.Doc.Root!.Name == Presentation + "Window")
            .ToList();

        Assert.True(windows.Count >= 6, $"Solo se encontraron {windows.Count} ventanas.");
        Assert.All(windows, w => Assert.Equal(
            "{ThemeResource ApplicationPageBackgroundThemeBrush}",
            (string?)w.Doc.Root!.Elements().First(e => e.Name.LocalName != "Window.Resources").Attribute("Background")));

        string chrome = Code("WindowChrome.cs");
        int guard = chrome.IndexOf("if (!MicaController.IsSupported())", StringComparison.Ordinal);
        Assert.True(guard >= 0, "WindowChrome no comprueba MicaController.IsSupported().");
        Assert.True(chrome.IndexOf("new MicaBackdrop()", StringComparison.Ordinal) > guard, "Mica se pide antes de comprobar si hay soporte.");
        Assert.True(chrome.IndexOf("Colors.Transparent", StringComparison.Ordinal) > guard, "El fondo sólido se retira sin comprobar si hay Mica.");
    }

    /// <summary>
    /// F-13: los botones de la barra de título los pinta el sistema con <c>PreferredTheme</c>, desde un único sitio.
    /// </summary>
    /// <remarks>
    /// Hasta F-13, <c>TitleBarHelper</c> fijaba a mano blanco o negro y los colores de hover, pulsado e inactivo, y
    /// cada una de las seis ventanas tenía que acordarse de llamarlo.
    /// </remarks>
    [Fact]
    public void TitleBarButtons_FollowTheTheme_ThroughPreferredTheme()
    {
        string chrome = Code("WindowChrome.cs");
        Assert.Contains("TitleBar.PreferredTheme =", chrome, StringComparison.Ordinal);
        Assert.Contains("root.ActualThemeChanged +=", chrome, StringComparison.Ordinal);

        var manualColors = Directory.EnumerateFiles(UiDirectory(), "*.cs")
            .Where(f => File.ReadAllText(f).Contains("TitleBar.Button", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();

        Assert.False(File.Exists(Path.Combine(UiDirectory(), "TitleBarHelper.cs")), "TitleBarHelper.cs ha vuelto.");
        Assert.True(manualColors.Count == 0, "Colores de la barra de título fijados a mano en: " + string.Join(", ", manualColors));
    }

    private static string Code(string file) => File.ReadAllText(Path.Combine(UiDirectory(), file));

    private static IEnumerable<(string File, XDocument Doc)> Xaml() =>
        Directory.EnumerateFiles(UiDirectory(), "*.xaml").Select(f => (Path.GetFileName(f), XDocument.Load(f)));

    private static string UiDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WingetUSoft.slnx")))
            dir = dir.Parent;

        Assert.True(dir is not null, "No se encontró la raíz del repositorio (WingetUSoft.slnx) desde " + AppContext.BaseDirectory);
        return Path.Combine(dir!.FullName, "src", "WingetUSoft", "UI");
    }
}
