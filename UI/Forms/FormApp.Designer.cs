namespace WingetUSoft
{
    partial class FormApp
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            menuStrip1 = new MenuStrip();
            opcionesToolStripMenuItem = new ToolStripMenuItem();
            modoActualizacionToolStripMenuItem = new ToolStripMenuItem();
            silenciosaToolStripMenuItem = new ToolStripMenuItem();
            interactivaToolStripMenuItem = new ToolStripMenuItem();
            sepOpciones = new ToolStripSeparator();
            historialToolStripMenuItem = new ToolStripMenuItem();
            modoOscuroToolStripMenuItem = new ToolStripMenuItem();
            exportarListaToolStripMenuItem = new ToolStripMenuItem();
            configuracionToolStripMenuItem = new ToolStripMenuItem();
            sepOpciones2 = new ToolStripSeparator();
            ctxMenuFila = new ContextMenuStrip(components);
            ctxItemActualizar = new ToolStripMenuItem();
            ctxSep1 = new ToolStripSeparator();
            ctxItemCopiarNombre = new ToolStripMenuItem();
            ctxItemCopiarId = new ToolStripMenuItem();
            ctxSep2 = new ToolStripSeparator();
            ctxItemBuscarWeb = new ToolStripMenuItem();
            ctxSep3 = new ToolStripSeparator();
            ctxItemExcluir = new ToolStripMenuItem();
            layoutRoot = new TableLayoutPanel();
            pnlHeader = new Panel();
            layoutHeader = new TableLayoutPanel();
            lblTitulo = new Label();
            lblSubtitulo = new Label();
            lblDetalleEstado = new Label();
            lblAtajos = new Label();
            gbAcciones = new GroupBox();
            pnlAcciones = new FlowLayoutPanel();
            btnConsultarActualizaciones = new Button();
            btnConsultarActDesconocidas = new Button();
            btnActualizarSeleccionados = new Button();
            btnActualizarTodosPro = new Button();
            btnCancelar = new Button();
            splitMain = new SplitContainer();
            gbActulizacionesDisp = new GroupBox();
            dgvListaProgramas = new DataGridView();
            gbLog = new GroupBox();
            rtbLog = new RichTextBox();
            pnlStatus = new Panel();
            layoutStatus = new TableLayoutPanel();
            lblEstado = new Label();
            progressBar1 = new ProgressBar();
            lblDescarga = new Label();
            pbDescarga = new ProgressBar();
            menuStrip1.SuspendLayout();
            ctxMenuFila.SuspendLayout();
            layoutRoot.SuspendLayout();
            pnlHeader.SuspendLayout();
            layoutHeader.SuspendLayout();
            gbAcciones.SuspendLayout();
            pnlAcciones.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            gbActulizacionesDisp.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvListaProgramas).BeginInit();
            gbLog.SuspendLayout();
            pnlStatus.SuspendLayout();
            layoutStatus.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { opcionesToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new Padding(8, 3, 0, 3);
            menuStrip1.Size = new Size(1224, 30);
            menuStrip1.TabIndex = 0;
            // 
            // opcionesToolStripMenuItem
            // 
            opcionesToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { modoActualizacionToolStripMenuItem, modoOscuroToolStripMenuItem, sepOpciones, exportarListaToolStripMenuItem, configuracionToolStripMenuItem, sepOpciones2, historialToolStripMenuItem });
            opcionesToolStripMenuItem.Name = "opcionesToolStripMenuItem";
            opcionesToolStripMenuItem.Size = new Size(85, 24);
            opcionesToolStripMenuItem.Text = "&Opciones";
            // 
            // modoActualizacionToolStripMenuItem
            // 
            modoActualizacionToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { silenciosaToolStripMenuItem, interactivaToolStripMenuItem });
            modoActualizacionToolStripMenuItem.Name = "modoActualizacionToolStripMenuItem";
            modoActualizacionToolStripMenuItem.Size = new Size(246, 26);
            modoActualizacionToolStripMenuItem.Text = "Modo de a&ctualización";
            // 
            // silenciosaToolStripMenuItem
            // 
            silenciosaToolStripMenuItem.Checked = true;
            silenciosaToolStripMenuItem.CheckState = CheckState.Checked;
            silenciosaToolStripMenuItem.Name = "silenciosaToolStripMenuItem";
            silenciosaToolStripMenuItem.Size = new Size(163, 26);
            silenciosaToolStripMenuItem.Text = "&Silenciosa";
            // 
            // interactivaToolStripMenuItem
            // 
            interactivaToolStripMenuItem.Name = "interactivaToolStripMenuItem";
            interactivaToolStripMenuItem.Size = new Size(163, 26);
            interactivaToolStripMenuItem.Text = "&Interactiva";
            // 
            // sepOpciones
            // 
            sepOpciones.Name = "sepOpciones";
            sepOpciones.Size = new Size(243, 6);
            // 
            // historialToolStripMenuItem
            // 
            historialToolStripMenuItem.Name = "historialToolStripMenuItem";
            historialToolStripMenuItem.Size = new Size(246, 26);
            historialToolStripMenuItem.Text = "Ver &historial";
            // 
            // modoOscuroToolStripMenuItem
            // 
            modoOscuroToolStripMenuItem.CheckOnClick = true;
            modoOscuroToolStripMenuItem.Name = "modoOscuroToolStripMenuItem";
            modoOscuroToolStripMenuItem.Size = new Size(246, 26);
            modoOscuroToolStripMenuItem.Text = "Modo &oscuro";
            // 
            // exportarListaToolStripMenuItem
            // 
            exportarListaToolStripMenuItem.Name = "exportarListaToolStripMenuItem";
            exportarListaToolStripMenuItem.Size = new Size(246, 26);
            exportarListaToolStripMenuItem.Text = "&Exportar lista...";
            // 
            // configuracionToolStripMenuItem
            // 
            configuracionToolStripMenuItem.Name = "configuracionToolStripMenuItem";
            configuracionToolStripMenuItem.Size = new Size(246, 26);
            configuracionToolStripMenuItem.Text = "&Configuración...";
            // 
            // sepOpciones2
            // 
            sepOpciones2.Name = "sepOpciones2";
            sepOpciones2.Size = new Size(243, 6);
            // 
            // ctxMenuFila
            // 
            ctxMenuFila.ImageScalingSize = new Size(20, 20);
            ctxMenuFila.Items.AddRange(new ToolStripItem[] { ctxItemActualizar, ctxSep1, ctxItemCopiarNombre, ctxItemCopiarId, ctxSep2, ctxItemBuscarWeb, ctxSep3, ctxItemExcluir });
            ctxMenuFila.Name = "ctxMenuFila";
            ctxMenuFila.Size = new Size(265, 154);
            // 
            // ctxItemActualizar
            // 
            ctxItemActualizar.Name = "ctxItemActualizar";
            ctxItemActualizar.Size = new Size(264, 24);
            ctxItemActualizar.Text = "&Actualizar este programa";
            // 
            // ctxSep1
            // 
            ctxSep1.Name = "ctxSep1";
            ctxSep1.Size = new Size(261, 6);
            // 
            // ctxItemCopiarNombre
            // 
            ctxItemCopiarNombre.Name = "ctxItemCopiarNombre";
            ctxItemCopiarNombre.Size = new Size(264, 24);
            ctxItemCopiarNombre.Text = "Copiar &nombre";
            // 
            // ctxItemCopiarId
            // 
            ctxItemCopiarId.Name = "ctxItemCopiarId";
            ctxItemCopiarId.Size = new Size(264, 24);
            ctxItemCopiarId.Text = "Copiar &Id";
            // 
            // ctxSep2
            // 
            ctxSep2.Name = "ctxSep2";
            ctxSep2.Size = new Size(261, 6);
            // 
            // ctxItemBuscarWeb
            // 
            ctxItemBuscarWeb.Name = "ctxItemBuscarWeb";
            ctxItemBuscarWeb.Size = new Size(264, 24);
            ctxItemBuscarWeb.Text = "Abrir búsqueda &web";
            // 
            // ctxSep3
            // 
            ctxSep3.Name = "ctxSep3";
            ctxSep3.Size = new Size(261, 6);
            // 
            // ctxItemExcluir
            // 
            ctxItemExcluir.Name = "ctxItemExcluir";
            ctxItemExcluir.Size = new Size(264, 24);
            ctxItemExcluir.Text = "&Excluir de actualizaciones";
            // 
            // layoutRoot
            // 
            layoutRoot.ColumnCount = 1;
            layoutRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutRoot.Controls.Add(pnlHeader, 0, 0);
            layoutRoot.Controls.Add(gbAcciones, 0, 1);
            layoutRoot.Controls.Add(splitMain, 0, 2);
            layoutRoot.Controls.Add(pnlStatus, 0, 3);
            layoutRoot.Dock = DockStyle.Fill;
            layoutRoot.Location = new Point(0, 30);
            layoutRoot.Name = "layoutRoot";
            layoutRoot.Padding = new Padding(16, 14, 16, 16);
            layoutRoot.RowCount = 4;
            layoutRoot.RowStyles.Add(new RowStyle());
            layoutRoot.RowStyles.Add(new RowStyle());
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layoutRoot.RowStyles.Add(new RowStyle());
            layoutRoot.Size = new Size(1224, 772);
            layoutRoot.TabIndex = 1;
            // 
            // pnlHeader
            // 
            pnlHeader.Controls.Add(layoutHeader);
            pnlHeader.Dock = DockStyle.Fill;
            pnlHeader.Margin = new Padding(0, 0, 0, 12);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Padding = new Padding(18, 16, 18, 14);
            pnlHeader.Size = new Size(1192, 128);
            pnlHeader.TabIndex = 0;
            // 
            // layoutHeader
            // 
            layoutHeader.ColumnCount = 1;
            layoutHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutHeader.Controls.Add(lblTitulo, 0, 0);
            layoutHeader.Controls.Add(lblSubtitulo, 0, 1);
            layoutHeader.Controls.Add(lblDetalleEstado, 0, 2);
            layoutHeader.Controls.Add(lblAtajos, 0, 3);
            layoutHeader.Dock = DockStyle.Fill;
            layoutHeader.Location = new Point(18, 16);
            layoutHeader.Name = "layoutHeader";
            layoutHeader.RowCount = 4;
            layoutHeader.RowStyles.Add(new RowStyle());
            layoutHeader.RowStyles.Add(new RowStyle());
            layoutHeader.RowStyles.Add(new RowStyle());
            layoutHeader.RowStyles.Add(new RowStyle());
            layoutHeader.Size = new Size(1156, 98);
            layoutHeader.TabIndex = 0;
            // 
            // lblTitulo
            // 
            lblTitulo.AutoSize = true;
            lblTitulo.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitulo.Location = new Point(0, 0);
            lblTitulo.Margin = new Padding(0, 0, 0, 4);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new Size(339, 35);
            lblTitulo.TabIndex = 0;
            lblTitulo.Text = "Actualiza tus programas con winget";
            // 
            // lblSubtitulo
            // 
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.Location = new Point(0, 39);
            lblSubtitulo.Margin = new Padding(0, 0, 0, 10);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.Size = new Size(857, 20);
            lblSubtitulo.TabIndex = 1;
            lblSubtitulo.Text = "Consulta, filtra y actualiza paquetes desde una sola vista. Las acciones se adaptan al modo oscuro, al tamaño de ventana y al estado de la operación.";
            // 
            // lblDetalleEstado
            // 
            lblDetalleEstado.AutoEllipsis = true;
            lblDetalleEstado.Dock = DockStyle.Fill;
            lblDetalleEstado.Location = new Point(0, 69);
            lblDetalleEstado.Margin = new Padding(0, 0, 0, 6);
            lblDetalleEstado.Name = "lblDetalleEstado";
            lblDetalleEstado.Size = new Size(1156, 20);
            lblDetalleEstado.TabIndex = 2;
            lblDetalleEstado.Text = "Selecciona un programa para ver sus detalles antes de actualizar.";
            // 
            // lblAtajos
            // 
            lblAtajos.AutoSize = true;
            lblAtajos.Location = new Point(0, 95);
            lblAtajos.Margin = new Padding(0);
            lblAtajos.Name = "lblAtajos";
            lblAtajos.Size = new Size(402, 20);
            lblAtajos.TabIndex = 3;
            lblAtajos.Text = "Atajos: F5 consultar, Ctrl+A marcar, Supr excluir, Esc cancelar.";
            // 
            // gbAcciones
            // 
            gbAcciones.Controls.Add(pnlAcciones);
            gbAcciones.Dock = DockStyle.Fill;
            gbAcciones.Location = new Point(16, 154);
            gbAcciones.Margin = new Padding(0, 0, 0, 12);
            gbAcciones.Name = "gbAcciones";
            gbAcciones.Padding = new Padding(14, 10, 14, 14);
            gbAcciones.Size = new Size(1192, 122);
            gbAcciones.TabIndex = 1;
            gbAcciones.TabStop = false;
            gbAcciones.Text = "Acciones rápidas";
            // 
            // pnlAcciones
            // 
            pnlAcciones.Controls.Add(btnConsultarActualizaciones);
            pnlAcciones.Controls.Add(btnConsultarActDesconocidas);
            pnlAcciones.Controls.Add(btnActualizarSeleccionados);
            pnlAcciones.Controls.Add(btnActualizarTodosPro);
            pnlAcciones.Controls.Add(btnCancelar);
            pnlAcciones.Dock = DockStyle.Fill;
            pnlAcciones.Location = new Point(14, 30);
            pnlAcciones.Name = "pnlAcciones";
            pnlAcciones.Padding = new Padding(0, 4, 0, 0);
            pnlAcciones.Size = new Size(1164, 78);
            pnlAcciones.TabIndex = 0;
            pnlAcciones.WrapContents = true;
            // 
            // btnConsultarActualizaciones
            // 
            btnConsultarActualizaciones.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnConsultarActualizaciones.Margin = new Padding(0, 0, 10, 10);
            btnConsultarActualizaciones.Name = "btnConsultarActualizaciones";
            btnConsultarActualizaciones.Size = new Size(220, 48);
            btnConsultarActualizaciones.TabIndex = 0;
            btnConsultarActualizaciones.Text = "&Consultar actualizaciones";
            btnConsultarActualizaciones.UseVisualStyleBackColor = true;
            // 
            // btnConsultarActDesconocidas
            // 
            btnConsultarActDesconocidas.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnConsultarActDesconocidas.Margin = new Padding(0, 0, 10, 10);
            btnConsultarActDesconocidas.Name = "btnConsultarActDesconocidas";
            btnConsultarActDesconocidas.Size = new Size(220, 48);
            btnConsultarActDesconocidas.TabIndex = 1;
            btnConsultarActDesconocidas.Text = "Consultar con &desconocidas";
            btnConsultarActDesconocidas.UseVisualStyleBackColor = true;
            // 
            // btnActualizarSeleccionados
            // 
            btnActualizarSeleccionados.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnActualizarSeleccionados.Margin = new Padding(0, 0, 10, 10);
            btnActualizarSeleccionados.Name = "btnActualizarSeleccionados";
            btnActualizarSeleccionados.Size = new Size(220, 48);
            btnActualizarSeleccionados.TabIndex = 2;
            btnActualizarSeleccionados.Text = "Actualizar &seleccionados";
            btnActualizarSeleccionados.UseVisualStyleBackColor = true;
            // 
            // btnActualizarTodosPro
            // 
            btnActualizarTodosPro.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnActualizarTodosPro.Margin = new Padding(0, 0, 10, 10);
            btnActualizarTodosPro.Name = "btnActualizarTodosPro";
            btnActualizarTodosPro.Size = new Size(220, 48);
            btnActualizarTodosPro.TabIndex = 3;
            btnActualizarTodosPro.Text = "Actualizar &todo";
            btnActualizarTodosPro.UseVisualStyleBackColor = true;
            // 
            // btnCancelar
            // 
            btnCancelar.Enabled = false;
            btnCancelar.Margin = new Padding(0, 0, 10, 10);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(160, 48);
            btnCancelar.TabIndex = 4;
            btnCancelar.Text = "&Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            // 
            // splitMain
            // 
            splitMain.Dock = DockStyle.Fill;
            splitMain.FixedPanel = FixedPanel.Panel2;
            splitMain.Location = new Point(16, 288);
            splitMain.Margin = new Padding(0, 0, 0, 12);
            splitMain.Name = "splitMain";
            splitMain.Orientation = Orientation.Horizontal;
            // 
            // splitMain.Panel1
            // 
            splitMain.Panel1.Controls.Add(gbActulizacionesDisp);
            // 
            // splitMain.Panel2
            // 
            splitMain.Panel2.Controls.Add(gbLog);
            splitMain.Panel1MinSize = 260;
            splitMain.Panel2MinSize = 140;
            splitMain.Size = new Size(1192, 408);
            splitMain.SplitterDistance = 254;
            splitMain.TabIndex = 2;
            // 
            // gbActulizacionesDisp
            // 
            gbActulizacionesDisp.Controls.Add(dgvListaProgramas);
            gbActulizacionesDisp.Dock = DockStyle.Fill;
            gbActulizacionesDisp.Location = new Point(0, 0);
            gbActulizacionesDisp.Name = "gbActulizacionesDisp";
            gbActulizacionesDisp.Padding = new Padding(12, 10, 12, 12);
            gbActulizacionesDisp.Size = new Size(1192, 254);
            gbActulizacionesDisp.TabIndex = 0;
            gbActulizacionesDisp.TabStop = false;
            gbActulizacionesDisp.Text = "Actualizaciones disponibles";
            // 
            // dgvListaProgramas
            // 
            dgvListaProgramas.AllowUserToAddRows = false;
            dgvListaProgramas.AllowUserToDeleteRows = false;
            dgvListaProgramas.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvListaProgramas.ContextMenuStrip = ctxMenuFila;
            dgvListaProgramas.Dock = DockStyle.Fill;
            dgvListaProgramas.Name = "dgvListaProgramas";
            dgvListaProgramas.RowHeadersVisible = false;
            dgvListaProgramas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvListaProgramas.TabIndex = 0;
            // 
            // gbLog
            // 
            gbLog.Controls.Add(rtbLog);
            gbLog.Dock = DockStyle.Fill;
            gbLog.Location = new Point(0, 0);
            gbLog.Name = "gbLog";
            gbLog.Padding = new Padding(10, 10, 10, 10);
            gbLog.Size = new Size(1192, 150);
            gbLog.TabIndex = 0;
            gbLog.TabStop = false;
            gbLog.Text = "Actividad y resultados";
            // 
            // rtbLog
            // 
            rtbLog.BorderStyle = BorderStyle.None;
            rtbLog.Dock = DockStyle.Fill;
            rtbLog.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            rtbLog.Name = "rtbLog";
            rtbLog.ReadOnly = true;
            rtbLog.ScrollBars = RichTextBoxScrollBars.Vertical;
            rtbLog.TabIndex = 0;
            rtbLog.WordWrap = false;
            // 
            // pnlStatus
            // 
            pnlStatus.Controls.Add(layoutStatus);
            pnlStatus.Dock = DockStyle.Fill;
            pnlStatus.Margin = new Padding(0);
            pnlStatus.Name = "pnlStatus";
            pnlStatus.Padding = new Padding(18, 14, 18, 16);
            pnlStatus.Size = new Size(1192, 122);
            pnlStatus.TabIndex = 3;
            // 
            // layoutStatus
            // 
            layoutStatus.ColumnCount = 1;
            layoutStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutStatus.Controls.Add(lblEstado, 0, 0);
            layoutStatus.Controls.Add(progressBar1, 0, 1);
            layoutStatus.Controls.Add(lblDescarga, 0, 2);
            layoutStatus.Controls.Add(pbDescarga, 0, 3);
            layoutStatus.Dock = DockStyle.Fill;
            layoutStatus.Location = new Point(18, 14);
            layoutStatus.Name = "layoutStatus";
            layoutStatus.RowCount = 4;
            layoutStatus.RowStyles.Add(new RowStyle());
            layoutStatus.RowStyles.Add(new RowStyle());
            layoutStatus.RowStyles.Add(new RowStyle());
            layoutStatus.RowStyles.Add(new RowStyle());
            layoutStatus.Size = new Size(1156, 92);
            layoutStatus.TabIndex = 0;
            // 
            // lblEstado
            // 
            lblEstado.AutoEllipsis = true;
            lblEstado.Dock = DockStyle.Fill;
            lblEstado.Location = new Point(0, 0);
            lblEstado.Margin = new Padding(0, 0, 0, 8);
            lblEstado.Name = "lblEstado";
            lblEstado.Size = new Size(1156, 20);
            lblEstado.TabIndex = 0;
            lblEstado.Text = "Listo.";
            // 
            // progressBar1
            // 
            progressBar1.Dock = DockStyle.Fill;
            progressBar1.Location = new Point(0, 28);
            progressBar1.Margin = new Padding(0, 0, 0, 8);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(1156, 14);
            progressBar1.TabIndex = 1;
            // 
            // lblDescarga
            // 
            lblDescarga.AutoEllipsis = true;
            lblDescarga.Dock = DockStyle.Fill;
            lblDescarga.Location = new Point(0, 50);
            lblDescarga.Margin = new Padding(0, 0, 0, 8);
            lblDescarga.Name = "lblDescarga";
            lblDescarga.Size = new Size(1156, 20);
            lblDescarga.TabIndex = 2;
            lblDescarga.Visible = false;
            // 
            // pbDescarga
            // 
            pbDescarga.Dock = DockStyle.Fill;
            pbDescarga.Location = new Point(0, 78);
            pbDescarga.Margin = new Padding(0);
            pbDescarga.Maximum = 100;
            pbDescarga.Name = "pbDescarga";
            pbDescarga.Size = new Size(1156, 14);
            pbDescarga.TabIndex = 3;
            pbDescarga.Visible = false;
            // 
            // FormApp
            // 
            AcceptButton = btnConsultarActualizaciones;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1224, 802);
            Controls.Add(layoutRoot);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            MinimumSize = new Size(980, 720);
            Name = "FormApp";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "WingetUSoft - Actualiza tus programas";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ctxMenuFila.ResumeLayout(false);
            layoutRoot.ResumeLayout(false);
            pnlHeader.ResumeLayout(false);
            layoutHeader.ResumeLayout(false);
            layoutHeader.PerformLayout();
            gbAcciones.ResumeLayout(false);
            pnlAcciones.ResumeLayout(false);
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            gbActulizacionesDisp.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvListaProgramas).EndInit();
            gbLog.ResumeLayout(false);
            pnlStatus.ResumeLayout(false);
            layoutStatus.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem opcionesToolStripMenuItem;
        private ToolStripMenuItem modoActualizacionToolStripMenuItem;
        private ToolStripMenuItem silenciosaToolStripMenuItem;
        private ToolStripMenuItem interactivaToolStripMenuItem;
        private ToolStripSeparator sepOpciones;
        private ToolStripMenuItem historialToolStripMenuItem;
        private ToolStripMenuItem modoOscuroToolStripMenuItem;
        private ToolStripMenuItem exportarListaToolStripMenuItem;
        private ToolStripMenuItem configuracionToolStripMenuItem;
        private ToolStripSeparator sepOpciones2;
        private ContextMenuStrip ctxMenuFila;
        private ToolStripMenuItem ctxItemActualizar;
        private ToolStripSeparator ctxSep1;
        private ToolStripMenuItem ctxItemCopiarNombre;
        private ToolStripMenuItem ctxItemCopiarId;
        private ToolStripSeparator ctxSep2;
        private ToolStripMenuItem ctxItemBuscarWeb;
        private ToolStripSeparator ctxSep3;
        private ToolStripMenuItem ctxItemExcluir;
        private TableLayoutPanel layoutRoot;
        private Panel pnlHeader;
        private TableLayoutPanel layoutHeader;
        private Label lblTitulo;
        private Label lblSubtitulo;
        private Label lblDetalleEstado;
        private Label lblAtajos;
        private GroupBox gbAcciones;
        private FlowLayoutPanel pnlAcciones;
        private Button btnConsultarActualizaciones;
        private Button btnConsultarActDesconocidas;
        private Button btnActualizarSeleccionados;
        private Button btnActualizarTodosPro;
        private Button btnCancelar;
        private SplitContainer splitMain;
        private GroupBox gbActulizacionesDisp;
        private DataGridView dgvListaProgramas;
        private GroupBox gbLog;
        private RichTextBox rtbLog;
        private Panel pnlStatus;
        private TableLayoutPanel layoutStatus;
        private Label lblEstado;
        private ProgressBar progressBar1;
        private Label lblDescarga;
        private ProgressBar pbDescarga;
    }
}