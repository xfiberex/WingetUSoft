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

/// <summary>Barra de menús: textos localizados y lo que hace cada entrada.</summary>
/// <remarks>
/// Parte de <c>MainWindow</c>. La clase acumulaba una decena de responsabilidades en un solo
/// archivo de más de 2 000 líneas (T4-02); esto la reparte sin mover una línea de lógica.
/// </remarks>
public sealed partial class MainWindow
{
    // --- Menu Handlers ---

    /// <summary>
    /// Aplica el idioma actual (<see cref="L"/>) a los textos del menú principal. El resto de la UI
    /// se extrae por completo en el Tier A #7 (ver ROADMAP.md); por ahora solo cubre este menú.
    /// </summary>
    private void ApplyLocalizedStrings()
    {
        if (!string.IsNullOrEmpty(_appVersionStr))
        {
            Title = L.T("app.titleBase");
            TitleTextBlock.Text = Title;
        }

        ctxActualizar.Text = L.T("ctx.update");
        ctxCopiarNombre.Text = L.T("ctx.copyName");
        ctxCopiarId.Text = L.T("ctx.copyId");
        ctxBuscarWeb.Text = L.T("ctx.viewOnWingetRun");
        ctxExcluir.Text = L.T("ctx.exclude");
        // Solo el texto: la tecla la atiende el acelerador de lvPackages. Se traduce aquí porque WinUI la
        // rotularía en el idioma de Windows, no en el de la aplicación.
        ctxExcluir.KeyboardAcceleratorTextOverride = L.T("key.delete");

        txtHeaderTitle.Text = L.T("header.title");
        txtSubtitulo.Text = L.T("header.subtitle");
        btnInstalarUpdate.Content = L.T("btn.installNow");
        txtAccionesTitle.Text = L.T("actions.title");
        btnConsultar.Content = L.T("btn.checkUpdates");
        btnConsultarDesconocidas.Content = L.T("btn.checkUnknown");
        btnActualizarTodo.Content = L.T("btn.updateAll");
        // btnActualizarSeleccionados lleva el contador: lo rotula UpdateSelectionSummary(), al final.
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
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(chkSelectAll, L.T("grid.selectAllAccessible"));
        ToolTipService.SetToolTip(chkSelectAll, L.T("grid.selectAllAccessible"));
        colNombre.Text = L.T("list.colName");
        colId.Text = L.T("list.colId");
        colVersion.Text = L.T("list.colVersion");
        colDisponible.Text = L.T("list.colAvailable");
        colFuente.Text = L.T("list.colSource");
        colExcl.Text = L.T("list.colExcluded");
        UpdateSortIndicators();   // relee el nombre accesible de cada cabecera en el idioma nuevo
        txtLogHeader.Text = L.T("log.header");
        activityLog.SetAccessibleName(L.T("log.header"));
        if (!progressRing.IsActive) txtEstado.Text = L.T("status.ready");
        UpdateSelectionDetails();
        UpdateSelectionSummary();
        UpdateListState();

        btnHerramientas.Content = L.T("menu.tools");
        menuExportar.Text = L.T("menu.export");
        menuConfiguracion.Text = L.T("menu.settings");
        menuBuscarInstalar.Text = L.T("menu.searchInstall");
        menuExportarWinget.Text = L.T("menu.exportWinget");
        menuImportarWinget.Text = L.T("menu.importWinget");
        menuVerHistorial.Text = L.T("menu.history");
        menuDesinstalar.Text = L.T("menu.uninstall");
        btnAyuda.Content = L.T("menu.help");
        menuBuscarActualizacion.Text = L.T("menu.checkUpdate");
        menuWhatsNew.Text = L.T("menu.whatsnew");
        menuLicencia.Text = L.T("menu.license");
        menuAvisosTerceros.Text = L.T("menu.thirdParty");
        menuAcercaDe.Text = L.T("menu.about");
        RefreshTrayMenu();
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
            SuggestedFileName = L.ExportFileName("export.fileUpdates")
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

    /// <summary>
    /// Ventanas secundarias abiertas, una por tipo (F-08).
    /// </summary>
    /// <remarks>
    /// Cada clic en el menú creaba una ventana nueva: con dos Configuraciones abiertas sobre el mismo
    /// <see cref="AppSettings"/> ganaba la última en guardar y esta ventana reaplicaba los cambios dos veces
    /// (comprobado en la app real: dos invocaciones dejaban dos ventanas).
    /// </remarks>
    private readonly Dictionary<Type, Window> _openWindows = [];

    /// <summary>
    /// Abre una ventana secundaria y espera a que se cierre; si ya había una de ese tipo, la trae al frente.
    /// </summary>
    /// <returns>
    /// La ventana, si esta llamada la abrió; <c>null</c> si solo reactivó la existente. Así lo que se hace al
    /// cerrarla —reaplicar ajustes, avisar de que la lista pudo quedar obsoleta— ocurre una sola vez.
    /// </returns>
    private async Task<T?> OpenSingleInstanceAsync<T>(Func<T> create) where T : Window
    {
        if (_openWindows.TryGetValue(typeof(T), out Window? open))
        {
            if (open.AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized } presenter)
                presenter.Restore();
            open.Activate();
            return null;
        }

        T window = create();
        _openWindows[typeof(T)] = window;

        var closed = new TaskCompletionSource();
        window.Closed += (_, _) =>
        {
            _openWindows.Remove(typeof(T));
            closed.TrySetResult();
        };
        window.Activate();

        await closed.Task;
        return window;
    }

