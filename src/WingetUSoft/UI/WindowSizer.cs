using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace WingetUSoft;

/// <summary>
/// Wrapper delgado reutilizado por las 5 ventanas de la app: calcula el tamaño/posición según el
/// DPI del monitor y el área de trabajo mediante <see cref="WindowSizing.ComputeSizeAndCenter"/>
/// (matemática pura en <c>Core/WindowSizing.cs</c>) y aplica también un tamaño mínimo escalado.
/// Esta clase solo hace de puente con las APIs de WinUI/Win32 (P/Invoke + <see cref="AppWindow"/>).
/// </summary>
internal static class WindowSizer
{
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    /// <summary>
    /// Redimensiona y centra <paramref name="appWindow"/> según el DPI del monitor, acotado a su
    /// área de trabajo, y aplica un tamaño mínimo (también escalado por DPI) sin fijar la ventana
    /// (sigue siendo redimensionable/maximizable).
    /// </summary>
    /// <param name="appWindow">Ventana de la aplicación a redimensionar/centrar.</param>
    /// <param name="hwnd">Handle Win32 de la ventana (para consultar el DPI).</param>
    /// <param name="designWidthDip">Ancho de diseño en DIP.</param>
    /// <param name="designHeightDip">Alto de diseño en DIP.</param>
    /// <param name="minWidthDip">Ancho mínimo en DIP.</param>
    /// <param name="minHeightDip">Alto mínimo en DIP.</param>
    public static void Apply(AppWindow appWindow, IntPtr hwnd, int designWidthDip, int designHeightDip, int minWidthDip, int minHeightDip)
    {
        uint dpi = GetDpiForWindow(hwnd);
        double scale = dpi > 0 ? dpi / 96.0 : 1.0;

        var work = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
        WindowBounds b = WindowSizing.ComputeSizeAndCenter(
            designWidthDip, designHeightDip, scale,
            work.X, work.Y, work.Width, work.Height, marginDip: 16);

        appWindow.Resize(new SizeInt32(b.Width, b.Height));
        appWindow.Move(new PointInt32(b.X, b.Y));

        if (appWindow.Presenter is OverlappedPresenter p)
        {
            var (minW, minH) = WindowSizing.ScaleMinSize(
                minWidthDip, minHeightDip, scale, work.Width, work.Height, marginDip: 16);
            p.PreferredMinimumWidth = minW;
            p.PreferredMinimumHeight = minH;
        }
    }
}
