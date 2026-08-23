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

public sealed partial class MainWindow
{
    /// <summary>
    /// Traduce lo que va contando <see cref="UpgradeBatchRunner"/> a lo que se ve en la ventana: barra
    /// de progreso, texto de estado, icono de la barra de tareas, registro de actividad e historial.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Es la mitad «de ventana» del bucle que antes estaba todo junto dentro de
    /// <c>UpdatePackagesAsync</c> (T4-02/T4-03). Separarlas es lo que permite probar el lote sin
    /// abrir una ventana ni lanzar un proceso.
    /// </para>
    /// <para>
    /// Todos los métodos corren en el hilo de UI: <see cref="UpgradeBatchRunner"/> los invoca desde el
    /// mismo contexto de sincronización en el que se le llamó, y el progreso de descarga llega por un
    /// <see cref="Progress{T}"/> creado ahí mismo, que despacha en la cola de la UI.
    /// </para>
    /// </remarks>
    private sealed class BatchUiObserver(MainWindow window) : IUpgradeBatchObserver
    {
        // El índice del paquete en curso se recuerda para poder cerrar su tramo de la barra cuando
        // termina: PackageSucceeded/PackageFailed no lo reciben, y leer el valor actual de la barra
        // para sumarle uno daría saltos si el progreso de descarga la había dejado a medio tramo.
        private int _index;

        public void PackageStarting(int index, int total, WingetPackage package)
        {
            _index = index;
            window.txtEstado.Text = L.T("status.updating", index + 1, total, package.Name);
            window.SetProgressValue(index);
            TaskbarProgress.SetValue(window._hWnd, index * 100 / total);
            window.AppendLog(L.T("log.startingPackage", index + 1, total, package.Name, package.Id));
        }

        public void DownloadProgress(int index, int total, WingetPackage package, WingetProgressInfo info)
        {
            if (info.TotalBytes <= 0) return;

            window.UpdateLogDownloadLine(info);

            // El paquete en curso aporta su fracción descargada, para que la barra avance dentro de
            // cada paquete y no solo al saltar de uno al siguiente.
            window.SetProgressValue(index + (double)info.DownloadedBytes / info.TotalBytes);

            string speedText = info.SpeedBytesPerSecond > 0
                ? $"  ·  {FormatBytes((long)info.SpeedBytesPerSecond)}/s" : "";
            string etaText = Throughput.FormatEta(Throughput.Eta(
                info.TotalBytes - info.DownloadedBytes, info.SpeedBytesPerSecond)) is { Length: > 0 } eta
                ? L.T("eta.remaining", eta) : "";

            window.txtEstado.Text =
                L.T("status.updatingProgress", index + 1, total, package.Name) + speedText + etaText;
        }

        public void LogLine(string line) => window.AppendLog(line);

        public void PackageSucceeded(WingetPackage package)
        {
            window.RecordSuccessfulUpgrade(package);
            window.SetProgressValue(_index + 1);
        }

        // El motivo ya viene traducido por UpgradeResult.GetFailureReason(); aquí solo se pinta.
        // No se abre ningún diálogo: eso detendría el lote en mitad del bucle esperando un clic.
        // Tampoco se acumula el fallo: la lista la devuelve el propio lote y la ventana la vuelca de
        // una vez en _failedUpgrades, para que el resumen final no dependa de dos caminos distintos.
        public void PackageFailed(WingetPackage package, string reason)
        {
            window.AppendLog($"  ✖ {package.Name}: {reason}", LogLineKind.Error);
            window.SetProgressValue(_index + 1);
        }
    }
}
