using Microsoft.UI.Xaml;

namespace WingetUSoft;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
        // Sin placas detrás del texto con contraste alto (F-24). Con un tema de contraste, WinUI pinta por defecto una
        // placa del color de la ventana detrás de cada texto: en «Actualizar todo», el icono y el texto salían en un
        // recuadro negro sobre el botón cian del acento. Aquí todos los colores salen ya del sistema (el registro deja
        // de forzar los suyos con contraste alto), así que la placa sobra. Va por código: puesto como atributo en
        // App.xaml se compilaba, pero no llegaba a aplicarse.
        HighContrastAdjustment = ApplicationHighContrastAdjustment.None;
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
