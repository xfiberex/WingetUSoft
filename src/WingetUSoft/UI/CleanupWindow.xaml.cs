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

public sealed partial class CleanupWindow : Window
{
    private readonly AppSettings _settings;
    private readonly IReadOnlyList<WingetPackage> _uninstalledPackages;
    private readonly ObservableCollection<CleanupItemViewModel> _items = [];
    private CancellationTokenSource? _cts;

    private AppWindow _appWindow = null!;
    private IntPtr _hWnd;

    public CleanupWindow(AppSettings settings, IEnumerable<WingetPackage> uninstalledPackages)
    {
        InitializeComponent();

        // La barra de estado es una región activa: sin esto, un lector de pantalla nunca anuncia
        // el progreso ni el resultado, porque el foco está en el botón, no en la barra (T1-07).
        LiveRegion.TrackStatusText(txtEstado);
        _settings = settings;
        _uninstalledPackages = [.. uninstalledPackages];

        (_appWindow, _hWnd) = WindowChrome.Apply(
            this, AppTitleBar, _settings.ThemeMode,
            designWidthDip: 960, designHeightDip: 700, minWidthDip: 720, minHeightDip: 520);

        lvItems.ItemsSource = _items;

        ApplyLocalizedStrings();

        Closed += (_, _) => _cts?.Cancel();

        if (Content is FrameworkElement root)
        {
            root.Loaded += async (_, _) => await ScanAsync();
        }
    }

    private void ApplyLocalizedStrings()
    {
        Title = L.T("cleanup.windowTitle");
        txtTitleBar.Text = L.T("cleanup.titleBar");
        txtHeaderTitle.Text = L.T("cleanup.headerTitle");
        txtSubtitulo.Text = L.T("cleanup.scanning");
        txtWarning.Text = L.T("cleanup.warning");
        btnEscanear.Content = L.T("btn.rescan");
        txtEliminarLabel.Text = L.T("btn.deleteSelected");
        // El contenido es glifo + texto: sin nombre explícito, el lector de pantalla no tendría qué anunciar.
        AutomationProperties.SetName(btnEliminar, txtEliminarLabel.Text);
        btnSelAll.Content = L.T("btn.selectAll");
        btnDeselAll.Content = L.T("btn.deselectAll");
        btnCancelar.Content = L.T("btn.cancel");
        txtListHeader.Text = L.T("cleanup.listHeader");
        colRuta.Text = L.T("cleanup.colPath");
        colTipo.Text = L.T("cleanup.colType");
        colTamano.Text = L.T("cleanup.colSize");
        colPrograma.Text = L.T("cleanup.colProgram");
        txtLogHeader.Text = L.T("log.activity");
        activityLog.SetAccessibleName(L.T("log.activity"));
        if (!progressRing.IsActive) txtEstado.Text = L.T("status.ready");
    }

    // ---- Scanning -----------------------------------------------------------

    private async Task ScanAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        _items.Clear();
        SetUIBusy(true);
        txtEstado.Text = L.T("cleanup.scanningStatus");
        btnEliminar.IsEnabled = false;

