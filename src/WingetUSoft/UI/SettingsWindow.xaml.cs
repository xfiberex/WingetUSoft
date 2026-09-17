using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WingetUSoft;

/// <summary>Una fila de la lista de paquetes excluidos de Configuración.</summary>
/// <remarks>
/// El botón de cada fila se anuncia con el paquete («Quitar Git.Git de los excluidos»): diez botones que solo dijeran
/// «Quitar» no permitirían saber cuál es cuál con un lector de pantalla.
/// </remarks>
public sealed class ExcludedPackageViewModel(string id)
{
    public string Id { get; } = id;
    public string RemoveText => L.T("settings.removeExcluded");
    public string RemoveLabel => L.T("settings.removeExcludedAccessible", Id);
}

public sealed partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly ObservableCollection<ExcludedPackageViewModel> _excluded = [];

    public bool SavedChanges { get; private set; }

    public SettingsWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();

        WindowChrome.Apply(
            this, AppTitleBar, settings.ThemeMode,
            designWidthDip: 760, designHeightDip: 560, minWidthDip: 640, minHeightDip: 480);

        _excluded.CollectionChanged += (_, _) => UpdateExcludedState();

        ApplyLocalizedStrings();
        LoadFromSettings();
    }

    private void ApplyLocalizedStrings()
    {
        Title = L.T("settings.title");
        txtTitleBar.Text = L.T("settings.title");
        txtHeaderTitle.Text = L.T("settings.title");
        txtHeaderSubtitle.Text = L.T("settings.subtitle");

        txtAppearanceTitle.Text = L.T("settings.appearanceTitle");
        cardTheme.Header = L.T("pref.theme");
        cardTheme.Description = L.T("settings.themeDesc");
        itemThemeSystem.Content = L.T("pref.themeSystem");
        itemThemeLight.Content = L.T("pref.themeLight");
        itemThemeDark.Content = L.T("pref.themeDark");
        cardLanguage.Header = L.T("pref.lang");
        cardLanguage.Description = L.T("settings.langDesc");
        itemLangEs.Content = L.T("pref.lang.es");
        itemLangEn.Content = L.T("pref.lang.en");
        itemLangPt.Content = L.T("pref.lang.pt");
        itemLangFr.Content = L.T("pref.lang.fr");
        itemLangIt.Content = L.T("pref.lang.it");

        txtUpdatesTitle.Text = L.T("settings.updatesTitle");
        cardMode.Header = L.T("pref.updateMode");
        cardMode.Description = L.T("settings.modeDesc");
        itemModeSilent.Content = L.T("pref.silent");
        itemModeInteractive.Content = L.T("pref.interactive");
        cardAdmin.Header = L.T("settings.runAsAdmin");
        cardAdmin.Description = L.T("settings.adminDesc");
        cardInterval.Header = L.T("settings.intervalHeader");
        cardInterval.Description = L.T("settings.intervalDesc");
        itemIntervalOff.Content = L.T("settings.intervalOff");
        itemInterval30.Content = L.T("settings.interval30");
        itemInterval60.Content = L.T("settings.interval60");
        itemInterval120.Content = L.T("settings.interval120");

        txtLogTitle.Text = L.T("settings.logTitle");
        cardLogToFile.Header = L.T("settings.logToFile");
        cardLogToFile.Description = L.T("settings.logToFileDesc", AppSettings.LogRetentionDays);
        cardLogFolder.Header = L.T("settings.logFolder");
        cardLogFolder.Description = AppSettings.LogDirectory;
        btnAbrirCarpeta.Content = L.T("settings.openFolder");

        txtNotifTrayTitle.Text = L.T("settings.notifTrayTitle");
        cardNotifications.Header = L.T("settings.showNotifications");
        cardNotifications.Description = L.T("settings.notificationsDesc");
        cardTray.Header = L.T("settings.minimizeToTray");
        cardTray.Description = L.T("settings.trayDesc");

        txtExcludedTitle.Text = L.T("settings.excludedTitle");
        cardExcluded.Header = L.T("settings.excludedSubtitle");
        btnLimpiar.Content = L.T("btn.clearList");
        foreach (var toggle in new[] { tsAdministrador, tsLogArchivo, tsShowNotifications, tsMinimizeToTray })
        {
            toggle.OnContent = L.T("toggle.on");
            toggle.OffContent = L.T("toggle.off");
        }
        btnGuardar.Content = L.T("btn.save");
        btnCancelar.Content = L.T("btn.cancel");
    }

    // El índice del ComboBox de idioma se traduce con un switch explícito, y no con un cast desde
    // AppLang: atarlos por su valor ordinal haría que reordenar el enum (o los ComboBoxItem) cambiara
    // el idioma de la app en silencio, sin que nada dejara de compilar.
    private static AppLang LangFromIndex(int index) => index switch
    {
        1 => AppLang.En,
        2 => AppLang.Pt,
        3 => AppLang.Fr,
        4 => AppLang.It,
        _ => AppLang.Es
    };

    private static int IndexFromLang(AppLang lang) => lang switch
    {
        AppLang.En => 1,
        AppLang.Pt => 2,
        AppLang.Fr => 3,
        AppLang.It => 4,
        _ => 0
    };

    private void LoadFromSettings()
    {
        // ThemeMode: 0 = sistema, 1 = claro, 2 = oscuro -- el mismo orden que los elementos del ComboBox.
        cmbTema.SelectedIndex = _settings.ThemeMode is 1 or 2 ? _settings.ThemeMode : 0;
        cmbIdioma.SelectedIndex = IndexFromLang(L.Current);
        cmbModo.SelectedIndex = _settings.SilentMode ? 0 : 1;

        cmbIntervalo.SelectedIndex = _settings.AutoCheckIntervalMinutes switch
        {
            30 => 1,
            60 => 2,
            120 => 3,
            _ => 0
        };
        tsLogArchivo.IsOn = _settings.LogToFile;
        tsAdministrador.IsOn = _settings.RunUpdatesAsAdministrator;
        tsShowNotifications.IsOn = _settings.ShowNotifications;
        tsMinimizeToTray.IsOn = _settings.MinimizeToTray;

        _excluded.Clear();
        foreach (var id in _settings.ExcludedIds)
            _excluded.Add(new ExcludedPackageViewModel(id));

        lstExcluidos.ItemsSource = _excluded;
        UpdateExcludedState();
    }

    /// <summary>
    /// Con paquetes, la fila cuenta cuántos hay; sin ninguno, explica cómo excluir uno y la lista desaparece en
    /// lugar de quedar como un hueco en blanco.
    /// </summary>
    private void UpdateExcludedState()
    {
        bool any = _excluded.Count > 0;
        cardExcluded.Description = any
            ? L.P("settings.excludedCount", _excluded.Count, _excluded.Count)
            : L.T("settings.excludedEmpty");
        panelExcluidos.Visibility = any ? Visibility.Visible : Visibility.Collapsed;
        btnLimpiar.IsEnabled = any;
    }

    private void BtnQuitarFila_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id } && _excluded.FirstOrDefault(x => x.Id == id) is { } row)
            _excluded.Remove(row);
    }

    private void BtnLimpiar_Click(object sender, RoutedEventArgs e) =>
        _excluded.Clear();

    /// <summary>Abre la carpeta de registros en el Explorador, creándola si todavía no se ha escrito ninguno.</summary>
    private void BtnAbrirCarpeta_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(AppSettings.LogDirectory);
            Process.Start(new ProcessStartInfo(AppSettings.LogDirectory) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            _ = ShowErrorAsync(L.T("settings.openFolderError", ex.Message));
        }
    }

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        _settings.ThemeMode = cmbTema.SelectedIndex is 1 or 2 ? cmbTema.SelectedIndex : 0;
        _settings.SilentMode = cmbModo.SelectedIndex != 1;

        AppLang lang = LangFromIndex(cmbIdioma.SelectedIndex);
        _settings.Language = L.ToCode(lang);

        _settings.AutoCheckIntervalMinutes = cmbIntervalo.SelectedIndex switch
        {
            1 => 30,
            2 => 60,
            3 => 120,
            _ => 0
        };
        _settings.LogToFile = tsLogArchivo.IsOn;
        _settings.RunUpdatesAsAdministrator = tsAdministrador.IsOn;
        _settings.ShowNotifications = tsShowNotifications.IsOn;
        _settings.MinimizeToTray = tsMinimizeToTray.IsOn;
        _settings.ExcludedIds = [.. _excluded.Select(x => x.Id)];

        if (!_settings.Save())
        {
            // La ventana NO se cierra: cerrarla daría por buenos unos cambios que no llegaron al
            // disco y que se perderían al reiniciar. El usuario decide si reintenta o cancela.
            string detail = _settings.LastSaveError is null
                ? L.T("msg.saveSettingsError")
                : $"{L.T("msg.saveSettingsError")}\n\n{_settings.LastSaveError.Text}";
            await ShowErrorAsync(detail);
            return;
        }

        // Solo con los cambios ya en disco se aplica el idioma al proceso. Quien lo relee para
        // repintarse es MainWindow al cerrarse esta ventana (MenuConfiguracion_Click), y lee de L.
        L.Set(lang);

        SavedChanges = true;
        Close();
    }

    private Task ShowErrorAsync(string detail)
    {
        var dialog = WindowDialogHelper.Prepare(new ContentDialog
        {
            Title = L.T("error.configTitle"),
            Content = detail,
            CloseButtonText = L.T("btn.close")
        }, Content.XamlRoot);
        return dialog.ShowAsync().AsTask();
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) => Close();
}
