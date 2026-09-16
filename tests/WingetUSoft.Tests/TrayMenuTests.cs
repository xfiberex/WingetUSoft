using System.Text.RegularExpressions;
using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Menú del icono de bandeja (F-09), comprobado sobre el código fuente.
/// </summary>
/// <remarks>
/// El menú nativo de la bandeja queda fuera del alcance de los UI tests (vive en el área de notificación del
/// shell, no en el árbol de la app), así que lo que se fija aquí son las tres cosas que lo hacen funcionar; el
/// clic real se verifica a mano.
/// </remarks>
public sealed class TrayMenuTests
{
    private static readonly string Tray = File.ReadAllText(Path.Combine(UiDirectory(), "MainWindow.Tray.cs"));

    /// <summary>
    /// Con «Minimizar a la bandeja al cerrar» activo, cerrar oculta la ventana: sin una entrada «Salir» no
    /// quedaba ninguna forma de terminar la app desde la interfaz.
    /// </summary>
    [Fact]
    public void TrayMenu_OffersAWayToExit()
    {
        Assert.Contains("ContextFlyout = BuildTrayMenu()", Tray, StringComparison.Ordinal);
        Assert.Matches(@"L\.T\(""tray\.exit""\)[^;]*Command\s*=\s*TrayCommand\(ExitFromTray\)", Tray);
    }

    /// <summary>
    /// H.NotifyIcon convierte el menú en uno nativo y de cada entrada solo ejecuta <c>Command</c>: un
    /// manejador de <c>Click</c> compilaría y no se ejecutaría nunca.
    /// </summary>
    [Fact]
    public void TrayMenuItems_UseCommands_NotClickHandlers()
    {
        string menu = Regex.Match(Tray, @"private MenuFlyout BuildTrayMenu\(\)\s*\{(?<body>.*?)\n    \}", RegexOptions.Singleline)
            .Groups["body"].Value;

        Assert.NotEmpty(menu);
        Assert.DoesNotContain("Click", menu, StringComparison.Ordinal);
        Assert.Equal(3, Regex.Matches(menu, @"Command\s*=\s*TrayCommand\(").Count);
    }

    /// <summary>
    /// El icono se crea desde código, fuera del árbol XAML: sin <c>ForceCreate</c> nunca se registra en el área de
    /// notificación, y cerrar la ventana dejaba el proceso vivo sin icono al que volver.
    /// </summary>
    [Fact]
    public void TrayIcon_IsForceCreated_WithoutEfficiencyMode() =>
        Assert.Matches(@"_trayIcon\.ForceCreate\(\s*enablesEfficiencyMode:\s*false\s*\)", Tray);

    /// <summary>«Salir» tiene que saltarse la intercepción del cierre, o volvería a esconder la ventana.</summary>
    [Fact]
    public void Closing_HidesToTray_OnlyWhenExitWasNotRequested() =>
        Assert.Matches(@"if\s*\(\s*_settings\.MinimizeToTray\s*&&\s*!_exitRequested\s*\)", Tray);

    private static string UiDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WingetUSoft.slnx")))
            dir = dir.Parent;

        Assert.True(dir is not null, "No se encontró la raíz del repositorio (WingetUSoft.slnx) desde " + AppContext.BaseDirectory);
        return Path.Combine(dir!.FullName, "src", "WingetUSoft", "UI");
    }
}
