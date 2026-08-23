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

/// <summary>Actualización de la propia aplicación desde GitHub Releases.</summary>
/// <remarks>
/// Parte de <c>MainWindow</c>. La clase acumulaba una decena de responsabilidades en un solo
/// archivo de más de 2 000 líneas (T4-02); esto la reparte sin mover una línea de lógica.
/// </remarks>
public sealed partial class MainWindow
{
    private async Task CheckForAppUpdateAsync()
    {
        GitHubReleaseInfo? info = await GitHubUpdateService.CheckForUpdateAsync();
        if (info is null) return;

        _appUpdateUrl = info.DownloadUrl;
        _appUpdateChecksumUrl = info.ChecksumUrl;
        infoBarUpdate.Title = L.T("update.newVersionTitle", info.Version);
        infoBarUpdate.Message = BuildChangelogMessage(info.Notes, L.T("update.pressInstallNow"));
        infoBarUpdate.IsOpen = true;
        menuBuscarActualizacion.Text = L.T("menu.installVersion", info.Version);
    }

    /// <summary>Antepone el changelog (si lo hay) al mensaje base, truncado para no desbordar el diálogo/InfoBar.</summary>
    private static string BuildChangelogMessage(string notesMarkdown, string baseMessage)
    {
        string plain = ReleaseNotes.ToPlainText(notesMarkdown);
        if (string.IsNullOrWhiteSpace(plain)) return baseMessage;

        const int maxLength = 500;
        if (plain.Length > maxLength)
            plain = plain[..maxLength].TrimEnd() + "…";

        return $"{L.T("update.changelog")}\n{plain}\n\n{baseMessage}";
    }

    private async void LnkDescargarUpdate_Click(object sender, RoutedEventArgs e) =>
        await DownloadAndInstallUpdateAsync();

    /// <summary>
    /// Descarga el instalador de la nueva versión, lo verifica y lo lanza.
    /// </summary>
    /// <remarks>
    /// Vive aparte del manejador del clic porque hay dos caminos que llegan aquí: el botón de la
    /// InfoBar y la confirmación de «Buscar actualización». El segundo llamaba directamente al
    /// manejador, que es <c>async void</c>: nadie podía esperar a que la descarga terminara y sus
    /// excepciones se escapaban del <c>try</c> del llamador.
    /// </remarks>
    private async Task DownloadAndInstallUpdateAsync()
    {
        if (string.IsNullOrEmpty(_appUpdateUrl)) return;

        btnInstalarUpdate.IsEnabled = false;
        btnInstalarUpdate.Content = L.T("btn.downloading");
        pbUpdate.Visibility = Visibility.Visible;
        infoBarUpdate.IsClosable = false;

        var downloadStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var progress = new Progress<double>(p =>
        {
            pbUpdate.Value = p;
            TaskbarProgress.SetValue(_hWnd, (int)(p * 100));

            // Sin acceso a los bytes totales aquí (la API solo reporta la fracción p), se extrapola
            // el tiempo restante a partir del tiempo transcurrido y el propio progreso.
            string etaText = "";
            if (p > 0.01)
            {
                TimeSpan estimatedTotal = TimeSpan.FromSeconds(downloadStopwatch.Elapsed.TotalSeconds / p);
                TimeSpan remaining = estimatedTotal - downloadStopwatch.Elapsed;
                if (Throughput.FormatEta(remaining) is { Length: > 0 } eta)
                    etaText = L.T("eta.label", eta);
            }
            infoBarUpdate.Message = L.T("update.downloadingProgress", $"{p:P0}", etaText);
        });

        try
        {
            // El "using" mantiene retenido el instalador verificado hasta después de lanzarlo:
            // soltarlo antes reabriría la ventana en la que se le puede sustituir el binario a un
            // proceso que va a pedir administrador (ver VerifiedInstaller).
            using VerifiedInstaller installer = await GitHubUpdateService.DownloadInstallerAsync(
                _appUpdateUrl, _appUpdateChecksumUrl, progress);
            infoBarUpdate.Message = L.T("update.installingRestart");
            TaskbarProgress.SetIndeterminate(_hWnd);

            Process.Start(new ProcessStartInfo(installer.Path)
            {
                Arguments = "/VERYSILENT /NORESTART /autoinstall=1",
                UseShellExecute = true
            });
            await Task.Delay(1500);
            Application.Current.Exit();
        }
        catch (Exception ex)
        {
            TaskbarProgress.Clear(_hWnd);
            pbUpdate.Visibility = Visibility.Collapsed;
            btnInstalarUpdate.IsEnabled = true;
            btnInstalarUpdate.Content = L.T("btn.installNow");
            infoBarUpdate.IsClosable = true;
            infoBarUpdate.Severity = InfoBarSeverity.Error;
            infoBarUpdate.Message = L.T("error.genericPrefix", ex.Message);
        }
    }

    private async void MenuBuscarActualizacion_Click(object sender, RoutedEventArgs e)
    {
        menuBuscarActualizacion.IsEnabled = false;
        string originalText = menuBuscarActualizacion.Text;
        menuBuscarActualizacion.Text = L.T("update.checking");
        try
        {
            GitHubReleaseInfo? info = await GitHubUpdateService.CheckForUpdateAsync();
            if (info is null)
            {
                menuBuscarActualizacion.Text = originalText;
                await ShowDialogAsync(L.T("update.noUpdatesTitle"),
                    L.T("update.noUpdatesBody"));
            }
            else
            {
                _appUpdateUrl = info.DownloadUrl;
                _appUpdateChecksumUrl = info.ChecksumUrl;
                menuBuscarActualizacion.Text = L.T("menu.installVersion", info.Version);
                infoBarUpdate.Title = L.T("update.newVersionTitle", info.Version);
                infoBarUpdate.Message = BuildChangelogMessage(info.Notes, L.T("update.pressInstallNow"));
                infoBarUpdate.IsOpen = true;

                string confirmBody = BuildChangelogMessage(info.Notes,
                    L.T("update.confirmInstall"));
                if (await ShowConfirmDialogAsync(L.T("update.availTitle"),
                    $"{L.T("update.availBody", info.Version)}\n\n{confirmBody}"))
                {
                    await DownloadAndInstallUpdateAsync();
                }
            }
        }
        finally
        {
            menuBuscarActualizacion.IsEnabled = true;
        }
    }
}
