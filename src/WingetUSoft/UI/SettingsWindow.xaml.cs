using System.Collections.ObjectModel;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;

namespace WingetUSoft;

public sealed partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly ObservableCollection<string> _excludedIds = [];

    public bool SavedChanges { get; private set; }

    public SettingsWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();

        // Window sizing
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"));
        WindowSizer.Apply(appWindow, hWnd, designWidthDip: 760, designHeightDip: 560, minWidthDip: 640, minHeightDip: 480);

        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = settings.ThemeMode switch { 1 => ElementTheme.Light, 2 => ElementTheme.Dark, _ => ElementTheme.Default };
            root.ActualThemeChanged += (_, _) => UpdateTitleBarButtonColors(appWindow);
        }

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        UpdateTitleBarButtonColors(appWindow);

        ApplyLocalizedStrings();
        LoadFromSettings();
    }

    private void UpdateTitleBarButtonColors(AppWindow appWindow) =>
        TitleBarHelper.UpdateButtonColors(appWindow, Content, _settings.ThemeMode);

    private void ApplyLocalizedStrings()
    {
        Title = L.T("settings.title");
        txtTitleBar.Text = L.T("settings.title");
        txtHeaderTitle.Text = L.T("settings.title");
        txtHeaderSubtitle.Text = L.T("settings.subtitle");

        txtAppearanceTitle.Text = L.T("settings.appearanceTitle");
        txtThemeLabel.Text = L.T("pref.theme");
        rbTemaSistema.Content = L.T("pref.themeSystem");
        rbTemaClaro.Content = L.T("pref.themeLight");
        rbTemaOscuro.Content = L.T("pref.themeDark");
        cmbIdioma.Header = L.T("pref.lang");
        itemLangEs.Content = L.T("pref.lang.es");
        itemLangEn.Content = L.T("pref.lang.en");
        itemLangPt.Content = L.T("pref.lang.pt");
        itemLangFr.Content = L.T("pref.lang.fr");
        itemLangIt.Content = L.T("pref.lang.it");

        txtUpdatesTitle.Text = L.T("settings.updatesTitle");
        txtUpdateModeLabel.Text = L.T("pref.updateMode");
        rbModoSilencioso.Content = L.T("pref.silent");
        rbModoInteractivo.Content = L.T("pref.interactive");
        chkAdministrador.Content = L.T("settings.runAsAdmin");
        cmbIntervalo.Header = L.T("settings.intervalHeader");
        itemIntervalOff.Content = L.T("settings.intervalOff");
        itemInterval30.Content = L.T("settings.interval30");
        itemInterval60.Content = L.T("settings.interval60");
        itemInterval120.Content = L.T("settings.interval120");

        txtLogTitle.Text = L.T("settings.logTitle");
        chkLogArchivo.Content = L.T("settings.logToFile");
        txtLogDirLabel.Text = L.T("settings.logDirLabel");
        txtNotifTrayTitle.Text = L.T("settings.notifTrayTitle");
        txtShowNotifLabel.Text = L.T("settings.showNotifications");
        txtMinimizeTrayLabel.Text = L.T("settings.minimizeToTray");
        txtExcludedTitle.Text = L.T("settings.excludedTitle");
        txtExcludedSubtitle.Text = L.T("settings.excludedSubtitle");
        btnQuitar.Content = L.T("btn.removeSelected");
        btnLimpiar.Content = L.T("btn.clearList");
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
        // ThemeMode: 0 = sistema, 1 = claro, 2 = oscuro -- el mismo orden que los RadioButton.
        rbTema.SelectedIndex = _settings.ThemeMode is 1 or 2 ? _settings.ThemeMode : 0;
        cmbIdioma.SelectedIndex = IndexFromLang(L.Current);
        rbModo.SelectedIndex = _settings.SilentMode ? 0 : 1;

        cmbIntervalo.SelectedIndex = _settings.AutoCheckIntervalMinutes switch
        {
            30 => 1,
            60 => 2,
            120 => 3,
            _ => 0
        };
        chkLogArchivo.IsChecked = _settings.LogToFile;
        chkAdministrador.IsChecked = _settings.RunUpdatesAsAdministrator;
        tsShowNotifications.IsOn = _settings.ShowNotifications;
        tsMinimizeToTray.IsOn = _settings.MinimizeToTray;
        txtLogPath.Text = AppSettings.LogDirectory;

        _excludedIds.Clear();
        foreach (var id in _settings.ExcludedIds)
            _excludedIds.Add(id);

        lstExcluidos.ItemsSource = _excludedIds;
    }

    private void BtnQuitar_Click(object sender, RoutedEventArgs e)
    {
        if (lstExcluidos.SelectedItem is string id)
            _excludedIds.Remove(id);
    }

    private void BtnLimpiar_Click(object sender, RoutedEventArgs e) =>
        _excludedIds.Clear();

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        _settings.ThemeMode = rbTema.SelectedIndex is 1 or 2 ? rbTema.SelectedIndex : 0;
        _settings.SilentMode = rbModo.SelectedIndex != 1;

        AppLang lang = LangFromIndex(cmbIdioma.SelectedIndex);
        _settings.Language = L.ToCode(lang);

        _settings.AutoCheckIntervalMinutes = cmbIntervalo.SelectedIndex switch
        {
            1 => 30,
            2 => 60,
            3 => 120,
            _ => 0
        };
        _settings.LogToFile = chkLogArchivo.IsChecked == true;
        _settings.RunUpdatesAsAdministrator = chkAdministrador.IsChecked == true;
        _settings.ShowNotifications = tsShowNotifications.IsOn;
        _settings.MinimizeToTray = tsMinimizeToTray.IsOn;
        _settings.ExcludedIds = [.. _excludedIds];

        if (!_settings.Save())
        {
            // La ventana NO se cierra: cerrarla daría por buenos unos cambios que no llegaron al
            // disco y que se perderían al reiniciar. El usuario decide si reintenta o cancela.
            await ShowSaveErrorAsync();
            return;
        }

        // Solo con los cambios ya en disco se aplica el idioma al proceso. Quien lo relee para
        // repintarse es MainWindow al cerrarse esta ventana (MenuConfiguracion_Click), y lee de L.
        L.Set(lang);

        SavedChanges = true;
        Close();
    }

    private Task ShowSaveErrorAsync()
    {
        string detail = string.IsNullOrWhiteSpace(_settings.LastSaveError)
            ? L.T("msg.saveSettingsError")
            : $"{L.T("msg.saveSettingsError")}\n\n{_settings.LastSaveError}";

        var dialog = new ContentDialog
        {
            XamlRoot = Content.XamlRoot,
            Title = L.T("error.configTitle"),
            Content = detail,
            CloseButtonText = L.T("btn.close")
        };
        return dialog.ShowAsync().AsTask();
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) => Close();
}
