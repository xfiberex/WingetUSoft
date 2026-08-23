namespace WingetUSoft;

/// <summary>
/// Lo que la orquestación de lotes necesita de winget, y nada más.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="WingetService"/> es estático: cómodo de llamar, imposible de sustituir. Eso dejaba el
/// corazón del producto —el bucle que actualiza un lote de paquetes y decide qué contar como acierto,
/// qué como fallo y cuándo parar— sin más forma de probarlo que arrancar la app entera con FlaUI y
/// dejar que lanzara procesos de verdad (T4-03).
/// </para>
/// <para>
/// La interfaz se queda deliberadamente **estrecha**: solo la operación que el lote usa. No pretende
/// ser una fachada de winget, sino la costura mínima por la que <see cref="UpgradeBatchRunner"/> se
/// puede probar con un doble.
/// </para>
/// </remarks>
public interface IWingetService
{
    /// <summary>Actualiza un paquete. Misma semántica que <see cref="WingetService.UpgradePackageAsync"/>.</summary>
    /// <param name="packageId">Id del paquete en winget.</param>
    /// <param name="silent">Pasa <c>--silent</c> en vez de <c>--interactive</c>.</param>
    /// <param name="progress">Progreso de descarga del paquete en curso, si winget lo reporta.</param>
    /// <param name="cancellationToken">Cancelación del lote completo.</param>
    /// <param name="logProgress">Líneas de salida de winget, para el registro de actividad.</param>
    Task<UpgradeResult> UpgradePackageAsync(
        string packageId,
        bool silent,
        IProgress<WingetProgressInfo>? progress,
        CancellationToken cancellationToken,
        IProgress<string>? logProgress);
}

/// <summary>
/// Implementación real: delega en la clase estática <see cref="WingetService"/>.
/// </summary>
/// <remarks>
/// Es adrede una cáscara sin lógica. Todo lo que decida algo vive en <see cref="UpgradeBatchRunner"/>,
/// que sí se puede probar; aquí solo queda la llamada al proceso, que es justo lo que un test no
/// quiere ejecutar.
/// </remarks>
public sealed class WingetServiceAdapter : IWingetService
{
    /// <summary>Instancia compartida: la clase no tiene estado propio.</summary>
    public static readonly WingetServiceAdapter Instance = new();

    /// <inheritdoc />
    public Task<UpgradeResult> UpgradePackageAsync(
        string packageId,
        bool silent,
        IProgress<WingetProgressInfo>? progress,
        CancellationToken cancellationToken,
        IProgress<string>? logProgress) =>
        WingetService.UpgradePackageAsync(
            packageId, silent, runAsAdministrator: false, progress, cancellationToken, logProgress);
}
