using System.ComponentModel;

namespace WingetUSoft;

public sealed class CleanupItemViewModel : INotifyPropertyChanged
{
    private bool _isSelected = true;

    public string Path       { get; init; } = "";
    public bool   IsDirectory { get; init; }
    public long   SizeBytes   { get; init; }
    public string DisplaySize { get; init; } = "";
    public string PackageName { get; init; } = "";

    public string TypeLabel => IsDirectory ? "Carpeta" : "Archivo";

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
