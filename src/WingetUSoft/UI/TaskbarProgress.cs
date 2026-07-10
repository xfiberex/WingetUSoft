using System.Runtime.InteropServices;

namespace WingetUSoft;

/// <summary>
/// Progreso en el icono de la barra de tareas de Windows (<c>ITaskbarList3</c>), visible con la app
/// minimizada — complementa el aviso al terminar (<see cref="Notifier"/>). Vía COM/Win32; si el shell
/// no expone la interfaz (poco común), las llamadas quedan en no-op y el progreso solo se ve en la app.
/// </summary>
public static class TaskbarProgress
{
    private enum TbpFlag : uint
    {
        NoProgress    = 0x0,
        Indeterminate = 0x1,
        Normal        = 0x2,
        Error         = 0x4,
        Paused        = 0x8,
    }

    [ComImport, Guid("56FDF344-FD6D-11D0-958A-006097C9A090")]
    private class TaskbarInstance;

    // Únicos miembros de ITaskbarList/2/3 que se usan; deben declararse en orden (vtable COM) desde
    // el principio de la interfaz, aunque no se invoquen todos.
    [ComImport, Guid("EA1AFB91-9E28-4B86-90E9-9E9F8A5EEFAF"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITaskbarList3
    {
        void HrInit();
        void AddTab(IntPtr hwnd);
        void DeleteTab(IntPtr hwnd);
        void ActivateTab(IntPtr hwnd);
        void SetActiveAlt(IntPtr hwnd);
        void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);
        void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
        void SetProgressState(IntPtr hwnd, TbpFlag tbpFlags);
    }

    private static readonly ITaskbarList3? Instance = CreateInstance();

    private static ITaskbarList3? CreateInstance()
    {
        try { return (ITaskbarList3)new TaskbarInstance(); }
        catch { return null; }
    }

    /// <summary>Progreso normal (0-100 %) en el icono de la barra de tareas.</summary>
    public static void SetValue(IntPtr hwnd, int percent)
    {
        try
        {
            Instance?.SetProgressState(hwnd, TbpFlag.Normal);
            Instance?.SetProgressValue(hwnd, (ulong)Math.Clamp(percent, 0, 100), 100);
        }
        catch { /* el progreso en la barra nunca debe romper la app */ }
    }

    /// <summary>Progreso indeterminado (barra animada, sin porcentaje) en el icono.</summary>
    public static void SetIndeterminate(IntPtr hwnd)
    {
        try { Instance?.SetProgressState(hwnd, TbpFlag.Indeterminate); }
        catch { }
    }

    /// <summary>Quita el progreso del icono (operación terminada, con o sin error).</summary>
    public static void Clear(IntPtr hwnd)
    {
        try { Instance?.SetProgressState(hwnd, TbpFlag.NoProgress); }
        catch { }
    }
}
