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

/// <summary>Registro de actividad y temporizador de auto-comprobación.</summary>
/// <remarks>
/// Parte de <c>MainWindow</c>. La clase acumulaba una decena de responsabilidades en un solo
/// archivo de más de 2 000 líneas (T4-02); esto la reparte sin mover una línea de lógica.
/// </remarks>
public sealed partial class MainWindow
{
    // --- Auto Check Timer ---

    private void UpdateAutoCheckTimer()
    {
        _autoCheckTimer?.Stop();
        _autoCheckTimer = null;
        if (_settings.AutoCheckIntervalMinutes <= 0) return;
        _autoCheckTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(_settings.AutoCheckIntervalMinutes)
        };
        _autoCheckTimer.Tick += async (_, _) =>
        {
            if (_cts is not null) return;
            int prevCount = _packages.Count;
            await LoadPackagesAsync(_lastIncludeUnknown);
            if (_packages.Count > prevCount)
                txtEstado.Text += "  " + L.T("status.newUpdatesAvailable");
        };
        _autoCheckTimer.Start();
    }

    // --- Logging ---

    private void ClearLog() => activityLog.Clear();

    /// <summary>
    /// Añade una línea al registro y la vuelca al archivo del día.
    /// </summary>
    /// <remarks>
    /// Pintar, recortar y seguir el scroll es cosa de <see cref="ActivityLog"/>. Lo que se queda aquí
    /// es lo propio de esta ventana: deducir el tipo de línea por su prefijo —solo esta retransmite la
    /// salida cruda de winget, donde el tipo no viene dado— y el volcado a disco.
    /// </remarks>
    private void AppendLog(string text, LogLineKind kind = LogLineKind.Normal)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        if (kind == LogLineKind.Normal)
        {
            if (text.StartsWith("  ✔", StringComparison.Ordinal)) kind = LogLineKind.Success;
            else if (text.StartsWith("  ✖", StringComparison.Ordinal) ||
                     text.StartsWith("  ✘", StringComparison.Ordinal)) kind = LogLineKind.Error;
            else if (text.Length > 0 && text[0] == '[') kind = LogLineKind.Accent;
        }

        activityLog.Append(text, kind);
        AppendLogFile(text);
    }

    /// <summary>Repinta el registro cuando cambia el tema (lo engancha WindowChrome).</summary>
    private void RecolorLog() => activityLog.Recolor();

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1_073_741_824L) return $"{bytes / 1_073_741_824.0:F1} GB";
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F1} MB";
        if (bytes >= 1_024) return $"{bytes / 1_024.0:F1} KB";
        return $"{bytes} B";
    }

    private static string BuildProgressBar(int percent, int width = 20)
    {
        int filled = Math.Clamp((int)Math.Round(percent / 100.0 * width), 0, width);
        return $"[{new string('\u2588', filled)}{new string('\u2591', width - filled)}] {percent,3}%";
    }

    private void UpdateLogDownloadLine(WingetProgressInfo info)
    {
        int percent = (int)Math.Clamp(info.DownloadedBytes * 100L / info.TotalBytes, 0, 100);
        string dl = FormatBytes(info.DownloadedBytes);
        string total = FormatBytes(info.TotalBytes);
        string speed = info.SpeedBytesPerSecond > 0
            ? $"  {FormatBytes((long)info.SpeedBytesPerSecond)}/s"
            : "";
        string eta = Throughput.FormatEta(Throughput.Eta(info.TotalBytes - info.DownloadedBytes, info.SpeedBytesPerSecond)) is { Length: > 0 } etaText
            ? $"  ETA {etaText}"
            : "";
        string bar = BuildProgressBar(percent);
        string line = $"  \u2193  {dl} / {total}  {bar}{speed}{eta}";

        // La barra de descarga se reescribe en su sitio en vez de dejar cientos de líneas casi
        // iguales; el prefijo es lo que identifica a la suya.
        activityLog.AppendOrReplaceLast("  ↓", line, LogLineKind.Warning);
    }

    private void AppendLogFile(string text)
    {
        if (!_settings.LogToFile) return;
        _fileLog.Write(text);
    }

    /// <summary>
    /// Aviso de que el registro a disco se apagó. Llega desde la tarea de fondo del <see cref="FileLog"/>,
    /// así que hay que volver al hilo de UI antes de tocar nada de la ventana.
    /// </summary>
    private void OnFileLogFailed(string message)
    {
        Trace.WriteLine($"No se pudo escribir el archivo de log: {message}");
        DispatcherQueue.TryEnqueue(() => AppendLog(L.T("log.logFileFailed", message), LogLineKind.Warning));
    }
}
