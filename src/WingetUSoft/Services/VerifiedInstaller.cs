namespace WingetUSoft;

/// <summary>
/// Un instalador ya descargado **y verificado**, con el archivo retenido para que nadie pueda
/// sustituirlo entre la verificación y el momento de ejecutarlo.
/// </summary>
/// <remarks>
/// <para>
/// Cierra una ventana TOCTOU (<i>time-of-check to time-of-use</i>) real. Hasta aquí, el instalador se
/// descargaba a una ruta fija y predecible (<c>%TEMP%\WingetUSoft_Update.exe</c>), se cerraba el
/// <c>FileStream</c> —obligatorio desde el arreglo de la v1.4.1, porque si no la verificación no podía
/// ni abrir el archivo—, se verificaba la firma o el SHA-256, y **después** se llamaba a
/// <c>Process.Start</c> sin nada que impidiera cambiar el archivo por otro entre ambos pasos.
/// </para>
/// <para>
/// Lo que estaba en juego no es poca cosa: el instalador es <c>PrivilegesRequired=admin</c>, así que
/// quien consiguiera colar su binario en esa ruta obtendría **administrador a través de un UAC que el
/// usuario reconoce como legítimo** —el de su propia aplicación actualizándose—.
/// </para>
/// <para>
/// Dos defensas, y hacen falta las dos:
/// <list type="number">
///   <item>La ruta deja de ser adivinable: cada descarga estrena un subdirectorio de nombre aleatorio,
///   recién creado, así que no hay archivo previo ni enlace plantado esperando.</item>
///   <item>El archivo se mantiene abierto con <c>FileShare.Read</c> desde **antes** de verificar hasta
///   **después** de lanzarlo: se puede leer (verificar y ejecutar lo necesitan) pero no escribir ni
///   borrar. Sin esto, la ruta aleatoria solo estrecha la ventana; no la cierra.</item>
/// </list>
/// </para>
/// </remarks>
internal sealed class VerifiedInstaller : IDisposable
{
    private readonly FileStream _lease;

    internal VerifiedInstaller(string path, FileStream lease)
    {
        Path = path;
        _lease = lease;
    }

    /// <summary>Ruta del ejecutable verificado. Válida mientras este objeto no se libere.</summary>
    internal string Path { get; }

    /// <summary>
    /// Suelta el archivo. Solo después de esto puede alguien tocarlo, así que se libera cuando el
    /// instalador ya está en marcha (o cuando se ha decidido no lanzarlo).
    /// </summary>
    public void Dispose() => _lease.Dispose();
}
