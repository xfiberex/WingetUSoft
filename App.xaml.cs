using Microsoft.UI.Xaml;

namespace WingetUSoft;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
        UnhandledException += (s, e) =>
        {
            try
            {
                string crashDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "WingetUSoft");
                Directory.CreateDirectory(crashDir);
                File.WriteAllText(Path.Combine(crashDir, "crash.log"),
                    $"UnhandledException: {e.Exception}");
            }
            catch { }
            e.Handled = true;
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
