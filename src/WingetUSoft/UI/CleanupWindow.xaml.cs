using System.Collections.ObjectModel;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
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
    private int _logLineCount;

    private AppWindow _appWindow = null!;
    private IntPtr _hWnd;

    public CleanupWindow(AppSettings settings, IEnumerable<WingetPackage> uninstalledPackages)
    {
        InitializeComponent();
        _settings = settings;
        _uninstalledPackages = [.. uninstalledPackages];

        var hWnd     = WindowNative.GetWindowHandle(this);
        _hWnd = hWnd;
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        _appWindow   = AppWindow.GetFromWindowId(windowId);
        _appWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"));
        _appWindow.Resize(new Windows.Graphics.SizeInt32(960, 700));

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();

        if (Content is FrameworkElement contentRoot)
        {
            contentRoot.ActualThemeChanged += (_, _) => UpdateTitleBarButtonColors();
            contentRoot.RequestedTheme = _settings.ThemeMode switch
            {
                1 => ElementTheme.Light,
                2 => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }

        lvItems.ItemsSource = _items;

        ApplyLocalizedStrings();

        Closed += (_, _) => _cts?.Cancel();

        if (Content is FrameworkElement root)
        {
            root.Loaded += async (_, _) =>
            {
                UpdateTitleBarButtonColors();
                await ScanAsync();
            };
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
        btnEliminar.Content = L.T("btn.deleteSelected");
        btnSelAll.Content = L.T("btn.selectAll");
        btnDeselAll.Content = L.T("btn.deselectAll");
        btnCancelar.Content = L.T("btn.cancel");
        txtListHeader.Text = L.T("cleanup.listHeader");
        colRuta.Text = L.T("cleanup.colPath");
        colTipo.Text = L.T("cleanup.colType");
        colTamano.Text = L.T("cleanup.colSize");
        colPrograma.Text = L.T("cleanup.colProgram");
        txtLogHeader.Text = L.T("log.activity");
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

        bool confirmed = await ShowConfirmDialogAsync(
            L.T("cleanup.confirmDeleteTitle"),
            L.T("cleanup.confirmDeleteBody", toDelete.Count));
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
                if (item.IsDirectory)
                    await Task.Run(() => Directory.Delete(item.Path, recursive: true), _cts.Token);
                else
                    File.Delete(item.Path);

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

    private enum LogLineKind { Normal, Success, Error, Warning }

    private void ClearLog()
    {
        rtbLog.Blocks.Clear();
        _logLineCount = 0;
    }

    private void AppendLog(string text, LogLineKind kind = LogLineKind.Normal)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        SolidColorBrush brush = kind switch
        {
            LogLineKind.Success => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 56,  122, 77)),
            LogLineKind.Error   => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 186, 70,  54)),
            LogLineKind.Warning => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 177, 118, 38)),
            _                   => (SolidColorBrush)Application.Current.Resources["TextFillColorPrimaryBrush"]
        };

        var paragraph = new Paragraph();
        paragraph.Inlines.Add(new Run { Text = text, Foreground = brush });
        rtbLog.Blocks.Add(paragraph);
        _logLineCount++;

        if (_logLineCount > 200 && rtbLog.Blocks.Count > 1)
        {
            rtbLog.Blocks.RemoveAt(0);
            _logLineCount--;
        }

        scrollLog.ChangeView(null, scrollLog.ScrollableHeight, null);
    }

    // ---- Theme --------------------------------------------------------------

    private void UpdateTitleBarButtonColors() =>
        TitleBarHelper.UpdateButtonColors(_appWindow, Content, _settings.ThemeMode);

    // ---- Dialogs ------------------------------------------------------------

    private Task ShowDialogAsync(string title, string message) =>
        WindowDialogHelper.ShowDialogAsync(Content.XamlRoot, title, message);

    private Task<bool> ShowConfirmDialogAsync(string title, string message) =>
        WindowDialogHelper.ShowConfirmDialogAsync(Content.XamlRoot, title, message,
            primaryText: L.T("btn.yesDelete"));
}