    private async void MenuConfiguracion_Click(object sender, RoutedEventArgs e)
    {
        if (await OpenSingleInstanceAsync(() => new SettingsWindow(_settings)) is not { SavedChanges: true })
            return;

        _silentMode = _settings.SilentMode;
        UpdateAutoCheckTimer();
        LoadPackagesToGrid();
        ApplyTheme(_settings.ThemeMode);
        // El idioma también se elige aquí desde el Tier C #5. SettingsWindow ya lo fijó en L al
        // guardar; esta ventana sigue rotulada en el idioma viejo hasta que se relea.
        ApplyLocalizedStrings();
    }

    private async void MenuHistorial_Click(object sender, RoutedEventArgs e) =>
        await OpenSingleInstanceAsync(() => new HistoryWindow(_settings.History, _settings.ThemeMode));

    private async void MenuDesinstalar_Click(object sender, RoutedEventArgs e) =>
        await OpenSingleInstanceAsync(() => new UninstallWindow(_settings));

    private async void MenuBuscarInstalar_Click(object sender, RoutedEventArgs e)
    {
        var searchWindow = await OpenSingleInstanceAsync(() => new SearchWindow(_settings));

        // Instalar software cambia lo que hay en el equipo: la lista de actualizaciones en pantalla
        // puede haber quedado obsoleta. No se reconsulta sola (winget tarda y el usuario no lo pidió),
        // pero el estado deja de afirmar un recuento que ya no se puede garantizar.
        if (searchWindow is { InstalledSomething: true } && _listState == ListState.Ready)
            txtEstado.Text = L.T("status.listMayBeStale");
    }

