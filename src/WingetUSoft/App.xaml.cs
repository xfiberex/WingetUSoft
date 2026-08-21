using Microsoft.UI.Xaml;

namespace WingetUSoft;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
        // Un fallo no controlado se anota SIEMPRE (añadiendo, con fecha) y solo se traga si es de un
        // tipo del que se sabe volver. Tragárselos todos dejaba la app viva en un estado desconocido
        // y sin avisar a nadie; ver CrashLog.
        UnhandledException += (s, e) =>
        {
            CrashLog.Write("App.UnhandledException", e.Exception);

            if (CrashLog.IsRecoverable(e.Exception))
            {
                e.Handled = true;
                return;
            }

            CrashLog.Notify();
            e.Handled = false;
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