        try
        {
            var found = await CleanupScanner.ScanAsync(_uninstalledPackages, _cts.Token);
            foreach (var item in found)
                _items.Add(item);

            string pkgList = string.Join(", ", _uninstalledPackages.Select(p => p.Name));
            if (found.Count == 0)
            {
                txtSubtitulo.Text = L.T("cleanup.noResiduesFound", pkgList);
                txtEstado.Text    = L.T("cleanup.noResiduesStatus");
            }
            else
            {
                txtSubtitulo.Text = L.T("cleanup.potentialResidues", pkgList);
                txtEstado.Text    = L.T("cleanup.foundResidues", found.Count);
                btnEliminar.IsEnabled = true;
            }
        }
        catch (OperationCanceledException)
        {
            txtEstado.Text = L.T("cleanup.scanCancelled");
        }
        catch (Exception ex)
        {
            txtEstado.Text = L.T("cleanup.scanError");
            await ShowDialogAsync(L.T("error.title"), ex.Message);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            SetUIBusy(false);
        }
    }

    // ---- Deletion -----------------------------------------------------------

    private async Task DeleteSelectedAsync()
    {
        var toDelete = _items.Where(i => i.IsSelected).ToList();
        if (toDelete.Count == 0)
        {
            await ShowDialogAsync(L.T("cleanup.noSelectionTitle"), L.T("cleanup.noSelectionBody"));
            return;
        }

        // Borrado irreversible (y recursivo en las carpetas): la última pantalla antes de ejecutar
        // tiene que nombrar las rutas concretas, no solo cuántas son. Mismo formato que la
        // confirmación de "Actualizar todo" en MainWindow.
        string lista = string.Join("\n  • ", toDelete.Take(10).Select(i => i.Path));
        if (toDelete.Count > 10) lista += L.T("list.andMore", toDelete.Count - 10);

        // Irreversible y recursivo: Cancelar es el botón por defecto, así que Intro no borra (F-04).
        bool confirmed = await WindowDialogHelper.ShowConfirmDialogAsync(
            Content.XamlRoot,
            L.T("cleanup.confirmDeleteTitle"),
            L.T("cleanup.confirmDeleteBody", toDelete.Count, lista),
            L.T("cleanup.confirmPrimary"),
            destructive: true);
        if (!confirmed) return;

        _cts = new CancellationTokenSource();
        SetUIBusy(true);
        ClearLog();
        int deleted = 0, failed = 0;
        var opStopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < toDelete.Count; i++)
        {
            if (_cts.IsCancellationRequested) break;
            var item = toDelete[i];
            TaskbarProgress.SetValue(_hWnd, i * 100 / toDelete.Count);

            try
            {
                // Las dos ramas van al hilo de fondo. Borrar un archivo parece barato, pero no lo es
                // siempre: sobre una unidad de red o un disco dormido, un solo File.Delete bloquea el
                // hilo de UI y la ventana se queda sin repintar justo mientras informa del progreso.
                await Task.Run(() =>
                {
                    if (item.IsDirectory) Directory.Delete(item.Path, recursive: true);
                    else File.Delete(item.Path);
                }, _cts.Token);

                _items.Remove(item);
                AppendLog(L.T("cleanup.deletedLog", item.Path), LogLineKind.Success);
                deleted++;
            }
            catch (UnauthorizedAccessException)
            {
                AppendLog(L.T("cleanup.noPermissionLog", item.Path), LogLineKind.Error);
                failed++;
            }
            catch (Exception ex)
            {
                AppendLog(L.T("cleanup.deleteErrorLog", System.IO.Path.GetFileName(item.Path), ex.Message), LogLineKind.Error);
                failed++;
            }
        }

        txtEstado.Text        = L.T("cleanup.completedStatus", deleted, failed);
        btnEliminar.IsEnabled = _items.Any(i => i.IsSelected);

        TaskbarProgress.Clear(_hWnd);
        bool cancelled = _cts.IsCancellationRequested;
        _cts?.Dispose();
        _cts = null;
        SetUIBusy(false);

        if (Notifier.ShouldNotify(opStopwatch.Elapsed, _settings.ShowNotifications, cancelled, TimeSpan.FromSeconds(10)))
            Notifier.OperationFinished(_hWnd);
    }

    // ---- UI Helpers ---------------------------------------------------------

    private void SetUIBusy(bool busy)
    {
        btnEscanear.IsEnabled = !busy;
        btnEliminar.IsEnabled = !busy && _items.Any(i => i.IsSelected);
        btnSelAll.IsEnabled   = !busy;
        btnDeselAll.IsEnabled = !busy;
        btnCancelar.IsEnabled = busy;
        progressRing.IsActive = busy;
    }

    // ---- Event Handlers -----------------------------------------------------

    private async void BtnEscanear_Click(object sender, RoutedEventArgs e) =>
        await ScanAsync();

    private async void BtnEliminar_Click(object sender, RoutedEventArgs e) =>
        await DeleteSelectedAsync();

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        btnCancelar.IsEnabled = false;
        txtEstado.Text = L.T("status.cancelling");
    }

    private void BtnSelAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _items)
            item.IsSelected = true;
        btnEliminar.IsEnabled = _items.Count > 0;
    }

    private void BtnDeselAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _items)
            item.IsSelected = false;
        btnEliminar.IsEnabled = false;
    }

    private void OnItemCheckChanged(object sender, RoutedEventArgs e)
    {
        btnEliminar.IsEnabled = _cts is null && _items.Any(i => i.IsSelected);
    }

    // ---- Logging ------------------------------------------------------------

    private void ClearLog() => activityLog.Clear();

    private void AppendLog(string text, LogLineKind kind = LogLineKind.Normal) =>
        activityLog.Append(text, kind);

    // ---- Dialogs ------------------------------------------------------------

    private Task ShowDialogAsync(string title, string message) =>
        WindowDialogHelper.ShowDialogAsync(Content.XamlRoot, title, message);
}
