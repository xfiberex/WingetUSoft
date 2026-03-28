namespace WingetUSoft
{
    internal sealed class FormHistory : Form
    {
        private readonly List<HistoryEntry> _history;
        private readonly bool _darkMode;
        private TableLayoutPanel _layoutRoot = null!;
        private Panel _pnlHeader = null!;
        private Label _lblTitle = null!;
        private Label _lblSummary = null!;
        private DataGridView _dgv = null!;
        private Button _btnCerrar = null!;

        public FormHistory(List<HistoryEntry> history, bool darkMode)
        {
            _history = history;
            _darkMode = darkMode;
            BuildUI();
            LoadHistory();
            ApplyTheme(_darkMode);
        }

        private void BuildUI()
        {
            Text = "Historial de actualizaciones";
            Size = new Size(1000, 620);
            MinimumSize = new Size(760, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Font = new Font("Segoe UI", 9F);

            _layoutRoot = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 1,
                RowCount = 3
            };
            _layoutRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _layoutRoot.RowStyles.Add(new RowStyle());
            _layoutRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _layoutRoot.RowStyles.Add(new RowStyle());

            _pnlHeader = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 16, 18, 16),
                Margin = new Padding(0, 0, 0, 12)
            };

            var headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerLayout.RowStyles.Add(new RowStyle());
            headerLayout.RowStyles.Add(new RowStyle());

            _lblTitle = new Label
            {
                Text = "Historial reciente",
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 4)
            };

            _lblSummary = new Label
            {
                AutoSize = true,
                Text = "Consulta el resultado de las ultimas actualizaciones ejecutadas desde la aplicacion."
            };

            headerLayout.Controls.Add(_lblTitle, 0, 0);
            headerLayout.Controls.Add(_lblSummary, 0, 1);
            _pnlHeader.Controls.Add(headerLayout);

            _dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoGenerateColumns = false,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeight = 36,
                ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText
            };

            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colFecha", HeaderText = "Fecha", Width = 155 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNombre", HeaderText = "Nombre", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "Id", Width = 210 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDe", HeaderText = "Version origen", Width = 120 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colA", HeaderText = "Version destino", Width = 120 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEstado", HeaderText = "Estado", Width = 120 });

            var buttonsBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };

            _btnCerrar = new Button
            {
                Text = "Cerrar",
                DialogResult = DialogResult.Cancel,
                Size = new Size(118, 40),
                Margin = new Padding(10, 0, 0, 0)
            };

            buttonsBar.Controls.Add(_btnCerrar);

            _layoutRoot.Controls.Add(_pnlHeader, 0, 0);
            _layoutRoot.Controls.Add(_dgv, 0, 1);
            _layoutRoot.Controls.Add(buttonsBar, 0, 2);
            Controls.Add(_layoutRoot);

            AcceptButton = _btnCerrar;
            CancelButton = _btnCerrar;
        }

        private void LoadHistory()
        {
            if (_history.Count == 0)
            {
                _dgv.Rows.Add("-", "No hay entradas en el historial.", "", "", "", "Sin datos");
                _dgv.ClearSelection();
                _lblSummary.Text = "Aun no se ha registrado ninguna actualizacion desde la aplicacion.";
                return;
            }

            int successCount = 0;
            int failedCount = 0;

            foreach (var entry in _history)
            {
                int idx = _dgv.Rows.Add(
                    entry.Date.ToString("dd/MM/yyyy HH:mm"),
                    entry.PackageName,
                    entry.PackageId,
                    entry.FromVersion,
                    entry.ToVersion,
                    entry.Success ? "Exito" : "Fallido");

                _dgv.Rows[idx].Tag = entry;

                if (entry.Success)
                    successCount++;
                else
                    failedCount++;
            }

            _lblSummary.Text = $"{_history.Count} registro(s) cargados. Exito: {successCount}. Fallidos: {failedCount}.";
        }

        private void ApplyTheme(bool dark)
        {
            UiPalette palette = UiTheme.GetPalette(dark);

            UiTheme.ApplyForm(this, palette);
            _layoutRoot.BackColor = palette.WindowBackground;
            UiTheme.StylePanel(_pnlHeader, palette, raised: true);
            _lblTitle.ForeColor = palette.Text;
            UiTheme.StyleLabel(_lblSummary, palette, muted: true);
            UiTheme.StyleDataGridView(_dgv, palette);
            UiTheme.StyleButton(_btnCerrar, palette, AppButtonKind.Secondary);

            foreach (DataGridViewRow row in _dgv.Rows)
            {
                if (row.Tag is not HistoryEntry entry)
                {
                    row.DefaultCellStyle.ForeColor = palette.MutedText;
                    continue;
                }

                row.DefaultCellStyle.ForeColor = palette.Text;
                row.Cells["colEstado"].Style.ForeColor = entry.Success ? palette.Success : palette.Danger;
                row.Cells["colEstado"].Style.Font = new Font(_dgv.Font, FontStyle.Bold);
            }
        }
    }
}