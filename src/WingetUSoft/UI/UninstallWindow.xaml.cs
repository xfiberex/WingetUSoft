using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace WingetUSoft;

/// <summary>
/// Fila de la lista de programas instalados. Envuelve <see cref="WingetPackage"/> para darle lo que solo
/// existe en la vista: el nombre accesible (F-02).
/// </summary>
/// <remarks>
/// Hasta F-02 las filas eran el <see cref="WingetPackage"/> tal cual, sin nombre accesible, así que el
/// <c>ListViewItem</c> heredaba su <c>ToString()</c> y un lector de pantalla anunciaba
/// «WingetUSoft.WingetPackage» (comprobado en la app real).
/// </remarks>
public sealed class InstalledPackageViewModel(WingetPackage package, bool isMarked = false) : INotifyPropertyChanged
{
    private bool _isMarked = isMarked;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Casilla de la fila: los programas marcados son los que se desinstalan en lote.</summary>
    public bool IsMarked
    {
        get => _isMarked;
        set
        {
            if (_isMarked == value) return;
            _isMarked = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMarked)));
        }
    }

    /// <summary>Nombre accesible de la casilla: sin él se anunciaría solo como «casilla».</summary>
    public string SelectLabel => L.T("uninstall.selectAccessible", Name);

    public WingetPackage Package { get; } = package;
    public string Name => Package.Name;
    public string Id => Package.Id;
    public string Version => Package.Version;
    public string VersionText => L.VersionText(Version);
    public string Source => Package.Source;

    /// <summary>
    /// Casi todo lo instalado no viene de winget sino de «Agregar o quitar programas», y no tiene origen:
    /// en ese caso la etiqueta no termina en un «origen» vacío.
    /// </summary>
    public string RowLabel => string.IsNullOrWhiteSpace(Source)
        ? L.T("uninstall.rowAccessibleNoSource", Name, VersionText)
        : L.T("uninstall.rowAccessible", Name, VersionText, Source);
}

public sealed partial class UninstallWindow : Window
{
    private readonly AppSettings _settings;
    private readonly ObservableCollection<InstalledPackageViewModel> _packageViewModels = [];
    private List<WingetPackage> _allPackages = [];

    /// <summary>Programas marcados. Vive fuera de las filas porque estas se recrean al filtrar: buscar no desmarca.</summary>
    private readonly HashSet<WingetPackage> _marked = [];
    private string _searchFilter = "";
    private CancellationTokenSource? _cts;

    /// <summary>Estado de la lista, que decide qué cuenta el panel superpuesto (F-18).</summary>
    private enum ListState { Loading, Ready, Cancelled, Error }
    private ListState _listState = ListState.Loading;
    private bool _initialized;

    private AppWindow _appWindow = null!;
    private IntPtr _hWnd;

    public UninstallWindow(AppSettings settings)
    {
        InitializeComponent();
        LogExpander.Attach(expLog, rowLog, activityLog, expandedMinHeight: 90);

        // La barra de estado es una región activa: sin esto, un lector de pantalla nunca anuncia
        // el progreso ni el resultado, porque el foco está en el botón, no en la barra (T1-07).
        LiveRegion.TrackStatusText(txtEstado);
        _settings = settings;

        (_appWindow, _hWnd) = WindowChrome.Apply(
            this, AppTitleBar, _settings.ThemeMode,
            designWidthDip: 900, designHeightDip: 700, minWidthDip: 720, minHeightDip: 520);

        lvPackages.ItemsSource = _packageViewModels;

        ApplyLocalizedStrings();

        var root = Content as FrameworkElement;
        if (root is not null)
        {
            root.Loaded += async (_, _) => await LoadPackagesAsync();
        }

        _initialized = true;
    }

    private void ApplyLocalizedStrings()
    {
        Title = L.T("uninstall.windowTitle");
        txtTitleBar.Text = L.T("uninstall.titleBar");
        btnRefresh.Content = L.T("btn.refreshList");
        UpdateUninstallLabel();
        AutomationProperties.SetName(chkSelectAll, L.T("grid.selectAllAccessible"));
        ToolTipService.SetToolTip(chkSelectAll, L.T("grid.selectAllAccessible"));
        btnCancelar.Content = L.T("btn.cancel");
        txtBuscarLabel.Text = L.T("search.label");
        txtBuscar.PlaceholderText = L.T("search.placeholder");
        colNombre.Text = L.T("list.colName");
        colId.Text = L.T("list.colId");
        colVersion.Text = L.T("list.colVersion");
        colFuente.Text = L.T("list.colSource");
        expLog.Header = L.T("log.activity");
        activityLog.SetAccessibleName(L.T("log.activity"));
        if (!progressRing.IsActive) txtEstado.Text = L.T("status.ready");
    }

