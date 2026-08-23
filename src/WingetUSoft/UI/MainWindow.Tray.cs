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

/// <summary>Icono de bandeja, minimizar al cerrar y aviso de resultado del lote.</summary>
/// <remarks>
/// Parte de <c>MainWindow</c>. La clase acumulaba una decena de responsabilidades en un solo
/// archivo de más de 2 000 líneas (T4-02); esto la reparte sin mover una línea de lógica.
/// </remarks>
public sealed partial class MainWindow
{
    #region Tray Icon & Notifications

    private void InitializeTrayIcon()
    {
        if (_trayIcon is not null) return;

        _trayIcon = new H.NotifyIcon.TaskbarIcon
        {
            ToolTipText = "WingetUSoft"
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

        var cmd = new Microsoft.UI.Xaml.Input.XamlUICommand();
        cmd.ExecuteRequested += (_, _) => DispatcherQueue.TryEnqueue(RestoreFromTray);
        _trayIcon.DoubleClickCommand = cmd;
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
        if (_trayIcon is not null)
        {
            _trayIcon.Visibility = Visibility.Visible;
            _appWindow.Hide();
        }
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_settings.MinimizeToTray)
        {
            args.Cancel = true;
            MinimizeToTray();
            return;
        }

        // Cleanup tray icon
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
            ? L.T("notif.updatedSuccess", success)
            : L.T("notif.updatedMixed", success, failed);

        txtEstado.Text = message;
    }

    #endregion
}
