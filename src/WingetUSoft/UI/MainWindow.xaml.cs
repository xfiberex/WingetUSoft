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

public sealed partial class MainWindow : Window
{

    /// <summary>Estado de la consulta, que decide qué muestra el panel superpuesto a la tabla.</summary>
    private enum ListState { Initial, Loading, Ready, Cancelled, Error }

    /// <summary>Paquete que falló al actualizar, acumulado para el resumen único del final del lote.</summary>

    private ListState _listState = ListState.Initial;
    private readonly List<FailedUpgrade> _failedUpgrades = [];

    /// <summary>
    /// Ids marcados con la casilla. Es la fuente de verdad de la selección, no el ViewModel: buscar,
    /// ordenar o filtrar reconstruye las filas, y antes eso borraba lo que el usuario llevaba marcado.
    /// No confundir con <c>lvPackages.SelectedItem</c>, que es la fila resaltada para ver detalles.
    /// </summary>
    private readonly HashSet<string> _selectedIds = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Evita recontar la selección una vez por fila durante un marcado masivo o una recarga.</summary>
    private bool _suppressSelectionSync;

    /// <summary>Si las acciones están habilitadas por estado (winget disponible, no ocupado).</summary>
    private bool _actionsEnabled = true;

    private readonly ObservableCollection<PackageViewModel> _packageViewModels = [];
    private List<WingetPackage> _allPackages = [];
    private List<WingetPackage> _packages = [];
    private bool _silentMode = true;
    private bool _lastIncludeUnknown;
    private CancellationTokenSource? _cts;
    /// <summary>Preferencias del usuario, leídas de disco.</summary>
    /// <remarks>
    /// Se carga en el constructor y no en un inicializador de campo: ahí la E/S ocurre **antes** del
    /// cuerpo del constructor y antes de <c>InitializeComponent</c>, así que un fallo de disco
    /// reventaría con la ventana a medio construir y sin nada donde mostrar el aviso (T4-02).
    /// </remarks>
    private readonly AppSettings _settings;
    private DispatcherTimer? _autoCheckTimer;
    private bool _cancelStopsCurrentProcess = true;
    /// <summary>Escritor del registro a disco. Encola y escribe fuera del hilo de UI (T2-01).</summary>
    private readonly FileLog _fileLog;
    private bool _initialized;
    private int _excludedFilter = 0;
    private string _searchFilter = "";
    private int _sortColumn = 0;   // 0=none 1=Name 2=Id 3=Version 4=Available 5=Source
    private bool _sortDescending = false;
    private CancellationTokenSource? _packageInfoCts;
    private string? _appUpdateUrl;
    private string? _appUpdateChecksumUrl;
    private H.NotifyIcon.TaskbarIcon? _trayIcon;
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
    private MenuFlyoutItem ctxOmitirVersion = null!;

    /// <summary>Fila de <c>ContentGrid</c> con la tabla y el registro; las anteriores son las tarjetas superiores.</summary>
    private const int TableRow = 3;

    /// <summary>
    /// El alto mínimo de la página se recalcula cada vez que cambia una tarjeta superior: la de filtros crece al
    /// estrechar la ventana (los botones pasan a otra línea) y la cabecera, al abrirse el aviso de actualización.
    /// </summary>
    private void TrackContentMinHeight()
    {
        foreach (var card in ContentGrid.Children.OfType<FrameworkElement>().Where(e => Grid.GetRow(e) < TableRow))
            card.SizeChanged += (_, _) => UpdateContentMinHeight();
    }

    /// <summary>
    /// Por debajo de este alto, la página desplaza en lugar de seguir encogiendo la tabla (F-01): las tarjetas
    /// superiores tal como miden ahora, más el piso de la fila de la tabla, más el relleno.
    /// </summary>
    private void UpdateContentMinHeight()
    {
        double cards = ContentGrid.Children.OfType<FrameworkElement>()
            .Where(e => Grid.GetRow(e) < TableRow && e.Visibility == Visibility.Visible)
            .Sum(e => e.ActualHeight + e.Margin.Top + e.Margin.Bottom);

        ContentGrid.MinHeight = ContentGrid.Padding.Top + cards + ContentGrid.RowDefinitions[TableRow].MinHeight
            + ContentGrid.Padding.Bottom;
    }

