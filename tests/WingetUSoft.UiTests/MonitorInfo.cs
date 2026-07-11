using System.Runtime.InteropServices;

namespace WingetUSoft.UiTests;

/// <summary>
/// P/Invoke de Win32 compartido para consultar el área de trabajo (<c>WorkArea</c>) del monitor bajo
/// una ventana, en píxeles físicos. Extraído de <see cref="LayoutTests"/> (que ya lo usaba para
/// <c>MainWindow_FitsWithinMonitorWorkArea</c>) para que <c>SnapLayoutTests</c> -- la regresión de
/// snap layouts de Windows 11, ROADMAP.md Tier B #7 -- lo reutilice sin duplicar el P/Invoke.
///
/// Requiere que el proceso de test sea PerMonitorV2 DPI-aware (ver <see cref="DpiAwareness"/>, en
/// <c>LayoutTests.cs</c>) para que estas coordenadas físicas casen con el <c>BoundingRectangle</c> que
/// reporta UI Automation para la ventana de la app.
/// </summary>
internal static class MonitorInfoHelper
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr MonitorFromRect(ref RECT lprc, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    /// <summary>
    /// Área de trabajo (en píxeles físicos) del monitor más cercano al rectángulo <paramref name="bounds"/>
    /// (normalmente el <c>BoundingRectangle</c> de la ventana bajo prueba).
    /// </summary>
    internal static RECT GetWorkArea(RECT bounds)
    {
        var rect = bounds;
        var hMonitor = MonitorFromRect(ref rect, MONITOR_DEFAULTTONEAREST);
        if (hMonitor == IntPtr.Zero)
            throw new InvalidOperationException("MonitorFromRect no encontró un monitor para el rectángulo dado.");

        var info = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(hMonitor, ref info))
            throw new InvalidOperationException("GetMonitorInfo falló al consultar el área de trabajo del monitor.");

        return info.rcWork;
    }
}