    /// <summary>
    /// Exporta los paquetes instalados al **JSON nativo de winget** (no al CSV/TSV de "Exportar lista",
    /// que es para leerlo en una hoja de cálculo). Este archivo sirve para reinstalarlo todo en otro
    /// equipo con *Importar paquetes*, o con `winget import` desde la consola.
    /// </summary>
    private async void MenuExportarWinget_Click(object sender, RoutedEventArgs e)
    {
        var includeVersions = new CheckBox
        {
            Content = L.T("export.includeVersions"),
            IsChecked = false,
        };
        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(new TextBlock { Text = L.T("export.wingetBody"), TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(includeVersions);
        panel.Children.Add(new TextBlock
        {
            Text = L.T("export.includeVersionsHint"),
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.75,
            FontSize = 12,
        });

        var dialog = WindowDialogHelper.Prepare(new ContentDialog
        {
            Title = L.T("export.wingetTitle"),
            Content = panel,
            PrimaryButtonText = L.T("export.wingetContinue"),
            CloseButtonText = L.T("btn.cancel"),
            DefaultButton = ContentDialogButton.Primary,
        }, Content.XamlRoot);
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = L.ExportFileName("export.filePackages"),
        };
        picker.FileTypeChoices.Add("JSON", [".json"]);
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

        var file = await picker.PickSaveFileAsync();
        if (file is null) return;

        SetUIBusy(true);
        ShowIndeterminateProgress();
        txtEstado.Text = L.T("status.exportingWinget");
        AppendLog(L.T("log.exportingWinget", file.Name), LogLineKind.Accent);

        try
        {
            var result = await WingetService.ExportPackagesAsync(
                file.Path,
                includeVersions.IsChecked == true);

            if (result.Success)
            {
                txtEstado.Text = L.T("status.wingetExported", file.Name);
                AppendLog(L.T("log.exportWingetDone", file.Path), LogLineKind.Success);
            }
            else
            {
                txtEstado.Text = L.T("status.wingetExportFailed");
                AppendLog(L.T("log.exportWingetFailed", result.ExitCode), LogLineKind.Error);
                await ShowDialogAsync(L.T("error.title"), L.T("msg.exportWingetError", result.ExitCode));
            }
        }
        catch (Exception ex)
        {
            txtEstado.Text = L.T("status.wingetExportFailed");
            AppendLog(L.T("error.genericPrefix", ex.Message), LogLineKind.Error);
            await ShowDialogAsync(L.T("error.title"), ex.Message);
        }
        finally
        {
            HideProgress();
            SetUIBusy(false);
        }
    }

    /// <summary>
    /// Instala en este equipo los paquetes de un archivo de exportación de winget. Es la operación más
    /// destructiva del menú (instala software), así que se confirma nombrando el archivo y advirtiendo
    /// de que puede tardar y pedir UAC por cada instalador.
    /// </summary>
    private async void MenuImportarWinget_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
        picker.FileTypeFilter.Add(".json");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

        var file = await picker.PickSingleFileAsync();
        if (file is null) return;

        if (!await ShowConfirmDialogAsync(L.T("import.confirmTitle"), L.T("import.confirmBody", file.Name),
                L.T("import.confirmPrimary")))
            return;

        _cancelStopsCurrentProcess = true;
        _cts = new CancellationTokenSource();
        SetUIBusy(true);
        ShowIndeterminateProgress();
        txtEstado.Text = L.T("status.importing");
        AppendLog(L.T("log.importing", file.Name), LogLineKind.Accent);
        TaskbarProgress.SetIndeterminate(_hWnd);

        try
        {
            var result = await WingetService.ImportPackagesAsync(
                file.Path,
                _settings.SilentMode,
                progress: null,
                cancellationToken: _cts.Token,
                logProgress: new Progress<string>(line =>
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        AppendLog(line.TrimEnd());
                }));

            // winget import devuelve un código != 0 si ALGÚN paquete no se pudo instalar, aunque el resto
            // sí. Con --ignore-unavailable eso es lo normal (paquetes que ya no están en el catálogo), así
            // que no se presenta como fallo total: se dice que terminó y se remite al registro.
            if (result.Success)
            {
                txtEstado.Text = L.T("status.importDone");
                AppendLog(L.T("log.importDone"), LogLineKind.Success);
            }
            else
            {
                txtEstado.Text = L.T("status.importPartial");
                AppendLog(L.T("log.importPartial", result.ExitCode), LogLineKind.Warning);
                await ShowDialogAsync(L.T("info.title"), L.T("msg.importPartial"));
            }
        }
        catch (OperationCanceledException)
        {
            txtEstado.Text = L.T("status.importCancelled");
            AppendLog(L.T("log.importCancelled"), LogLineKind.Warning);
        }
        catch (Exception ex)
        {
            txtEstado.Text = L.T("status.importFailed");
            AppendLog(L.T("error.genericPrefix", ex.Message), LogLineKind.Error);
            await ShowDialogAsync(L.T("error.title"), ex.Message);
        }
        finally
        {
            TaskbarProgress.Clear(_hWnd);
            HideProgress();
            SetUIBusy(false);
            _cts?.Dispose();
            _cts = null;
        }
    }
}
