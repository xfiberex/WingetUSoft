using System.Collections.ObjectModel;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace WingetUSoft;

/// <summary>
/// Fila de resultados de búsqueda. Envuelve <see cref="WingetSearchResult"/> para exponer a XAML la
/// etiqueta localizada de "ya instalado" (el modelo de Core no conoce la localización ni debe).
/// </summary>
public sealed class SearchResultViewModel(WingetSearchResult result)
{
    public string Name => result.Name;
    public string Id => result.Id;
    public string Version => result.Version;
    public string Source => result.Source;
    public bool IsInstalled => result.IsInstalled;
    public string InstalledLabel => L.T("search.installed");

    /// <summary>
    /// Nombre accesible de la fila. Sin esto, el <c>ListViewItem</c> hereda el <c>ToString()</c> del
    /// ViewModel y un lector de pantalla anuncia "WingetUSoft.SearchResultViewModel" (mismo problema que
    /// se vio en la tabla de la ventana principal al conducir la app real).
    /// </summary>
    public string RowLabel => IsInstalled
        ? L.T("search.rowAccessibleInstalled", Name, Version, Source)
        : L.T("search.rowAccessible", Name, Version, Source);
}

/// <summary>
/// Buscar e instalar software del catálogo de winget.
/// </summary>
/// <remarks>
/// Esta ventana es el **cambio de alcance** del Tier E: hasta la v1.7.0 la app solo gestionaba lo que ya
/// estaba instalado (actualizar / desinstalar). Instalar software nuevo es la única superficie de la app
/// que añade programas al equipo, así que se confirma siempre antes de ejecutar nada.
///
/// No eleva: winget instala como el usuario y cada instalador pide UAC por su cuenta si lo necesita
/// (misma política que el resto de la app, que corre asInvoker).
/// </remarks>
public sealed partial class SearchWindow : Window
{
    private const int LogMaxLines = 400;

    private readonly AppSettings _settings;
    private readonly ObservableCollection<SearchResultViewModel> _results = [];
    private CancellationTokenSource? _cts;
    private int _logLineCount;

    private AppWindow _appWindow = null!;
    private IntPtr _hWnd;

    /// <summary>True si se instaló algo: la ventana principal lo usa para avisar de que su lista pudo quedar obsoleta.</summary>
    public bool InstalledSomething { get; private set; }

    public SearchWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        _hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(_hWnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _appWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"));
        WindowSizer.Apply(_appWindow, _hWnd, designWidthDip: 980, designHeightDip: 720, minWidthDip: 720, minHeightDip: 520);

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        SystemBackdrop = new MicaBackdrop();

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

        lvResults.ItemsSource = _results;
        ApplyLocalizedStrings();
        UpdateTitleBarButtonColors();

        if (Content is FrameworkElement root)
            root.Loaded += (_, _) => txtBuscar.Focus(FocusState.Programmatic);
    }

    private void UpdateTitleBarButtonColors() =>
        TitleBarHelper.UpdateButtonColors(_appWindow, Content, _settings.ThemeMode);

    private void ApplyLocalizedStrings()
    {
        Title = L.T("search.windowTitle");
        txtTitleBar.Text = L.T("search.windowTitle");
        txtHeaderTitle.Text = L.T("search.header");
        txtSubtitulo.Text = L.T("search.subtitle");
        txtBuscar.PlaceholderText = L.T("search.placeholder");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(txtBuscar, L.T("search.placeholderAccessible"));
        btnBuscar.Content = L.T("search.search");
        btnInstalar.Content = L.T("search.install");
        btnCancelar.Content = L.T("btn.cancel");
        txtListHeader.Text = L.T("search.results");
        txtLogHeader.Text = L.T("log.header");
        colNombre.Text = L.T("list.colName");
        colId.Text = L.T("list.colId");
        colVersion.Text = L.T("list.colVersion");
        colFuente.Text = L.T("list.colSource");
        colEstado.Text = L.T("search.colState");
        txtEstado.Text = L.T("search.ready");
        txtContador.Text = "";
    }

    // --- Búsqueda ---

    private async void BtnBuscar_Click(object sender, RoutedEventArgs e) => await SearchAsync();

