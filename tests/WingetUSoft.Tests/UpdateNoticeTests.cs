using System.Xml.Linq;
using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Aviso de versión nueva de la ventana principal (F-22).
/// </summary>
/// <remarks>
/// Hasta F-22 el aviso vivía dentro de la tarjeta de cabecera y su mensaje cargaba hasta 500 caracteres de
/// changelog: una <c>InfoBar</c> está pensada para un estado breve, no para un muro de texto.
/// </remarks>
public sealed class UpdateNoticeTests
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void UpdateNotice_SitsAtTheTopOfThePage_NotInsideTheHeaderCard()
    {
        var bar = MainWindowXaml().Descendants(Presentation + "InfoBar")
            .Single(e => (string?)e.Attribute(Xaml + "Name") == "infoBarUpdate");

        Assert.Equal("ContentGrid", (string?)bar.Parent?.Attribute(Xaml + "Name"));
        Assert.Equal("0", (string?)bar.Attribute("Grid.Row"));
    }

    /// <summary>El changelog queda a un clic, y el mensaje del aviso se queda en una línea.</summary>
    [Fact]
    public void UpdateNotice_LinksToTheChangelog_InsteadOfInliningIt()
    {
        var bar = MainWindowXaml().Descendants(Presentation + "InfoBar")
            .Single(e => (string?)e.Attribute(Xaml + "Name") == "infoBarUpdate");

        Assert.Contains(bar.Descendants(Presentation + "HyperlinkButton"),
            e => (string?)e.Attribute(Xaml + "Name") == "lnkVerNovedades");

        string code = File.ReadAllText(Path.Combine(UiDirectory(), "MainWindow.AppUpdate.cs"));
        Assert.DoesNotContain("infoBarUpdate.Message = BuildChangelogMessage", code, StringComparison.Ordinal);
        Assert.Contains("new WhatsNewDialog", code, StringComparison.Ordinal);
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