    // --- Package Loading ---

    private async Task LoadPackagesAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        SetUIBusy(true);
        txtEstado.Text = L.T("uninstall.loadingList");
        _listState = ListState.Loading;
        UpdateListState();

        try
        {
            _allPackages = await WingetService.GetInstalledPackagesAsync(_cts.Token);
            // Una lista nueva empieza sin marcar: una casilla no debe sobrevivir a lo que ya no se ve igual.
            _marked.Clear();
            _listState = ListState.Ready;
            ApplyFilter();
            txtEstado.Text = L.P("uninstall.foundCount", _allPackages.Count, _allPackages.Count);
        }
        catch (OperationCanceledException)
        {
            txtEstado.Text = L.T("uninstall.loadCancelled");
            _listState = ListState.Cancelled;
        }
        catch (Exception ex)
        {
            txtEstado.Text = L.T("uninstall.loadError");
            _listState = ListState.Error;
            await ShowDialogAsync(L.T("error.title"), ex.Message);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            SetUIBusy(false);
            UpdateListState();
        }
    }

    /// <summary>
    /// Cuenta en la propia lista si está cargando, si falló o por qué está vacía (F-18): hasta entonces solo lo
    /// decía la barra de estado del pie.
    /// </summary>
    private void UpdateListState()
    {
        switch (_listState)
        {
            case ListState.Loading:
                panelListState.ShowLoading(L.T("uninstall.loadingList"), L.T("list.stateLoadingBody"));
                return;
            case ListState.Cancelled:
                panelListState.Show(L.T("list.stateCancelledTitle"), L.T("list.stateCancelledBody"),
                    ListStatePanel.Glyph.Sync, L.T("btn.retry"));
                return;
            case ListState.Error:
                panelListState.Show(L.T("list.stateErrorTitle"), L.T("list.stateErrorBody"),
                    ListStatePanel.Glyph.Warning, L.T("btn.retry"));
                return;
        }

        if (_packageViewModels.Count > 0)
            panelListState.Hide();
        else if (_allPackages.Count == 0)
            panelListState.Show(L.T("uninstall.stateEmptyTitle"), L.T("uninstall.stateEmptyBody"),
                ListStatePanel.Glyph.CheckMark);
        else if (_searchFilter.Trim() is { Length: > 0 } search)
            panelListState.Show(L.T("list.stateNoMatchTitle"), L.T("list.stateNoMatchSearch", search),
                ListStatePanel.Glyph.Search);
        else
            panelListState.Show(L.T("list.stateNoMatchTitle"), L.T("list.stateNoMatchFilters"),
                ListStatePanel.Glyph.Search);
    }

    /// <summary>Reintentar la carga desde el propio panel.</summary>
    private async void PanelListState_ActionInvoked(object sender, RoutedEventArgs e)
    {
        if (_cts is null) await LoadPackagesAsync();
    }

    private void ApplyFilter()
    {
        _packageViewModels.Clear();
        string search = _searchFilter.Trim();
        IEnumerable<WingetPackage> filtered = _allPackages;

        if (!string.IsNullOrEmpty(search))
            filtered = filtered.Where(p =>
                p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Id.Contains(search, StringComparison.OrdinalIgnoreCase));

        foreach (var pkg in filtered)
        {
            var row = new InstalledPackageViewModel(pkg, _marked.Contains(pkg));
            row.PropertyChanged += Row_PropertyChanged;
            _packageViewModels.Add(row);
        }
        UpdateSelectionUi();

        // El recuento solo se ve al filtrar («12 de 115»): sin filtro repetía lo que ya dice la barra de estado
        // («Se encontraron 115 programas instalados»), dos veces en la misma ventana (crítica de diseño, 2026-11-20).
        bool isFiltered = _packageViewModels.Count != _allPackages.Count;
        txtContador.Visibility = isFiltered ? Visibility.Visible : Visibility.Collapsed;
        txtContador.Text = _packageViewModels.Count == _allPackages.Count
            ? L.P("uninstall.countAll", _allPackages.Count, _allPackages.Count)
            : L.T("uninstall.countFiltered", _packageViewModels.Count, _allPackages.Count);

        // Filtrar puede vaciar la lista: el panel lo explica en cuanto hay datos cargados.
        if (_listState == ListState.Ready) UpdateListState();
    }

    // --- Uninstall ---

    /// <summary>Lo marcado con las casillas; sin marcas, la fila resaltada, como hasta ahora.</summary>
    private List<WingetPackage> GetUninstallTargets()
    {
        if (_marked.Count > 0) return _allPackages.Where(_marked.Contains).ToList();

        var targets = new List<WingetPackage>();
        if (lvPackages.SelectedItem is InstalledPackageViewModel { Package: var pkg }) targets.Add(pkg);
        return targets;
    }

    /// <summary>
    /// Irreversible: Cancelar es el botón por defecto, así que Intro no desinstala (F-04). En lote nombra los
    /// programas, también los marcados que la búsqueda tenga ocultos en ese momento.
    /// </summary>
    private Task<bool> ConfirmUninstallAsync(List<WingetPackage> targets)
    {
        string body;
        if (targets.Count == 1)
        {
            body = L.T("uninstall.confirmBody", targets[0].Name, targets[0].Id);
        }
        else
        {
            string list = string.Join("\n  • ", targets.Take(10).Select(p => p.Name));
            if (targets.Count > 10) list += L.T("list.andMore", targets.Count - 10);
            body = L.T("uninstall.confirmBatchBody", targets.Count, list);
        }

        return WindowDialogHelper.ShowConfirmDialogAsync(
            Content.XamlRoot,
            L.T("uninstall.confirmTitle"),
            body,
            L.T("uninstall.confirmPrimary"),
            destructive: true);
    }

    private async Task UninstallSelectedAsync()
    {
        var targets = GetUninstallTargets();
        if (targets.Count == 0 || !await ConfirmUninstallAsync(targets)) return;

        _cts = new CancellationTokenSource();
        LogExpander.Reveal(expLog);   // la salida de winget se sigue en el registro (F-17)
        SetUIBusy(true);
        ClearLog();
        var opStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var installed = _allPackages;   // la lista se reemplaza al recargar: el lote decide con la que vio el usuario
        UninstallBatchResult? result = null;
        TaskbarProgress.SetIndeterminate(_hWnd);

        try
        {
            result = await UninstallBatch.RunAsync(
                targets,
                (pkg, ct) => WingetService.UninstallPackageAsync(
                    pkg.Id, _settings.SilentMode, ct, UninstallBatch.VersionToDisambiguate(pkg, installed)),
                onStarting: (pkg, position) =>
                {
                    txtEstado.Text = targets.Count == 1
                        ? L.T("uninstall.uninstalling", pkg.Name)
                        : L.T("uninstall.batchProgress", position, targets.Count, pkg.Name);
                    AppendLog(L.T("uninstall.startingLog", pkg.Name, pkg.Id));
                },
                onSucceeded: pkg => AppendLog(L.T("uninstall.successLog", pkg.Name), LogLineKind.Success),
                onFailed: failure => AppendLog(L.T("uninstall.errorLog", failure.Package.Name, failure.Reason), LogLineKind.Error),
                cancellationToken: _cts.Token);
        }
        catch (Exception ex)
        {
            AppendLog(L.T("log.genericError", ex.Message), LogLineKind.Error);
            txtEstado.Text = L.T("uninstall.errorStatus");
            await ShowDialogAsync(L.T("error.title"), ex.Message);
        }
        finally
        {
            TaskbarProgress.Clear(_hWnd);
            _cts?.Dispose();
            _cts = null;
            SetUIBusy(false);
        }

        if (result is not null) await FinishUninstallAsync(targets.Count, result);

        if (Notifier.ShouldNotify(opStopwatch.Elapsed, _settings.ShowNotifications, result?.Cancelled == true, TimeSpan.FromSeconds(10)))
            Notifier.OperationFinished(_hWnd);
    }

    /// <summary>
    /// Cierra el lote: una sola ventana de limpieza con todo lo desinstalado, la lista releída y un único resumen
    /// de los fallos, en vez de un diálogo por programa que interrumpiría el lote.
    /// </summary>
    private async Task FinishUninstallAsync(int total, UninstallBatchResult result)
    {
        if (result.Cancelled) AppendLog(L.T("uninstall.cancelledLog"), LogLineKind.Warning);

        if (result.Succeeded.Count > 0)
        {
            var cleanupWin = new CleanupWindow(_settings, result.Succeeded);
            cleanupWin.Activate();
        }

        // Al cancelar, el programa en curso pudo quedar desinstalado: solo la lista real lo dice.
        if (result.Succeeded.Count > 0 || result.Cancelled) await LoadPackagesAsync();

        txtEstado.Text = (result.Cancelled, total) switch
        {
            (true, 1) => L.T("status.cancelled"),
            (true, _) => L.T("uninstall.batchCancelled", result.Succeeded.Count, result.Failed.Count, result.NotCompleted),
            (false, 1) when result.Succeeded.Count == 1 => L.T("uninstall.successStatus", result.Succeeded[0].Name),
            (false, 1) => L.T("uninstall.errorStatus"),
            _ => L.T("uninstall.batchSummary", result.Succeeded.Count, result.Failed.Count)
        };

        if (result.Failed.Count == 1)
        {
            var failure = result.Failed[0];
            await ShowDialogAsync(L.T("uninstall.errorTitle"),
                L.T("uninstall.errorBody", failure.Package.Name, failure.Reason));
        }
        else if (result.Failed.Count > 1)
        {
            string list = string.Join("\n  • ", result.Failed.Take(10).Select(f => $"{f.Package.Name}: {f.Reason}"));
            if (result.Failed.Count > 10) list += L.T("list.andMore", result.Failed.Count - 10);
            await ShowDialogAsync(L.T("uninstall.errorTitle"), L.T("uninstall.batchErrorBody", list));
        }
    }

    // --- Selection ---

    private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(InstalledPackageViewModel.IsMarked) || sender is not InstalledPackageViewModel row)
            return;

        if (row.IsMarked) _marked.Add(row.Package);
        else _marked.Remove(row.Package);
        UpdateSelectionUi();
    }

    /// <summary>Con alguna fila visible sin marcar, las marca todas; si ya lo estaban, las desmarca.</summary>
    private void ChkSelectAll_Click(object sender, RoutedEventArgs e)
    {
        bool mark = _packageViewModels.Any(r => !r.IsMarked);
        foreach (var row in _packageViewModels) row.IsMarked = mark;
        UpdateSelectionUi();
    }

    private void UpdateUninstallLabel() =>
        CommandLabel.Set(btnUninstall, txtUninstallLabel, _marked.Count > 1
            ? L.T("uninstall.uninstallCount", _marked.Count)
            : L.T("uninstall.uninstallSelected"));

    private void UpdateSelectionUi()
    {
        UpdateUninstallLabel();
        btnUninstall.IsEnabled = _cts is null && (_marked.Count > 0 || lvPackages.SelectedItem is not null);
        chkSelectAll.IsEnabled = _cts is null && _packageViewModels.Count > 0;

        int visibleMarked = _packageViewModels.Count(r => r.IsMarked);
        chkSelectAll.IsChecked = visibleMarked == 0
            ? false
            : visibleMarked == _packageViewModels.Count ? true : null;
    }

    // --- UI Helpers ---

    private void SetUIBusy(bool busy)
    {
        btnRefresh.IsEnabled = !busy;
        btnCancelar.IsEnabled = busy;
        lvPackages.IsEnabled = !busy;
        progressRing.IsActive = busy;
        UpdateSelectionUi();
    }

    // --- Event Handlers ---

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e) =>
        await LoadPackagesAsync();

    private async void BtnUninstall_Click(object sender, RoutedEventArgs e) =>
        await UninstallSelectedAsync();

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        btnCancelar.IsEnabled = false;
        txtEstado.Text = L.T("status.cancelling");
    }

    private void LvPackages_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSelectionUi();
    }

    private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_initialized) return;
        _searchFilter = txtBuscar.Text;
        ApplyFilter();
    }

    // --- Logging ---

    private void ClearLog() => activityLog.Clear();

    private void AppendLog(string text, LogLineKind kind = LogLineKind.Normal) =>
        activityLog.Append(text, kind);

    // --- Dialogs ---

    private Task ShowDialogAsync(string title, string message) =>
        WindowDialogHelper.ShowDialogAsync(Content.XamlRoot, title, message);
}
