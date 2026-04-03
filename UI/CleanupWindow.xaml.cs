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

    public CleanupWindow(AppSettings settings, IEnumerable<WingetPackage> uninstalledPackages)
    {
        InitializeComponent();
        _settings = settings;
        _uninstalledPackages = [.. uninstalledPackages];

        var hWnd     = WindowNative.GetWindowHandle(this);
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

    // ---- Scanning -----------------------------------------------------------

    private async Task ScanAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        _items.Clear();
        SetUIBusy(true);
        txtEstado.Text = "Escaneando residuos...";
        btnEliminar.IsEnabled = false;

        try
        {
            var found = await CleanupScanner.ScanAsync(_uninstalledPackages, _cts.Token);
            foreach (var item in found)
                _items.Add(item);

            string pkgList = string.Join(", ", _uninstalledPackages.Select(p => p.Name));
            if (found.Count == 0)
            {
                txtSubtitulo.Text = $"No se encontraron residuos de: {pkgList}.";
                txtEstado.Text    = "No se encontraron residuos.";
            }
            else
            {
                txtSubtitulo.Text = $"Residuos potenciales de: {pkgList}. Verifica cada elemento antes de eliminar.";
                txtEstado.Text    = $"Se encontraron {found.Count} residuo(s) potencial(es).";
                btnEliminar.IsEnabled = true;
            }
        }
        catch (OperationCanceledException)
        {
            txtEstado.Text = "Escaneo cancelado.";
        }
        catch (Exception ex)
        {
            txtEstado.Text = "Error durante el escaneo.";
            await ShowDialogAsync("Error", ex.Message);
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
            await ShowDialogAsync("Sin selección", "No hay elementos seleccionados para eliminar.");
            return;
        }

        bool confirmed = await ShowConfirmDialogAsync(
            "Confirmar eliminación",
            $"¿Eliminar {toDelete.Count} elemento(s) seleccionado(s)?\n\nEsta acción no se puede deshacer.");
        if (!confirmed) return;

        _cts = new CancellationTokenSource();
        SetUIBusy(true);
        ClearLog();
        int deleted = 0, failed = 0;

        foreach (var item in toDelete)
        {
            if (_cts.IsCancellationRequested) break;

            try
            {
                if (item.IsDirectory)
                    await Task.Run(() => Directory.Delete(item.Path, recursive: true), _cts.Token);
                else
                    File.Delete(item.Path);

                _items.Remove(item);
                AppendLog($"  ✔ Eliminado: {item.Path}", LogLineKind.Success);
                deleted++;
            }
            catch (UnauthorizedAccessException)
            {
                AppendLog($"  ✖ Sin permisos: {item.Path}", LogLineKind.Error);
                failed++;
            }
            catch (Exception ex)
            {
                AppendLog($"  ✖ Error en {System.IO.Path.GetFileName(item.Path)}: {ex.Message}", LogLineKind.Error);
                failed++;
            }
        }

        txtEstado.Text        = $"Completado — Eliminados: {deleted} | Errores: {failed}";
        btnEliminar.IsEnabled = _items.Any(i => i.IsSelected);

        _cts?.Dispose();
        _cts = null;
        SetUIBusy(false);
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
        txtEstado.Text = "Cancelando...";
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

    private void UpdateTitleBarButtonColors()
    {
        if (_appWindow?.TitleBar is not { } titleBar) return;

        bool isDark = Content is FrameworkElement fe
            ? fe.ActualTheme == ElementTheme.Dark
            : _settings.ThemeMode == 2;

        titleBar.ButtonBackgroundColor         = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        if (isDark)
        {
            titleBar.ButtonForegroundColor         = Colors.White;
            titleBar.ButtonHoverForegroundColor    = Colors.White;
            titleBar.ButtonHoverBackgroundColor    = Windows.UI.Color.FromArgb(32, 255, 255, 255);
            titleBar.ButtonPressedForegroundColor  = Colors.White;
            titleBar.ButtonPressedBackgroundColor  = Windows.UI.Color.FromArgb(16, 255, 255, 255);
            titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(128, 255, 255, 255);
        }
        else
        {
            titleBar.ButtonForegroundColor         = Colors.Black;
            titleBar.ButtonHoverForegroundColor    = Colors.Black;
            titleBar.ButtonHoverBackgroundColor    = Windows.UI.Color.FromArgb(32, 0, 0, 0);
            titleBar.ButtonPressedForegroundColor  = Colors.Black;
            titleBar.ButtonPressedBackgroundColor  = Windows.UI.Color.FromArgb(16, 0, 0, 0);
            titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(128, 0, 0, 0);
        }
    }

    // ---- Dialogs ------------------------------------------------------------

    private async Task ShowDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title           = title,
            Content         = message,
            CloseButtonText = "Aceptar",
            XamlRoot        = Content.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private async Task<bool> ShowConfirmDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title               = title,
            Content             = message,
            PrimaryButtonText   = "Sí, eliminar",
            CloseButtonText     = "No",
            DefaultButton       = ContentDialogButton.Close,
            XamlRoot            = Content.XamlRoot
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
}
