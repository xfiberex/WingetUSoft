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

/// <summary>Orquestación de un lote de actualizaciones: la parte que sigue siendo de la ventana.</summary>
/// <remarks>
/// Parte de <c>MainWindow</c>. La clase acumulaba una decena de responsabilidades en un solo
/// archivo de más de 2 000 líneas (T4-02); esto la reparte sin mover una línea de lógica.
/// </remarks>
public sealed partial class MainWindow
{
    /// <summary>
    /// Actualiza exactamente los paquetes recibidos. La lista la decide cada punto de entrada (botón
    /// de seleccionados, "actualizar todo", menú contextual), en vez de deducirla aquí de las filas
    /// visibles: un filtro o una búsqueda activos no deben cambiar lo que el usuario mandó actualizar.
    /// </summary>
    private async Task UpdatePackagesAsync(List<WingetPackage> packagesToUpdate)
    {
        if (packagesToUpdate.Count == 0) return;

        bool runAsAdministrator = _settings.RunUpdatesAsAdministrator;

        if (runAsAdministrator)
        {
            string adminMessage = packagesToUpdate.Count == 1
                ? L.T("admin.confirmSingleBody")
                : L.T("admin.confirmBatchBody", packagesToUpdate.Count);

            if (!await ShowConfirmDialogAsync(L.T("admin.confirmTitle"), adminMessage, L.T("admin.confirmPrimary")))
                return;
        }

        _cts = new CancellationTokenSource();
        _cancelStopsCurrentProcess = !runAsAdministrator;
        _rowTracker.Begin(packagesToUpdate);   // todas las filas del lote, «en cola» (F-19)
        SetUIBusy(true);

        int success = 0;
        int failed = 0;
        bool cancelled = false;
        bool shouldReload = false;
        bool historyChanged = false;
        var opStopwatch = System.Diagnostics.Stopwatch.StartNew();

        ClearLog();
        _failedUpgrades.Clear();

        try
        {
            if (runAsAdministrator)
            {
                // El lote elevado corre en un único proceso: winget no nos va reportando en qué
                // paquete va con precisión suficiente para una barra determinada.
                ShowIndeterminateProgress();
                TaskbarProgress.SetIndeterminate(_hWnd);
                (success, failed, cancelled) = await UpdatePackagesAsAdministratorAsync(packagesToUpdate);
                historyChanged = success > 0;
            }
            else
            {
                ShowDeterminateProgress(packagesToUpdate.Count);

                // El bucle en sí vive en UpgradeBatchRunner (T4-03): aquí solo queda lo que es de la
                // ventana —barra, texto de estado, registro e historial—, que llega por el observador.
                UpgradeBatchOutcome batch = await UpgradeBatchRunner.RunAsync(
                    WingetServiceAdapter.Instance,
                    packagesToUpdate,
                    _silentMode,
                    new BatchUiObserver(this),
                    _cts.Token);

                success   = batch.Succeeded;
                failed    = batch.Failed;
                cancelled = batch.Cancelled;
                historyChanged = batch.Succeeded > 0;
                _failedUpgrades.AddRange(batch.Failures);
            }

            if (cancelled)
            {
                txtEstado.Text = L.T("status.cancelledCompleted", success, failed);
                return;
            }

            if (success > 0)
            {
                txtEstado.Text = L.T("status.completedSuccessReload", success, failed);
                shouldReload = true;
            }
            else
            {
                txtEstado.Text = L.T("status.updateCompleted", success, failed);
            }
        }
        catch (OperationCanceledException)
        {
            txtEstado.Text = L.T("status.cancelledCompleted", success, failed);
        }
        catch (Exception ex)
        {
            txtEstado.Text = L.T("status.updateError");
            // Algunas excepciones de WinRT llegan sin mensaje, y el diálogo salía vacío: sin nada que buscar ni que
            // contar en un issue. El tipo y el HRESULT siempre están, y también quedan en el registro.
            string detail = string.IsNullOrWhiteSpace(ex.Message)
                ? $"{ex.GetType().Name} (0x{ex.HResult:X8})"
                : ex.Message;
            AppendLog($"  ✖ {detail}", LogLineKind.Error);
            await ShowDialogAsync(L.T("error.updateTitle"), detail);
        }
        finally
        {
            if (historyChanged)
                TrySaveSettings(L.T("msg.historySaveError"), updateStatusLabel: false);

            TaskbarProgress.Clear(_hWnd);
            _cts?.Dispose();
            _cts = null;
            _rowTracker.Finish();   // lo que no llegó a acabar, sin estado: el lote ya no va a volver a ello
            SetUIBusy(false);
        }

        // Un solo diálogo con todos los fallos, ya terminado el lote. Antes se abría uno por paquete
        // fallido desde dentro del bucle, lo que detenía el lote hasta que el usuario cerraba cada uno.
        if (_failedUpgrades.Count > 0)
            await ShowFailureSummaryAsync();

        ShowBatchResultInStatusBar(success, failed);

        // Aviso al terminar (sonido + parpadeo de la barra de tareas): solo si la operación fue
        // larga (≥ 10 s), no se canceló y el usuario no está ya mirando la ventana.
        if (Notifier.ShouldNotify(opStopwatch.Elapsed, _settings.ShowNotifications, cancelled, TimeSpan.FromSeconds(10)))
            Notifier.OperationFinished(_hWnd);

        if (shouldReload)
            await LoadPackagesAsync(_lastIncludeUnknown, keepBatchFailures: true);
    }

