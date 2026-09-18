using System.ComponentModel;

namespace WingetUSoft;

/// <summary>
/// Fila de la tabla de la ventana principal: envuelve un <see cref="WingetPackage"/> y le añade lo
/// que solo existe en la vista — la casilla de selección y las etiquetas accesibles.
/// </summary>
/// <remarks>
/// Vivía dentro de <c>MainWindow.xaml.cs</c>, que acumulaba una decena de responsabilidades (T4-02).
/// No depende de la ventana: solo de <see cref="WingetPackage"/> y de <see cref="L"/>.
/// </remarks>
public sealed class PackageViewModel : INotifyPropertyChanged
{
    private bool _isSelected;
    private bool _isLocked;
    private RowOperation _operation;

    public WingetPackage Package { get; }
    public string Name => Package.Name;
    public string Id => Package.Id;
    public string Version => Package.Version;
    public string Available => Package.Available;

    /// <summary>Lo que ve el usuario. Version y Available siguen crudas: con ellas se ordena y se compara.</summary>
    public string VersionText => L.VersionText(Version);
    public string AvailableText => L.VersionText(Available);
    public string Source => Package.Source;

    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected != value) { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); } }
    }

    public bool IsExcluded { get; set; }

    /// <summary>True si el usuario omitió **esta** versión disponible (ver <see cref="SkippedVersions"/>).</summary>
    public bool IsVersionSkipped { get; set; }

    /// <summary>Un paquete excluido u omitido nunca se actualiza en lote, así que su casilla se
    /// deshabilita: marcarla haría que la fila dijera una cosa y el contador del botón otra. Durante una
    /// operación tampoco se puede marcar: la tabla queda en modo lectura (F-19).</summary>
    public bool IsSelectable => !IsExcluded && !IsVersionSkipped && !IsLocked;

    /// <summary>
    /// Modo lectura mientras corre una operación (F-19). Hasta entonces la tabla entera se deshabilitaba y se
    /// quedaba en gris: no se podía ni recorrer para ver cómo iba el lote.
    /// </summary>
    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if (_isLocked == value) return;
            _isLocked = value;
            Notify(nameof(IsLocked));
            Notify(nameof(IsSelectable));
        }
    }

    /// <summary>La fila se atenúa igual en ambos casos: es un paquete que el lote va a saltarse.</summary>
    public bool IsDimmed => IsExcluded || IsVersionSkipped;

    /// <summary>En qué punto está el paquete dentro del lote en curso (F-19). Lo pone la ventana desde <see cref="BatchRowTracker"/>.</summary>
    public RowOperation Operation
    {
        get => _operation;
        set
        {
            if (_operation == value) return;
            _operation = value;
            foreach (string name in new[] { nameof(Operation), nameof(StatusGlyph), nameof(HasStatusIcon),
                                            nameof(ShowStatusGlyph), nameof(IsOperationRunning), nameof(StatusLabel),
                                            nameof(RowLabel) })
                Notify(name);
        }
    }

    public bool IsOperationRunning => Operation.State == RowOperationState.Running;

    /// <summary>
    /// Icono de estado de la fila. El del lote manda sobre los demás mientras exista: es lo que está pasando
    /// ahora. Excluido y omitido se distinguen a golpe de vista: bloqueo (permanente, todo el paquete) frente
    /// a pausa (esta versión, temporal). Ninguno es rojo — no son errores, son decisiones del usuario
    /// (Tier C #4).
    /// </summary>
    public string StatusGlyph => Operation.State switch
    {
        RowOperationState.Queued => "\uE823",      // reloj: en cola
        RowOperationState.Succeeded => "\uE73E",   // marca de verificación
        RowOperationState.Failed => "\uE783",      // error
        _ => IsExcluded ? "\uE8D8" : IsVersionSkipped ? "\uE769" : ""
    };

    public bool HasStatusIcon => IsDimmed || Operation.State != RowOperationState.None;

    /// <summary>El paquete en curso lleva un anillo en lugar de glifo.</summary>
    public bool ShowStatusGlyph => HasStatusIcon && !IsOperationRunning;

    /// <summary>
    /// Etiqueta accesible (localizada) del icono de estado: sin ella, un lector de pantalla no lo anuncia.
    /// También es su tooltip, así que el motivo de un fallo se lee pasando el ratón por la fila.
    /// </summary>
    public string StatusLabel => Operation.State switch
    {
        RowOperationState.Queued => L.T("grid.opQueued"),
        RowOperationState.Running => L.T("grid.opRunning"),
        RowOperationState.Succeeded => L.T("grid.opSucceeded"),
        RowOperationState.Failed => L.T("grid.opFailed", Operation.Reason),
        _ => IsExcluded
            ? L.T("grid.excludedAccessible")
            : IsVersionSkipped ? L.T("grid.skippedAccessible", AvailableText) : ""
    };

    /// <summary>Etiqueta accesible de la casilla de la fila: sin ella se anuncia solo como "casilla".</summary>
    public string SelectLabel => L.T("grid.selectAccessible", Name);

    /// <summary>
    /// Nombre accesible de la fila entera. Sin esto, el <c>ListViewItem</c> hereda el <c>ToString()</c>
    /// del ViewModel y un lector de pantalla anuncia literalmente "WingetUSoft.PackageViewModel"
    /// (comprobado en el árbol de automatización de la app real). Durante un lote añade su estado, con el
    /// motivo si falló: recorrer la lista con el lector de pantalla cuenta cómo va (F-19).
    /// </summary>
    public string RowLabel => Operation.State == RowOperationState.None
        ? L.T("grid.rowAccessible", Name, VersionText, AvailableText)
        : L.T("grid.rowAccessible", Name, VersionText, AvailableText) + ", " + StatusLabel;

    public PackageViewModel(WingetPackage package) => Package = package;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Notify(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
