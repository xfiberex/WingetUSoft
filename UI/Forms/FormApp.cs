using System.Diagnostics;

namespace WingetUSoft
{
    public partial class FormApp : Form
    {
        private const string DefaultSelectionDetails = "Selecciona un programa para ver sus detalles antes de actualizar.";
        private const string EmptySelectionDetails = "Todavia no hay datos cargados. Pulsa \"Consultar actualizaciones\" para empezar.";
        private const int LogMaxLines = 400;
        private const int LogTrimToLine = 50;

        private List<WingetPackage> _packages = [];
        private List<WingetPackage> _allPackages = [];
        private bool _silentMode = true;
        private bool _lastIncludeUnknown;
        private bool _selectAll;
        private CancellationTokenSource? _cts;
        private readonly AppSettings _settings = AppSettings.Load();
        private readonly ToolTip _toolTip = new();
        private System.Windows.Forms.Timer? _autoCheckTimer;
        private ComboBox _cmbFiltroFuente = null!;
        private Panel _pnlFiltro = null!;
        private bool _cancelStopsCurrentProcess = true;
        private bool _fileLoggingAvailable = true;
        private readonly object _logLock = new();

        public FormApp()
        {
            InitializeComponent();
            ConfigureDataGridView();
            WireEvents();

            lblEstado.Font = new Font(lblEstado.Font.FontFamily, 9.5f, FontStyle.Bold);
            lblDetalleEstado.Font = new Font(lblDetalleEstado.Font.FontFamily, 9.25f, FontStyle.Regular);
            lblDescarga.Font = new Font(lblDescarga.Font.FontFamily, 9f, FontStyle.Regular);
            UpdateSelectionDetails();

            _silentMode = _settings.SilentMode;
            silenciosaToolStripMenuItem.Checked = _silentMode;
            interactivaToolStripMenuItem.Checked = !_silentMode;

            SetupTooltips();

            SetupSourceFilter();
            ApplyTheme(_settings.DarkMode);
            modoOscuroToolStripMenuItem.Checked = _settings.DarkMode;
            UpdateAutoCheckTimer();
            Shown += (_, _) => ShowSettingsLoadWarningIfNeeded();

            Load += async (_, _) =>
            {
                string? version = await WingetService.CheckWingetAvailableAsync();
                if (version is null)
                {
                    btnConsultarActualizaciones.Enabled = false;
                    btnConsultarActDesconocidas.Enabled = false;
                    btnActualizarTodosPro.Enabled = false;
                    btnActualizarSeleccionados.Enabled = false;
                    lblEstado.Text = "winget no esta disponible. Instalalo desde Microsoft Store.";
                    lblDetalleEstado.Text = "La aplicacion necesita App Installer o una version reciente de Windows para consultar actualizaciones.";
                    RefreshButtonStyles();
                    MessageBox.Show(
                        "No se encontró winget en el sistema.\n\n" +
                        "Instala 'App Installer' desde la Microsoft Store o actualiza Windows.",
                        "winget no disponible",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
                Text = $"WingetUSoft - Actualiza tus programas  [{version}]";
                lblEstado.Text = "Listo. Pulsa 'Consultar actualizaciones' para comenzar.";
                UpdateSelectionDetails();
            };
        }

        private void ConfigureDataGridView()
        {
            dgvListaProgramas.AutoGenerateColumns = false;
            dgvListaProgramas.Columns.Clear();
            dgvListaProgramas.AllowUserToResizeRows = false;
            dgvListaProgramas.MultiSelect = false;
            dgvListaProgramas.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            dgvListaProgramas.ColumnHeadersHeight = 38;
            dgvListaProgramas.RowTemplate.Height = 34;

            dgvListaProgramas.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "colSeleccionar",
                HeaderText = "Sel.",
                Width = 72,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            dgvListaProgramas.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colNombre",
                HeaderText = "Nombre",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.Automatic
            });

            dgvListaProgramas.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colId",
                HeaderText = "Id",
                ReadOnly = true,
                Width = 220,
                SortMode = DataGridViewColumnSortMode.Automatic
            });

