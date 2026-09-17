using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
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

/// <summary>Icono de bandeja, minimizar al cerrar y aviso de resultado del lote.</summary>
/// <remarks>
/// Parte de <c>MainWindow</c>. La clase acumulaba una decena de responsabilidades en un solo
/// archivo de más de 2 000 líneas (T4-02); esto la reparte sin mover una línea de lógica.
/// </remarks>
public sealed partial class MainWindow
{
    #region Tray Icon & Notifications

    /// <summary>«Salir» desde la bandeja: el cierre ya no se intercepta para minimizar (F-09).</summary>
    private bool _exitRequested;

    /// <summary>Evita liberar dos veces lo que se libera al salir (el registro a disco no lo admite).</summary>
    private bool _exitResourcesReleased;

    private void InitializeTrayIcon()
    {
        if (_trayIcon is not null) return;

        _trayIcon = new H.NotifyIcon.TaskbarIcon
        {
            ToolTipText = "WingetUSoft",
            // Clic simple para volver a la ventana, sin la espera con la que se descarta un doble clic.
            NoLeftClickDelay = true,
            LeftClickCommand = TrayCommand(RestoreFromTray),
            DoubleClickCommand = TrayCommand(RestoreFromTray),
            ContextFlyout = BuildTrayMenu(),
        };

        try
        {
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
                _trayIcon.Icon = new System.Drawing.Icon(
                    System.Drawing.Icon.ExtractAssociatedIcon(exePath)!,
                    new System.Drawing.Size(32, 32));
        }
        catch
        {
            _trayIcon.Icon = System.Drawing.SystemIcons.Application;
        }
    }

    /// <summary>
    /// Menú del icono de bandeja (F-09). Hasta F-09 el icono solo respondía al doble clic, y con «Minimizar a la
    /// bandeja al cerrar» activo no quedaba ninguna forma de salir de la app desde la interfaz.
    /// </summary>
    /// <remarks>
    /// H.NotifyIcon convierte el <see cref="MenuFlyout"/> en un menú nativo del sistema y de cada entrada solo
    /// ejecuta su <c>Command</c>: el evento <c>Click</c> nunca llega a dispararse. Por eso todas van con comando.
    /// </remarks>
    private MenuFlyout BuildTrayMenu()
    {
        var menu = new MenuFlyout();
        menu.Items.Add(new MenuFlyoutItem { Text = L.T("tray.open"), Command = TrayCommand(RestoreFromTray) });
        menu.Items.Add(new MenuFlyoutItem { Text = L.T("btn.checkUpdates"), Command = TrayCommand(CheckUpdatesFromTray) });
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(new MenuFlyoutItem { Text = L.T("tray.exit"), Command = TrayCommand(ExitFromTray) });
        return menu;
    }

    /// <summary>Comando para el icono de bandeja: la acción se lleva siempre al hilo de UI.</summary>
    private XamlUICommand TrayCommand(DispatcherQueueHandler action)
    {
        var command = new XamlUICommand();
        command.ExecuteRequested += (_, _) => DispatcherQueue.TryEnqueue(action);
        return command;
    }

    /// <summary>Tras cambiar el idioma, el menú de la bandeja (si ya existe) se reconstruye con los textos nuevos.</summary>
    private void RefreshTrayMenu()
    {
        if (_trayIcon is not null)
            _trayIcon.ContextFlyout = BuildTrayMenu();
    }

    private void CheckUpdatesFromTray()
    {
        RestoreFromTray();
        if (_cts is null)
            _ = LoadPackagesAsync(_lastIncludeUnknown);
    }

    private void ExitFromTray()
    {
        _exitRequested = true;
        ReleaseResourcesOnExit();
        Application.Current.Exit();
    }

    private void RestoreFromTray()
    {
        _appWindow.Show();
        if (_trayIcon is not null)
            _trayIcon.Visibility = Visibility.Collapsed;
    }

    private void MinimizeToTray()
    {
        InitializeTrayIcon();
        if (_trayIcon is null) return;

        _trayIcon.Visibility = Visibility.Visible;
        // Un TaskbarIcon creado desde código no está en el árbol XAML y nunca recibe el Loaded con el que
        // H.NotifyIcon registra el icono en el área de notificación: sin esto la ventana se ocultaba y el
        // proceso seguía vivo sin icono al que volver. Sin modo eficiencia: la app sigue consultando en segundo plano.
        if (!_trayIcon.IsCreated)
            _trayIcon.ForceCreate(enablesEfficiencyMode: false);
        _appWindow.Hide();
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_settings.MinimizeToTray && !_exitRequested)
        {
            args.Cancel = true;
            MinimizeToTray();
            return;
        }

        ReleaseResourcesOnExit();
    }

    private void ReleaseResourcesOnExit()
    {
        if (_exitResourcesReleased) return;
        _exitResourcesReleased = true;

        if (_trayIcon is not null)
        {
            _trayIcon.Visibility = Visibility.Collapsed;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        _autoCheckTimer?.Stop();
        _cts?.Cancel();
        _fileLog.Dispose();   // vacía la cola: lo último del registro también llega al archivo
    }

    /// <summary>
    /// Deja en la barra de estado el resultado del lote que acaba de terminar.
    /// </summary>
    /// <remarks>
    /// <b>No</b> lo rige «Mostrar notificaciones»: esto no es una notificación, es el resultado de lo
    /// que el usuario acaba de pedir, y quien apaga los avisos no está pidiendo que se le oculte.
    /// El ajuste rige el aviso de verdad —sonido y parpadeo de la barra de tareas—, que da
    /// <see cref="Notifier"/> con su propio umbral de duración.
    /// </remarks>
    private void ShowBatchResultInStatusBar(int success, int failed)
    {
        string message = failed == 0
            ? L.P("notif.updatedSuccess", success, success)
            : L.T("notif.updatedMixed", success, failed);

        txtEstado.Text = message;
    }

    #endregion
}
