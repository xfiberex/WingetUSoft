using System.Collections.ObjectModel;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace WingetUSoft;

public sealed partial class UninstallWindow : Window
{
    private readonly AppSettings _settings;
    private readonly ObservableCollection<WingetPackage> _packageViewModels = [];
    private List<WingetPackage> _allPackages = [];
    private string _searchFilter = "";
    private CancellationTokenSource? _cts;
    private bool _initialized;
    private int _logLineCount;

    private AppWindow _appWindow = null!;
    private IntPtr _hWnd;

    public UninstallWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        var hWnd = WindowNative.GetWindowHandle(this);
        _hWnd = hWnd;
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _appWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"));
        _appWindow.Resize(new Windows.Graphics.SizeInt32(900, 700));

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();

        if (Content is FrameworkElement contentRoot)
        {
            contentRoot.ActualThemeChanged += (_, _) => UpdateTitleBarButtonColors();
            // Apply same theme as main settings
            contentRoot.RequestedTheme = _settings.ThemeMode switch
            {
                1 => ElementTheme.Light,
                2 => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }

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
        btnUninstall.Content = L.T("uninstall.uninstallSelected");
        btnCancelar.Content = L.T("btn.cancel");
        txtBuscarLabel.Text = L.T("search.label");
        txtBuscar.PlaceholderText = L.T("search.placeholder");
        txtListHeader.Text = L.T("uninstall.listHeader");
        colNombre.Text = L.T("list.colName");
        colId.Text = L.T("list.colId");
        colVersion.Text = L.T("list.colVersion");
        colFuente.Text = L.T("list.colSource");
        txtLogHeader.Text = L.T("log.activity");
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
            _packageViewModels.Add(pkg);

        txtContador.Text = _packageViewModels.Count == _allPackages.Count
            ? L.T("uninstall.countAll", _allPackages.Count)
            : L.T("uninstall.countFiltered", _packageViewModels.Count, _allPackages.Count);
    }

    // --- Uninstall ---

    private async Task UninstallSelectedAsync()
    {
        if (lvPackages.SelectedItem is not WingetPackage pkg) return;

        bool confirmed = await ShowConfirmDialogAsync(
            L.T("uninstall.confirmTitle"),
            L.T("uninstall.confirmBody", pkg.Name, pkg.Id));

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
            LogLineKind.Success => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 56, 122, 77)),
            LogLineKind.Error   => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 186, 70, 54)),
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

    // --- Theme ---

    private void UpdateTitleBarButtonColors() =>
        TitleBarHelper.UpdateButtonColors(_appWindow, Content, _settings.ThemeMode);

    // --- Dialogs ---

    private Task ShowDialogAsync(string title, string message) =>
        WindowDialogHelper.ShowDialogAsync(Content.XamlRoot, title, message);

    private Task<bool> ShowConfirmDialogAsync(string title, string message) =>
        WindowDialogHelper.ShowConfirmDialogAsync(Content.XamlRoot, title, message);
}
