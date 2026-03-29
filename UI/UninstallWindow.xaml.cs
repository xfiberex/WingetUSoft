using System.Collections.ObjectModel;
using Microsoft.UI;
using Microsoft.UI.Text;
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

    public UninstallWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _appWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"));
        _appWindow.Resize(new Windows.Graphics.SizeInt32(900, 700));

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

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

    // --- Package Loading ---

    private async Task LoadPackagesAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        SetUIBusy(true);
        txtEstado.Text = "Cargando lista de programas instalados...";

        try
        {
            _allPackages = await WingetService.GetInstalledPackagesAsync(_cts.Token);
            ApplyFilter();
            txtEstado.Text = $"Se encontraron {_allPackages.Count} programa(s) instalado(s).";
        }
        catch (OperationCanceledException)
        {
            txtEstado.Text = "Carga cancelada.";
        }
        catch (Exception ex)
        {
            txtEstado.Text = "Error al cargar la lista.";
            await ShowDialogAsync("Error", ex.Message);
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
            ? $"{_allPackages.Count} programa(s)"
            : $"{_packageViewModels.Count} de {_allPackages.Count}";
    }

    // --- Uninstall ---

    private async Task UninstallSelectedAsync()
    {
        if (lvPackages.SelectedItem is not WingetPackage pkg) return;

        bool confirmed = await ShowConfirmDialogAsync(
            "Confirmar desinstalación",
            $"¿Desea desinstalar \"{pkg.Name}\" ({pkg.Id})?\n\nEsta acción no se puede deshacer.");

        if (!confirmed) return;

        _cts = new CancellationTokenSource();
        SetUIBusy(true);
        txtEstado.Text = $"Desinstalando: {pkg.Name}...";
        ClearLog();
        AppendLog($"Iniciando desinstalación: {pkg.Name} ({pkg.Id})");

        try
        {
            var result = await WingetService.UninstallPackageAsync(pkg.Id, _settings.SilentMode, _cts.Token);

            if (result.Success)
            {
                AppendLog($"  ✔ {pkg.Name}: desinstalado correctamente.", LogLineKind.Success);
                txtEstado.Text = $"Desinstalado correctamente: {pkg.Name}";

                var cleanupWin = new CleanupWindow(_settings, [pkg]);
                cleanupWin.Activate();

                await LoadPackagesAsync();
            }
            else
            {
                string reason = result.GetFailureReason();
                AppendLog($"  ✖ {pkg.Name}: {reason}", LogLineKind.Error);
                txtEstado.Text = "Error al desinstalar.";
                await ShowDialogAsync("Error de desinstalación",
                    $"No se pudo desinstalar \"{pkg.Name}\".\n\nMotivo: {reason}");
            }
        }
        catch (OperationCanceledException)
        {
            AppendLog("Desinstalación cancelada.", LogLineKind.Warning);
            txtEstado.Text = "Cancelado.";
        }
        catch (Exception ex)
        {
            AppendLog($"  ✖ Error: {ex.Message}", LogLineKind.Error);
            txtEstado.Text = "Error al desinstalar.";
            await ShowDialogAsync("Error", ex.Message);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            SetUIBusy(false);
        }
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
        txtEstado.Text = "Cancelando...";
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

    private void UpdateTitleBarButtonColors()
    {
        if (_appWindow?.TitleBar is not { } titleBar) return;

        bool isDark = Content is FrameworkElement fe
            ? fe.ActualTheme == ElementTheme.Dark
            : _settings.ThemeMode == 2;

        titleBar.ButtonBackgroundColor         = Microsoft.UI.Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;

        if (isDark)
        {
            titleBar.ButtonForegroundColor         = Microsoft.UI.Colors.White;
            titleBar.ButtonHoverForegroundColor    = Microsoft.UI.Colors.White;
            titleBar.ButtonHoverBackgroundColor    = Windows.UI.Color.FromArgb(32, 255, 255, 255);
            titleBar.ButtonPressedForegroundColor  = Microsoft.UI.Colors.White;
            titleBar.ButtonPressedBackgroundColor  = Windows.UI.Color.FromArgb(16, 255, 255, 255);
            titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(128, 255, 255, 255);
        }
        else
        {
            titleBar.ButtonForegroundColor         = Microsoft.UI.Colors.Black;
            titleBar.ButtonHoverForegroundColor    = Microsoft.UI.Colors.Black;
            titleBar.ButtonHoverBackgroundColor    = Windows.UI.Color.FromArgb(32, 0, 0, 0);
            titleBar.ButtonPressedForegroundColor  = Microsoft.UI.Colors.Black;
            titleBar.ButtonPressedBackgroundColor  = Windows.UI.Color.FromArgb(16, 0, 0, 0);
            titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(128, 0, 0, 0);
        }
    }

    // --- Dialogs ---

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
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
}