    /// <summary>Intro en la caja busca: es lo que espera cualquiera que escriba y pulse Enter.</summary>
    private async void TxtBuscar_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            await SearchAsync();
        }
    }

    private async Task SearchAsync()
    {
        string query = txtBuscar.Text.Trim();
        if (string.IsNullOrEmpty(query))
        {
            txtEstado.Text = L.T("search.emptyQuery");
            return;
        }

        _cts = new CancellationTokenSource();
        SetBusy(true);
        _results.Clear();
        txtContador.Text = "";
        txtEstado.Text = L.T("search.searching", query);

        try
        {
            var found = await WingetService.SearchPackagesAsync(query, _cts.Token);

            // Cruce con lo ya instalado: winget search no lo dice, y sin esto el usuario intentaría
            // instalar algo que ya tiene (winget fallaría con un error poco claro).
            var installedIds = await GetInstalledIdsAsync(_cts.Token);
            foreach (var r in found)
                r.IsInstalled = installedIds.Contains(r.Id);

            foreach (var r in found)
                _results.Add(new SearchResultViewModel(r));

            txtEstado.Text = found.Count == 0
                ? L.T("search.noResults", query)
                : L.T("search.found", found.Count);
            txtContador.Text = found.Count == 0 ? "" : L.T("search.countLabel", found.Count);
        }
        catch (OperationCanceledException)
        {
            txtEstado.Text = L.T("search.cancelled");
        }
        catch (Exception ex)
        {
            txtEstado.Text = L.T("search.error");
            AppendLog(L.T("error.genericPrefix", ex.Message), LogLineKind.Error);
        }
        finally
        {
            SetBusy(false);
            _cts?.Dispose();
            _cts = null;
            UpdateInstallButton();
        }
    }

    /// <summary>Ids instalados, en un set case-insensitive. Si la consulta falla, se devuelve vacío: no
    /// saber si algo está instalado no debe impedir buscar.</summary>
    private static async Task<HashSet<string>> GetInstalledIdsAsync(CancellationToken ct)
    {
        try
        {
            var installed = await WingetService.GetInstalledPackagesAsync(ct);
            return new HashSet<string>(installed.Select(p => p.Id), StringComparer.OrdinalIgnoreCase);
        }
        catch (OperationCanceledException) { throw; }
        catch { return new HashSet<string>(StringComparer.OrdinalIgnoreCase); }
    }

    // --- Instalación ---

    private void LvResults_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateInstallButton();

    private void UpdateInstallButton()
    {
        var selected = lvResults.SelectedItem as SearchResultViewModel;
        // Un paquete ya instalado no se "instala" otra vez: para eso está la ventana principal (actualizar).
        btnInstalar.IsEnabled = selected is not null && !selected.IsInstalled && _cts is null;
    }

    private async void BtnInstalar_Click(object sender, RoutedEventArgs e)
    {
        if (lvResults.SelectedItem is not SearchResultViewModel selected) return;

        bool confirmed = await ShowConfirmAsync(
            L.T("search.confirmInstallTitle"),
            L.T("search.confirmInstallBody", selected.Name, selected.Id, selected.Version));
        if (!confirmed) return;

        _cts = new CancellationTokenSource();
        SetBusy(true);
        txtEstado.Text = L.T("search.installing", selected.Name);
        AppendLog(L.T("search.logInstalling", selected.Name, selected.Id), LogLineKind.Accent);

        try
        {
            var result = await WingetService.InstallPackageAsync(
                selected.Id,
                _settings.SilentMode,
                progress: null,
                cancellationToken: _cts.Token,
                logProgress: new Progress<string>(line =>
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        AppendLog(line.TrimEnd());
                }));

            if (result.Success)
            {
                InstalledSomething = true;
                txtEstado.Text = L.T("search.installOk", selected.Name);
                AppendLog(L.T("search.logInstallOk", selected.Name), LogLineKind.Success);
                _settings.AddHistory(new HistoryEntry
                {
                    Date = DateTime.Now,
                    PackageName = selected.Name,
                    PackageId = selected.Id,
                    FromVersion = "",
                    ToVersion = selected.Version,
                    Success = true,
                });
                _settings.Save();

                // Repite la búsqueda para que la fila pase a "Instalado": dejarla como estaba invitaría a
                // instalarlo otra vez.
                await SearchAsync();
            }
            else
            {
                txtEstado.Text = L.T("search.installFailed", selected.Name);
                AppendLog(L.T("search.logInstallFailed", selected.Name, result.ExitCode), LogLineKind.Error);
                await ShowInfoAsync(L.T("error.title"), L.T("search.installFailedBody", selected.Name, result.ExitCode));
            }
        }
        catch (OperationCanceledException)
        {
            txtEstado.Text = L.T("search.installCancelled");
            AppendLog(L.T("search.installCancelled"), LogLineKind.Warning);
        }
        catch (Exception ex)
        {
            txtEstado.Text = L.T("search.installFailed", selected.Name);
            AppendLog(L.T("error.genericPrefix", ex.Message), LogLineKind.Error);
            await ShowInfoAsync(L.T("error.title"), ex.Message);
        }
        finally
        {
            SetBusy(false);
            _cts?.Dispose();
            _cts = null;
            UpdateInstallButton();
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        btnCancelar.IsEnabled = false;
    }

    // --- UI helpers ---

    private void SetBusy(bool busy)
    {
        btnBuscar.IsEnabled = !busy;
        txtBuscar.IsEnabled = !busy;
        btnCancelar.IsEnabled = busy;
        btnInstalar.IsEnabled = !busy && lvResults.SelectedItem is SearchResultViewModel { IsInstalled: false };
        progressRing.IsActive = busy;
    }

    private Task<bool> ShowConfirmAsync(string title, string body) =>
        WindowDialogHelper.ShowConfirmDialogAsync(Content.XamlRoot, title, body);

    private Task ShowInfoAsync(string title, string body) =>
        WindowDialogHelper.ShowDialogAsync(Content.XamlRoot, title, body);

    private void AppendLog(string text, LogLineKind kind = LogLineKind.Normal)
    {
        var paragraph = new Paragraph();
        var run = new Run { Text = text };

        if (kind != LogLineKind.Normal)
            run.Foreground = new SolidColorBrush(LogPalette.For(kind, IsDarkTheme()));

        paragraph.Inlines.Add(run);
        rtbLog.Blocks.Add(paragraph);
        _logLineCount++;

        if (_logLineCount > LogMaxLines)
        {
            rtbLog.Blocks.RemoveAt(0);
            _logLineCount--;
        }

        scrollLog.UpdateLayout();
        scrollLog.ChangeView(null, scrollLog.ScrollableHeight, null);
    }

    /// <summary>
    /// El tema se lee del propio control, no de <c>Application.Current</c>: el tema se fuerza por
    /// elemento, así que con "Claro" sobre un Windows oscuro la aplicación seguiría diciendo "oscuro" y
    /// el registro saldría con los colores contrarios (mismo motivo que en MainWindow, Tier C #4).
    /// </summary>
    private bool IsDarkTheme() => rtbLog.ActualTheme == ElementTheme.Dark;
}
