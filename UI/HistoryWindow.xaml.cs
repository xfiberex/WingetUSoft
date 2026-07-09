using System.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
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
        StatusDisplay = entry.Success ? L.T("history.statusSuccess") : L.T("history.statusFailed");
    }
}

public sealed partial class HistoryWindow : Window
{
    private readonly List<HistoryEntry> _allHistory;
    private string _searchFilter = "";
    private HistoryStatusFilter _statusFilter = HistoryStatusFilter.All;
    private bool _initialized;

    public HistoryWindow(List<HistoryEntry> history, int themeMode)
    {
        InitializeComponent();
        _allHistory = history;

        // Window sizing
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"));
        appWindow.Resize(new Windows.Graphics.SizeInt32(1000, 620));

        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = themeMode switch { 1 => ElementTheme.Light, 2 => ElementTheme.Dark, _ => ElementTheme.Default };
            root.ActualThemeChanged += (_, _) => UpdateTitleBarButtonColors(appWindow);
        }

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        UpdateTitleBarButtonColors(appWindow);

        ApplyLocalizedStrings();
        _initialized = true;
        ApplyFilter();
    }

    private void UpdateTitleBarButtonColors(AppWindow appWindow) =>
        TitleBarHelper.UpdateButtonColors(appWindow, Content, themeModeFallback: 0);

    private void ApplyLocalizedStrings()
    {
        Title = L.T("history.titleBar");
        txtTitleBar.Text = L.T("history.titleBar");
        txtHeaderTitle.Text = L.T("history.headerTitle");
        txtBuscarLabel.Text = L.T("search.label");
        txtBuscar.PlaceholderText = L.T("search.placeholder");
        txtEstadoLabel.Text = L.T("history.stateLabel");
        menuEstadoTodos.Text = L.T("filter.all");
        menuEstadoExito.Text = L.T("history.statusSuccess");
        menuEstadoFallido.Text = L.T("history.statusFailed");
        btnFiltroEstado.Content = _statusFilter switch
        {
            HistoryStatusFilter.Success => L.T("history.statusSuccess"),
            HistoryStatusFilter.Failed => L.T("history.statusFailed"),
            _ => L.T("filter.all"),
        };
        btnExportarCsv.Content = L.T("btn.exportCsv");
        colFecha.Text = L.T("history.colDate");
        colNombre.Text = L.T("list.colName");
        colId.Text = L.T("list.colId");
        colVersionOrigen.Text = L.T("history.colVersionFrom");
        colVersionDestino.Text = L.T("history.colVersionTo");
        colEstado.Text = L.T("history.colStatus");
        btnCerrar.Content = L.T("btn.close");
    }

    private void ApplyFilter()
    {
        if (!_initialized) return;

        List<HistoryEntry> filtered = HistoryFilter.Apply(_allHistory, _searchFilter, _statusFilter);
        LoadHistory(filtered, _allHistory.Count);
    }

    private void LoadHistory(List<HistoryEntry> history, int totalCount)
    {
        if (totalCount == 0)
        {
            txtSummary.Text = L.T("history.noEntriesYet");
            lvHistory.ItemsSource = new[]
            {
                new HistoryEntryViewModel(new HistoryEntry
                {
                    Date = DateTime.MinValue,
                    PackageName = L.T("history.noEntriesRow"),
                    Success = false
                })
            };
            return;
        }

        int successCount = history.Count(e => e.Success);
        int failedCount = history.Count - successCount;
        txtSummary.Text = history.Count == totalCount
            ? L.T("history.summaryAll", history.Count, successCount, failedCount)
            : L.T("history.summaryFiltered", history.Count, totalCount, successCount, failedCount);

        lvHistory.ItemsSource = history.Select(e => new HistoryEntryViewModel(e)).ToList();
    }

    private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchFilter = txtBuscar.Text;
        ApplyFilter();
    }

    private void MenuEstadoTodos_Click(object sender, RoutedEventArgs e) => SetStatusFilter(HistoryStatusFilter.All, L.T("filter.all"));
    private void MenuEstadoExito_Click(object sender, RoutedEventArgs e) => SetStatusFilter(HistoryStatusFilter.Success, L.T("history.statusSuccess"));
    private void MenuEstadoFallido_Click(object sender, RoutedEventArgs e) => SetStatusFilter(HistoryStatusFilter.Failed, L.T("history.statusFailed"));

    private void SetStatusFilter(HistoryStatusFilter filter, string label)
    {
        _statusFilter = filter;
        btnFiltroEstado.Content = label;
        ApplyFilter();
    }

    private async void BtnExportarCsv_Click(object sender, RoutedEventArgs e)
    {
        List<HistoryEntry> filtered = HistoryFilter.Apply(_allHistory, _searchFilter, _statusFilter);
        if (filtered.Count == 0)
        {
            await WindowDialogHelper.ShowDialogAsync(Content.XamlRoot, L.T("info.title"), L.T("msg.noDataToExport"));
            return;
        }

        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"historial_{DateTime.Now:yyyy-MM-dd}"
        };
        picker.FileTypeChoices.Add("CSV", [".csv"]);

        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        var file = await picker.PickSaveFileAsync();
        if (file is null) return;

        var sb = new StringBuilder();
        sb.AppendLine(DelimitedTextExporter.BuildRow(',', L.T("history.colDate"), L.T("list.colName"), L.T("list.colId"), L.T("history.colVersionFrom"), L.T("history.colVersionTo"), L.T("history.colStatus")));
        foreach (var entry in filtered)
            sb.AppendLine(DelimitedTextExporter.BuildRow(',',
                entry.Date.ToString("dd/MM/yyyy HH:mm"),
                entry.PackageName,
                entry.PackageId,
                entry.FromVersion,
                entry.ToVersion,
                entry.Success ? L.T("history.statusSuccess") : L.T("history.statusFailed")));

        await Windows.Storage.FileIO.WriteTextAsync(file, sb.ToString());
        txtSummary.Text = L.T("history.exportedTo", file.Name);
    }

    private void BtnCerrar_Click(object sender, RoutedEventArgs e) => Close();
}