    public MainWindow()
    {
        InitializeComponent();
        TrackContentMinHeight();

        _settings = AppSettings.Load();

        // La barra de estado es una región activa: sin esto, un lector de pantalla nunca anuncia
        // el progreso ni el resultado, porque el foco está en el botón, no en la barra (T1-07).
        LiveRegion.TrackStatusText(txtEstado);
        _fileLog = new FileLog(OnFileLogFailed);

        // Purga de registros viejos: al arrancar es el único momento en que nadie escribe todavía.
        // En segundo plano porque toca disco y no hay nada que esperar de ella (T2-02).
        _ = Task.Run(() => AppSettings.PurgeOldLogs());

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
        // El texto se recalcula al abrir el menú (ver LvPackages_RightTapped): omitir es un interruptor,
        // y ofrecer "Omitir esta versión" sobre una fila ya omitida sería mentirle al usuario.
        ctxOmitirVersion = new MenuFlyoutItem { Text = L.T("ctx.skipVersion") };
        ctxOmitirVersion.Click += CtxOmitirVersion_Click;
        ctxMenuRow = new MenuFlyout();
        ctxMenuRow.Items.Add(ctxActualizar);
        ctxMenuRow.Items.Add(new MenuFlyoutSeparator());
        ctxMenuRow.Items.Add(ctxCopiarNombre);
        ctxMenuRow.Items.Add(ctxCopiarId);
        ctxMenuRow.Items.Add(new MenuFlyoutSeparator());
        ctxMenuRow.Items.Add(ctxBuscarWeb);
        ctxMenuRow.Items.Add(new MenuFlyoutSeparator());
        ctxMenuRow.Items.Add(ctxOmitirVersion);
        ctxMenuRow.Items.Add(ctxExcluir);

        // RecolorLog va como trabajo extra del cambio de tema: los colores del registro son por
        // tema (ver LogPalette) y esta es la unica ventana que repinta lo ya escrito.
        (_appWindow, _hWnd) = WindowChrome.Apply(
            this, AppTitleBar, _settings.ThemeMode,
            designWidthDip: 1180, designHeightDip: 820, minWidthDip: 900, minHeightDip: 600,
            onThemeChanged: RecolorLog);

        // El mismo icono, dentro de la barra de titulo personalizada: al extender el contenido
        // sobre la barra Windows deja de dibujar el icono del sistema, así que hay que pintarlo a
        // mano o la ventana queda con titulo pero sin marca.
        TitleBarIcon.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(WindowChrome.IconPath));

        _appWindow.Closing += OnAppWindowClosing;

        lvPackages.ItemsSource = _packageViewModels;

