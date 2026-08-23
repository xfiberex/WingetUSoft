namespace WingetUSoft;

/// <summary>Cómo terminó un lote de actualizaciones.</summary>
/// <param name="Succeeded">Paquetes actualizados sin error.</param>
/// <param name="Failed">Paquetes que winget rechazó o que fallaron.</param>
/// <param name="Cancelled">El usuario canceló antes de terminar la lista.</param>
/// <param name="Failures">Los fallos, en orden, con el motivo ya legible.</param>
public readonly record struct UpgradeBatchOutcome(
    int Succeeded,
    int Failed,
    bool Cancelled,
    IReadOnlyList<FailedUpgrade> Failures);

/// <summary>Un paquete que no se pudo actualizar, con el motivo que se le enseña al usuario.</summary>
public readonly record struct FailedUpgrade(string Name, string Id, string Reason);

/// <summary>
/// Lo que el lote le va contando a quien lo mira, sin saber si es una ventana o un test.
/// </summary>
/// <remarks>
/// Todos los miembros tienen implementación vacía a propósito: un consumidor que solo quiera el
/// resultado no debería tener que escribir cinco métodos en blanco, y un test que solo comprueba una
/// señal tampoco.
/// </remarks>
public interface IUpgradeBatchObserver
{
    /// <summary>Va a empezar el paquete <paramref name="index"/> (base 0) de <paramref name="total"/>.</summary>
    void PackageStarting(int index, int total, WingetPackage package) { }

    /// <summary>Progreso de descarga del paquete en curso. <paramref name="index"/> es base 0.</summary>
    void DownloadProgress(int index, int total, WingetPackage package, WingetProgressInfo info) { }

    /// <summary>Una línea de salida de winget, para el registro de actividad.</summary>
    void LogLine(string line) { }

    /// <summary>El paquete se actualizó.</summary>
    void PackageSucceeded(WingetPackage package) { }

    /// <summary>El paquete falló, con el motivo ya traducido.</summary>
    void PackageFailed(WingetPackage package, string reason) { }
}

/// <summary>
/// El bucle que actualiza un lote de paquetes: llama a winget uno a uno, cuenta aciertos y fallos y
/// para cuando se cancela.
/// </summary>
/// <remarks>
/// <para>
/// Es la lógica más importante del producto y hasta T4-03 solo se podía ejercitar arrancando la app
/// real, porque vivía dentro de <c>MainWindow.UpdatePackagesAsync</c> mezclada con la barra de
/// progreso, el texto de estado, el icono de la barra de tareas y el registro. Aquí no hay nada de
/// eso: lo que la UI necesita saber se emite por <see cref="IUpgradeBatchObserver"/>, y con qué habla
/// se decide por <see cref="IWingetService"/>.
/// </para>
/// <para>
/// **Qué NO hace, deliberadamente:** no toca la UI, no guarda historial, no muestra diálogos y no
/// decide si hay que recargar la lista. Todo eso son consecuencias del resultado y las sigue tomando
/// la ventana, que es quien tiene contexto para ello.
/// </para>
/// </remarks>
public static class UpgradeBatchRunner
{
    /// <summary>
    /// Recorre <paramref name="packages"/> en orden y los actualiza de uno en uno.
    /// </summary>
    /// <remarks>
    /// La cancelación se comprueba en dos sitios —la excepción y el token— porque winget puede
    /// terminar por su cuenta justo mientras se cancela: sin la segunda comprobación, el lote seguiría
    /// con el paquete siguiente después de que el usuario pulsara «Cancelar».
    /// </remarks>
    public static async Task<UpgradeBatchOutcome> RunAsync(
        IWingetService winget,
        IReadOnlyList<WingetPackage> packages,
        bool silent,
        IUpgradeBatchObserver observer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(winget);
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(observer);

        int succeeded = 0;
        bool cancelled = false;
        var failures = new List<FailedUpgrade>();

        for (int index = 0; index < packages.Count; index++)
        {
            WingetPackage package = packages[index];
            int total = packages.Count;

            observer.PackageStarting(index, total, package);

            // Copia local del índice: el callback de progreso se despacha en la cola de la UI y puede
            // llegar cuando el bucle ya avanzó. Capturar la variable del for daría el paquete
            // equivocado en el texto de estado y en la barra.
            int current = index;
            var progress = new Progress<WingetProgressInfo>(
                info => observer.DownloadProgress(current, total, package, info));
            var log = new Progress<string>(observer.LogLine);

            try
            {
                UpgradeResult result = await winget
                    .UpgradePackageAsync(package.Id, silent, progress, cancellationToken, log)
                    .ConfigureAwait(false);

                if (result.Success)
                {
                    succeeded++;
                    observer.PackageSucceeded(package);
                }
                else
                {
                    string reason = result.GetFailureReason();
                    failures.Add(new FailedUpgrade(package.Name, package.Id, reason));
                    observer.PackageFailed(package, reason);
                }
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
                break;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                cancelled = true;
                break;
            }
        }

        return new UpgradeBatchOutcome(succeeded, failures.Count, cancelled, failures);
    }
}
