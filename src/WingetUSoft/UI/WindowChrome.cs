using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace WingetUSoft;

/// <summary>Handles nativos de una ventana ya vestida por <see cref="WindowChrome"/>.</summary>
/// <param name="AppWindow">La <see cref="Microsoft.UI.Windowing.AppWindow"/> asociada.</param>
/// <param name="Hwnd">El HWND Win32, que hace falta para el dimensionado por DPI.</param>
internal readonly record struct WindowHandles(AppWindow AppWindow, IntPtr Hwnd);

/// <summary>
/// El arranque común de las seis ventanas: icono, barra de título extendida, fondo Mica, tamaño por
/// DPI y tema.
/// </summary>
/// <remarks>
/// Estaba copiado en las seis, con las diferencias justas para no poder fiarse de ninguna: unas
/// aplicaban el tema antes de extender la barra y otras después, unas cualificaban <c>MicaBackdrop</c>
/// y otras no. Ese es el problema real de un bloque duplicado seis veces — no las líneas repetidas,
/// sino que cada copia deriva por su cuenta y ya nadie sabe cuál es la buena.
///
/// El tamaño de diseño y los mínimos **no** se unifican: son propios de cada ventana (la principal
/// necesita 1180×820, la de configuración 760×560) y meterlos aquí solo trasladaría la divergencia.
/// </remarks>
internal static class WindowChrome
{
    /// <summary>
    /// Viste la ventana y devuelve sus handles.
    /// </summary>
    /// <param name="window">La ventana que se está construyendo.</param>
    /// <param name="titleBar">Elemento del XAML que hace de barra de título (<c>AppTitleBar</c>).</param>
    /// <param name="themeMode">0 = el del sistema, 1 = claro, 2 = oscuro.</param>
    /// <param name="onThemeChanged">
    /// Trabajo extra al cambiar el tema, además de recolorear los botones de la barra. La ventana
    /// principal lo usa para repintar el registro, cuyos colores dependen del tema.
    /// </param>
    internal static WindowHandles Apply(
        Window window,
        UIElement titleBar,
        int themeMode,
        int designWidthDip,
        int designHeightDip,
        int minWidthDip,
        int minHeightDip,
        Action? onThemeChanged = null)
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(window);
        AppWindow appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hWnd));

        appWindow.SetIcon(IconPath);
        WindowSizer.Apply(appWindow, hWnd, designWidthDip, designHeightDip, minWidthDip, minHeightDip);

        window.ExtendsContentIntoTitleBar = true;
        window.SetTitleBar(titleBar);
        window.SystemBackdrop = new MicaBackdrop();

        if (window.Content is FrameworkElement root)
        {
            root.RequestedTheme = ToElementTheme(themeMode);
            root.ActualThemeChanged += (_, _) =>
            {
                TitleBarHelper.UpdateButtonColors(appWindow, window.Content, themeMode);
                onThemeChanged?.Invoke();
            };
        }

        return new WindowHandles(appWindow, hWnd);
    }

    /// <summary>Ruta del icono de la aplicación, junto al ejecutable.</summary>
    internal static string IconPath =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");

    /// <summary>Traduce el ajuste de tema (0/1/2) al del árbol visual.</summary>
    internal static ElementTheme ToElementTheme(int themeMode) => themeMode switch
    {
        1 => ElementTheme.Light,
        2 => ElementTheme.Dark,
        _ => ElementTheme.Default
    };
}
