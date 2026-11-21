namespace WingetUSoft;

/// <summary>Un programa que no se pudo desinstalar y el motivo, ya en el idioma de la interfaz.</summary>
public sealed record UninstallFailure(WingetPackage Package, string Reason);

/// <summary>Resultado de una desinstalación en lote.</summary>
/// <param name="Succeeded">Programas desinstalados: los únicos para los que se ofrece limpiar residuos.</param>
/// <param name="Failed">Programas en los que winget falló.</param>
/// <param name="NotCompleted">
/// Programas sin terminar al cancelar, incluido el que estaba en curso: de ese no se sabe en qué estado quedó, y
/// por eso la ventana vuelve a leer la lista.
/// </param>
/// <param name="Cancelled">True si el usuario canceló el lote.</param>
public sealed record UninstallBatchResult(
    IReadOnlyList<WingetPackage> Succeeded,
    IReadOnlyList<UninstallFailure> Failed,
    int NotCompleted,
    bool Cancelled);

/// <summary>
/// Recorre una desinstalación en lote, de uno en uno. Lógica sin UI ni procesos: la desinstalación llega como
/// delegado, así que se prueba con un doble.
/// </summary>
public static class UninstallBatch
{
    /// <summary>
    /// Versión que hay que pasar a winget para señalar este programa, o <c>null</c> si el Id ya basta.
    /// </summary>
    /// <remarks>
    /// Con dos versiones instaladas bajo el mismo Id, <c>winget uninstall --id X --exact</c> se niega con
    /// <c>0x8A150016</c> y no se podía quitar ninguna. Solo se pasa cuando el Id está repetido, y solo si es una
    /// versión de verdad: winget también lista valores como <c>Unknown</c> o <c>&lt; 14.1.0</c>, que no sirven como
    /// argumento. En ese caso winget seguirá negándose, que es preferible a desinstalar otra versión.
    /// </remarks>
    public static string? VersionToDisambiguate(WingetPackage package, IEnumerable<WingetPackage> installed)
    {
        bool repeated = installed.Count(p => string.Equals(p.Id, package.Id, StringComparison.OrdinalIgnoreCase)) > 1;
        string version = package.Version.Trim();
        return repeated && version.Length > 0 && char.IsAsciiDigit(version[0]) ? version : null;
    }

    /// <summary>
    /// Desinstala los programas en orden. Un fallo no corta el lote; cancelar sí, y lo pendiente no se intenta.
    /// </summary>
    /// <param name="packages">Programas a desinstalar.</param>
    /// <param name="uninstall">Desinstala un programa.</param>
    /// <param name="onStarting">Se llama antes de cada programa, con su posición (desde 1).</param>
    /// <param name="onSucceeded">Se llama al desinstalar un programa.</param>
    /// <param name="onFailed">Se llama cuando un programa falla.</param>
    /// <param name="cancellationToken">Cancela el lote.</param>
    public static async Task<UninstallBatchResult> RunAsync(
        IReadOnlyList<WingetPackage> packages,
        Func<WingetPackage, CancellationToken, Task<UpgradeResult>> uninstall,
        Action<WingetPackage, int>? onStarting = null,
        Action<WingetPackage>? onSucceeded = null,
        Action<UninstallFailure>? onFailed = null,
        CancellationToken cancellationToken = default)
    {
        var succeeded = new List<WingetPackage>();
        var failed = new List<UninstallFailure>();

        for (int i = 0; i < packages.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                return new UninstallBatchResult(succeeded, failed, packages.Count - i, Cancelled: true);

            var package = packages[i];
            onStarting?.Invoke(package, i + 1);

            UninstallFailure? failure = null;
            try
            {
                // SIN ConfigureAwait(false), a propósito: los avisos tocan controles y tienen que volver al hilo de
                // la interfaz. Es el mismo error que rompió los lotes de actualización de la 1.8.8 a la 1.10.0 (F-19).
                var result = await uninstall(package, cancellationToken);
                if (result.Success)
                {
                    succeeded.Add(package);
                    onSucceeded?.Invoke(package);
                }
                else
                {
                    failure = new UninstallFailure(package, result.GetFailureReason());
                }
            }
            catch (OperationCanceledException)
            {
                return new UninstallBatchResult(succeeded, failed, packages.Count - i, Cancelled: true);
            }
            catch (Exception ex)
            {
                failure = new UninstallFailure(package, ex.Message);
            }

            if (failure is not null)
            {
                failed.Add(failure);
                onFailed?.Invoke(failure);
            }
        }

        return new UninstallBatchResult(succeeded, failed, 0, Cancelled: false);
    }
}
