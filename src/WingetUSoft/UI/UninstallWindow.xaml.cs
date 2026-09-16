using System.Collections.ObjectModel;
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
public sealed class InstalledPackageViewModel(WingetPackage package)
{
    public WingetPackage Package { get; } = package;
    public string Name => Package.Name;
    public string Id => Package.Id;
    public string Version => Package.Version;
    public string Source => Package.Source;

    /// <summary>
    /// Casi todo lo instalado no viene de winget sino de «Agregar o quitar programas», y no tiene origen:
    /// en ese caso la etiqueta no termina en un «origen» vacío.
    /// </summary>
    public string RowLabel => string.IsNullOrWhiteSpace(Source)
        ? L.T("uninstall.rowAccessibleNoSource", Name, Version)
        : L.T("uninstall.rowAccessible", Name, Version, Source);
}

public sealed partial class UninstallWindow : Window
{
    private readonly AppSettings _settings;
    private readonly ObservableCollection<InstalledPackageViewModel> _packageViewModels = [];
    private List<WingetPackage> _allPackages = [];
    private string _searchFilter = "";
    private CancellationTokenSource? _cts;
    private bool _initialized;

    private AppWindow _appWindow = null!;
    private IntPtr _hWnd;

    public UninstallWindow(AppSettings settings)
    {
        InitializeComponent();

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
            root.Loaded += async (_, _) =>
            {
                UpdateTitleBarButtonColors();
                await LoadPackagesAsync();
            };
        }

        _initialized = true;
    }

    private void ApplyLocalizedStrings()
    {
        Title = L.T("uninstall.windowTitle");
        txtTitleBar.Text = L.T("uninstall.titleBar");
        txtHeaderTitle.Text = L.T("uninstall.headerTitle");
        txtSubtitulo.Text = L.T("uninstall.headerSubtitle");
        btnRefresh.Content = L.T("btn.refreshList");
        txtUninstallLabel.Text = L.T("uninstall.uninstallSelected");
        // El contenido es glifo + texto: sin nombre explícito, el lector de pantalla no tendría qué anunciar.
        AutomationProperties.SetName(btnUninstall, txtUninstallLabel.Text);
        btnCancelar.Content = L.T("btn.cancel");
        txtBuscarLabel.Text = L.T("search.label");
        txtBuscar.PlaceholderText = L.T("search.placeholder");
        txtListHeader.Text = L.T("uninstall.listHeader");
        colNombre.Text = L.T("list.colName");
        colId.Text = L.T("list.colId");
        colVersion.Text = L.T("list.colVersion");
        colFuente.Text = L.T("list.colSource");
        txtLogHeader.Text = L.T("log.activity");
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

        try
        {
            _allPackages = await WingetService.GetInstalledPackagesAsync(_cts.Token);
            ApplyFilter();
            txtEstado.Text = L.T("uninstall.foundCount", _allPackages.Count);
        }
        catch (OperationCanceledException)
        {
            txtEstado.Text = L.T("uninstall.loadCancelled");
        }
        catch (Exception ex)
        {
            txtEstado.Text = L.T("uninstall.loadError");
            await ShowDialogAsync(L.T("error.title"), ex.Message);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            SetUIBusy(false);
        }
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
            _packageViewModels.Add(new InstalledPackageViewModel(pkg));

        txtContador.Text = _packageViewModels.Count == _allPackages.Count
            ? L.T("uninstall.countAll", _allPackages.Count)
            : L.T("uninstall.countFiltered", _packageViewModels.Count, _allPackages.Count);
    }

    // --- Uninstall ---

    private async Task UninstallSelectedAsync()
    {
        if (lvPackages.SelectedItem is not InstalledPackageViewModel { Package: var pkg }) return;

        // Irreversible: Cancelar es el botón por defecto, así que Intro no desinstala (F-04).
        bool confirmed = await WindowDialogHelper.ShowConfirmDialogAsync(
            Content.XamlRoot,
            L.T("uninstall.confirmTitle"),
            L.T("uninstall.confirmBody", pkg.Name, pkg.Id),
            L.T("uninstall.confirmPrimary"),
            destructive: true);

        if (!confirmed) return;

        _cts = new CancellationTokenSource();
        SetUIBusy(true);
        txtEstado.Text = L.T("uninstall.uninstalling", pkg.Name);
        ClearLog();
        AppendLog(L.T("uninstall.startingLog", pkg.Name, pkg.Id));
        var opStopwatch = System.Diagnostics.Stopwatch.StartNew();
        bool cancelled = false;
        TaskbarProgress.SetIndeterminate(_hWnd);

        try
        {
            var result = await WingetService.UninstallPackageAsync(pkg.Id, _settings.SilentMode, _cts.Token);

            if (result.Success)
            {
                AppendLog(L.T("uninstall.successLog", pkg.Name), LogLineKind.Success);
                txtEstado.Text = L.T("uninstall.successStatus", pkg.Name);

                var cleanupWin = new CleanupWindow(_settings, [pkg]);
                cleanupWin.Activate();

                await LoadPackagesAsync();
            }
            else
            {
                string reason = result.GetFailureReason();
                AppendLog(L.T("uninstall.errorLog", pkg.Name, reason), LogLineKind.Error);
                txtEstado.Text = L.T("uninstall.errorStatus");
                await ShowDialogAsync(L.T("uninstall.errorTitle"),
                    L.T("uninstall.errorBody", pkg.Name, reason));
            }
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
            AppendLog(L.T("uninstall.cancelledLog"), LogLineKind.Warning);
            txtEstado.Text = L.T("status.cancelled");
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

        if (Notifier.ShouldNotify(opStopwatch.Elapsed, _settings.ShowNotifications, cancelled, TimeSpan.FromSeconds(10)))
            Notifier.OperationFinished(_hWnd);
    }

    // --- UI Helpers ---

    private void SetUIBusy(bool busy)
    {
        btnRefresh.IsEnabled = !busy;
        btnUninstall.IsEnabled = !busy && lvPackages.SelectedItem is not null;
        btnCancelar.IsEnabled = busy;
        lvPackages.IsEnabled = !busy;
        progressRing.IsActive = busy;
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
        btnUninstall.IsEnabled = lvPackages.SelectedItem is not null && _cts is null;
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

    // --- Theme ---

    private void UpdateTitleBarButtonColors() =>
        TitleBarHelper.UpdateButtonColors(_appWindow, Content, _settings.ThemeMode);

    // --- Dialogs ---

    private Task ShowDialogAsync(string title, string message) =>
        WindowDialogHelper.ShowDialogAsync(Content.XamlRoot, title, message);
}
