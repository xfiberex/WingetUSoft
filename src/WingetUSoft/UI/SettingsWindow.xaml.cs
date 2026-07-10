using System.Collections.ObjectModel;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
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
        appWindow.Resize(new Windows.Graphics.SizeInt32(760, 560));

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
        txtAutoCheckTitle.Text = L.T("settings.autoCheckTitle");
        cmbIntervalo.Header = L.T("settings.intervalHeader");
        itemIntervalOff.Content = L.T("settings.intervalOff");
        itemInterval30.Content = L.T("settings.interval30");
        itemInterval60.Content = L.T("settings.interval60");
        itemInterval120.Content = L.T("settings.interval120");
        txtOptionsTitle.Text = L.T("settings.optionsTitle");
        chkLogArchivo.Content = L.T("settings.logToFile");
        chkAdministrador.Content = L.T("settings.runAsAdmin");
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

    private void LoadFromSettings()
    {
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

    private void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
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
        _settings.Save();
        SavedChanges = true;
        Close();
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) => Close();
}