        _silentMode = _settings.SilentMode;
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
        ApplyLocalizedStrings();

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
                    txtEstado.Text = L.T("winget.unavailableStatus");
                    txtInfoDescripcion.Text = L.T("winget.unavailableDetail");
                    await ShowDialogAsync(L.T("winget.unavailableTitle"), L.T("winget.unavailableBody"));
                    return;
                }
                // La reserva no inventa un número: si el ensamblado no declarase versión —cosa que no
                // pasa, la estampa el .csproj— una cadena cableada solo podría mentir, y ya se había
                // quedado en "v1.1.0" mientras la app iba por la 1.8.
                var appVer = typeof(MainWindow).Assembly.GetName().Version;
                _appVersionStr = appVer is not null
                    ? $"v{appVer.Major}.{appVer.Minor}.{appVer.Build}"
                    : "";
                Title = L.T("app.titleBase");
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
        // Por defecto WinUI muestra un tooltip con la tecla del atajo (ese "F5" flotante al detener el
        // puntero). Los atajos ya se listan en la barra superior, así que el globito solo estorba: lo
        // ocultamos para todos los aceleradores enganchados a Content.
        Content.KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden;

        // F5 - Refresh
        var f5 = new KeyboardAccelerator { Key = Windows.System.VirtualKey.F5 };
        f5.Invoked += (_, _) => { if (_cts is null) _ = LoadPackagesAsync(_lastIncludeUnknown); };
        Content.KeyboardAccelerators.Add(f5);

        // Ctrl+A - Marcar todo (para desmarcar, la casilla de la cabecera)
        var ctrlA = new KeyboardAccelerator
        {
            Key = Windows.System.VirtualKey.A,
            Modifiers = Windows.System.VirtualKeyModifiers.Control
        };
        ctrlA.Invoked += (_, args) =>
        {
            if (IsTextInputFocused()) return;   // deja que Ctrl+A seleccione el texto del cuadro
            SetAllVisibleSelected(true);
            args.Handled = true;
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
        del.Invoked += (_, args) =>
        {
            if (IsTextInputFocused()) return;   // en el buscador, Supr borra caracteres
            if (GetSelectedPackage() is not null)
            {
                CtxExcluir_Click(null, null);
                args.Handled = true;
            }
        };
        Content.KeyboardAccelerators.Add(del);
    }

    /// <summary>
    /// Los aceleradores viven en la raíz del contenido, así que se disparan también con el foco dentro
    /// del buscador: sin esta guarda, Ctrl+A marcaba todos los paquetes en vez de seleccionar el texto,
    /// y Supr excluía un paquete en vez de borrar un carácter.
    /// </summary>
    private bool IsTextInputFocused() =>
        FocusManager.GetFocusedElement(Content.XamlRoot) is TextBox or PasswordBox or AutoSuggestBox or RichEditBox;

    private void SetActionButtonsEnabled(bool enabled)
    {
        _actionsEnabled = enabled;
        btnConsultar.IsEnabled = enabled;
        btnConsultarDesconocidas.IsEnabled = enabled;
        btnActualizarTodo.IsEnabled = enabled;
        // "Actualizar seleccionados" depende además de que haya algo marcado.
        UpdateSelectionSummary();
    }

    // --- Selección por casilla (independiente de la fila resaltada) ---

    /// <summary>Paquetes marcados que el lote puede actualizar. Sigue a <see cref="_selectedIds"/>, no a
    /// las filas visibles: una búsqueda activa oculta filas pero no desmarca lo que el usuario ya eligió.</summary>
    private List<WingetPackage> GetCheckedPackages() =>
        [.. _packages.Where(p => _selectedIds.Contains(p.Id) && IsUpgradable(p))];

    /// <summary>
    /// Un paquete entra en un lote si no está excluido (permanente) ni tiene **esta** versión omitida.
    /// Es el único sitio donde se decide: lo usan tanto "Actualizar seleccionados" como "Actualizar todo".
    /// </summary>
    private bool IsUpgradable(WingetPackage p) =>
        !_settings.ExcludedIds.Contains(p.Id)
        && !SkippedVersions.IsSkipped(_settings.SkippedVersions, p.Id, p.Available);

    private void OnPackageSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PackageViewModel.IsSelected)) return;
        if (sender is not PackageViewModel vm) return;

        if (vm.IsSelected) _selectedIds.Add(vm.Id);
        else _selectedIds.Remove(vm.Id);

        if (!_suppressSelectionSync) UpdateSelectionSummary();
    }

    /// <summary>Refleja la selección en el botón de actualizar y en la casilla tri-estado de la cabecera.</summary>
    private void UpdateSelectionSummary()
    {
        int checkedCount = GetCheckedPackages().Count;

        btnActualizarSeleccionados.Content = checkedCount == 0
            ? L.T("btn.updateSelected")
            : L.T("btn.updateSelectedCount", checkedCount);

        // Deshabilitado sin nada marcado: antes se podía pulsar y la única respuesta era un diálogo
        // "no hay programas seleccionados".
        btnActualizarSeleccionados.IsEnabled = _actionsEnabled && checkedCount > 0;

        // Las filas excluidas no cuentan: su casilla está deshabilitada y nunca se actualizan.
        var selectable = _packageViewModels.Where(v => v.IsSelectable).ToList();
        int selectableChecked = selectable.Count(v => v.IsSelected);

        chkSelectAll.IsEnabled = selectable.Count > 0 && _actionsEnabled;
        // Indeterminado (null) = selección parcial. Asignar IsChecked por código no dispara Click,
        // así que esto no reentra en ChkSelectAll_Click.
        chkSelectAll.IsChecked = selectable.Count > 0 && selectableChecked == selectable.Count
            ? true
            : selectableChecked == 0 ? false : null;
    }

    private void ChkSelectAll_Click(object sender, RoutedEventArgs e)
    {
        // Un clic significa "marcar todo" o "desmarcar todo". El ciclo por defecto de un CheckBox
        // tri-estado dejaría pasar al usuario por el estado indeterminado, que aquí no significa nada.
        SetAllVisibleSelected(_packageViewModels.Any(v => v.IsSelectable && !v.IsSelected));
    }

    private void SetAllVisibleSelected(bool selected)
    {
        _suppressSelectionSync = true;
        foreach (var vm in _packageViewModels.Where(v => v.IsSelectable))
            vm.IsSelected = selected;
        _suppressSelectionSync = false;

        UpdateSelectionSummary();
    }

    private void SetUIBusy(bool busy)
    {
        SetActionButtonsEnabled(!busy);
        btnCancelar.IsEnabled = busy;
        lvPackages.IsEnabled = !busy;
        progressRing.IsActive = busy;
        if (!busy)
        {
            _cancelStopsCurrentProcess = true;
            HideProgress();
        }
    }

    /// <summary>Barra de progreso sin fin: la operación avanza pero no sabemos cuánto falta.</summary>
    private void ShowIndeterminateProgress()
    {
        pbGlobal.IsIndeterminate = true;
        pbGlobal.Visibility = Visibility.Visible;
    }

    /// <summary>Barra de progreso 0..<paramref name="total"/>, avanzada con <see cref="SetProgressValue"/>.</summary>
    private void ShowDeterminateProgress(double total)
    {
        pbGlobal.IsIndeterminate = false;
        pbGlobal.Minimum = 0;
        pbGlobal.Maximum = total;
        pbGlobal.Value = 0;
        pbGlobal.Visibility = Visibility.Visible;
    }

    private void SetProgressValue(double value) => pbGlobal.Value = Math.Clamp(value, pbGlobal.Minimum, pbGlobal.Maximum);

    private void HideProgress()
    {
        pbGlobal.Visibility = Visibility.Collapsed;
        pbGlobal.IsIndeterminate = false;
        pbGlobal.Value = 0;
    }

    /// <summary>
    /// Sincroniza el panel superpuesto a la tabla con el estado actual: consultando, sin datos aún,
    /// todo al día, sin coincidencias con los filtros, o consulta cancelada/fallida. Cuando hay filas
    /// que mostrar el panel se oculta y la tabla queda visible.
    /// </summary>
    private void UpdateListState()
    {
        switch (_listState)
        {
            case ListState.Loading:
                ShowListState(L.T("list.stateLoadingTitle"), L.T("list.stateLoadingBody"), loading: true);
                return;

            case ListState.Cancelled:
                ShowListState(L.T("list.stateCancelledTitle"), L.T("list.stateCancelledBody"), Glyph.Sync);
                return;

            case ListState.Error:
                ShowListState(L.T("list.stateErrorTitle"), L.T("list.stateErrorBody"), Glyph.Warning);
                return;
        }

        if (_packageViewModels.Count > 0)
        {
            ringListState.IsActive = false;
            panelListState.Visibility = Visibility.Collapsed;
            return;
        }

        if (_listState == ListState.Initial)
            ShowListState(L.T("list.stateInitialTitle"), L.T("list.stateInitialBody"), Glyph.Sync);
        else if (_packages.Count == 0)
            ShowListState(L.T("list.stateUpToDateTitle"), L.T("list.stateUpToDateBody"), Glyph.CheckMark);
        else if (_searchFilter.Trim() is { Length: > 0 } search)
            ShowListState(L.T("list.stateNoMatchTitle"), L.T("list.stateNoMatchSearch", search), Glyph.Search);
        else
            ShowListState(L.T("list.stateNoMatchTitle"), L.T("list.stateNoMatchFilters"), Glyph.Search);
    }

    /// <summary>Glifos de Segoe MDL2 Assets usados por el panel de estado de la tabla.</summary>
    private static class Glyph
    {
        public const string Sync = "\uE895";
        public const string CheckMark = "\uE73E";
        public const string Search = "\uE721";
        public const string Warning = "\uE7BA";
    }

    private void ShowListState(string title, string body, string glyph = "", bool loading = false)
    {
        txtListStateTitle.Text = title;
        txtListStateBody.Text = body;

        ringListState.IsActive = loading;
        ringListState.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
        iconListState.Visibility = loading ? Visibility.Collapsed : Visibility.Visible;
        if (!loading) iconListState.Glyph = glyph;

        panelListState.Visibility = Visibility.Visible;
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
            new PackageViewModel(pkg)
            {
                IsExcluded = _settings.ExcludedIds.Contains(pkg.Id),
                IsVersionSkipped = SkippedVersions.IsSkipped(_settings.SkippedVersions, pkg.Id, pkg.Available),
                IsSelected = _selectedIds.Contains(pkg.Id),   // la marca sobrevive a buscar/ordenar/filtrar
            });

        if (_excludedFilter == 1) viewModels = viewModels.Where(v => !v.IsExcluded);
        else if (_excludedFilter == 2) viewModels = viewModels.Where(v => v.IsExcluded);

        viewModels = ApplySort(viewModels);

        // La suscripción va después de construir el ViewModel: el IsSelected del inicializador de
        // arriba solo restaura lo que ya está en _selectedIds y no debe recontar nada.
        foreach (var vm in viewModels)
        {
            vm.PropertyChanged += OnPackageSelectionChanged;
            _packageViewModels.Add(vm);
        }

        UpdateSelectionDetails();
        UpdateSelectionSummary();
        UpdateListState();
    }

    private IEnumerable<PackageViewModel> ApplySort(IEnumerable<PackageViewModel> items)
    {
        // Nombre/Id/Fuente son texto y se ordenan como texto. Versión y Disponible NO: compararlas
        // carácter a carácter pone "1.10.0" antes que "1.9.0". Ver VersionOrder.
        return _sortColumn switch
        {
            1 => Order(v => v.Name, StringComparer.CurrentCultureIgnoreCase),
            2 => Order(v => v.Id, StringComparer.OrdinalIgnoreCase),
            3 => Order(v => v.Version, VersionOrder.Comparer!),
            4 => Order(v => v.Available, VersionOrder.Comparer!),
            5 => Order(v => v.Source, StringComparer.OrdinalIgnoreCase),
            _ => items
        };

        IEnumerable<PackageViewModel> Order(Func<PackageViewModel, string> key, IComparer<string> comparer) =>
            _sortDescending
                ? items.OrderByDescending(key, comparer)
                : items.OrderBy(key, comparer);
    }

    private async Task LoadPackagesAsync(bool includeUnknown)
    {
        _lastIncludeUnknown = includeUnknown;
        _cancelStopsCurrentProcess = true;
        _cts = new CancellationTokenSource();
        SetUIBusy(true);
        ShowIndeterminateProgress();

        // La tabla se vacía al empezar: dejar las filas de la consulta anterior bajo el panel
        // "Consultando..." sería enseñar datos que ya estamos reemplazando. La selección también se
        // descarta: la consulta trae una lista nueva y marcar por Id lo que ya no está no significa nada.
        _listState = ListState.Loading;
        _selectedIds.Clear();
        _packageViewModels.Clear();
        UpdateSelectionSummary();
        UpdateListState();

        txtEstado.Text = includeUnknown
            ? L.T("status.queryingUpdatesUnknown")
            : L.T("status.queryingUpdates");

        try
        {
            _allPackages = await WingetService.GetUpgradablePackagesAsync(includeUnknown, _cts.Token);
            _listState = ListState.Ready;

            // Las omisiones caducan solas: si el paquete ya no aparece (se actualizó por fuera o se
            // desinstaló) o winget ofrece otra versión, la omisión guardada no significa nada. Sin esta
            // poda, settings.json acumularía omisiones muertas para siempre.
            if (SkippedVersions.Prune(_settings.SkippedVersions, _allPackages) > 0)
                TrySaveSettings(L.T("msg.saveSkippedError"));

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
            _listState = ListState.Cancelled;
            _packageViewModels.Clear();
            txtEstado.Text = L.T("status.queryCancelled");
            UpdateSelectionDetails();
        }
        catch (Exception ex)
        {
            _listState = ListState.Error;
            _packageViewModels.Clear();
            txtEstado.Text = L.T("status.queryError");
            AppendLog(L.T("error.genericPrefix", ex.Message), LogLineKind.Error);
            await ShowDialogAsync(L.T("error.title"), ex.Message);
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            SetUIBusy(false);
            UpdateListState();
        }
    }

    // --- Button Event Handlers ---

    private async void BtnConsultar_Click(object sender, RoutedEventArgs e) =>
        await LoadPackagesAsync(includeUnknown: false);

    private async void BtnConsultarDesconocidas_Click(object sender, RoutedEventArgs e) =>
        await LoadPackagesAsync(includeUnknown: true);

    private async void BtnActualizarSeleccionados_Click(object sender, RoutedEventArgs e) =>
        await UpdatePackagesAsync(GetCheckedPackages());

    private async void BtnActualizarTodo_Click(object sender, RoutedEventArgs e)
    {
        var pendientes = _packages.Where(IsUpgradable).ToList();
        if (pendientes.Count == 0)
        {
            await ShowDialogAsync(L.T("info.title"), L.T("msg.noPackagesToUpdate"));
            return;
        }
        string lista = string.Join("\n  \u2022 ", pendientes.Take(10).Select(p => p.Name));
        if (pendientes.Count > 10) lista += L.T("list.andMore", pendientes.Count - 10);
        if (!await ShowConfirmDialogAsync(L.T("confirm.updateTitle"),
                L.T("confirm.updateBody", pendientes.Count, lista),
                L.T("confirm.updatePrimary")))
            return;
        await UpdatePackagesAsync(pendientes);
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        btnCancelar.IsEnabled = false;
        txtEstado.Text = GetCancelStatusText();
    }

    // --- Helpers ---

    private WingetPackage? GetSelectedPackage()
    {
        return (lvPackages.SelectedItem as PackageViewModel)?.Package;
    }

    /// <summary>
    /// Sin paquete seleccionado, el panel de información indica qué hacer a continuación. Con uno, el panel
    /// muestra su descripción, que carga <see cref="LoadPackageInfoPanelAsync"/>; hasta F-10 aquí se
    /// escribía además una línea «nombre | id | versiones | origen» que repetía lo que ya dice la fila.
    /// </summary>
    private void UpdateSelectionDetails()
    {
        if (GetSelectedPackage() is not null) return;

        txtInfoDescripcion.Text = _packages.Count == 0 ? L.T("header.detailEmpty") : L.T("header.detailDefault");
    }

    private void ApplyTheme(int themeMode)
    {
        if (Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = WindowChrome.ToElementTheme(themeMode);
        }
    }

    private bool TrySaveSettings(string userMessage, bool updateStatusLabel = true)
    {
        if (_settings.Save())
            return true;

        if (updateStatusLabel)
            txtEstado.Text = L.T("error.saveConfigStatus");

        _ = ShowDialogAsync(L.T("error.configTitle"),
            _settings.LastSaveError is null
                ? userMessage
                : $"{userMessage}\n\n{_settings.LastSaveError.Text}");

        return false;
    }

    private void ShowSettingsLoadWarningIfNeeded()
    {
        if (_settings.LastLoadError is null)
            return;
        _ = ShowDialogAsync(L.T("msg.configResetTitle"), _settings.LastLoadError.Text);
    }

    private async void MenuWhatsNew_Click(object sender, RoutedEventArgs e) =>
        await ShowWhatsNewAsync();

    private async void MenuAcercaDe_Click(object sender, RoutedEventArgs e)
    {
        var dlg = WindowDialogHelper.Prepare(new AboutDialog(), Content.XamlRoot);
        await dlg.ShowAsync();
    }

    private async void MenuLicencia_Click(object sender, RoutedEventArgs e) =>
        await ShowLegalTextAsync(L.T("menu.license"), LegalText.License());

    private async void MenuAvisosTerceros_Click(object sender, RoutedEventArgs e) =>
        await ShowLegalTextAsync(L.T("menu.thirdParty"), LegalText.ThirdParty());

    /// <summary>Muestra un texto legal embebido (licencia / avisos de terceros) en un diálogo con scroll.</summary>
    private async Task ShowLegalTextAsync(string title, string body)
    {
        var dlg = WindowDialogHelper.Prepare(new LegalTextDialog(title, body), Content.XamlRoot);
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

        var dlg = WindowDialogHelper.Prepare(
            new WhatsNewDialog(
                info?.Version ?? version,
                info?.Notes ?? "",
                info?.HtmlUrl ?? $"https://github.com/xfiberex/WingetUSoft/releases"),
            Content.XamlRoot);
        await dlg.ShowAsync();
    }

    private Task ShowDialogAsync(string title, string message) =>
        WindowDialogHelper.ShowDialogAsync(Content.XamlRoot, title, message);

    private Task<bool> ShowConfirmDialogAsync(string title, string message, string primaryText) =>
        WindowDialogHelper.ShowConfirmDialogAsync(Content.XamlRoot, title, message, primaryText);

}
