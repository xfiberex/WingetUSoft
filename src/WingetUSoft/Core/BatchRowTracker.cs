namespace WingetUSoft;

/// <summary>En qué punto está un paquete dentro de un lote de actualizaciones.</summary>
public enum RowOperationState
{
    /// <summary>No forma parte del lote en curso, o el lote no llegó a empezarlo.</summary>
    None,
    Queued,
    Running,
    Succeeded,
    Failed
}

/// <summary>Estado de un paquete en el lote y, si falló, el motivo ya traducido.</summary>
public readonly record struct RowOperation(RowOperationState State, string Reason = "");

/// <summary>
/// Lleva el estado de cada paquete de un lote —en cola, en curso, correcto o fallido— para que la tabla lo
/// enseñe fila a fila (F-19).
/// </summary>
/// <remarks>
/// <para>
/// Hasta F-19 la tabla se deshabilitaba entera durante el lote y el avance solo se veía en la barra de estado y en
/// el registro. El estado no puede vivir en el ViewModel de la fila, porque buscar, ordenar o filtrar reconstruye
/// las filas: vive aquí, indexado por Id, y la ventana lo vuelve a aplicar cada vez que las reconstruye.
/// </para>
/// <para>
/// Es a la vez un <see cref="IUpgradeBatchObserver"/>, así que el lote normal lo alimenta solo, y expone los mismos
/// pasos como métodos para el lote elevado, que informa por otra vía (<c>ReportElevatedBatchStatus</c>).
/// </para>
/// </remarks>
public sealed class BatchRowTracker : IUpgradeBatchObserver
{
    private readonly Dictionary<string, RowOperation> _states = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Un paquete cambió de estado. <see cref="RowOperationState.None"/> significa que ya no tiene.</summary>
    public event Action<string, RowOperation>? Changed;

    /// <summary>Estado actual de un paquete; <see cref="RowOperationState.None"/> si no está en el lote.</summary>
    public RowOperation Get(string packageId) =>
        _states.TryGetValue(packageId, out var state) ? state : default;

    /// <summary>Empieza un lote: descarta lo anterior y pone en cola todos sus paquetes.</summary>
    public void Begin(IEnumerable<WingetPackage> packages)
    {
        ArgumentNullException.ThrowIfNull(packages);
        Clear();
        foreach (var package in packages)
            Set(package.Id, new RowOperation(RowOperationState.Queued));
    }

    public void MarkRunning(string packageId) => Set(packageId, new RowOperation(RowOperationState.Running));

    public void MarkSucceeded(string packageId) => Set(packageId, new RowOperation(RowOperationState.Succeeded));

    public void MarkFailed(string packageId, string reason) =>
        Set(packageId, new RowOperation(RowOperationState.Failed, reason));

    /// <summary>
    /// Termina el lote. Lo que seguía en cola o en curso no llegó a acabar —el lote se canceló— y vuelve a no
    /// tener estado: dejarlo «en cola» diría que todavía va a actualizarse.
    /// </summary>
    public void Finish()
    {
        foreach (var (id, state) in _states.ToList())
        {
            if (state.State is RowOperationState.Queued or RowOperationState.Running)
                Remove(id);
        }
    }

    /// <summary>
    /// Tras la recarga que sigue a un lote, solo sigue interesando lo que falló: lo correcto ya no aparece en la
    /// lista, y el fallo es justo lo que el usuario querrá encontrar para reintentarlo.
    /// </summary>
    public void KeepOnlyFailures()
    {
        foreach (var (id, state) in _states.ToList())
        {
            if (state.State != RowOperationState.Failed)
                Remove(id);
        }
    }

    public void Clear()
    {
        foreach (string id in _states.Keys.ToList())
            Remove(id);
    }

    // --- IUpgradeBatchObserver: el lote normal informa por aquí ---

    void IUpgradeBatchObserver.PackageStarting(int index, int total, WingetPackage package) => MarkRunning(package.Id);

    void IUpgradeBatchObserver.PackageSucceeded(WingetPackage package) => MarkSucceeded(package.Id);

    void IUpgradeBatchObserver.PackageFailed(WingetPackage package, string reason) => MarkFailed(package.Id, reason);

    private void Set(string packageId, RowOperation state)
    {
        _states[packageId] = state;
        Changed?.Invoke(packageId, state);
    }

    private void Remove(string packageId)
    {
        if (_states.Remove(packageId))
            Changed?.Invoke(packageId, default);
    }
}