    private async Task<(int Success, int Failed, bool Cancelled)> UpdatePackagesAsAdministratorAsync(List<WingetPackage> packagesToUpdate)
    {
        var packagesById = packagesToUpdate.ToDictionary(pkg => pkg.Id, StringComparer.OrdinalIgnoreCase);

        txtEstado.Text = packagesToUpdate.Count == 1
            ? L.T("status.adminUpdatingSingle", packagesToUpdate[0].Name)
            : L.T("status.adminUpdatingBatch", packagesToUpdate.Count);

        AppendLog(packagesToUpdate.Count == 1
            ? L.T("log.adminSingleSession")
            : L.T("log.adminBatchSession", packagesToUpdate.Count));

        IProgress<WingetProgressInfo> adminDownloadProgress = new Progress<WingetProgressInfo>(info =>
        {
            if (info.TotalBytes > 0)
                UpdateLogDownloadLine(info);
        });

        var batchResult = await WingetService.UpgradePackagesAsAdministratorAsync(
            packagesToUpdate.Select(p => p.Id),
            _silentMode,
            _cts!.Token,
            new Progress<string>(s => AppendLog(s)),
            new Progress<UpgradeBatchStatusInfo>(status => ReportElevatedBatchStatus(status, packagesById)),
            adminDownloadProgress);

        if (batchResult.UserCancelled)
        {
            AppendLog($"  ✖ {batchResult.ErrorOutput}");
            return (0, 0, true);
        }

        if (batchResult.CancelledAfterCurrentPackage && batchResult.Items.Count == 0)
        {
            AppendLog(L.T("log.adminCancelledBeforeStart"));
            return (0, 0, true);
        }

        if (batchResult.Items.Count == 0 && !string.IsNullOrWhiteSpace(batchResult.ErrorOutput))
        {
            AppendLog($"  ✖ {batchResult.ErrorOutput}");
            await ShowDialogAsync(L.T("error.updateTitle"), batchResult.ErrorOutput);
            return (0, packagesToUpdate.Count, false);
        }

        int success = 0;
        int failed = 0;
        bool cancelled = false;
        var resultsById = batchResult.Items.ToDictionary(item => item.PackageId, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < packagesToUpdate.Count; i++)
        {
            var pkg = packagesToUpdate[i];

            if (!resultsById.TryGetValue(pkg.Id, out var item))
            {
                if (batchResult.CancelledAfterCurrentPackage)
                {
                    cancelled = true;
                    AppendLog(L.T("log.packageNotStarted", i + 1, packagesToUpdate.Count, pkg.Name, pkg.Id));
                    break;
                }

                failed++;
                AppendLog(L.T("log.resultUnavailable", i + 1, packagesToUpdate.Count, pkg.Name, pkg.Id));
                RecordFailedUpgrade(pkg, L.T("msg.noElevatedResult"));
                _rowTracker.MarkFailed(pkg.Id, L.T("msg.noElevatedResult"));
                continue;
            }

            txtEstado.Text = L.T("status.processingResult", i + 1, packagesToUpdate.Count, pkg.Name);
            AppendLog(L.T("log.packageFinished", i + 1, packagesToUpdate.Count, pkg.Name, pkg.Id));

            if (item.Result.Success)
            {
                success++;
                RecordSuccessfulUpgrade(pkg);
                _rowTracker.MarkSucceeded(pkg.Id);
            }
            else
            {
                failed++;
                string reason = item.Result.GetFailureReason();
                RecordFailedUpgrade(pkg, reason);
                _rowTracker.MarkFailed(pkg.Id, reason);
            }
        }

        return (success, failed, cancelled);
    }

