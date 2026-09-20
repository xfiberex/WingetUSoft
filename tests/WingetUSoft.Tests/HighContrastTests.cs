using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Temas de contraste de Windows (F-24): lo que la prueba en «Acuático» y «Desierto» destapó y los tests unitarios no
/// pueden ver solos. La paleta del registro la cubre <see cref="LogPaletteTests"/>.
/// </summary>
public sealed class HighContrastTests
{
    /// <summary>
    /// Sin placas detrás del texto: con contraste alto, WinUI pintaba el icono y el texto de «Actualizar todo» dentro
    /// de un recuadro del color de la ventana sobre el botón cian del acento. Puesto como atributo en <c>App.xaml</c>
    /// no llegaba a aplicarse, así que va por código.
    /// </summary>
    [Fact]
    public void App_DisablesTheTextBackplate()
    {
        string code = File.ReadAllText(Path.Combine(SourceDirectory(), "App.xaml.cs"));

        Assert.Contains("HighContrastAdjustment = ApplicationHighContrastAdjustment.None;", code, StringComparison.Ordinal);
    }

    /// <summary>
    /// Con contraste alto no hay Mica: su color de reserva es un gris que no es el de la paleta del usuario. La página
    /// recupera el pincel del sistema, y la decisión se repite cuando el usuario activa o desactiva el contraste.
    /// </summary>
    [Fact]
    public void WindowChrome_DropsMica_UnderHighContrast()
    {
        string code = File.ReadAllText(Path.Combine(SourceDirectory(), "UI", "WindowChrome.cs"));

        Assert.Contains("if (ContrastMode.IsActive)", code, StringComparison.Ordinal);
        Assert.Contains("window.SystemBackdrop = null;", code, StringComparison.Ordinal);
        Assert.Contains("\"ApplicationPageBackgroundThemeBrush\"", code, StringComparison.Ordinal);
        Assert.Contains("ContrastMode.Changed += onChanged;", code, StringComparison.Ordinal);
        Assert.Contains("ContrastMode.Changed -= onChanged", code, StringComparison.Ordinal);
    }

    private static string SourceDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WingetUSoft.slnx")))
            dir = dir.Parent;

        Assert.True(dir is not null, "No se encontró la raíz del repositorio (WingetUSoft.slnx) desde " + AppContext.BaseDirectory);
        return Path.Combine(dir!.FullName, "src", "WingetUSoft");
    }
}
