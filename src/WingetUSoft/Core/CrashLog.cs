using System.Runtime.InteropServices;

namespace WingetUSoft;

/// <summary>
/// Registro de fallos no controlados: **añade**, nunca sobrescribe, y avisa al usuario cuando el fallo
/// no es de los recuperables.
/// </summary>
/// <remarks>
/// Antes de T1-12 el manejador de <c>App.UnhandledException</c> hacía tres cosas mal a la vez:
/// escribía con <c>File.WriteAllText</c> —así que **la segunda excepción borraba la evidencia de la
/// primera**, justo cuando una cadena de fallos es lo más informativo que hay—, no ponía fecha, y
/// marcaba <c>Handled = true</c> de forma incondicional, dejando la aplicación viva en un estado
/// desconocido sin decirle nada a nadie. Un usuario cuya app se quedó a medias no tenía forma de saber
/// que algo había pasado, y el archivo que debía explicarlo solo guardaba el último episodio.
///
/// El aviso se da con <c>MessageBox</c> de Win32 y no con un <c>ContentDialog</c>: es síncrono, no
/// necesita <c>XamlRoot</c> ni un despachador vivo, y tiene que funcionar precisamente cuando el árbol
/// visual puede estar roto o el proceso a punto de terminar.
/// </remarks>
internal static class CrashLog
{
    /// <summary>Por encima de esto el archivo se recorta y se queda con la mitad más reciente.</summary>
    private const long MaxBytes = 256 * 1024;

    private const uint MB_OK = 0x0;
    private const uint MB_ICONERROR = 0x10;
    private const uint MB_SETFOREGROUND = 0x10000;
    private const uint MB_TOPMOST = 0x40000;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW")]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    internal static string FilePath => Path.Combine(AppSettings.DataDirectoryPath, "crash.log");

    /// <summary>
    /// Tipos que se consideran recuperables: la operación en curso se pierde, pero la aplicación sigue
    /// siendo usable. Solo para estos se traga la excepción; cualquier otra cosa es un fallo de verdad.
    /// </summary>
    /// <remarks>
    /// Son los fallos propios de lo que hace esta app —hablar con winget, con el disco y con GitHub—
    /// llegando por una vía sin <c>try</c> (una continuación, un evento). Fuera de esta lista no se
    /// sabe en qué estado quedó la aplicación, y seguir como si nada es peor que terminar.
    /// </remarks>
    internal static bool IsRecoverable(Exception ex) => ex is
        OperationCanceledException or
        IOException or
        UnauthorizedAccessException or
        System.Net.Http.HttpRequestException or
        TimeoutException;

    /// <summary>Anota el fallo. Nunca lanza: se la llama justo cuando ya ha ido algo mal.</summary>
    internal static void Write(string origin, Exception ex)
    {
        try
        {
            Directory.CreateDirectory(AppSettings.DataDirectoryPath);
            TrimIfTooLarge();

            // Separador + fecha: sin la fecha, dos entradas seguidas son indistinguibles.
            string nl = Environment.NewLine;
            string entry =
                new string('-', 72) + nl
                + $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {origin}" + nl
                + ex + nl;

            File.AppendAllText(FilePath, entry);
        }
        catch
        {
            // Si ni siquiera se puede anotar el fallo, no queda nada mejor que hacer.
        }
    }

    /// <summary>Aviso visible: un fallo silencioso deja al usuario sin saber que perdió la operación.</summary>
    internal static void Notify()
    {
        try
        {
            MessageBox(IntPtr.Zero,
                L.T("crash.body", FilePath),
                L.T("crash.title"),
                MB_OK | MB_ICONERROR | MB_SETFOREGROUND | MB_TOPMOST);
        }
        catch
        {
        }
    }

    /// <summary>
    /// Recorta por tamaño conservando **lo más reciente**: el historial es útil, pero un archivo que
    /// crece sin límite en un bucle de excepciones no lo es.
    /// </summary>
    private static void TrimIfTooLarge()
    {
        var file = new FileInfo(FilePath);
        if (!file.Exists || file.Length <= MaxBytes) return;

        string[] lines = File.ReadAllLines(FilePath);
        File.WriteAllLines(FilePath, lines[(lines.Length / 2)..]);
    }
}
