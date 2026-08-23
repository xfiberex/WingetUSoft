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

    public WingetPackage Package { get; }
    public string Name => Package.Name;
    public string Id => Package.Id;
    public string Version => Package.Version;
    public string Available => Package.Available;
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
    /// deshabilita: marcarla haría que la fila dijera una cosa y el contador del botón otra.</summary>
    public bool IsSelectable => !IsExcluded && !IsVersionSkipped;

    /// <summary>La fila se atenúa igual en ambos casos: es un paquete que el lote va a saltarse.</summary>
    public bool IsDimmed => IsExcluded || IsVersionSkipped;

    /// <summary>
    /// Icono de estado de la fila. Excluido y omitido son cosas distintas y se distinguen a golpe de
    /// vista: bloqueo (permanente, todo el paquete) frente a pausa (esta versión, temporal). Ninguno es
    /// rojo — no son errores, son decisiones del usuario (Tier C #4).
    /// </summary>
    public string StatusGlyph => IsExcluded ? "" : IsVersionSkipped ? "" : "";

    public bool HasStatusIcon => IsDimmed;

    /// <summary>Etiqueta accesible (localizada) del icono de estado: sin ella, un lector de pantalla no lo anuncia.</summary>
    public string StatusLabel => IsExcluded
        ? L.T("grid.excludedAccessible")
        : IsVersionSkipped ? L.T("grid.skippedAccessible", Available) : "";

    /// <summary>Etiqueta accesible de la casilla de la fila: sin ella se anuncia solo como "casilla".</summary>
    public string SelectLabel => L.T("grid.selectAccessible", Name);

    /// <summary>
    /// Nombre accesible de la fila entera. Sin esto, el <c>ListViewItem</c> hereda el <c>ToString()</c>
    /// del ViewModel y un lector de pantalla anuncia literalmente "WingetUSoft.PackageViewModel"
    /// (comprobado en el árbol de automatización de la app real).
    /// </summary>
    public string RowLabel => L.T("grid.rowAccessible", Name, Version, Available);

    public PackageViewModel(WingetPackage package) => Package = package;

    public event PropertyChangedEventHandler? PropertyChanged;
}
