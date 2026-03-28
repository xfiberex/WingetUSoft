namespace WingetUSoft
{
    internal sealed class FormSettings : Form
    {
        private readonly AppSettings _settings;
        private TableLayoutPanel _layoutRoot = null!;
        private GroupBox _gbGeneral = null!;
        private GroupBox _gbExcluidos = null!;
        private Label _lblIntervalo = null!;
        private Label _lblLogPath = null!;
        private Label _lblAdminInfo = null!;
        private ComboBox _cmbIntervalo = null!;
        private CheckBox _chkLogArchivo = null!;
        private CheckBox _chkModoAdministrador = null!;
        private TextBox _txtLogPath = null!;
        private ListBox _lstExcluidos = null!;
        private Button _btnQuitar = null!;
        private Button _btnLimpiar = null!;
        private Button _btnOk = null!;
        private Button _btnCancelar = null!;

        public FormSettings(AppSettings settings)
        {
            _settings = settings;
            BuildUI();
            LoadFromSettings();
            ApplyTheme(_settings.DarkMode);
        }

        private void BuildUI()
        {
            Text = "Configuracion";
            Size = new Size(760, 560);
            MinimumSize = new Size(640, 500);
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

            _gbGeneral = new GroupBox
            {
                Text = "Preferencias",
                Dock = DockStyle.Top,
                Padding = new Padding(14, 10, 14, 14),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 12)
            };

            var generalLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            generalLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            generalLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            _lblIntervalo = new Label
            {
                Text = "Auto-verificar cada:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 10, 10)
            };

            _cmbIntervalo = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 0, 0, 10)
            };
            _cmbIntervalo.Items.AddRange(["Desactivado", "30 minutos", "1 hora", "2 horas"]);

            _chkLogArchivo = new CheckBox
            {
                Text = "Guardar log de actualizaciones en archivo",
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8)
            };

            _lblLogPath = new Label
            {
                Text = "Ruta del log:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 10, 10)
            };

            _txtLogPath = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Margin = new Padding(0, 0, 0, 10)
            };

            _chkModoAdministrador = new CheckBox
            {
                Text = "Instalar y actualizar usando permisos de administrador",
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8)
            };

            _lblAdminInfo = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Text = "Windows mostrara UAC al actualizar y el progreso detallado se sustituira por un indicador general.",
                Margin = new Padding(0)
            };

            generalLayout.Controls.Add(_lblIntervalo, 0, 0);
            generalLayout.Controls.Add(_cmbIntervalo, 1, 0);
            generalLayout.Controls.Add(_chkLogArchivo, 0, 1);
            generalLayout.SetColumnSpan(_chkLogArchivo, 2);
            generalLayout.Controls.Add(_lblLogPath, 0, 2);
            generalLayout.Controls.Add(_txtLogPath, 1, 2);
            generalLayout.Controls.Add(_chkModoAdministrador, 0, 3);
            generalLayout.SetColumnSpan(_chkModoAdministrador, 2);
            generalLayout.Controls.Add(_lblAdminInfo, 0, 4);
            generalLayout.SetColumnSpan(_lblAdminInfo, 2);
            _gbGeneral.Controls.Add(generalLayout);

            _gbExcluidos = new GroupBox
            {
                Text = "Exclusiones de actualizacion automatica",
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 10, 14, 14),
                Margin = new Padding(0, 0, 0, 12)
            };

            var exclusionsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            exclusionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            exclusionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));

            _lstExcluidos = new ListBox
            {
                Dock = DockStyle.Fill,
                IntegralHeight = false,
                HorizontalScrollbar = true,
                Margin = new Padding(0, 0, 12, 0)
            };

            var actionsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            actionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            _btnQuitar = new Button
            {
                Text = "Quitar seleccionado",
                Dock = DockStyle.Top,
                Height = 38,
                Margin = new Padding(0, 0, 0, 10)
            };
            _btnQuitar.Click += (_, _) =>
            {
                if (_lstExcluidos.SelectedItem is string id)
                    _lstExcluidos.Items.Remove(id);
            };

            _btnLimpiar = new Button
            {
                Text = "Limpiar todo",
                Dock = DockStyle.Top,
                Height = 38,
                Margin = new Padding(0)
            };
            _btnLimpiar.Click += (_, _) => _lstExcluidos.Items.Clear();

            actionsLayout.Controls.Add(_btnQuitar, 0, 0);
            actionsLayout.Controls.Add(_btnLimpiar, 0, 1);
            exclusionsLayout.Controls.Add(_lstExcluidos, 0, 0);
            exclusionsLayout.Controls.Add(actionsLayout, 1, 0);
            _gbExcluidos.Controls.Add(exclusionsLayout);

            var buttonsBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };

            _btnCancelar = new Button
            {
                Text = "Cancelar",
                DialogResult = DialogResult.Cancel,
                Size = new Size(118, 40),
                Margin = new Padding(10, 0, 0, 0)
            };

            _btnOk = new Button
            {
                Text = "Guardar cambios",
                DialogResult = DialogResult.OK,
                Size = new Size(140, 40),
                Margin = new Padding(10, 0, 0, 0)
            };
            _btnOk.Click += BtnOk_Click;

            buttonsBar.Controls.Add(_btnCancelar);
            buttonsBar.Controls.Add(_btnOk);

            _layoutRoot.Controls.Add(_gbGeneral, 0, 0);
            _layoutRoot.Controls.Add(_gbExcluidos, 0, 1);
            _layoutRoot.Controls.Add(buttonsBar, 0, 2);
            Controls.Add(_layoutRoot);

            AcceptButton = _btnOk;
            CancelButton = _btnCancelar;
        }

        private void LoadFromSettings()
        {
            _cmbIntervalo.SelectedIndex = _settings.AutoCheckIntervalMinutes switch
            {
                30 => 1,
                60 => 2,
                120 => 3,
                _ => 0
            };
            _chkLogArchivo.Checked = _settings.LogToFile;
            _chkModoAdministrador.Checked = _settings.RunUpdatesAsAdministrator;
            _txtLogPath.Text = AppSettings.LogDirectory;

            foreach (var id in _settings.ExcludedIds)
                _lstExcluidos.Items.Add(id);
        }

        private void ApplyTheme(bool dark)
        {
            UiPalette palette = UiTheme.GetPalette(dark);

            UiTheme.ApplyForm(this, palette);
            _layoutRoot.BackColor = palette.WindowBackground;
            UiTheme.StyleGroupBox(_gbGeneral, palette);
            UiTheme.StyleGroupBox(_gbExcluidos, palette);
            UiTheme.StyleLabel(_lblIntervalo, palette);
            UiTheme.StyleLabel(_lblLogPath, palette);
            UiTheme.StyleLabel(_lblAdminInfo, palette, muted: true);
            UiTheme.StyleComboBox(_cmbIntervalo, palette);
            UiTheme.StyleCheckBox(_chkLogArchivo, palette);
            UiTheme.StyleCheckBox(_chkModoAdministrador, palette);
            UiTheme.StyleTextBox(_txtLogPath, palette);
            UiTheme.StyleListBox(_lstExcluidos, palette);
            UiTheme.StyleButton(_btnQuitar, palette, AppButtonKind.Secondary);
            UiTheme.StyleButton(_btnLimpiar, palette, AppButtonKind.Danger);
            UiTheme.StyleButton(_btnOk, palette, AppButtonKind.Primary);
            UiTheme.StyleButton(_btnCancelar, palette, AppButtonKind.Secondary);
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            _settings.AutoCheckIntervalMinutes = _cmbIntervalo.SelectedIndex switch
            {
                1 => 30,
                2 => 60,
                3 => 120,
                _ => 0
            };
            _settings.LogToFile = _chkLogArchivo.Checked;
            _settings.RunUpdatesAsAdministrator = _chkModoAdministrador.Checked;
            _settings.ExcludedIds = [.. _lstExcluidos.Items.Cast<string>()];
            _settings.Save();
        }
    }
}