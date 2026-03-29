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

    public PackageViewModel(WingetPackage package) => Package = package;

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed partial class MainWindow : Window
{
    private const string DefaultSelectionDetails = "Selecciona un programa para ver sus detalles antes de actualizar.";
    private const string EmptySelectionDetails = "Todavia no hay datos cargados. Pulsa \"Consultar actualizaciones\" para empezar.";
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
        ctxActualizar = new MenuFlyoutItem { Text = "Actualizar este programa" };
        ctxActualizar.Click += CtxActualizar_Click;
        ctxCopiarNombre = new MenuFlyoutItem { Text = "Copiar nombre" };
        ctxCopiarNombre.Click += CtxCopiarNombre_Click;
        ctxCopiarId = new MenuFlyoutItem { Text = "Copiar Id" };
        ctxCopiarId.Click += CtxCopiarId_Click;
        ctxBuscarWeb = new MenuFlyoutItem { Text = "Abrir búsqueda web" };
        ctxBuscarWeb.Click += CtxBuscarWeb_Click;
        ctxExcluir = new MenuFlyoutItem { Text = "Excluir de actualizaciones" };
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
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new Windows.Graphics.SizeInt32(1180, 820));

        // Set title bar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        lvPackages.ItemsSource = _packageViewModels;

        _silentMode = _settings.SilentMode;
        menuSilenciosa.IsChecked = _silentMode;
        menuInteractiva.IsChecked = !_silentMode;
        menuModoOscuro.IsChecked = _settings.DarkMode;

        if (_settings.DarkMode)
            ApplyTheme(true);

        UpdateAutoCheckTimer();
        UpdateSelectionDetails();

        // Loaded event
        var root = Content as FrameworkElement;
        if (root is not null)
        {
            root.Loaded += async (_, _) =>
            {
                ShowSettingsLoadWarningIfNeeded();

                string? version = await WingetService.CheckWingetAvailableAsync();
                if (version is null)
                {
                    SetActionButtonsEnabled(false);
                    txtEstado.Text = "winget no esta disponible. Instalalo desde Microsoft Store.";
                    txtDetalleEstado.Text = "La aplicacion necesita App Installer o una version reciente de Windows.";
                    await ShowDialogAsync("winget no disponible",
                        "No se encontró winget en el sistema.\n\nInstala 'App Installer' desde la Microsoft Store o actualiza Windows.");
                    return;
                }
                Title = $"WingetUSoft - Actualiza tus programas  [{version}]";
                TitleTextBlock.Text = Title;
                txtEstado.Text = "Listo. Pulsa 'Consultar actualizaciones' para comenzar.";
                UpdateSelectionDetails();
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
                CtxExcluir_Click(null, null!);
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
        ? "Cancelando..."
        : "Cancelando después de la operación actual...";

    private void LoadPackagesToGrid()
    {
        _packageViewModels.Clear();
        foreach (var pkg in _packages)
        {
            _packageViewModels.Add(new PackageViewModel(pkg)
            {
                IsExcluded = _settings.ExcludedIds.Contains(pkg.Id)
            });
        }
        UpdateSelectionDetails();
    }

    private async Task LoadPackagesAsync(bool includeUnknown)
    {
        _lastIncludeUnknown = includeUnknown;
        _cancelStopsCurrentProcess = true;
        _cts = new CancellationTokenSource();
        SetUIBusy(true);
        txtEstado.Text = includeUnknown
            ? "Consultando actualizaciones (incluidas desconocidas)..."
            : "Consultando actualizaciones disponibles...";

        try
        {
            _allPackages = await WingetService.GetUpgradablePackagesAsync(includeUnknown, _cts.Token);
            UpdateSourceFilter();
            ApplySourceFilter();
            LoadPackagesToGrid();

            txtUpdatesHeader.Text = includeUnknown
                ? "Actualizaciones disponibles (incluidas desconocidas)"
                : "Actualizaciones disponibles";

            string sufijo = includeUnknown ? " (incluidas desconocidas)" : "";
            txtEstado.Text = _packages.Count == 0
                ? $"No se encontraron actualizaciones disponibles{sufijo}."
                : $"Se encontraron {_packages.Count} actualización(es) disponible(s){sufijo}.";
        }
        catch (OperationCanceledException)
        {
            _packageViewModels.Clear();
            txtEstado.Text = "Consulta cancelada.";
            UpdateSelectionDetails();
        }
        catch (Exception ex)
        {
            txtEstado.Text = "Error al consultar actualizaciones.";
            await ShowDialogAsync("Error", ex.Message);
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
            await ShowDialogAsync("Información", "No hay programas seleccionados para actualizar.");
            return;
        }

        if (runAsAdministrator)
        {
            string adminMessage = packagesToUpdate.Count == 1
                ? "La actualización se ejecutará con permisos de administrador.\n\nWindows mostrará el aviso de UAC y el progreso detallado se reemplazará por un indicador general.\n\n¿Desea continuar?"
                : $"Las {packagesToUpdate.Count} actualizaciones se ejecutarán con permisos de administrador.\n\nWindows pedirá confirmación de UAC una sola vez para todo el lote y el progreso detallado se reemplazará por un indicador general.\n\n¿Desea continuar?";

            if (!await ShowConfirmDialogAsync("Confirmar modo administrador", adminMessage))
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

        ClearLog();

        try
        {
            if (runAsAdministrator)
            {
                (success, failed, cancelled) = await UpdatePackagesAsAdministratorAsync(packagesToUpdate);
                historyChanged = success > 0;
            }
            else
            {
                for (int i = 0; i < packagesToUpdate.Count; i++)
                {
                    var pkg = packagesToUpdate[i];
                    txtEstado.Text = $"Actualizando ({i + 1}/{packagesToUpdate.Count}): {pkg.Name}...";

                    IProgress<WingetProgressInfo>? progressReporter = new Progress<WingetProgressInfo>(info =>
                    {
                        if (info.TotalBytes > 0)
                            UpdateLogDownloadLine(info);
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
                txtEstado.Text = $"Cancelado. Completados: {success}, Fallidos: {failed}.";
                return;
            }

            if (success > 0)
            {
                txtEstado.Text = $"Completada. Éxito: {success}, Fallidos: {failed}. Actualizando lista...";
                shouldReload = true;
            }
            else
            {
                txtEstado.Text = $"Actualización completada. Éxito: {success}, Fallidos: {failed}.";
            }
        }
        catch (OperationCanceledException)
        {
            txtEstado.Text = $"Cancelado. Completados: {success}, Fallidos: {failed}.";
        }
        catch (Exception ex)
        {
            txtEstado.Text = "Error al actualizar programas.";
            await ShowDialogAsync("Error de actualización", ex.Message);
        }
        finally
        {
            if (historyChanged)
                TrySaveSettings("No se pudo guardar el historial de actualizaciones.", updateStatusLabel: false);

            _cts?.Dispose();
            _cts = null;
            SetUIBusy(false);
        }

        if (shouldReload)
            await LoadPackagesAsync(_lastIncludeUnknown);
    }

    private async Task<(int Success, int Failed, bool Cancelled)> UpdatePackagesAsAdministratorAsync(List<WingetPackage> packagesToUpdate)
    {
        var packagesById = packagesToUpdate.ToDictionary(pkg => pkg.Id, StringComparer.OrdinalIgnoreCase);

        txtEstado.Text = packagesToUpdate.Count == 1
            ? $"Actualizando en modo administrador: {packagesToUpdate[0].Name}..."
            : $"Actualizando {packagesToUpdate.Count} programas en modo administrador...";

        AppendLog(packagesToUpdate.Count == 1
            ? "Ejecutando la actualización en una única sesión de administrador."
            : $"Ejecutando {packagesToUpdate.Count} actualizaciones en una única sesión de administrador.");

        var batchResult = await WingetService.UpgradePackagesAsAdministratorAsync(
            packagesToUpdate.Select(p => p.Id),
            _silentMode,
            _cts!.Token,
            new Progress<string>(s => AppendLog(s)),
            new Progress<UpgradeBatchStatusInfo>(status => ReportElevatedBatchStatus(status, packagesById)));

        if (batchResult.UserCancelled)
        {
            AppendLog($"  ✖ {batchResult.ErrorOutput}");
            return (0, 0, true);
        }

        if (batchResult.CancelledAfterCurrentPackage && batchResult.Items.Count == 0)
        {
            AppendLog("La operación se canceló antes de iniciar el lote elevado.");
            return (0, 0, true);
        }

        if (batchResult.Items.Count == 0 && !string.IsNullOrWhiteSpace(batchResult.ErrorOutput))
        {
            AppendLog($"  ✖ {batchResult.ErrorOutput}");
            await ShowDialogAsync("Error de actualización", batchResult.ErrorOutput);
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
                    AppendLog($"[{i + 1}/{packagesToUpdate.Count}] Cancelado antes de iniciar: {pkg.Name} ({pkg.Id})");
                    break;
                }

                failed++;
                AppendLog($"[{i + 1}/{packagesToUpdate.Count}] Resultado no disponible: {pkg.Name} ({pkg.Id})");
                await HandleFailedUpgrade(pkg, "No se recibió un resultado de la actualización elevada.");
                continue;
            }

            txtEstado.Text = $"Procesando resultado ({i + 1}/{packagesToUpdate.Count}): {pkg.Name}...";
            AppendLog($"[{i + 1}/{packagesToUpdate.Count}] Finalizado: {pkg.Name} ({pkg.Id})");

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
                AppendLog("Preparando lote elevado...");
                break;
            case "running" when packagesById.TryGetValue(status.PackageId, out var pkg):
                txtEstado.Text = $"Actualizando ({status.CurrentIndex}/{status.TotalCount}) en modo administrador: {pkg.Name}...";
                AppendLog($"[{status.CurrentIndex}/{status.TotalCount}] En ejecución: {pkg.Name} ({pkg.Id})");
                break;
            case "cancelled":
                AppendLog("Cancelando después del paquete actual...");
                break;
            case "completed":
                AppendLog("Lote elevado finalizado. Procesando resultados...");
                break;
        }
    }

    private void RecordSuccessfulUpgrade(WingetPackage pkg)
    {
        AppendLog($"  \u2714 {pkg.Name}: actualizado correctamente.", LogLineKind.Success);
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
        await ShowDialogAsync("Error de actualización",
            $"No se pudo actualizar \"{pkg.Name}\" (Id: {pkg.Id}).\n\n" +
            $"Motivo: {reason}\n\n" +
            "Por seguridad, la aplicación no abrirá búsquedas web automáticas para descargas manuales.\n" +
            $"Use el Id del paquete ({pkg.Id}) para verificar manualmente el sitio oficial del proveedor o revisar el paquete directamente con winget.");
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
            await ShowDialogAsync("Información", "No hay programas para actualizar.");
            return;
        }
        string lista = string.Join("\n  \u2022 ", pendientes.Take(10).Select(p => p.Name));
        if (pendientes.Count > 10) lista += $"\n  ... y {pendientes.Count - 10} más";
        if (!await ShowConfirmDialogAsync("Confirmar actualización",
                $"Se van a actualizar {pendientes.Count} programa(s):\n\n  \u2022 {lista}\n\n¿Desea continuar?"))
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
        TrySaveSettings("No se pudo guardar el modo de actualización.");
    }

    private void MenuInteractiva_Click(object sender, RoutedEventArgs e)
    {
        _silentMode = false;
        menuSilenciosa.IsChecked = false;
        menuInteractiva.IsChecked = true;
        _settings.SilentMode = false;
        TrySaveSettings("No se pudo guardar el modo de actualización.");
    }

    private void MenuModoOscuro_Click(object sender, RoutedEventArgs e)
    {
        _settings.DarkMode = menuModoOscuro.IsChecked;
        TrySaveSettings("No se pudo guardar la configuración visual.");
        ApplyTheme(_settings.DarkMode);
    }

    private async void MenuExportar_Click(object sender, RoutedEventArgs e)
    {
        if (_packages.Count == 0)
        {
            await ShowDialogAsync("Información", "No hay datos para exportar.");
            return;
        }

        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"actualizaciones_{DateTime.Now:yyyy-MM-dd}"
        };
        picker.FileTypeChoices.Add("CSV", [".csv"]);
        picker.FileTypeChoices.Add("Texto", [".txt"]);

        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        var file = await picker.PickSaveFileAsync();

        if (file is null) return;

        char sep = file.FileType.Equals(".csv", StringComparison.OrdinalIgnoreCase) ? ',' : '\t';
        var sb = new StringBuilder();
        sb.AppendLine(DelimitedTextExporter.BuildRow(sep, "Nombre", "Id", "Versión actual", "Disponible", "Fuente"));
        foreach (var pkg in _packages)
            sb.AppendLine(DelimitedTextExporter.BuildRow(sep, pkg.Name, pkg.Id, pkg.Version, pkg.Available, pkg.Source));

        await Windows.Storage.FileIO.WriteTextAsync(file, sb.ToString());
        txtEstado.Text = $"Lista exportada: {file.Name}";
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
            ApplyTheme(_settings.DarkMode);
        }
    }

    private async void MenuHistorial_Click(object sender, RoutedEventArgs e)
    {
        var historyWindow = new HistoryWindow(_settings.History, _settings.DarkMode);
        historyWindow.Activate();

        var tcs = new TaskCompletionSource();
        historyWindow.Closed += (_, _) => tcs.TrySetResult();
        await tcs.Task;
    }

    // --- DataGrid Events ---

    private void LvPackages_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdateSelectionDetails();

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
        var dp = new DataPackage();
        dp.SetText(pkg.Id);
        Clipboard.SetContent(dp);
        await ShowDialogAsync("Id copiado",
            $"Se copió el Id del paquete al portapapeles:\n\n{pkg.Id}\n\n" +
            "Revise manualmente el paquete y confirme que la descarga proviene del sitio oficial del proveedor.");
    }

    private void CtxExcluir_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedPackage() is not { } pkg) return;
        if (_settings.ExcludedIds.Contains(pkg.Id))
            _settings.ExcludedIds.Remove(pkg.Id);
        else
            _settings.ExcludedIds.Add(pkg.Id);
        TrySaveSettings("No se pudieron guardar las exclusiones.");
        LoadPackagesToGrid();
    }

    // --- Source Filter ---

    private void UpdateSourceFilter()
    {
        string? current = cmbFuente.SelectedIndex > 0 ? (cmbFuente.SelectedItem as ComboBoxItem)?.Content as string : null;
        cmbFuente.SelectionChanged -= CmbFuente_SelectionChanged;
        cmbFuente.Items.Clear();
        cmbFuente.Items.Add(new ComboBoxItem { Content = "Todas las fuentes" });
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
        string sufijo = _lastIncludeUnknown ? " (incluidas desconocidas)" : "";
        txtEstado.Text = _packages.Count == 0
            ? $"No se encontraron actualizaciones disponibles{sufijo}."
            : $"Se encontraron {_packages.Count} actualización(es) disponible(s){sufijo}.";
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
        string bar = BuildProgressBar(percent);
        string line = $"  \u2193  {dl} / {total}  {bar}{speed}";

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
            txtDetalleEstado.Text = _packages.Count == 0 ? EmptySelectionDetails : DefaultSelectionDetails;
            return;
        }

        string state = _settings.ExcludedIds.Contains(pkg.Id)
            ? "Excluido de actualizaciones automaticas"
            : "Listo para actualizar";

        txtDetalleEstado.Text = $"{pkg.Name} | {pkg.Id} | {pkg.Version} -> {pkg.Available} | {pkg.Source} | {state}";
    }

    private void ApplyTheme(bool dark)
    {
        if (Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = dark ? ElementTheme.Dark : ElementTheme.Light;
        }
    }

    private bool TrySaveSettings(string userMessage, bool updateStatusLabel = true)
    {
        if (_settings.Save())
            return true;

        if (updateStatusLabel)
            txtEstado.Text = "No se pudo guardar la configuración.";

        _ = ShowDialogAsync("Error de configuración",
            string.IsNullOrWhiteSpace(_settings.LastSaveError)
                ? userMessage
                : $"{userMessage}\n\n{_settings.LastSaveError}");

        return false;
    }

    private void ShowSettingsLoadWarningIfNeeded()
    {
        if (string.IsNullOrWhiteSpace(_settings.LastLoadError))
            return;
        _ = ShowDialogAsync("Configuración restablecida", _settings.LastLoadError);
    }

    private async Task ShowDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "Aceptar",
            XamlRoot = Content.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private async Task<bool> ShowConfirmDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "Sí",
            CloseButtonText = "No",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
}
