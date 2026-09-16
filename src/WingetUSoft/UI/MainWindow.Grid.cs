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

/// <summary>La tabla: orden, búsqueda, panel de información, menú contextual y filtro por origen.</summary>
/// <remarks>
/// Parte de <c>MainWindow</c>. La clase acumulaba una decena de responsabilidades en un solo
/// archivo de más de 2 000 líneas (T4-02); esto la reparte sin mover una línea de lógica.
/// </remarks>
public sealed partial class MainWindow
{
    // --- Sort / Search ---

    private void SortHeader_Click(object sender, RoutedEventArgs e)
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
        sortSource.Text    = SortIndicator(5);

        // El triángulo ▲/▼ es la única pista del orden actual, y no la ve quien usa un lector de
        // pantalla: el estado va también en el nombre accesible del botón, que se relee al cambiarlo.
        DescribeSortHeader(btnColNombre, colNombre.Text, 1);
        DescribeSortHeader(btnColId, colId.Text, 2);
        DescribeSortHeader(btnColVersion, colVersion.Text, 3);
        DescribeSortHeader(btnColDisponible, colDisponible.Text, 4);
        DescribeSortHeader(btnColFuente, colFuente.Text, 5);
    }

    private void DescribeSortHeader(Button header, string columnName, int col)
    {
        string state = _sortColumn != col
            ? L.T("list.sortNone")
            : _sortDescending ? L.T("list.sortDescending") : L.T("list.sortAscending");

        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
            header, L.T("list.sortHeaderAccessible", columnName, state));
        // La cabecera se recorta con puntos suspensivos cuando la columna es estrecha: el tooltip es
        // lo único que devuelve el nombre completo con el ratón.
        ToolTipService.SetToolTip(header, columnName);
    }

    private string SortIndicator(int col)
        => _sortColumn == col ? (_sortDescending ? " ▼" : " ▲") : "";

    private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_initialized) return;
        _searchFilter = txtBuscar.Text;

        // Una sola instancia por ventana: crear un temporizador y suscribir una lambda nueva en cada
        // pulsación dejaba tantos suscriptores vivos como teclas se hubieran escrito.
        if (_searchDebounceTimer is null)
        {
            _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _searchDebounceTimer.Tick += (_, _) =>
            {
                _searchDebounceTimer.Stop();
                LoadPackagesToGrid();
            };
        }

        // Reiniciar la cuenta: solo se filtra cuando pasan 300 ms sin escribir.
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    // --- Package Info Panel ---

    private async Task LoadPackageInfoPanelAsync(WingetPackage pkg)
    {
        _packageInfoCts?.Cancel();
        _packageInfoCts = new CancellationTokenSource();
        var token = _packageInfoCts.Token;

        txtInfoDescripcion.Text = L.T("pkg.loading");
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
        }
        catch (OperationCanceledException) { }
        catch
        {
            if (!token.IsCancellationRequested)
                txtInfoDescripcion.Text = pkg.Name;
        }
    }

    /// <summary>
    /// Sin paquete seleccionado el panel no se pliega —plegarlo movía la tabla (F-10)—: vuelve a la indicación
    /// de qué hacer, sin enlaces.
    /// </summary>
    private void ResetPackageInfoPanel()
    {
        _packageInfoCts?.Cancel();
        lnkHomepage.Visibility = Visibility.Collapsed;
        lnkNotasVersion.Visibility = Visibility.Collapsed;
        UpdateSelectionDetails();
    }

    // --- DataGrid Events ---

    private void LvPackages_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GetSelectedPackage() is { } pkg)
            _ = LoadPackageInfoPanelAsync(pkg);
        else
            ResetPackageInfoPanel();
    }

    /// <summary>
    /// Abre el menú contextual de la fila. Se engancha a <c>ContextRequested</c> y no a
    /// <c>RightTapped</c> (como hasta el Tier E) **a propósito**: `RightTapped` solo lo dispara el ratón,
    /// así que el menú era inalcanzable con teclado — ni Shift+F10 ni la tecla Menú lo abrían, y con él
    /// quedaban fuera del alcance de quien no usa ratón *todas* sus acciones (actualizar esta fila,
    /// copiar, excluir y omitir versión). `ContextRequested` lo dispara WinUI en los dos casos. Se
    /// descubrió conduciendo la app real, no en una revisión de código.
    /// </summary>
    private void LvPackages_ContextRequested(UIElement sender, ContextRequestedEventArgs e)
    {
        var element = e.OriginalSource as FrameworkElement;

        // Con teclado, el origen del evento es el ListView (no una celda), así que la fila se toma de la
        // selección: es justo la que el usuario tiene enfocada.
        var row = element?.DataContext as PackageViewModel ?? lvPackages.SelectedItem as PackageViewModel;
        if (row is null) return;

        lvPackages.SelectedItem = row;

        // Omitir/Excluir son interruptores: el ítem debe decir lo que hará al pulsarlo, no el nombre
        // genérico de la función. Se resuelve aquí, con la fila ya conocida.
        ctxOmitirVersion.Text = row.IsVersionSkipped
            ? L.T("ctx.unskipVersion")
            : L.T("ctx.skipVersion");
        ctxExcluir.Text = row.IsExcluded ? L.T("ctx.include") : L.T("ctx.exclude");

        // Un paquete excluido ya no se actualiza nunca: omitir una versión suya no significaría nada.
        ctxOmitirVersion.IsEnabled = !row.IsExcluded;

        // Con ratón hay posición de puntero; con teclado no, y el menú se ancla al contenedor de la fila.
        if (element is not null && e.TryGetPosition(element, out var position))
            ctxMenuRow.ShowAt(element, position);
        else
            ctxMenuRow.ShowAt((lvPackages.ContainerFromItem(row) as FrameworkElement) ?? lvPackages);

        e.Handled = true;
    }

    // --- Context Menu ---

    private async void CtxActualizar_Click(object sender, RoutedEventArgs e)
    {
        // Actualiza solo la fila del menú contextual, sin tocar las casillas: antes desmarcaba todo
        // y marcaba esta, así que el usuario perdía la selección que llevaba hecha.
        if (GetSelectedPackage() is { } pkg)
            await UpdatePackagesAsync([pkg]);
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
            L.T("confirm.openWingetRunBody", url),
            L.T("confirm.openWingetRunPrimary"));

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

    /// <summary>
    /// Omite (o deja de omitir) la versión disponible **de hoy** para ese paquete: la fila se atenúa y el
    /// lote la salta, pero el paquete reaparece solo cuando winget ofrezca otra versión. No es lo mismo
    /// que excluir (permanente, todo el paquete) — ver <see cref="SkippedVersions"/>.
    /// </summary>
    private void CtxOmitirVersion_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedPackage() is not { } pkg) return;

        if (SkippedVersions.IsSkipped(_settings.SkippedVersions, pkg.Id, pkg.Available))
        {
            SkippedVersions.Unskip(_settings.SkippedVersions, pkg.Id);
            txtEstado.Text = L.T("status.versionUnskipped", pkg.Name);
        }
        else
        {
            SkippedVersions.Skip(_settings.SkippedVersions, pkg.Id, pkg.Available);
            // Omitir desmarca: si no, la fila quedaría marcada pero con la casilla deshabilitada, y el
            // contador del botón contaría un paquete que el lote no va a tocar.
            _selectedIds.Remove(pkg.Id);
            txtEstado.Text = L.T("status.versionSkipped", pkg.Available, pkg.Name);
        }

        TrySaveSettings(L.T("msg.saveSkippedError"));
        LoadPackagesToGrid();
    }

    private void CtxExcluir_Click(object? sender, RoutedEventArgs? e)
    {
        if (GetSelectedPackage() is not { } pkg) return;
        if (_settings.ExcludedIds.Contains(pkg.Id))
        {
            _settings.ExcludedIds.Remove(pkg.Id);
        }
        else
        {
            _settings.ExcludedIds.Add(pkg.Id);
            // Excluir desmarca: si no, la fila quedaría con la casilla marcada pero deshabilitada.
            _selectedIds.Remove(pkg.Id);
        }
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
}
