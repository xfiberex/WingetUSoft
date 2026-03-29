using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace WingetUSoft;

internal sealed class HistoryEntryViewModel
{
    public string DateDisplay { get; }
    public string PackageName { get; }
    public string PackageId { get; }
    public string FromVersion { get; }
    public string ToVersion { get; }
    public string StatusDisplay { get; }
    public bool Success { get; }

    public HistoryEntryViewModel(HistoryEntry entry)
    {
        DateDisplay = entry.Date.ToString("dd/MM/yyyy HH:mm");
        PackageName = entry.PackageName;
        PackageId = entry.PackageId;
        FromVersion = entry.FromVersion;
        ToVersion = entry.ToVersion;
        Success = entry.Success;
        StatusDisplay = entry.Success ? "Exito" : "Fallido";
    }
}

public sealed partial class HistoryWindow : Window
{
    public HistoryWindow(List<HistoryEntry> history, bool darkMode)
    {
        InitializeComponent();

        // Window sizing
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new Windows.Graphics.SizeInt32(1000, 620));

        if (Content is FrameworkElement root)
            root.RequestedTheme = darkMode ? ElementTheme.Dark : ElementTheme.Light;

        LoadHistory(history);
    }

    private void LoadHistory(List<HistoryEntry> history)
    {
        if (history.Count == 0)
        {
            txtSummary.Text = "Aun no se ha registrado ninguna actualizacion desde la aplicacion.";
            lvHistory.ItemsSource = new[]
            {
                new HistoryEntryViewModel(new HistoryEntry
                {
                    Date = DateTime.MinValue,
                    PackageName = "No hay entradas en el historial.",
                    Success = false
                })
            };
            return;
        }

        int successCount = history.Count(e => e.Success);
        int failedCount = history.Count - successCount;
        txtSummary.Text = $"{history.Count} registro(s) cargados. Exito: {successCount}. Fallidos: {failedCount}.";

        lvHistory.ItemsSource = history.Select(e => new HistoryEntryViewModel(e)).ToList();
    }

    private void BtnCerrar_Click(object sender, RoutedEventArgs e) => Close();
}
