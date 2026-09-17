using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        ApplyBackdrop(window);

        if (window.Content is FrameworkElement root)
        {
            root.RequestedTheme = ToElementTheme(themeMode);
            // Loaded para el tema inicial (antes de cargar, ActualTheme aún no está resuelto) y
            // ActualThemeChanged para el cambio en caliente, tanto desde Configuración como desde Windows.
            root.Loaded += (_, _) => ApplyTitleBarTheme(appWindow, root);
            root.ActualThemeChanged += (_, _) =>
            {
                ApplyTitleBarTheme(appWindow, root);
                onThemeChanged?.Invoke();
            };
        }

        return new WindowHandles(appWindow, hWnd);
    }

    /// <summary>
    /// Botones de la barra de título (minimizar, maximizar, cerrar) en el tema que muestra la ventana (F-13).
    /// </summary>
    /// <remarks>
    /// Se usa el tema <b>resuelto</b> del contenido y no el ajuste: con «el del sistema», el ajuste no dice si
    /// la ventana está clara u oscura. Hasta F-13, <c>TitleBarHelper</c> fijaba a mano el blanco o el negro y
    /// los colores de hover, pulsado e inactivo, y cada ventana tenía que acordarse de llamarlo; desde Windows
    /// App SDK 1.7 el sistema los pinta con <c>PreferredTheme</c>.
    /// </remarks>
    private static void ApplyTitleBarTheme(AppWindow appWindow, FrameworkElement root) =>
        appWindow.TitleBar.PreferredTheme = root.ActualTheme == ElementTheme.Dark ? TitleBarTheme.Dark : TitleBarTheme.Light;

    /// <summary>
    /// Mica donde el sistema lo admite; el fondo sólido del XAML donde no (F-11).
    /// </summary>
    /// <remarks>
    /// Hasta F-11 se pedía Mica y la raíz de cada ventana lo tapaba entero con
    /// <c>ApplicationPageBackgroundThemeBrush</c>, que es opaco: los márgenes salían <c>#F3F3F3</c> / <c>#202020</c>
    /// planos. El XAML conserva ese pincel como reserva para Windows 10, que no tiene Mica (la app admite 19041);
    /// aquí solo se retira cuando Mica va a verse de verdad. Las tarjetas usan
    /// <c>CardBackgroundFillColorDefaultBrush</c>, que está pensado para ir encima.
    /// </remarks>
    private static void ApplyBackdrop(Window window)
    {
        if (!MicaController.IsSupported())
            return;

        window.SystemBackdrop = new MicaBackdrop();
        if (window.Content is Panel page)
            page.Background = new SolidColorBrush(Colors.Transparent);
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
