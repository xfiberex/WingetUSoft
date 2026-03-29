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
        appWindow.Resize(new Windows.Graphics.SizeInt32(760, 560));

        if (Content is FrameworkElement root)
            root.RequestedTheme = settings.DarkMode ? ElementTheme.Dark : ElementTheme.Light;

        LoadFromSettings();
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
        _settings.ExcludedIds = [.. _excludedIds];
        _settings.Save();
        SavedChanges = true;
        Close();
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) => Close();
}