            dgvListaProgramas.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colVersion",
                HeaderText = "Versión actual",
                ReadOnly = true,
                Width = 120,
                SortMode = DataGridViewColumnSortMode.Automatic
            });

            dgvListaProgramas.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDisponible",
                HeaderText = "Disponible",
                ReadOnly = true,
                Width = 120,
                SortMode = DataGridViewColumnSortMode.Automatic
            });

            dgvListaProgramas.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colFuente",
                HeaderText = "Fuente",
                ReadOnly = true,
                Width = 80,
                SortMode = DataGridViewColumnSortMode.Automatic
            });

            UiTheme.StyleDataGridView(dgvListaProgramas, UiTheme.GetPalette(_settings.DarkMode));
        }

        private void WireEvents()
        {
            btnConsultarActualizaciones.Click += BtnConsultarActualizaciones_Click;
            btnConsultarActDesconocidas.Click += BtnConsultarActDesconocidas_Click;
            btnActualizarTodosPro.Click += BtnActualizarTodosPro_Click;
            btnActualizarSeleccionados.Click += BtnActualizarSeleccionados_Click;
            silenciosaToolStripMenuItem.Click += SilenciosaToolStripMenuItem_Click;
            interactivaToolStripMenuItem.Click += InteractivaToolStripMenuItem_Click;
            btnCancelar.Click += BtnCancelar_Click;
            dgvListaProgramas.ColumnHeaderMouseClick += DgvListaProgramas_ColumnHeaderMouseClick;
            dgvListaProgramas.CellMouseDown += DgvListaProgramas_CellMouseDown;
            ctxMenuFila.Opening += CtxMenuFila_Opening;
            ctxItemActualizar.Click += CtxItemActualizar_Click;
            ctxItemCopiarNombre.Click += CtxItemCopiarNombre_Click;
            ctxItemCopiarId.Click += CtxItemCopiarId_Click;
            ctxItemBuscarWeb.Click += CtxItemBuscarWeb_Click;
            ctxItemExcluir.Click += CtxItemExcluir_Click;
            historialToolStripMenuItem.Click += HistorialToolStripMenuItem_Click;
            modoOscuroToolStripMenuItem.Click += ModoOscuroClick;
            exportarListaToolStripMenuItem.Click += ExportarListaClick;
            configuracionToolStripMenuItem.Click += ConfiguracionClick;
            dgvListaProgramas.SelectionChanged += DgvListaProgramas_SelectionChanged;
        }

        private void SetUIBusy(bool busy)
        {
            btnConsultarActualizaciones.Enabled = !busy;
            btnConsultarActDesconocidas.Enabled = !busy;
            btnActualizarTodosPro.Enabled = !busy;
            btnActualizarSeleccionados.Enabled = !busy;
            btnCancelar.Enabled = busy;
            dgvListaProgramas.Enabled = !busy;

            if (busy)
            {
                progressBar1.Style = ProgressBarStyle.Marquee;
            }
            else
            {
                progressBar1.Style = ProgressBarStyle.Continuous;
                progressBar1.Value = 0;
                pbDescarga.Style = ProgressBarStyle.Continuous;
                pbDescarga.Value = 0;
                pbDescarga.Visible = false;
                lblDescarga.Text = "";
                lblDescarga.Visible = false;
                _cancelStopsCurrentProcess = true;
            }

            RefreshButtonStyles();
        }

        private string GetCancelStatusText() => _cancelStopsCurrentProcess
            ? "Cancelando..."
            : "Cancelando después de la operación actual...";

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && _cts is not null)
            {
                _cts?.Cancel();
                btnCancelar.Enabled = false;
                lblEstado.Text = GetCancelStatusText();
                return true;
            }
            if (_cts is not null) return base.ProcessCmdKey(ref msg, keyData);

            switch (keyData)
            {
                case Keys.F5:
                    _ = LoadPackagesAsync(_lastIncludeUnknown);
                    return true;
                case Keys.Control | Keys.A when dgvListaProgramas.Rows.Count > 0:
                    _selectAll = !_selectAll;
                    foreach (DataGridViewRow row in dgvListaProgramas.Rows)
                        row.Cells["colSeleccionar"].Value = _selectAll;
                    dgvListaProgramas.RefreshEdit();
                    return true;
                case Keys.Delete when GetSelectedPackage() is not null:
                    CtxItemExcluir_Click(null, EventArgs.Empty);
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void LoadPackagesToGrid()
        {
            dgvListaProgramas.Rows.Clear();
            _selectAll = false;
            UiPalette palette = UiTheme.GetPalette(_settings.DarkMode);

            foreach (var pkg in _packages)
            {
                int idx = dgvListaProgramas.Rows.Add(false, pkg.Name, pkg.Id, pkg.Version, pkg.Available, pkg.Source);
                var row = dgvListaProgramas.Rows[idx];
                row.Tag = pkg;

                ApplyPackageRowStyle(row, pkg, palette);
            }

            UpdateSelectionDetails();
        }

        private async Task LoadPackagesAsync(bool includeUnknown)
        {
            _lastIncludeUnknown = includeUnknown;
            _cancelStopsCurrentProcess = true;
            _cts = new CancellationTokenSource();
            SetUIBusy(true);
            lblEstado.Text = includeUnknown
                ? "Consultando actualizaciones (incluidas desconocidas)..."
                : "Consultando actualizaciones disponibles...";

            try
            {
                _allPackages = await WingetService.GetUpgradablePackagesAsync(includeUnknown, _cts.Token);
                UpdateSourceFilter();
                ApplySourceFilter();
                LoadPackagesToGrid();

                gbActulizacionesDisp.Text = includeUnknown
                    ? "Actualizaciones disponibles (incluidas desconocidas)"
                    : "Actualizaciones disponibles";

                string sufijo = includeUnknown ? " (incluidas desconocidas)" : "";
                lblEstado.Text = _packages.Count == 0
                    ? $"No se encontraron actualizaciones disponibles{sufijo}."
                    : $"Se encontraron {_packages.Count} actualización(es) disponible(s){sufijo}.";
            }
            catch (OperationCanceledException)
            {
                dgvListaProgramas.Rows.Clear();
                lblEstado.Text = "Consulta cancelada.";
                UpdateSelectionDetails();
            }
            catch (Exception ex)
            {
                lblEstado.Text = "Error al consultar actualizaciones.";
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
                SetUIBusy(false);
            }
        }

        private async Task UpdatePackagesAsync(bool allPackages)
        {
            var packagesToUpdate = new List<WingetPackage>();
            bool runAsAdministrator = _settings.RunUpdatesAsAdministrator;

            if (allPackages)
            {
                packagesToUpdate.AddRange(_packages.Where(p => !_settings.ExcludedIds.Contains(p.Id)));
            }
            else
            {
                foreach (DataGridViewRow row in dgvListaProgramas.Rows)
                {
                    if (row.Cells["colSeleccionar"].Value is true && row.Tag is WingetPackage pkg
                        && !_settings.ExcludedIds.Contains(pkg.Id))
                        packagesToUpdate.Add(pkg);
                }
            }

            if (packagesToUpdate.Count == 0)
            {
                MessageBox.Show(
                    "No hay programas seleccionados para actualizar.",
                    "Información",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (runAsAdministrator)
            {
                string adminMessage = packagesToUpdate.Count == 1
                    ? "La actualización se ejecutará con permisos de administrador.\n\nWindows mostrará el aviso de UAC y el progreso detallado se reemplazará por un indicador general.\n\n¿Desea continuar?"
                    : $"Las {packagesToUpdate.Count} actualizaciones se ejecutarán con permisos de administrador.\n\nWindows pedirá confirmación de UAC una sola vez para todo el lote y el progreso detallado se reemplazará por un indicador general.\n\n¿Desea continuar?";

                if (MessageBox.Show(
                        adminMessage,
                        "Confirmar modo administrador",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
            }

            _cts = new CancellationTokenSource();
            _cancelStopsCurrentProcess = !runAsAdministrator;
            SetUIBusy(true);
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = packagesToUpdate.Count;
            progressBar1.Value = 0;

            int success = 0;
            int failed = 0;
            bool cancelled = false;
            bool shouldReload = false;
            bool historyChanged = false;

            rtbLog.Clear();

            try
            {
                if (runAsAdministrator)
                {
                    (success, failed, cancelled) = await UpdatePackagesAsAdministratorAsync(packagesToUpdate);
                    historyChanged = success > 0;
                }
                else
                {
                    for (int i = 0; i < packagesToUpdate.Count; i++)
                    {
                        var pkg = packagesToUpdate[i];
                        lblEstado.Text = $"Actualizando ({i + 1}/{packagesToUpdate.Count}): {pkg.Name}...";
                        pbDescarga.Style = ProgressBarStyle.Continuous;
                        pbDescarga.Value = 0;
                        lblDescarga.Text = "Iniciando descarga...";
                        pbDescarga.Visible = true;
                        lblDescarga.Visible = true;

                        IProgress<WingetProgressInfo>? progressReporter = new Progress<WingetProgressInfo>(info =>
                        {
                            if (info.TotalBytes <= 0) return;

                            pbDescarga.Value = (int)Math.Clamp(info.DownloadedBytes * 100L / info.TotalBytes, 0, 100);

                            string dl = FormatBytes(info.DownloadedBytes);
                            string total = FormatBytes(info.TotalBytes);
                            string speed = info.SpeedBytesPerSecond > 0
                                ? $"  —  {FormatBytes((long)info.SpeedBytesPerSecond)}/s"
                                : "";
                            lblDescarga.Text = $"{dl} / {total}{speed}";
                        });

                        try
                        {
                            AppendLog($"[{i + 1}/{packagesToUpdate.Count}] Iniciando: {pkg.Name} ({pkg.Id})");

                            var result = await WingetService.UpgradePackageAsync(
                                pkg.Id, _silentMode, false, progressReporter, _cts.Token,
                                new Progress<string>(AppendLog));

                            if (result.Success)
                            {
                                success++;
                                historyChanged = true;
                                RecordSuccessfulUpgrade(pkg);
                            }
                            else
                            {
                                failed++;
                                HandleFailedUpgrade(pkg, result.GetFailureReason());
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            cancelled = true;
                            break;
                        }

                        progressBar1.Value = i + 1;

                        if (_cts.Token.IsCancellationRequested)
                        {
                            cancelled = true;
                            break;
                        }
                    }
                }

                if (cancelled)
                {
                    lblEstado.Text = $"Cancelado. Completados: {success}, Fallidos: {failed}.";
                    return;
                }

                if (success > 0)
                {
                    lblEstado.Text = $"Completada. Éxito: {success}, Fallidos: {failed}. Actualizando lista...";
                    shouldReload = true;
                }
                else
                {
                    lblEstado.Text = $"Actualización completada. Éxito: {success}, Fallidos: {failed}.";
                }
            }
            catch (OperationCanceledException)
            {
                lblEstado.Text = $"Cancelado. Completados: {success}, Fallidos: {failed}.";
            }
            catch (Exception ex)
            {
                lblEstado.Text = "Error al actualizar programas.";
                MessageBox.Show(ex.Message, "Error de actualización", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (historyChanged)
                    TrySaveSettings("No se pudo guardar el historial de actualizaciones.", updateStatusLabel: false);

                _cts?.Dispose();
                _cts = null;
                SetUIBusy(false);
            }

            if (shouldReload)
                await LoadPackagesAsync(_lastIncludeUnknown);
        }

        private async Task<(int Success, int Failed, bool Cancelled)> UpdatePackagesAsAdministratorAsync(List<WingetPackage> packagesToUpdate)
        {
            var packagesById = packagesToUpdate.ToDictionary(pkg => pkg.Id, StringComparer.OrdinalIgnoreCase);

            progressBar1.Style = ProgressBarStyle.Marquee;
            pbDescarga.Style = ProgressBarStyle.Marquee;
            pbDescarga.Visible = true;
            lblDescarga.Visible = true;
            lblDescarga.Text = "Esperando confirmación de UAC...";
            lblEstado.Text = packagesToUpdate.Count == 1
                ? $"Actualizando en modo administrador: {packagesToUpdate[0].Name}..."
                : $"Actualizando {packagesToUpdate.Count} programas en modo administrador...";

            AppendLog(packagesToUpdate.Count == 1
                ? "Ejecutando la actualización en una única sesión de administrador."
                : $"Ejecutando {packagesToUpdate.Count} actualizaciones en una única sesión de administrador.");

            var batchResult = await WingetService.UpgradePackagesAsAdministratorAsync(
                packagesToUpdate.Select(p => p.Id),
                _silentMode,
                _cts!.Token,
                new Progress<string>(AppendLog),
                new Progress<UpgradeBatchStatusInfo>(status => ReportElevatedBatchStatus(status, packagesById)));

            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = packagesToUpdate.Count;
            progressBar1.Value = 0;
            pbDescarga.Style = ProgressBarStyle.Continuous;
            pbDescarga.Value = 0;

            if (batchResult.UserCancelled)
            {
                lblDescarga.Text = "Cancelado por el usuario.";
                AppendLog($"  ✖ {batchResult.ErrorOutput}");
                return (0, 0, true);
            }

            if (batchResult.CancelledAfterCurrentPackage && batchResult.Items.Count == 0)
            {
                lblDescarga.Text = "Cancelado.";
                AppendLog("La operación se canceló antes de iniciar el lote elevado.");
                return (0, 0, true);
            }

            if (batchResult.Items.Count == 0 && !string.IsNullOrWhiteSpace(batchResult.ErrorOutput))
            {
                lblDescarga.Text = "Error.";
                AppendLog($"  ✖ {batchResult.ErrorOutput}");
                MessageBox.Show(
                    batchResult.ErrorOutput,
                    "Error de actualización",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
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
                        lblDescarga.Text = "Cancelado.";
                        AppendLog($"[{i + 1}/{packagesToUpdate.Count}] Cancelado antes de iniciar: {pkg.Name} ({pkg.Id})");
                        break;
                    }

                    failed++;
                    AppendLog($"[{i + 1}/{packagesToUpdate.Count}] Resultado no disponible: {pkg.Name} ({pkg.Id})");
                    HandleFailedUpgrade(pkg, "No se recibió un resultado de la actualización elevada.");
                    progressBar1.Value = i + 1;
                    continue;
                }

                lblEstado.Text = $"Procesando resultado ({i + 1}/{packagesToUpdate.Count}): {pkg.Name}...";
                AppendLog($"[{i + 1}/{packagesToUpdate.Count}] Finalizado: {pkg.Name} ({pkg.Id})");

                if (item.Result.Success)
                {
                    success++;
                    RecordSuccessfulUpgrade(pkg);
                }
                else
                {
                    failed++;
                    HandleFailedUpgrade(pkg, item.Result.GetFailureReason());
                }

                progressBar1.Value = i + 1;
            }

            if (!cancelled)
                lblDescarga.Text = "Completado.";

            return (success, failed, cancelled);
        }

        private void ReportElevatedBatchStatus(
            UpgradeBatchStatusInfo status,
            IReadOnlyDictionary<string, WingetPackage> packagesById)
        {
            switch (status.Phase)
            {
                case "starting":
                    lblDescarga.Text = "Preparando lote elevado...";
                    break;
                case "running" when packagesById.TryGetValue(status.PackageId, out var pkg):
                    lblEstado.Text = $"Actualizando ({status.CurrentIndex}/{status.TotalCount}) en modo administrador: {pkg.Name}...";
                    lblDescarga.Text = $"En ejecución: {pkg.Name}";
                    AppendLog($"[{status.CurrentIndex}/{status.TotalCount}] En ejecución: {pkg.Name} ({pkg.Id})");
                    break;
                case "cancelled":
                    lblDescarga.Text = "Cancelando después del paquete actual...";
                    break;
                case "completed":
                    lblDescarga.Text = "Lote elevado finalizado. Procesando resultados...";
                    break;
            }
        }

        private void RecordSuccessfulUpgrade(WingetPackage pkg)
        {
            pbDescarga.Value = 100;
            lblDescarga.Text = "Completado.";
            AppendLog($"  \u2714 {pkg.Name}: actualizado correctamente.");
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

        private void HandleFailedUpgrade(WingetPackage pkg, string reason)
        {
            AppendLog($"  ✖ {pkg.Name}: {reason}");

            MessageBox.Show(
                $"No se pudo actualizar \"{pkg.Name}\" (Id: {pkg.Id}).\n\n" +
                $"Motivo: {reason}\n\n" +
                "Por seguridad, la aplicación no abrirá búsquedas web automáticas para descargas manuales.\n" +
                $"Use el Id del paquete ({pkg.Id}) para verificar manualmente el sitio oficial del proveedor o revisar el paquete directamente con winget.",
                "Error de actualización",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private async void BtnConsultarActualizaciones_Click(object? sender, EventArgs e)
        {
            await LoadPackagesAsync(includeUnknown: false);
        }

        private async void BtnConsultarActDesconocidas_Click(object? sender, EventArgs e)
        {
            await LoadPackagesAsync(includeUnknown: true);
        }

        private async void BtnActualizarTodosPro_Click(object? sender, EventArgs e)
        {
            var pendientes = _packages.Where(p => !_settings.ExcludedIds.Contains(p.Id)).ToList();
            if (pendientes.Count == 0)
            {
                MessageBox.Show("No hay programas para actualizar.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string lista = string.Join("\n  \u2022 ", pendientes.Take(10).Select(p => p.Name));
            if (pendientes.Count > 10) lista += $"\n  ... y {pendientes.Count - 10} más";
            if (MessageBox.Show(
                    $"Se van a actualizar {pendientes.Count} programa(s):\n\n  \u2022 {lista}\n\n\u00bfDesea continuar?",
                    "Confirmar actualización", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            await UpdatePackagesAsync(allPackages: true);
        }

        private async void BtnActualizarSeleccionados_Click(object? sender, EventArgs e)
        {
            await UpdatePackagesAsync(allPackages: false);
        }

        private void SilenciosaToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            _silentMode = true;
            silenciosaToolStripMenuItem.Checked = true;
            interactivaToolStripMenuItem.Checked = false;
            _settings.SilentMode = true;
            TrySaveSettings("No se pudo guardar el modo de actualización.");
        }

        private void InteractivaToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            _silentMode = false;
            silenciosaToolStripMenuItem.Checked = false;
            interactivaToolStripMenuItem.Checked = true;
            _settings.SilentMode = false;
            TrySaveSettings("No se pudo guardar el modo de actualización.");
        }

        private void BtnCancelar_Click(object? sender, EventArgs e)
        {
            _cts?.Cancel();
            btnCancelar.Enabled = false;
            lblEstado.Text = GetCancelStatusText();
            RefreshButtonStyles();
        }

        private void DgvListaProgramas_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex != dgvListaProgramas.Columns["colSeleccionar"]!.Index) return;

            _selectAll = !_selectAll;
            foreach (DataGridViewRow row in dgvListaProgramas.Rows)
                row.Cells["colSeleccionar"].Value = _selectAll;

            dgvListaProgramas.RefreshEdit();
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1_073_741_824L) return $"{bytes / 1_073_741_824.0:F1} GB";
            if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F1} MB";
            if (bytes >= 1_024) return $"{bytes / 1_024.0:F1} KB";
            return $"{bytes} B";
        }

        private void AppendLog(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (rtbLog.Lines.Length > LogMaxLines)
            {
                rtbLog.Select(0, rtbLog.GetFirstCharIndexFromLine(LogTrimToLine));
                rtbLog.SelectedText = "";
            }
            rtbLog.AppendText(text + Environment.NewLine);
            rtbLog.ScrollToCaret();
            AppendLogFile(text);
        }

        private void AppendLogFile(string text)
        {
            if (!_settings.LogToFile || !_fileLoggingAvailable) return;

            try
            {
                string path;
                lock (_logLock)
                {
                    Directory.CreateDirectory(AppSettings.LogDirectory);
                    path = Path.Combine(AppSettings.LogDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");
                    File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
                }
            }
            catch (Exception ex)
            {
                lock (_logLock) { _fileLoggingAvailable = false; }
                Trace.WriteLine($"No se pudo escribir el archivo de log: {ex.Message}");

                if (!IsDisposed)
                {
                    rtbLog.AppendText($"[aviso] No se pudo escribir el archivo de log: {ex.Message}{Environment.NewLine}");
                    rtbLog.ScrollToCaret();
                }
            }
        }

        private WingetPackage? GetSelectedPackage()
        {
            if (dgvListaProgramas.SelectedRows.Count == 0) return null;
            return dgvListaProgramas.SelectedRows[0].Tag as WingetPackage;
        }

        private void DgvListaProgramas_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                dgvListaProgramas.ClearSelection();
                dgvListaProgramas.Rows[e.RowIndex].Selected = true;
            }
        }

        private void CtxMenuFila_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            var pkg = GetSelectedPackage();
            bool hasPackage = pkg is not null;
            bool notBusy = !btnCancelar.Enabled;
            ctxItemActualizar.Enabled = hasPackage && notBusy;
            ctxItemCopiarNombre.Enabled = hasPackage;
            ctxItemCopiarId.Enabled = hasPackage;
            ctxItemBuscarWeb.Enabled = hasPackage;
            ctxItemExcluir.Enabled = hasPackage;
            if (pkg is not null)
                ctxItemExcluir.Text = _settings.ExcludedIds.Contains(pkg.Id)
                    ? "Quitar de exclusiones"
                    : "Excluir de actualizaciones";
            if (!hasPackage) e.Cancel = true;
        }

        private async void CtxItemActualizar_Click(object? sender, EventArgs e)
        {
            if (GetSelectedPackage() is null) return;
            foreach (DataGridViewRow row in dgvListaProgramas.Rows)
                row.Cells["colSeleccionar"].Value = false;
            dgvListaProgramas.SelectedRows[0].Cells["colSeleccionar"].Value = true;
            await UpdatePackagesAsync(allPackages: false);
        }

        private void CtxItemCopiarNombre_Click(object? sender, EventArgs e)
        {
            if (GetSelectedPackage() is { } pkg)
                Clipboard.SetText(pkg.Name);
        }

        private void CtxItemCopiarId_Click(object? sender, EventArgs e)
        {
            if (GetSelectedPackage() is { } pkg)
                Clipboard.SetText(pkg.Id);
        }

        private void CtxItemBuscarWeb_Click(object? sender, EventArgs e)
        {
            if (GetSelectedPackage() is not { } pkg) return;
            Clipboard.SetText(pkg.Id);
            MessageBox.Show(
                $"Se copió el Id del paquete al portapapeles:\n\n{pkg.Id}\n\n" +
                "Revise manualmente el paquete y confirme que la descarga proviene del sitio oficial del proveedor.",
                "Id copiado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void CtxItemExcluir_Click(object? sender, EventArgs e)
        {
            if (GetSelectedPackage() is not { } pkg) return;
            if (_settings.ExcludedIds.Contains(pkg.Id))
                _settings.ExcludedIds.Remove(pkg.Id);
            else
                _settings.ExcludedIds.Add(pkg.Id);
            TrySaveSettings("No se pudieron guardar las exclusiones.");
            LoadPackagesToGrid();
        }

        private void HistorialToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            using var form = new FormHistory(_settings.History, _settings.DarkMode);
            form.ShowDialog(this);
        }

        private void ModoOscuroClick(object? sender, EventArgs e)
        {
            _settings.DarkMode = modoOscuroToolStripMenuItem.Checked;
            TrySaveSettings("No se pudo guardar la configuración visual.");
            ApplyTheme(_settings.DarkMode);
        }

        private void ExportarListaClick(object? sender, EventArgs e)
        {
            if (_packages.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var sfd = new SaveFileDialog
            {
                Title = "Exportar lista de actualizaciones",
                Filter = "CSV (*.csv)|*.csv|Texto (*.txt)|*.txt",
                DefaultExt = "csv",
                FileName = $"actualizaciones_{DateTime.Now:yyyy-MM-dd}"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            char sep = Path.GetExtension(sfd.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase) ? ',' : '\t';
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(DelimitedTextExporter.BuildRow(sep, "Nombre", "Id", "Versión actual", "Disponible", "Fuente"));
            foreach (var pkg in _packages)
                sb.AppendLine(DelimitedTextExporter.BuildRow(sep, pkg.Name, pkg.Id, pkg.Version, pkg.Available, pkg.Source));
            File.WriteAllText(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);
            lblEstado.Text = $"Lista exportada: {Path.GetFileName(sfd.FileName)}";
        }

        private void ConfiguracionClick(object? sender, EventArgs e)
        {
            using var form = new FormSettings(_settings);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                _silentMode = _settings.SilentMode;
                silenciosaToolStripMenuItem.Checked = _silentMode;
                interactivaToolStripMenuItem.Checked = !_silentMode;
                UpdateAutoCheckTimer();
                LoadPackagesToGrid();
                ApplyTheme(_settings.DarkMode);
            }
        }

        private void DgvListaProgramas_SelectionChanged(object? sender, EventArgs e)
        {
            UpdateSelectionDetails();
        }

        private void SetupSourceFilter()
        {
            _pnlFiltro = new Panel { Height = 42, Dock = DockStyle.Top, Padding = new Padding(0, 0, 0, 8) };
            var lblFiltro = new Label
            {
                Text = "Fuente:",
                Dock = DockStyle.Left,
                Width = 68,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            };
            _cmbFiltroFuente = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbFiltroFuente.Items.Add("Todas las fuentes");
            _cmbFiltroFuente.SelectedIndex = 0;
            _cmbFiltroFuente.SelectedIndexChanged += CmbFiltroFuente_SelectedIndexChanged;
            _pnlFiltro.Controls.Add(_cmbFiltroFuente);
            _pnlFiltro.Controls.Add(lblFiltro);
            gbActulizacionesDisp.Controls.Add(_pnlFiltro);
        }

        private void UpdateSourceFilter()
        {
            string? current = _cmbFiltroFuente.SelectedIndex > 0 ? _cmbFiltroFuente.SelectedItem as string : null;
            _cmbFiltroFuente.SelectedIndexChanged -= CmbFiltroFuente_SelectedIndexChanged;
            _cmbFiltroFuente.Items.Clear();
            _cmbFiltroFuente.Items.Add("Todas las fuentes");
            foreach (var src in _allPackages.Select(p => p.Source).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s))
                _cmbFiltroFuente.Items.Add(src);
            _cmbFiltroFuente.SelectedIndex = current is not null && _cmbFiltroFuente.Items.Contains(current)
                ? _cmbFiltroFuente.Items.IndexOf(current)
                : 0;
            _cmbFiltroFuente.SelectedIndexChanged += CmbFiltroFuente_SelectedIndexChanged;
        }

        private void ApplySourceFilter()
        {
            _packages = _cmbFiltroFuente.SelectedIndex <= 0 || _cmbFiltroFuente.SelectedItem is not string src
                ? [.. _allPackages]
                : [.. _allPackages.Where(p => p.Source == src)];
        }

        private void CmbFiltroFuente_SelectedIndexChanged(object? sender, EventArgs e)
        {
            ApplySourceFilter();
            LoadPackagesToGrid();
            string sufijo = _lastIncludeUnknown ? " (incluidas desconocidas)" : "";
            lblEstado.Text = _packages.Count == 0
                ? $"No se encontraron actualizaciones disponibles{sufijo}."
                : $"Se encontraron {_packages.Count} actualización(es) disponible(s){sufijo}.";
        }

        private void UpdateAutoCheckTimer()
        {
            _autoCheckTimer?.Stop();
            _autoCheckTimer?.Dispose();
            _autoCheckTimer = null;
            if (_settings.AutoCheckIntervalMinutes <= 0) return;
            _autoCheckTimer = new System.Windows.Forms.Timer
            {
                Interval = _settings.AutoCheckIntervalMinutes * 60_000
            };
            _autoCheckTimer.Tick += async (_, _) =>
            {
                if (_cts is not null) return;
                int prevCount = _packages.Count;
                await LoadPackagesAsync(_lastIncludeUnknown);
                if (_packages.Count > prevCount)
                    lblEstado.Text += "  ¡Nuevas actualizaciones disponibles!";
            };
            _autoCheckTimer.Start();
        }

        private bool TrySaveSettings(string userMessage, bool updateStatusLabel = true)
        {
            if (_settings.Save())
                return true;

            if (updateStatusLabel)
                lblEstado.Text = "No se pudo guardar la configuración.";

            MessageBox.Show(
                string.IsNullOrWhiteSpace(_settings.LastSaveError)
                    ? userMessage
                    : $"{userMessage}\n\n{_settings.LastSaveError}",
                "Error de configuración",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return false;
        }

        private void ShowSettingsLoadWarningIfNeeded()
        {
            if (string.IsNullOrWhiteSpace(_settings.LastLoadError))
                return;

            MessageBox.Show(
                _settings.LastLoadError,
                "Configuración restablecida",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void ApplyTheme(bool dark)
        {
            UiPalette palette = UiTheme.GetPalette(dark);

            UiTheme.ApplyForm(this, palette);
            layoutRoot.BackColor = palette.WindowBackground;
            splitMain.BackColor = palette.Border;
            splitMain.Panel1.BackColor = palette.WindowBackground;
            splitMain.Panel2.BackColor = palette.WindowBackground;

            UiTheme.StylePanel(pnlHeader, palette, raised: true);
            UiTheme.StylePanel(pnlStatus, palette, raised: true);
            UiTheme.StylePanel(pnlAcciones, palette);
            UiTheme.StyleGroupBox(gbAcciones, palette);
            UiTheme.StyleGroupBox(gbActulizacionesDisp, palette);
            UiTheme.StyleGroupBox(gbLog, palette);
            UiTheme.StyleLabel(lblSubtitulo, palette, muted: true);
            UiTheme.StyleLabel(lblDetalleEstado, palette, muted: true);
            UiTheme.StyleLabel(lblAtajos, palette, muted: true);
            UiTheme.StyleLabel(lblEstado, palette);
            UiTheme.StyleLabel(lblDescarga, palette, muted: true);
            lblTitulo.ForeColor = palette.Text;

            UiTheme.StyleToolStrip(menuStrip1, palette);
            UiTheme.StyleToolStrip(ctxMenuFila, palette);
            UiTheme.StyleDataGridView(dgvListaProgramas, palette);
            UiTheme.StyleRichTextBox(rtbLog, palette);
            RefreshButtonStyles();

            if (_pnlFiltro is not null)
            {
                _pnlFiltro.BackColor = palette.Surface;
                _pnlFiltro.ForeColor = palette.Text;
                foreach (Control control in _pnlFiltro.Controls)
                {
                    if (control is Label label)
                        UiTheme.StyleLabel(label, palette, muted: true);
                    else if (control is ComboBox comboBox)
                        UiTheme.StyleComboBox(comboBox, palette);
                }
            }

            foreach (DataGridViewRow row in dgvListaProgramas.Rows)
            {
                if (row.Tag is WingetPackage pkg)
                    ApplyPackageRowStyle(row, pkg, palette);
            }
        }

        private void SetupTooltips()
        {
            _toolTip.SetToolTip(btnConsultarActualizaciones, "Consulta los programas con actualizaciones disponibles.");
            _toolTip.SetToolTip(btnConsultarActDesconocidas, "Consulta incluyendo paquetes con version desconocida (\"<\").");
            _toolTip.SetToolTip(btnActualizarTodosPro, "Actualiza todos los programas de la lista, excepto los excluidos.");
            _toolTip.SetToolTip(btnActualizarSeleccionados, "Actualiza solo los programas con la casilla marcada.");
            _toolTip.SetToolTip(btnCancelar, "Cancela la operación en curso.");

            if (dgvListaProgramas.Columns["colSeleccionar"] is DataGridViewColumn selectColumn)
                selectColumn.ToolTipText = "Haz clic en la cabecera para marcar o desmarcar toda la lista. Tambien puedes usar Ctrl+A.";

            if (dgvListaProgramas.Columns["colVersion"] is DataGridViewColumn versionColumn)
                versionColumn.ToolTipText = "Version instalada. El prefijo '<' indica version desconocida.";

            if (dgvListaProgramas.Columns["colDisponible"] is DataGridViewColumn availableColumn)
                availableColumn.ToolTipText = "Version disponible para actualizar.";

            if (dgvListaProgramas.Columns["colId"] is DataGridViewColumn idColumn)
                idColumn.ToolTipText = "Identificador unico del paquete en winget.";
        }

        private void RefreshButtonStyles()
        {
            UiPalette palette = UiTheme.GetPalette(_settings.DarkMode);
            UiTheme.StyleButton(btnConsultarActualizaciones, palette, AppButtonKind.Primary);
            UiTheme.StyleButton(btnConsultarActDesconocidas, palette, AppButtonKind.Secondary);
            UiTheme.StyleButton(btnActualizarSeleccionados, palette, AppButtonKind.Secondary);
            UiTheme.StyleButton(btnActualizarTodosPro, palette, AppButtonKind.Secondary);
            UiTheme.StyleButton(btnCancelar, palette, AppButtonKind.Danger);
        }

        private void ApplyPackageRowStyle(DataGridViewRow row, WingetPackage pkg, UiPalette palette)
        {
            row.DefaultCellStyle.BackColor = row.Index % 2 == 0 ? palette.Surface : palette.SurfaceRaised;
            row.DefaultCellStyle.ForeColor = palette.Text;
            row.DefaultCellStyle.SelectionBackColor = palette.Selection;
            row.DefaultCellStyle.SelectionForeColor = palette.AccentText;
            row.Cells["colVersion"].Style.ForeColor = palette.Text;
            row.Cells["colVersion"].Style.Font = dgvListaProgramas.Font;

            if (_settings.ExcludedIds.Contains(pkg.Id))
            {
                row.DefaultCellStyle.ForeColor = palette.ExcludedRowText;
                row.DefaultCellStyle.BackColor = palette.ExcludedRowBackground;
            }
            else if (pkg.Version.TrimStart().StartsWith("<", StringComparison.Ordinal))
            {
                row.Cells["colVersion"].Style.ForeColor = palette.WarningText;
                row.Cells["colVersion"].Style.Font = new Font(dgvListaProgramas.Font, FontStyle.Bold);
            }
        }

        private void UpdateSelectionDetails()
        {
            WingetPackage? pkg = GetSelectedPackage();

            if (pkg is null)
            {
                lblDetalleEstado.Text = _packages.Count == 0 ? EmptySelectionDetails : DefaultSelectionDetails;
                return;
            }

            string state = _settings.ExcludedIds.Contains(pkg.Id)
                ? "Excluido de actualizaciones automaticas"
                : "Listo para actualizar";

            lblDetalleEstado.Text = $"{pkg.Name} | {pkg.Id} | {pkg.Version} -> {pkg.Available} | {pkg.Source} | {state}";
        }
    }
}
