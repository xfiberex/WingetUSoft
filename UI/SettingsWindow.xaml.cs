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

        LoadFromSettings();
    }

    private void UpdateTitleBarButtonColors(AppWindow appWindow)
    {
        if (appWindow?.TitleBar is not { } titleBar) return;

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