    private void ReportElevatedBatchStatus(UpgradeBatchStatusInfo status, IReadOnlyDictionary<string, WingetPackage> packagesById)
    {
        switch (status.Phase)
        {
            case "starting":
                AppendLog(L.T("log.preparingElevatedBatch"));
                break;
            case "running" when packagesById.TryGetValue(status.PackageId, out var pkg):
                txtEstado.Text = L.T("status.adminUpdatingProgress", status.CurrentIndex, status.TotalCount, pkg.Name);
                _rowTracker.MarkRunning(pkg.Id);
                AppendLog(L.T("log.packageRunning", status.CurrentIndex, status.TotalCount, pkg.Name, pkg.Id));
                break;
            case "cancelled":
                AppendLog(L.T("log.cancellingAfterCurrent"));
                break;
            case "completed":
                AppendLog(L.T("log.elevatedBatchFinished"));
                break;
        }
    }

    private void RecordSuccessfulUpgrade(WingetPackage pkg)
    {
        AppendLog(L.T("log.upgradeSuccess", pkg.Name), LogLineKind.Success);
        _settings.AddHistory(new HistoryEntry
        {
            Date = DateTime.Now,
            PackageName = pkg.Name,
            PackageId = pkg.Id,
            FromVersion = pkg.Version,
            ToVersion = pkg.Available,
            Success = true
        });
    }

    /// <summary>
    /// Anota un fallo en el registro y lo acumula para <see cref="ShowFailureSummaryAsync"/>. No abre
    /// ningún diálogo: hacerlo aquí bloquearía el lote en mitad del bucle esperando un clic.
    /// </summary>
    private void RecordFailedUpgrade(WingetPackage pkg, string reason)
    {
        AppendLog($"  ✖ {pkg.Name}: {reason}", LogLineKind.Error);
        _failedUpgrades.Add(new FailedUpgrade(pkg.Name, pkg.Id, reason));
    }

    /// <summary>Diálogo único, al terminar el lote, con todos los paquetes que fallaron y su motivo.</summary>
    private Task ShowFailureSummaryAsync()
    {
        const int maxListed = 10;

        var sb = new StringBuilder();
        sb.AppendLine(_failedUpgrades.Count == 1
            ? L.T("error.failedSummarySingle")
            : L.T("error.failedSummaryHeader", _failedUpgrades.Count));
        sb.AppendLine();

        foreach (var failure in _failedUpgrades.Take(maxListed))
        {
            sb.AppendLine($"  • {failure.Name} ({failure.Id})");
            sb.AppendLine($"      {failure.Reason}");
        }

        if (_failedUpgrades.Count > maxListed)
            sb.AppendLine(L.T("list.andMore", _failedUpgrades.Count - maxListed));

        sb.AppendLine();
        sb.Append(L.T("error.failedSummaryFooter"));

        return ShowDialogAsync(L.T("error.updateTitle"), sb.ToString());
    }
}
