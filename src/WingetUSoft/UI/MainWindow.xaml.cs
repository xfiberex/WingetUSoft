using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace WingetUSoft;

public sealed class PackageViewModel : INotifyPropertyChanged
{
    private bool _isSelected;
    private string _installerSize = "";

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

    public string InstallerSize
    {
        get => _installerSize;
        set { if (_installerSize != value) { _installerSize = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InstallerSize))); } }
    }

    public bool IsExcluded { get; set; }

    /// <summary>Etiqueta accesible (localizada) para el icono "Excluido" solo-icono de la fila.</summary>
    public string ExcludedLabel => L.T("grid.excludedAccessible");

    public PackageViewModel(WingetPackage package) => Package = package;

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed partial class MainWindow : Window
{
    private const int LogMaxLines = 400;

    private readonly ObservableCollection<PackageViewModel> _packageViewModels = [];
    private List<WingetPackage> _allPackages = [];
    private List<WingetPackage> _packages = [];
    private bool _silentMode = true;
    private bool _lastIncludeUnknown;
    private CancellationTokenSource? _cts;
    private readonly AppSettings _settings = AppSettings.Load();
    private DispatcherTimer? _autoCheckTimer;
    private bool _cancelStopsCurrentProcess = true;
    private bool _fileLoggingAvailable = true;
    private readonly object _logLock = new();
    private int _logLineCount;
    private bool _initialized;
    private int _excludedFilter = 0;
    private string _searchFilter = "";
    private int _sortColumn = 0;   // 0=none 1=Name 2=Id 3=Version 4=Available 5=Size 6=Source
    private bool _sortDescending = false;
    private CancellationTokenSource? _packageInfoCts;
    private CancellationTokenSource? _sizeLoadCts;
    private string? _appUpdateUrl;
    private H.NotifyIcon.TaskbarIcon? _trayIcon;
    private readonly Dictionary<string, string> _sizeCache = new(StringComparer.OrdinalIgnoreCase);
    private DispatcherTimer? _searchDebounceTimer;

    private AppWindow _appWindow = null!;
    private IntPtr _hWnd;
    private string _appVersionStr = "";

    private MenuFlyout ctxMenuRow = null!;
    private MenuFlyoutItem ctxActualizar = null!;
    private MenuFlyoutItem ctxCopiarNombre = null!;
    private MenuFlyoutItem ctxCopiarId = null!;
    private MenuFlyoutItem ctxBuscarWeb = null!;
    private MenuFlyoutItem ctxExcluir = null!;

    public MainWindow()
    {
        InitializeComponent();

        // Build context menu
        ctxActualizar = new MenuFlyoutItem { Text = L.T("ctx.update") };
        ctxActualizar.Click += CtxActualizar_Click;
        ctxCopiarNombre = new MenuFlyoutItem { Text = L.T("ctx.copyName") };
        ctxCopiarNombre.Click += CtxCopiarNombre_Click;
        ctxCopiarId = new MenuFlyoutItem { Text = L.T("ctx.copyId") };
        ctxCopiarId.Click += CtxCopiarId_Click;
        ctxBuscarWeb = new MenuFlyoutItem { Text = L.T("ctx.viewOnWingetRun") };
        ctxBuscarWeb.Click += CtxBuscarWeb_Click;
        ctxExcluir = new MenuFlyoutItem { Text = L.T("ctx.exclude") };
        ctxExcluir.Click += CtxExcluir_Click;
        ctxMenuRow = new MenuFlyout();
        ctxMenuRow.Items.Add(ctxActualizar);
        ctxMenuRow.Items.Add(new MenuFlyoutSeparator());
        ctxMenuRow.Items.Add(ctxCopiarNombre);
        ctxMenuRow.Items.Add(ctxCopiarId);
        ctxMenuRow.Items.Add(new MenuFlyoutSeparator());
        ctxMenuRow.Items.Add(ctxBuscarWeb);
        ctxMenuRow.Items.Add(new MenuFlyoutSeparator());
        ctxMenuRow.Items.Add(ctxExcluir);

        // Set up window
        var hWnd = WindowNative.GetWindowHandle(this);
        _hWnd = hWnd;
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _appWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"));
        WindowSizer.Apply(_appWindow, _hWnd, designWidthDip: 1180, designHeightDip: 820, minWidthDip: 900, minHeightDip: 600);

        // Set title bar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Mica backdrop
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();

        // Window closing handler for minimize-to-tray
        _appWindow.Closing += OnAppWindowClosing;

        // Keep caption button colors in sync with the actual (resolved) theme
        if (Content is FrameworkElement contentRoot)
            contentRoot.ActualThemeChanged += (_, _) => UpdateTitleBarButtonColors();

        lvPackages.ItemsSource = _packageViewModels;

        _silentMode = _settings.SilentMode;
        menuSilenciosa.IsChecked = _silentMode;
        menuInteractiva.IsChecked = !_silentMode;

        // Set initial theme radio check
        switch (_settings.ThemeMode)
        {
            case 1: menuTemaClaro.IsChecked = true; break;
            case 2: menuTemaOscuro.IsChecked = true; break;
            default: menuTemaSistema.IsChecked = true; break;
        }
        ApplyTheme(_settings.ThemeMode);

        // Idioma: en el primer arranque (sin settings.json previo) se detecta el del sistema;
        // a partir de ahí manda la elección persistida del usuario.
        if (_settings.Language is null)
        {
            AppLang detected = _settings.LoadedFromFile
                ? AppLang.Es
                : L.FromCulture(System.Globalization.CultureInfo.CurrentUICulture.Name);
            _settings.Language = L.ToCode(detected);
            _settings.Save(); // silencioso: solo semilla el idioma, sin diálogo de error aquí
        }
        L.Set(L.FromCode(_settings.Language));
        SetLanguageRadioCheck(L.Current);
        ApplyLocalizedStrings();

        UpdateAutoCheckTimer();
        UpdateSelectionDetails();

        // Loaded event
        var root = Content as FrameworkElement;
        if (root is not null)
        {
            root.Loaded += async (_, _) =>
            {
                UpdateTitleBarButtonColors();
                ShowSettingsLoadWarningIfNeeded();

                string? version = await WingetService.CheckWingetAvailableAsync();
                if (version is null)
                {
                    SetActionButtonsEnabled(false);
                    txtEstado.Text = L.T("winget.unavailableStatus");
                    txtDetalleEstado.Text = L.T("winget.unavailableDetail");
                    await ShowDialogAsync(L.T("winget.unavailableTitle"), L.T("winget.unavailableBody"));
                    return;
                }
                var appVer = typeof(MainWindow).Assembly.GetName().Version;
                _appVersionStr = appVer is not null
                    ? $"v{appVer.Major}.{appVer.Minor}.{appVer.Build}"
                    : "v1.1.0";
                Title = $"{L.T("app.titleBase")}  [{_appVersionStr}]";
                TitleTextBlock.Text = Title;
                txtEstado.Text = L.T("status.readyToStart");
                UpdateSelectionDetails();
                await MaybeShowWhatsNewAsync();
                _ = CheckForAppUpdateAsync();
            };
        }

        // Keyboard accelerators
        AddKeyboardAccelerators();

        _initialized = true;
    }

    private void AddKeyboardAccelerators()
    {
        // F5 - Refresh
        var f5 = new KeyboardAccelerator { Key = Windows.System.VirtualKey.F5 };
        f5.Invoked += (_, _) => { if (_cts is null) _ = LoadPackagesAsync(_lastIncludeUnknown); };
        Content.KeyboardAccelerators.Add(f5);

        // Ctrl+A - Select all
        var ctrlA = new KeyboardAccelerator
        {
            Key = Windows.System.VirtualKey.A,
            Modifiers = Windows.System.VirtualKeyModifiers.Control
        };
        ctrlA.Invoked += (_, _) =>
        {
            bool allSelected = _packageViewModels.All(p => p.IsSelected);
            foreach (var vm in _packageViewModels)
                vm.IsSelected = !allSelected;
        };
        Content.KeyboardAccelerators.Add(ctrlA);

        // Escape - Cancel
        var esc = new KeyboardAccelerator { Key = Windows.System.VirtualKey.Escape };
        esc.Invoked += (_, _) =>
        {
            if (_cts is not null)
            {
                _cts.Cancel();
                btnCancelar.IsEnabled = false;
                txtEstado.Text = GetCancelStatusText();
            }
        };
        Content.KeyboardAccelerators.Add(esc);

        // Delete - Exclude
        var del = new KeyboardAccelerator { Key = Windows.System.VirtualKey.Delete };
        del.Invoked += (_, _) =>
        {
            if (GetSelectedPackage() is not null)
                CtxExcluir_Click(null, null);
        };
        Content.KeyboardAccelerators.Add(del);
    }

    private void SetActionButtonsEnabled(bool enabled)
    {
        btnConsultar.IsEnabled = enabled;
        btnConsultarDesconocidas.IsEnabled = enabled;
        btnActualizarTodo.IsEnabled = enabled;
        btnActualizarSeleccionados.IsEnabled = enabled;
    }

    private void SetUIBusy(bool busy)
    {
        SetActionButtonsEnabled(!busy);
        btnCancelar.IsEnabled = busy;
        lvPackages.IsEnabled = !busy;
        progressRing.IsActive = busy;
        if (!busy) _cancelStopsCurrentProcess = true;
    }

    private string GetCancelStatusText() => _cancelStopsCurrentProcess
        ? L.T("status.cancelling")
        : L.T("status.cancellingAfterCurrent");

    private void LoadPackagesToGrid()
    {
        _packageViewModels.Clear();
        string search = _searchFilter.Trim();
        IEnumerable<WingetPackage> filtered = _packages;

        if (!string.IsNullOrEmpty(search))
            filtered = filtered.Where(p =>
                p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Id.Contains(search, StringComparison.OrdinalIgnoreCase));

        IEnumerable<PackageViewModel> viewModels = filtered.Select(pkg =>
            new PackageViewModel(pkg) { IsExcluded = _settings.ExcludedIds.Contains(pkg.Id) });

        if (_excludedFilter == 1) viewModels = viewModels.Where(v => !v.IsExcluded);
        else if (_excludedFilter == 2) viewModels = viewModels.Where(v => v.IsExcluded);

        viewModels = ApplySort(viewModels);

        foreach (var vm in viewModels)
            _packageViewModels.Add(vm);

        UpdateSelectionDetails();
        _ = StartBackgroundSizeLoadingAsync([.. _packageViewModels]);
    }

    private IEnumerable<PackageViewModel> ApplySort(IEnumerable<PackageViewModel> items)
    {
        return _sortColumn switch
        {
            1 => _sortDescending ? items.OrderByDescending(v => v.Name) : items.OrderBy(v => v.Name),
            2 => _sortDescending ? items.OrderByDescending(v => v.Id) : items.OrderBy(v => v.Id),
            3 => _sortDescending ? items.OrderByDescending(v => v.Version) : items.OrderBy(v => v.Version),
            4 => _sortDescending ? items.OrderByDescending(v => v.Available) : items.OrderBy(v => v.Available),
            5 => _sortDescending ? items.OrderByDescending(v => v.InstallerSize) : items.OrderBy(v => v.InstallerSize),
            6 => _sortDescending ? items.OrderByDescending(v => v.Source) : items.OrderBy(v => v.Source),
            _ => items
        };
    }

    private async Task StartBackgroundSizeLoadingAsync(List<PackageViewModel> viewModels)
    {
        _sizeLoadCts?.Cancel();
        _sizeLoadCts = new CancellationTokenSource();
        var token = _sizeLoadCts.Token;

        using var semaphore = new SemaphoreSlim(2, 2);

        var tasks = viewModels.Select(async vm =>
        {
            if (_sizeCache.TryGetValue(vm.Id, out string? cached))
            {
                vm.InstallerSize = cached;
                return;
            }

            await semaphore.WaitAsync(token);
            try
            {
                var info = await WingetService.GetPackageInfoAsync(vm.Id, token);
                if (!token.IsCancellationRequested)
                {
                    string size = string.IsNullOrEmpty(info.InstallerSize) ? "—" : info.InstallerSize;
                    _sizeCache[vm.Id] = size;
                    vm.InstallerSize = size;
                }
            }
            catch (OperationCanceledException) { }
            catch { vm.InstallerSize = "—"; }
            finally { semaphore.Release(); }
        });

        try { await Task.WhenAll(tasks); }
        catch (OperationCanceledException) { }
    }

    private async Task LoadPackagesAsync(bool includeUnknown)
    {
        _lastIncludeUnknown = includeUnknown;
        _cancelStopsCurrentProcess = true;
        _sizeCache.Clear();
        _cts = new CancellationTokenSource();
        SetUIBusy(true);
        txtEstado.Text = includeUnknown
            ? L.T("status.queryingUpdatesUnknown")
            : L.T("status.queryingUpdates");

        try
        {
            _allPackages = await WingetService.GetUpgradablePackagesAsync(includeUnknown, _cts.Token);
            UpdateSourceFilter();
            ApplySourceFilter();
            LoadPackagesToGrid();

            txtUpdatesHeader.Text = includeUnknown
                ? L.T("list.headerUnknown")
                : L.T("list.header");

            string sufijo = includeUnknown ? L.T("list.suffixUnknown") : "";
            txtEstado.Text = _packages.Count == 0
                ? L.T("status.noUpdatesFound", sufijo)
                : L.T("status.updatesFound", _packages.Count, sufijo);
        }
        catch (OperationCanceledException)
        {
            _packageViewModels.Clear();
            txtEstado.Text = L.T("status.queryCancelled");
            UpdateSelectionDetails();
        }
        catch (Exception ex)
        {
            txtEstado.Text = L.T("status.queryError");
            await ShowDialogAsync(L.T("error.title"), ex.Message);
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            SetUIBusy(false);
        }
    }

    private async Task UpdatePackagesAsync(bool allPackages)
    {
        var packagesToUpdate = new List<WingetPackage>();
        bool runAsAdministrator = _settings.RunUpdatesAsAdministrator;

        if (allPackages)
        {
            packagesToUpdate.AddRange(_packages.Where(p => !_settings.ExcludedIds.Contains(p.Id)));
        }
        else
        {
            foreach (var vm in _packageViewModels)
            {
                if (vm.IsSelected && !_settings.ExcludedIds.Contains(vm.Id))
                    packagesToUpdate.Add(vm.Package);
            }
        }

        if (packagesToUpdate.Count == 0)
        {
            await ShowDialogAsync(L.T("info.title"), L.T("msg.noPackagesSelected"));
            return;
        }

        if (runAsAdministrator)
        {
            string adminMessage = packagesToUpdate.Count == 1
                ? L.T("admin.confirmSingleBody")
                : L.T("admin.confirmBatchBody", packagesToUpdate.Count);

            if (!await ShowConfirmDialogAsync(L.T("admin.confirmTitle"), adminMessage))
                return;
        }

        _cts = new CancellationTokenSource();
        _cancelStopsCurrentProcess = !runAsAdministrator;
        SetUIBusy(true);

        int success = 0;
        int failed = 0;
        bool cancelled = false;
        bool shouldReload = false;
        bool historyChanged = false;
        var opStopwatch = System.Diagnostics.Stopwatch.StartNew();

        ClearLog();

        try
        {
            if (runAsAdministrator)
            {
                TaskbarProgress.SetIndeterminate(_hWnd);
                (success, failed, cancelled) = await UpdatePackagesAsAdministratorAsync(packagesToUpdate);
                historyChanged = success > 0;
            }
            else
            {
                for (int i = 0; i < packagesToUpdate.Count; i++)
                {
                    var pkg = packagesToUpdate[i];
                    txtEstado.Text = L.T("status.updating", i + 1, packagesToUpdate.Count, pkg.Name);
                    TaskbarProgress.SetValue(_hWnd, i * 100 / packagesToUpdate.Count);

                    IProgress<WingetProgressInfo>? progressReporter = new Progress<WingetProgressInfo>(info =>
                    {
                        if (info.TotalBytes > 0)
                        {
                            UpdateLogDownloadLine(info);

                            string speedText = info.SpeedBytesPerSecond > 0
                                ? $"  ·  {FormatBytes((long)info.SpeedBytesPerSecond)}/s" : "";
                            string etaText = Throughput.FormatEta(Throughput.Eta(
                                info.TotalBytes - info.DownloadedBytes, info.SpeedBytesPerSecond)) is { Length: > 0 } e
                                ? L.T("eta.remaining", e) : "";
                            txtEstado.Text = L.T("status.updatingProgress", i + 1, packagesToUpdate.Count, pkg.Name) + speedText + etaText;
                        }
                    });

                    try
                    {
                        AppendLog($"[{i + 1}/{packagesToUpdate.Count}] Iniciando: {pkg.Name} ({pkg.Id})");

                        var result = await WingetService.UpgradePackageAsync(
                            pkg.Id, _silentMode, false, progressReporter, _cts.Token,
                            new Progress<string>(s => AppendLog(s)));

                        if (result.Success)
                        {
                            success++;
                            historyChanged = true;
                            RecordSuccessfulUpgrade(pkg);
                        }
                        else
                        {
                            failed++;
                            await HandleFailedUpgrade(pkg, result.GetFailureReason());
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        cancelled = true;
                        break;
                    }

                    if (_cts.Token.IsCancellationRequested)
                    {
                        cancelled = true;
                        break;
                    }
                }
            }

            if (cancelled)
            {
                txtEstado.Text = L.T("status.cancelledCompleted", success, failed);
                return;
            }

            if (success > 0)
            {
                txtEstado.Text = L.T("status.completedSuccessReload", success, failed);
                shouldReload = true;
            }
            else
            {
                txtEstado.Text = L.T("status.updateCompleted", success, failed);
            }
        }
        catch (OperationCanceledException)
        {
            txtEstado.Text = L.T("status.cancelledCompleted", success, failed);
        }
        catch (Exception ex)
        {
            txtEstado.Text = L.T("status.updateError");
            await ShowDialogAsync(L.T("error.updateTitle"), ex.Message);
        }
        finally
        {
            if (historyChanged)
                TrySaveSettings(L.T("msg.historySaveError"), updateStatusLabel: false);

            TaskbarProgress.Clear(_hWnd);
            _cts?.Dispose();
            _cts = null;
            SetUIBusy(false);
        }

        ShowUpdateNotification(success, failed);

        // Aviso al terminar (sonido + parpadeo de la barra de tareas): solo si la operación fue
        // larga (≥ 10 s), no se canceló y el usuario no está ya mirando la ventana.
        if (Notifier.ShouldNotify(opStopwatch.Elapsed, _settings.ShowNotifications, cancelled, TimeSpan.FromSeconds(10)))
            Notifier.OperationFinished(_hWnd);

        if (shouldReload)
            await LoadPackagesAsync(_lastIncludeUnknown);
    }

    private async Task<(int Success, int Failed, bool Cancelled)> UpdatePackagesAsAdministratorAsync(List<WingetPackage> packagesToUpdate)
    {
        var packagesById = packagesToUpdate.ToDictionary(pkg => pkg.Id, StringComparer.OrdinalIgnoreCase);

        txtEstado.Text = packagesToUpdate.Count == 1
            ? L.T("status.adminUpdatingSingle", packagesToUpdate[0].Name)
            : L.T("status.adminUpdatingBatch", packagesToUpdate.Count);

        AppendLog(packagesToUpdate.Count == 1
            ? L.T("log.adminSingleSession")
            : L.T("log.adminBatchSession", packagesToUpdate.Count));

        IProgress<WingetProgressInfo> adminDownloadProgress = new Progress<WingetProgressInfo>(info =>
        {
            if (info.TotalBytes > 0)
                UpdateLogDownloadLine(info);
        });

        var batchResult = await WingetService.UpgradePackagesAsAdministratorAsync(
            packagesToUpdate.Select(p => p.Id),
            _silentMode,
            _cts!.Token,
            new Progress<string>(s => AppendLog(s)),
            new Progress<UpgradeBatchStatusInfo>(status => ReportElevatedBatchStatus(status, packagesById)),
            adminDownloadProgress);

        if (batchResult.UserCancelled)
        {
            AppendLog($"  ✖ {batchResult.ErrorOutput}");
            return (0, 0, true);
        }

        if (batchResult.CancelledAfterCurrentPackage && batchResult.Items.Count == 0)
        {
            AppendLog(L.T("log.adminCancelledBeforeStart"));
            return (0, 0, true);
        }

        if (batchResult.Items.Count == 0 && !string.IsNullOrWhiteSpace(batchResult.ErrorOutput))
        {
            AppendLog($"  ✖ {batchResult.ErrorOutput}");
            await ShowDialogAsync(L.T("error.updateTitle"), batchResult.ErrorOutput);
            return (0, packagesToUpdate.Count, false);
        }

        int success = 0;
        int failed = 0;
        bool cancelled = false;
        var resultsById = batchResult.Items.ToDictionary(item => item.PackageId, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < packagesToUpdate.Count; i++)
        {
            var pkg = packagesToUpdate[i];

            if (!resultsById.TryGetValue(pkg.Id, out var item))
            {
                if (batchResult.CancelledAfterCurrentPackage)
                {
                    cancelled = true;
                    AppendLog(L.T("log.packageNotStarted", i + 1, packagesToUpdate.Count, pkg.Name, pkg.Id));
                    break;
                }

                failed++;
                AppendLog(L.T("log.resultUnavailable", i + 1, packagesToUpdate.Count, pkg.Name, pkg.Id));
                await HandleFailedUpgrade(pkg, L.T("msg.noElevatedResult"));
                continue;
            }

            txtEstado.Text = L.T("status.processingResult", i + 1, packagesToUpdate.Count, pkg.Name);
            AppendLog(L.T("log.packageFinished", i + 1, packagesToUpdate.Count, pkg.Name, pkg.Id));

            if (item.Result.Success)
            {
                success++;
                RecordSuccessfulUpgrade(pkg);
            }
            else
            {
                failed++;
                await HandleFailedUpgrade(pkg, item.Result.GetFailureReason());
            }
        }

        return (success, failed, cancelled);
    }

    private void ReportElevatedBatchStatus(UpgradeBatchStatusInfo status, IReadOnlyDictionary<string, WingetPackage> packagesById)
    {
        switch (status.Phase)
        {
            case "starting":
                AppendLog(L.T("log.preparingElevatedBatch"));
                break;
            case "running" when packagesById.TryGetValue(status.PackageId, out var pkg):
                txtEstado.Text = L.T("status.adminUpdatingProgress", status.CurrentIndex, status.TotalCount, pkg.Name);
                AppendLog(L.T("log.packageRunning", status.CurrentIndex, status.TotalCount, pkg.Name, pkg.Id));
                break;
            case "cancelled":
                AppendLog(L.T("log.cancellingAfterCurrent"));
                break;
            case "completed":
                AppendLog(L.T("log.elevatedBatchFinished"));
                break;
        }
    }

    private void RecordSuccessfulUpgrade(WingetPackage pkg)
    {
        AppendLog(L.T("log.upgradeSuccess", pkg.Name), LogLineKind.Success);
        _settings.AddHistory(new HistoryEntry
        {
            Date = DateTime.Now,
            PackageName = pkg.Name,
            PackageId = pkg.Id,
            FromVersion = pkg.Version,
            ToVersion = pkg.Available,
            Success = true
        });
    }

    private async Task HandleFailedUpgrade(WingetPackage pkg, string reason)
    {
        AppendLog($"  ✖ {pkg.Name}: {reason}", LogLineKind.Error);
        await ShowDialogAsync(L.T("error.updateTitle"),
            L.T("error.cannotUpdateBody", pkg.Name, pkg.Id, reason));
    }

    // --- Button Event Handlers ---

    private async void BtnConsultar_Click(object sender, RoutedEventArgs e) =>
        await LoadPackagesAsync(includeUnknown: false);

    private async void BtnConsultarDesconocidas_Click(object sender, RoutedEventArgs e) =>
        await LoadPackagesAsync(includeUnknown: true);

    private async void BtnActualizarSeleccionados_Click(object sender, RoutedEventArgs e) =>
        await UpdatePackagesAsync(allPackages: false);

    private async void BtnActualizarTodo_Click(object sender, RoutedEventArgs e)
    {
        var pendientes = _packages.Where(p => !_settings.ExcludedIds.Contains(p.Id)).ToList();
        if (pendientes.Count == 0)
        {
            await ShowDialogAsync(L.T("info.title"), L.T("msg.noPackagesToUpdate"));
            return;
        }
        string lista = string.Join("\n  \u2022 ", pendientes.Take(10).Select(p => p.Name));
        if (pendientes.Count > 10) lista += L.T("list.andMore", pendientes.Count - 10);
        if (!await ShowConfirmDialogAsync(L.T("confirm.updateTitle"),
                L.T("confirm.updateBody", pendientes.Count, lista)))
            return;
        await UpdatePackagesAsync(allPackages: true);
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        btnCancelar.IsEnabled = false;
        txtEstado.Text = GetCancelStatusText();
    }

    // --- Menu Handlers ---

    private void MenuSilenciosa_Click(object sender, RoutedEventArgs e)
    {
        _silentMode = true;
        menuSilenciosa.IsChecked = true;
        menuInteractiva.IsChecked = false;
        _settings.SilentMode = true;
        TrySaveSettings(L.T("msg.saveUpdateModeError"));
    }

    private void MenuInteractiva_Click(object sender, RoutedEventArgs e)
    {
        _silentMode = false;
        menuSilenciosa.IsChecked = false;
        menuInteractiva.IsChecked = true;
        _settings.SilentMode = false;
        TrySaveSettings(L.T("msg.saveUpdateModeError"));
    }

    private void MenuTemaSistema_Click(object sender, RoutedEventArgs e)
    {
        _settings.ThemeMode = 0;
        TrySaveSettings(L.T("msg.saveThemeError"));
        ApplyTheme(0);
    }

    private void MenuTemaClaro_Click(object sender, RoutedEventArgs e)
    {
        _settings.ThemeMode = 1;
        TrySaveSettings(L.T("msg.saveThemeError"));
        ApplyTheme(1);
    }

    private void MenuTemaOscuro_Click(object sender, RoutedEventArgs e)
    {
        _settings.ThemeMode = 2;
        TrySaveSettings(L.T("msg.saveThemeError"));
        ApplyTheme(2);
    }

    private void MenuIdiomaEs_Click(object sender, RoutedEventArgs e) => SetLanguage(AppLang.Es);
    private void MenuIdiomaEn_Click(object sender, RoutedEventArgs e) => SetLanguage(AppLang.En);
    private void MenuIdiomaPt_Click(object sender, RoutedEventArgs e) => SetLanguage(AppLang.Pt);
    private void MenuIdiomaFr_Click(object sender, RoutedEventArgs e) => SetLanguage(AppLang.Fr);
    private void MenuIdiomaIt_Click(object sender, RoutedEventArgs e) => SetLanguage(AppLang.It);

    private void SetLanguage(AppLang lang)
    {
        L.Set(lang);
        _settings.Language = L.ToCode(lang);
        TrySaveSettings(L.T("msg.saveLanguageError"));
        SetLanguageRadioCheck(lang);
        ApplyLocalizedStrings();
    }

    private void SetLanguageRadioCheck(AppLang lang)
    {
        menuIdiomaEs.IsChecked = lang == AppLang.Es;
        menuIdiomaEn.IsChecked = lang == AppLang.En;
        menuIdiomaPt.IsChecked = lang == AppLang.Pt;
        menuIdiomaFr.IsChecked = lang == AppLang.Fr;
        menuIdiomaIt.IsChecked = lang == AppLang.It;
    }

    /// <summary>
    /// Aplica el idioma actual (<see cref="L"/>) a los textos del menú principal. El resto de la UI
    /// se extrae por completo en el Tier A #7 (ver ROADMAP.md); por ahora solo cubre este menú.
    /// </summary>
    private void ApplyLocalizedStrings()
    {
        if (!string.IsNullOrEmpty(_appVersionStr))
        {
            Title = $"{L.T("app.titleBase")}  [{_appVersionStr}]";
            TitleTextBlock.Text = Title;
        }

        ctxActualizar.Text = L.T("ctx.update");
        ctxCopiarNombre.Text = L.T("ctx.copyName");
        ctxCopiarId.Text = L.T("ctx.copyId");
        ctxBuscarWeb.Text = L.T("ctx.viewOnWingetRun");
        ctxExcluir.Text = L.T("ctx.exclude");

        txtHeaderTitle.Text = L.T("header.title");
        txtSubtitulo.Text = L.T("header.subtitle");
        txtShortcuts.Text = L.T("header.shortcuts");
        btnInstalarUpdate.Content = L.T("btn.installNow");
        txtAccionesTitle.Text = L.T("actions.title");
        btnConsultar.Content = L.T("btn.checkUpdates");
        btnConsultarDesconocidas.Content = L.T("btn.checkUnknown");
        btnActualizarSeleccionados.Content = L.T("btn.updateSelected");
        btnActualizarTodo.Content = L.T("btn.updateAll");
        btnCancelar.Content = L.T("btn.cancel");
        txtFuenteLabel.Text = L.T("filter.sourceLabel");
        cmbFuenteAllItem.Content = L.T("filter.allSources");
        txtExcluidosLabel.Text = L.T("filter.excludedLabel");
        menuFiltroTodos.Text = L.T("filter.all");
        menuFiltroNoExcluidos.Text = L.T("filter.notExcluded");
        menuFiltroSoloExcluidos.Text = L.T("filter.onlyExcluded");
        if (_excludedFilter == 0) btnFiltroExcluidos.Content = L.T("filter.all");
        else if (_excludedFilter == 1) btnFiltroExcluidos.Content = L.T("filter.notExcluded");
        else btnFiltroExcluidos.Content = L.T("filter.onlyExcluded");
        txtBuscarLabel.Text = L.T("search.label");
        txtBuscar.PlaceholderText = L.T("search.placeholder");
        lnkHomepage.Content = L.T("info.homepage");
        lnkNotasVersion.Content = L.T("info.releaseNotes");
        txtUpdatesHeader.Text = L.T("list.header");
        colSel.Text = L.T("list.colSelect");
        colNombre.Text = L.T("list.colName");
        colId.Text = L.T("list.colId");
        colVersion.Text = L.T("list.colVersion");
        colDisponible.Text = L.T("list.colAvailable");
        colTam.Text = L.T("list.colSize");
        colFuente.Text = L.T("list.colSource");
        colExcl.Text = L.T("list.colExcluded");
        txtLogHeader.Text = L.T("log.header");
        if (!progressRing.IsActive) txtEstado.Text = L.T("status.ready");
        UpdateSelectionDetails();

        btnOpciones.Content = L.T("menu.options");
        menuModoActualizacion.Text = L.T("menu.updateMode");
        menuSilenciosa.Text = L.T("menu.silent");
        menuInteractiva.Text = L.T("menu.interactive");
        menuTema.Text = L.T("menu.theme");
        menuTemaSistema.Text = L.T("menu.themeSystem");
        menuTemaClaro.Text = L.T("menu.themeLight");
        menuTemaOscuro.Text = L.T("menu.themeDark");
        menuIdioma.Text = L.T("menu.lang");
        menuIdiomaEs.Text = L.T("menu.lang.es");
        menuIdiomaEn.Text = L.T("menu.lang.en");
        menuIdiomaPt.Text = L.T("menu.lang.pt");
        menuIdiomaFr.Text = L.T("menu.lang.fr");
        menuIdiomaIt.Text = L.T("menu.lang.it");
        menuExportar.Text = L.T("menu.export");
        menuConfiguracion.Text = L.T("menu.settings");
        menuVerHistorial.Text = L.T("menu.history");
        menuDesinstalar.Text = L.T("menu.uninstall");
        btnAyuda.Content = L.T("menu.help");
        menuBuscarActualizacion.Text = L.T("menu.checkUpdate");
        menuWhatsNew.Text = L.T("menu.whatsnew");
        menuAcercaDe.Text = L.T("menu.about");
    }

    private async void MenuExportar_Click(object sender, RoutedEventArgs e)
    {
        if (_packages.Count == 0)
        {
            await ShowDialogAsync(L.T("info.title"), L.T("msg.noDataToExport"));
            return;
        }

        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"actualizaciones_{DateTime.Now:yyyy-MM-dd}"
        };
        picker.FileTypeChoices.Add("CSV", [".csv"]);
        picker.FileTypeChoices.Add(L.T("export.txtFormat"), [".txt"]);

        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        var file = await picker.PickSaveFileAsync();

        if (file is null) return;

        char sep = file.FileType.Equals(".csv", StringComparison.OrdinalIgnoreCase) ? ',' : '\t';
        var sb = new StringBuilder();
        sb.AppendLine(DelimitedTextExporter.BuildRow(sep, L.T("list.colName"), L.T("list.colId"), L.T("export.colCurrentVersion"), L.T("list.colAvailable"), L.T("list.colSource")));
        foreach (var pkg in _packages)
            sb.AppendLine(DelimitedTextExporter.BuildRow(sep, pkg.Name, pkg.Id, pkg.Version, pkg.Available, pkg.Source));

        await Windows.Storage.FileIO.WriteTextAsync(file, sb.ToString());
        txtEstado.Text = L.T("status.listExported", file.Name);
    }

    private async void MenuConfiguracion_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_settings);
        settingsWindow.Activate();

        var tcs = new TaskCompletionSource();
        settingsWindow.Closed += (_, _) => tcs.TrySetResult();
        await tcs.Task;

        if (settingsWindow.SavedChanges)
        {
            _silentMode = _settings.SilentMode;
            menuSilenciosa.IsChecked = _silentMode;
            menuInteractiva.IsChecked = !_silentMode;
            UpdateAutoCheckTimer();
            LoadPackagesToGrid();
            ApplyTheme(_settings.ThemeMode);
        }
    }

    private async void MenuHistorial_Click(object sender, RoutedEventArgs e)
    {
        var historyWindow = new HistoryWindow(_settings.History, _settings.ThemeMode);
        historyWindow.Activate();

        var tcs = new TaskCompletionSource();
        historyWindow.Closed += (_, _) => tcs.TrySetResult();
        await tcs.Task;
    }

    private async void MenuDesinstalar_Click(object sender, RoutedEventArgs e)
    {
        var uninstallWindow = new UninstallWindow(_settings);
        uninstallWindow.Activate();

        var tcs = new TaskCompletionSource();
        uninstallWindow.Closed += (_, _) => tcs.TrySetResult();
        await tcs.Task;
    }

    // --- Sort / Search ---

    private void SortHeader_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement el || el.Tag is not string tagStr) return;
        if (!int.TryParse(tagStr, out int col)) return;

        if (_sortColumn == col)
            _sortDescending = !_sortDescending;
        else
        {
            _sortColumn = col;
            _sortDescending = false;
        }

        UpdateSortIndicators();
        LoadPackagesToGrid();
    }

    private void UpdateSortIndicators()
    {
        sortNombre.Text    = SortIndicator(1);
        sortId.Text        = SortIndicator(2);
        sortVersion.Text   = SortIndicator(3);
        sortAvailable.Text = SortIndicator(4);
        sortSize.Text      = SortIndicator(5);
        sortSource.Text    = SortIndicator(6);
    }

    private string SortIndicator(int col)
        => _sortColumn == col ? (_sortDescending ? " ▼" : " ▲") : "";

    private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_initialized) return;
        _searchFilter = txtBuscar.Text;

        _searchDebounceTimer?.Stop();
        _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _searchDebounceTimer.Tick += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            LoadPackagesToGrid();
        };
        _searchDebounceTimer.Start();
    }

    // --- Package Info Panel ---

    private async Task LoadPackageInfoPanelAsync(WingetPackage pkg)
    {
        _packageInfoCts?.Cancel();
        _packageInfoCts = new CancellationTokenSource();
        var token = _packageInfoCts.Token;

        panelPackageInfo.Visibility = Visibility.Visible;
        txtInfoDescripcion.Text = L.T("pkg.loading");
        txtInfoTamano.Text = "";
        lnkHomepage.Visibility = Visibility.Collapsed;
        lnkNotasVersion.Visibility = Visibility.Collapsed;

        try
        {
            await Task.Delay(280, token);
            var info = await WingetService.GetPackageInfoAsync(pkg.Id, token);
            if (token.IsCancellationRequested) return;

            txtInfoDescripcion.Text = string.IsNullOrEmpty(info.Description)
                ? pkg.Name
                : (info.Description.Length > 160 ? info.Description[..160] + "…" : info.Description);

            txtInfoTamano.Text = string.IsNullOrEmpty(info.InstallerSize)
                ? ""
                : L.T("pkg.sizeLabel", info.InstallerSize);

            if (!string.IsNullOrEmpty(info.Homepage)
                && Uri.TryCreate(info.Homepage, UriKind.Absolute, out var homeUri)
                && (homeUri.Scheme == Uri.UriSchemeHttps || homeUri.Scheme == Uri.UriSchemeHttp))
            {
                lnkHomepage.NavigateUri = homeUri;
                lnkHomepage.Visibility = Visibility.Visible;
            }

            if (!string.IsNullOrEmpty(info.ReleaseNotesUrl)
                && Uri.TryCreate(info.ReleaseNotesUrl, UriKind.Absolute, out var notesUri)
                && (notesUri.Scheme == Uri.UriSchemeHttps || notesUri.Scheme == Uri.UriSchemeHttp))
            {
                lnkNotasVersion.NavigateUri = notesUri;
                lnkNotasVersion.Visibility = Visibility.Visible;
            }

            // Update the size column for this package in the list
            if (!string.IsNullOrEmpty(info.InstallerSize))
            {
                var vm = _packageViewModels.FirstOrDefault(v => v.Id == pkg.Id);
                if (vm is not null) vm.InstallerSize = info.InstallerSize;
            }
        }
        catch (OperationCanceledException) { }
        catch
        {
            if (!token.IsCancellationRequested)
                txtInfoDescripcion.Text = pkg.Name;
        }
    }

    private void HidePackageInfoPanel()
    {
        _packageInfoCts?.Cancel();
        panelPackageInfo.Visibility = Visibility.Collapsed;
    }

    // --- DataGrid Events ---

    private void LvPackages_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSelectionDetails();
        if (GetSelectedPackage() is { } pkg)
            _ = LoadPackageInfoPanelAsync(pkg);
        else
            HidePackageInfoPanel();
    }

    private void LvPackages_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var element = e.OriginalSource as FrameworkElement;
        var row = element?.DataContext as PackageViewModel;
        if (row is not null)
        {
            lvPackages.SelectedItem = row;
            ctxMenuRow.ShowAt(element!, e.GetPosition(element));
        }
    }

    // --- Context Menu ---

    private async void CtxActualizar_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedPackage() is null) return;
        foreach (var vm in _packageViewModels) vm.IsSelected = false;
        if (lvPackages.SelectedItem is PackageViewModel selected)
            selected.IsSelected = true;
        await UpdatePackagesAsync(allPackages: false);
    }

    private void CtxCopiarNombre_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedPackage() is { } pkg)
        {
            var dp = new DataPackage();
            dp.SetText(pkg.Name);
            Clipboard.SetContent(dp);
        }
    }

    private void CtxCopiarId_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedPackage() is { } pkg)
        {
            var dp = new DataPackage();
            dp.SetText(pkg.Id);
            Clipboard.SetContent(dp);
        }
    }

    private async void CtxBuscarWeb_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedPackage() is not { } pkg) return;

        string url = BuildWingetRunUrl(pkg.Id);

        bool confirmed = await ShowConfirmDialogAsync(
            L.T("confirm.openWingetRunTitle"),
            L.T("confirm.openWingetRunBody", url));

        if (!confirmed) return;

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            await Windows.System.Launcher.LaunchUriAsync(uri);
    }

    private static string BuildWingetRunUrl(string packageId)
    {
        int dotIdx = packageId.IndexOf('.');
        if (dotIdx > 0 && dotIdx < packageId.Length - 1)
        {
            string publisher = Uri.EscapeDataString(packageId[..dotIdx]);
            string name = Uri.EscapeDataString(packageId[(dotIdx + 1)..]);
            return $"https://winget.run/pkg/{publisher}/{name}";
        }
        return $"https://winget.run/search?q={Uri.EscapeDataString(packageId)}";
    }

    private void CtxExcluir_Click(object? sender, RoutedEventArgs? e)
    {
        if (GetSelectedPackage() is not { } pkg) return;
        if (_settings.ExcludedIds.Contains(pkg.Id))
            _settings.ExcludedIds.Remove(pkg.Id);
        else
            _settings.ExcludedIds.Add(pkg.Id);
        TrySaveSettings(L.T("msg.saveExclusionsError"));
        LoadPackagesToGrid();
    }

    // --- Source Filter ---

    private void MenuFiltroTodos_Click(object sender, RoutedEventArgs e)
    {
        _excludedFilter = 0;
        btnFiltroExcluidos.Content = L.T("filter.all");
        LoadPackagesToGrid();
    }

    private void MenuFiltroNoExcluidos_Click(object sender, RoutedEventArgs e)
    {
        _excludedFilter = 1;
        btnFiltroExcluidos.Content = L.T("filter.notExcluded");
        LoadPackagesToGrid();
    }

    private void MenuFiltroSoloExcluidos_Click(object sender, RoutedEventArgs e)
    {
        _excludedFilter = 2;
        btnFiltroExcluidos.Content = L.T("filter.onlyExcluded");
        LoadPackagesToGrid();
    }

    private void UpdateSourceFilter()
    {
        string? current = cmbFuente.SelectedIndex > 0 ? (cmbFuente.SelectedItem as ComboBoxItem)?.Content as string : null;
        cmbFuente.SelectionChanged -= CmbFuente_SelectionChanged;
        cmbFuente.Items.Clear();
        cmbFuente.Items.Add(new ComboBoxItem { Content = L.T("filter.allSources") });
        int selectedIndex = 0;
        int idx = 1;
        foreach (var src in _allPackages.Select(p => p.Source).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s))
        {
            cmbFuente.Items.Add(new ComboBoxItem { Content = src });
            if (src == current) selectedIndex = idx;
            idx++;
        }
        cmbFuente.SelectedIndex = selectedIndex;
        cmbFuente.SelectionChanged += CmbFuente_SelectionChanged;
    }

    private void ApplySourceFilter()
    {
        string? selectedSource = null;
        if (cmbFuente.SelectedIndex > 0 && cmbFuente.SelectedItem is ComboBoxItem item)
            selectedSource = item.Content as string;

        _packages = selectedSource is null
            ? [.. _allPackages]
            : [.. _allPackages.Where(p => p.Source == selectedSource)];
    }

    private void CmbFuente_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized) return;
        ApplySourceFilter();
        LoadPackagesToGrid();
        string sufijo = _lastIncludeUnknown ? L.T("list.suffixUnknown") : "";
        txtEstado.Text = _packages.Count == 0
            ? L.T("status.noUpdatesFound", sufijo)
            : L.T("status.updatesFound", _packages.Count, sufijo);
    }

    // --- Auto Check Timer ---

    private void UpdateAutoCheckTimer()
    {
        _autoCheckTimer?.Stop();
        _autoCheckTimer = null;
        if (_settings.AutoCheckIntervalMinutes <= 0) return;
        _autoCheckTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(_settings.AutoCheckIntervalMinutes)
        };
        _autoCheckTimer.Tick += async (_, _) =>
        {
            if (_cts is not null) return;
            int prevCount = _packages.Count;
            await LoadPackagesAsync(_lastIncludeUnknown);
            if (_packages.Count > prevCount)
                txtEstado.Text += "  ¡Nuevas actualizaciones disponibles!";
        };
        _autoCheckTimer.Start();
    }

    // --- Logging ---

    private enum LogLineKind { Normal, Success, Error, Warning, Accent }

    private void ClearLog()
    {
        rtbLog.Blocks.Clear();
        _logLineCount = 0;
    }

    private void AppendLog(string text, LogLineKind kind = LogLineKind.Normal)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        if (kind == LogLineKind.Normal)
        {
            if (text.StartsWith("  \u2714", StringComparison.Ordinal)) kind = LogLineKind.Success;
            else if (text.StartsWith("  \u2716", StringComparison.Ordinal) ||
                     text.StartsWith("  \u2718", StringComparison.Ordinal)) kind = LogLineKind.Error;
            else if (text.Length > 0 && text[0] == '[') kind = LogLineKind.Accent;
        }

        SolidColorBrush brush = kind switch
        {
            LogLineKind.Success => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 56, 122, 77)),
            LogLineKind.Error => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 186, 70, 54)),
            LogLineKind.Warning => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 177, 118, 38)),
            LogLineKind.Accent => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 18, 109, 111)),
            _ => (SolidColorBrush)Application.Current.Resources["TextFillColorPrimaryBrush"]
        };

        var paragraph = new Paragraph();
        var run = new Run { Text = text, Foreground = brush };
        paragraph.Inlines.Add(run);
        rtbLog.Blocks.Add(paragraph);
        _logLineCount++;

        // Trim log if too large
        while (_logLineCount > LogMaxLines && rtbLog.Blocks.Count > 1)
        {
            rtbLog.Blocks.RemoveAt(0);
            _logLineCount--;
        }

        // Scroll to bottom
        scrollLog.ChangeView(null, scrollLog.ScrollableHeight, null);
        AppendLogFile(text);
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1_073_741_824L) return $"{bytes / 1_073_741_824.0:F1} GB";
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F1} MB";
        if (bytes >= 1_024) return $"{bytes / 1_024.0:F1} KB";
        return $"{bytes} B";
    }

    private static string BuildProgressBar(int percent, int width = 20)
    {
        int filled = Math.Clamp((int)Math.Round(percent / 100.0 * width), 0, width);
        return $"[{new string('\u2588', filled)}{new string('\u2591', width - filled)}] {percent,3}%";
    }

    private void UpdateLogDownloadLine(WingetProgressInfo info)
    {
        int percent = (int)Math.Clamp(info.DownloadedBytes * 100L / info.TotalBytes, 0, 100);
        string dl = FormatBytes(info.DownloadedBytes);
        string total = FormatBytes(info.TotalBytes);
        string speed = info.SpeedBytesPerSecond > 0
            ? $"  {FormatBytes((long)info.SpeedBytesPerSecond)}/s"
            : "";
        string eta = Throughput.FormatEta(Throughput.Eta(info.TotalBytes - info.DownloadedBytes, info.SpeedBytesPerSecond)) is { Length: > 0 } etaText
            ? $"  ETA {etaText}"
            : "";
        string bar = BuildProgressBar(percent);
        string line = $"  \u2193  {dl} / {total}  {bar}{speed}{eta}";

        // Update or add download progress line
        if (rtbLog.Blocks.Count > 0 && rtbLog.Blocks[^1] is Paragraph lastPara
            && lastPara.Inlines.Count > 0 && lastPara.Inlines[0] is Run lastRun
            && lastRun.Text.StartsWith("  \u2193", StringComparison.Ordinal))
        {
            lastRun.Text = line;
        }
        else
        {
            AppendLog(line, LogLineKind.Warning);
        }

        scrollLog.ChangeView(null, scrollLog.ScrollableHeight, null);
    }

    private void AppendLogFile(string text)
    {
        if (!_settings.LogToFile || !_fileLoggingAvailable) return;

        try
        {
            string path;
            lock (_logLock)
            {
                Directory.CreateDirectory(AppSettings.LogDirectory);
                path = Path.Combine(AppSettings.LogDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");
                File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
            }
        }
        catch (Exception ex)
        {
            lock (_logLock) { _fileLoggingAvailable = false; }
            Trace.WriteLine($"No se pudo escribir el archivo de log: {ex.Message}");
            AppendLog($"[aviso] No se pudo escribir el archivo de log: {ex.Message}", LogLineKind.Warning);
        }
    }

    // --- Helpers ---

    private WingetPackage? GetSelectedPackage()
    {
        return (lvPackages.SelectedItem as PackageViewModel)?.Package;
    }

    private void UpdateSelectionDetails()
    {
        WingetPackage? pkg = GetSelectedPackage();

        if (pkg is null)
        {
            txtDetalleEstado.Text = _packages.Count == 0 ? L.T("header.detailEmpty") : L.T("header.detailDefault");
            return;
        }

        string state = _settings.ExcludedIds.Contains(pkg.Id)
            ? L.T("pkg.excluded")
            : L.T("pkg.readyToUpdate");

        txtDetalleEstado.Text = $"{pkg.Name} | {pkg.Id} | {pkg.Version} -> {pkg.Available} | {pkg.Source} | {state}";
    }

    private void ApplyTheme(int themeMode)
    {
        if (Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = themeMode switch
            {
                1 => ElementTheme.Light,
                2 => ElementTheme.Dark,
                _ => ElementTheme.Default   // 0 = follow system
            };
        }
        UpdateTitleBarButtonColors();
    }

    private void UpdateTitleBarButtonColors() =>
        TitleBarHelper.UpdateButtonColors(_appWindow, Content, _settings.ThemeMode);

    private bool TrySaveSettings(string userMessage, bool updateStatusLabel = true)
    {
        if (_settings.Save())
            return true;

        if (updateStatusLabel)
            txtEstado.Text = L.T("error.saveConfigStatus");

        _ = ShowDialogAsync(L.T("error.configTitle"),
            string.IsNullOrWhiteSpace(_settings.LastSaveError)
                ? userMessage
                : $"{userMessage}\n\n{_settings.LastSaveError}");

        return false;
    }

    private void ShowSettingsLoadWarningIfNeeded()
    {
        if (string.IsNullOrWhiteSpace(_settings.LastLoadError))
            return;
        _ = ShowDialogAsync(L.T("msg.configResetTitle"), _settings.LastLoadError);
    }

    private async Task CheckForAppUpdateAsync()
    {
        GitHubReleaseInfo? info = await GitHubUpdateService.CheckForUpdateAsync();
        if (info is null) return;

        _appUpdateUrl = info.DownloadUrl;
        infoBarUpdate.Title = L.T("update.newVersionTitle", info.Version);
        infoBarUpdate.Message = BuildChangelogMessage(info.Notes, L.T("update.pressInstallNow"));
        infoBarUpdate.IsOpen = true;
        menuBuscarActualizacion.Text = L.T("menu.installVersion", info.Version);
    }

    /// <summary>Antepone el changelog (si lo hay) al mensaje base, truncado para no desbordar el diálogo/InfoBar.</summary>
    private static string BuildChangelogMessage(string notesMarkdown, string baseMessage)
    {
        string plain = ReleaseNotes.ToPlainText(notesMarkdown);
        if (string.IsNullOrWhiteSpace(plain)) return baseMessage;

        const int maxLength = 500;
        if (plain.Length > maxLength)
            plain = plain[..maxLength].TrimEnd() + "…";

        return $"{L.T("update.changelog")}\n{plain}\n\n{baseMessage}";
    }

    private async void LnkDescargarUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_appUpdateUrl)) return;

        btnInstalarUpdate.IsEnabled = false;
        btnInstalarUpdate.Content = L.T("btn.downloading");
        pbUpdate.Visibility = Visibility.Visible;
        infoBarUpdate.IsClosable = false;

        var downloadStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var progress = new Progress<double>(p =>
        {
            pbUpdate.Value = p;
            TaskbarProgress.SetValue(_hWnd, (int)(p * 100));

            // Sin acceso a los bytes totales aquí (la API solo reporta la fracción p), se extrapola
            // el tiempo restante a partir del tiempo transcurrido y el propio progreso.
            string etaText = "";
            if (p > 0.01)
            {
                TimeSpan estimatedTotal = TimeSpan.FromSeconds(downloadStopwatch.Elapsed.TotalSeconds / p);
                TimeSpan remaining = estimatedTotal - downloadStopwatch.Elapsed;
                if (Throughput.FormatEta(remaining) is { Length: > 0 } eta)
                    etaText = L.T("eta.label", eta);
            }
            infoBarUpdate.Message = L.T("update.downloadingProgress", $"{p:P0}", etaText);
        });

        try
        {
            string installerPath = await GitHubUpdateService.DownloadInstallerAsync(_appUpdateUrl, progress);
            infoBarUpdate.Message = L.T("update.installingRestart");
            TaskbarProgress.SetIndeterminate(_hWnd);

            Process.Start(new ProcessStartInfo(installerPath)
            {
                Arguments = "/VERYSILENT /NORESTART /autoinstall=1",
                UseShellExecute = true
            });
            await Task.Delay(1500);
            Application.Current.Exit();
        }
        catch (Exception ex)
        {
            TaskbarProgress.Clear(_hWnd);
            pbUpdate.Visibility = Visibility.Collapsed;
            btnInstalarUpdate.IsEnabled = true;
            btnInstalarUpdate.Content = L.T("btn.installNow");
            infoBarUpdate.IsClosable = true;
            infoBarUpdate.Severity = InfoBarSeverity.Error;
            infoBarUpdate.Message = L.T("error.genericPrefix", ex.Message);
        }
    }

    private async void MenuBuscarActualizacion_Click(object sender, RoutedEventArgs e)
    {
        menuBuscarActualizacion.IsEnabled = false;
        string originalText = menuBuscarActualizacion.Text;
        menuBuscarActualizacion.Text = L.T("update.checking");
        try
        {
            GitHubReleaseInfo? info = await GitHubUpdateService.CheckForUpdateAsync();
            if (info is null)
            {
                menuBuscarActualizacion.Text = originalText;
                await ShowDialogAsync(L.T("update.noUpdatesTitle"),
                    L.T("update.noUpdatesBody"));
            }
            else
            {
                _appUpdateUrl = info.DownloadUrl;
                menuBuscarActualizacion.Text = L.T("menu.installVersion", info.Version);
                infoBarUpdate.Title = L.T("update.newVersionTitle", info.Version);
                infoBarUpdate.Message = BuildChangelogMessage(info.Notes, L.T("update.pressInstallNow"));
                infoBarUpdate.IsOpen = true;

                string confirmBody = BuildChangelogMessage(info.Notes,
                    L.T("update.confirmInstall"));
                if (await ShowConfirmDialogAsync(L.T("update.availTitle"),
                    $"{L.T("update.availBody", info.Version)}\n\n{confirmBody}"))
                {
                    LnkDescargarUpdate_Click(sender, e);
                }
            }
        }
        finally
        {
            menuBuscarActualizacion.IsEnabled = true;
        }
    }

    private async void MenuWhatsNew_Click(object sender, RoutedEventArgs e) =>
        await ShowWhatsNewAsync();

    private async void MenuAcercaDe_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AboutDialog
        {
            XamlRoot = Content.XamlRoot,
            RequestedTheme = Content is FrameworkElement fe ? fe.RequestedTheme : ElementTheme.Default,
        };
        await dlg.ShowAsync();
    }

    /// <summary>
    /// Muestra las novedades una sola vez tras una actualización y persiste la versión actual como
    /// "vista". Se considera actualización si la versión cambió respecto a la última registrada, o si
    /// no había versión registrada pero la app ya se había usado (actualización desde una versión sin
    /// el campo, p. ej. desde antes de esta característica). En una instalación nueva no se muestra.
    /// </summary>
    private async Task MaybeShowWhatsNewAsync()
    {
        var appVer = typeof(MainWindow).Assembly.GetName().Version;
        string current = appVer is not null ? $"{appVer.Major}.{appVer.Minor}.{appVer.Build}" : "";
        if (string.IsNullOrEmpty(current)) return;

        string? seen = _settings.LastVersionSeen;

        bool updated = string.IsNullOrEmpty(seen)
            ? _settings.LoadedFromFile   // sin versión previa: solo si ya existía configuración (uso previo)
            : seen != current;           // con versión previa: mostrar si cambió

        _settings.LastVersionSeen = current;
        _settings.Save();

        if (updated) await ShowWhatsNewAsync();
    }

    /// <summary>
    /// Carga las notas de la versión instalada desde GitHub (por tag; si no, la última publicada) y
    /// las muestra en el diálogo de novedades. Si no hay red, el diálogo cae a un mensaje informativo.
    /// </summary>
    private async Task ShowWhatsNewAsync()
    {
        var appVer = typeof(MainWindow).Assembly.GetName().Version;
        string version = appVer is not null ? $"{appVer.Major}.{appVer.Minor}.{appVer.Build}" : "";

        GitHubReleaseInfo? info = await GitHubUpdateService.GetReleaseByTagAsync("v" + version)
            ?? await GitHubUpdateService.GetLatestReleaseAsync();

        var dlg = new WhatsNewDialog(
            info?.Version ?? version,
            info?.Notes ?? "",
            info?.HtmlUrl ?? $"https://github.com/xfiberex/WingetUSoft/releases")
        {
            XamlRoot = Content.XamlRoot,
            RequestedTheme = Content is FrameworkElement fe ? fe.RequestedTheme : ElementTheme.Default,
        };
        await dlg.ShowAsync();
    }

    private Task ShowDialogAsync(string title, string message) =>
        WindowDialogHelper.ShowDialogAsync(Content.XamlRoot, title, message);

    private Task<bool> ShowConfirmDialogAsync(string title, string message) =>
        WindowDialogHelper.ShowConfirmDialogAsync(Content.XamlRoot, title, message);

    #region Tray Icon & Notifications

    private void InitializeTrayIcon()
    {
        if (_trayIcon is not null) return;

        _trayIcon = new H.NotifyIcon.TaskbarIcon
        {
            ToolTipText = "WingetUSoft"
        };

        try
        {
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
                _trayIcon.Icon = new System.Drawing.Icon(
                    System.Drawing.Icon.ExtractAssociatedIcon(exePath)!,
                    new System.Drawing.Size(32, 32));
        }
        catch
        {
            _trayIcon.Icon = System.Drawing.SystemIcons.Application;
        }

        var cmd = new Microsoft.UI.Xaml.Input.XamlUICommand();
        cmd.ExecuteRequested += (_, _) => DispatcherQueue.TryEnqueue(RestoreFromTray);
        _trayIcon.DoubleClickCommand = cmd;
    }

    private void RestoreFromTray()
    {
        _appWindow.Show();
        if (_trayIcon is not null)
            _trayIcon.Visibility = Visibility.Collapsed;
    }

    private void MinimizeToTray()
    {
        InitializeTrayIcon();
        if (_trayIcon is not null)
        {
            _trayIcon.Visibility = Visibility.Visible;
            _appWindow.Hide();
        }
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_settings.MinimizeToTray)
        {
            args.Cancel = true;
            MinimizeToTray();
            return;
        }

        // Cleanup tray icon
        if (_trayIcon is not null)
        {
            _trayIcon.Visibility = Visibility.Collapsed;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        _autoCheckTimer?.Stop();
        _cts?.Cancel();
    }

    private void ShowUpdateNotification(int success, int failed)
    {
        if (!_settings.ShowNotifications) return;

        string message = failed == 0
            ? L.T("notif.updatedSuccess", success)
            : L.T("notif.updatedMixed", success, failed);

        txtEstado.Text = message;
    }

    #endregion
}
