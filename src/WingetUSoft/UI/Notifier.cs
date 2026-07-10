using System.Runtime.InteropServices;

namespace WingetUSoft;

/// <summary>
/// Aviso al usuario al terminar una operación larga: reproduce un sonido del sistema y hace parpadear
/// el botón de la app en la barra de tareas, pero solo cuando la ventana <b>no</b> está en primer plano
/// (si el usuario ya está mirando, no molesta). Vía Win32 (<c>user32.dll</c>).
/// </summary>
public static class Notifier
{
    private const uint FLASHW_TRAY      = 0x00000002;  // parpadear el botón de la barra de tareas
    private const uint FLASHW_TIMERNOFG = 0x0000000C;  // hasta que la ventana pase a primer plano
    private const uint MB_OK            = 0x00000000;  // sonido predeterminado del sistema

    [StructLayout(LayoutKind.Sequential)]
    private struct FLASHWINFO
    {
        public uint   cbSize;
        public IntPtr hwnd;
        public uint   dwFlags;
        public uint   uCount;
        public uint   dwTimeout;
    }

    [DllImport("user32.dll")]
    private static extern int FlashWindowEx(ref FLASHWINFO pwfi);

    [DllImport("user32.dll")]
    private static extern int MessageBeep(uint uType);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    /// <summary>
    /// Decide si avisar: solo si está habilitado, la operación no se canceló y duró al menos
    /// <paramref name="threshold"/>. Lógica pura y testeable (sin efectos).
    /// </summary>
    public static bool ShouldNotify(TimeSpan elapsed, bool enabled, bool cancelled, TimeSpan threshold)
        => enabled && !cancelled && elapsed >= threshold;

    /// <summary>
    /// Avisa de que terminó la operación (sonido + parpadeo de la barra de tareas), salvo que la ventana
    /// <paramref name="hwnd"/> ya esté en primer plano. Nunca lanza.
    /// </summary>
    public static void OperationFinished(IntPtr hwnd)
    {
        try
        {
            if (hwnd == IntPtr.Zero || GetForegroundWindow() == hwnd) return;

            MessageBeep(MB_OK);

            var info = new FLASHWINFO
            {
                cbSize    = (uint)Marshal.SizeOf<FLASHWINFO>(),
                hwnd      = hwnd,
                dwFlags   = FLASHW_TRAY | FLASHW_TIMERNOFG,
                uCount    = uint.MaxValue,
                dwTimeout = 0,
            };
            FlashWindowEx(ref info);
        }
        catch { /* el aviso nunca debe romper la app */ }
    }
}
