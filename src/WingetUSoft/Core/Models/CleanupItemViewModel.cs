using System.ComponentModel;

namespace WingetUSoft;

public sealed class CleanupItemViewModel : INotifyPropertyChanged
{
    private bool _isSelected = false;

    public string Path       { get; init; } = "";
    public bool   IsDirectory { get; init; }
    public long   SizeBytes   { get; init; }
    public string DisplaySize { get; init; } = "";
    public string PackageName { get; init; } = "";

    public string TypeLabel => IsDirectory ? L.T("cleanup.typeFolder") : L.T("cleanup.typeFile");

    /// <summary>
    /// Nombre accesible de la fila entera. Sin esto, el <c>ListViewItem</c> hereda el <c>ToString()</c>
    /// del ViewModel y un lector de pantalla anuncia "WingetUSoft.CleanupItemViewModel" (el mismo
    /// problema que ya se corrigió en la tabla principal y en la ventana de búsqueda).
    /// </summary>
    public string RowLabel => L.T("cleanup.rowAccessible", Path, TypeLabel, DisplaySize, PackageName);

    /// <summary>
    /// Etiqueta accesible de la casilla. Es la que decide qué carpetas se borran de forma recursiva,
    /// así que anunciarla solo como "casilla de verificación" deja al usuario sin saber qué marca.
    /// </summary>
    public string SelectLabel => L.T("cleanup.selectAccessible", Path);

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
