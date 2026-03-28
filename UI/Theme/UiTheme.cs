namespace WingetUSoft;

internal enum AppButtonKind
{
    Primary,
    Secondary,
    Danger
}

internal sealed class UiPalette
{
    public required bool IsDark { get; init; }
    public required Color WindowBackground { get; init; }
    public required Color Surface { get; init; }
    public required Color SurfaceRaised { get; init; }
    public required Color SurfaceInset { get; init; }
    public required Color Border { get; init; }
    public required Color Text { get; init; }
    public required Color MutedText { get; init; }
    public required Color Accent { get; init; }
    public required Color AccentText { get; init; }
    public required Color Selection { get; init; }
    public required Color Danger { get; init; }
    public required Color Warning { get; init; }
    public required Color Success { get; init; }
    public required Color InputBackground { get; init; }
    public required Color LogBackground { get; init; }
    public required Color LogText { get; init; }
    public required Color DisabledBackground { get; init; }
    public required Color DisabledText { get; init; }
    public required Color ExcludedRowBackground { get; init; }
    public required Color ExcludedRowText { get; init; }
    public required Color WarningText { get; init; }
}

internal static class UiTheme
{
    private static readonly UiPalette LightPalette = new()
    {
        IsDark = false,
        WindowBackground = Color.FromArgb(244, 240, 232),
        Surface = Color.FromArgb(255, 255, 255),
        SurfaceRaised = Color.FromArgb(250, 247, 241),
        SurfaceInset = Color.FromArgb(238, 233, 224),
        Border = Color.FromArgb(215, 206, 192),
        Text = Color.FromArgb(34, 39, 43),
        MutedText = Color.FromArgb(97, 105, 112),
        Accent = Color.FromArgb(18, 109, 111),
        AccentText = Color.White,
        Selection = Color.FromArgb(24, 125, 127),
        Danger = Color.FromArgb(186, 70, 54),
        Warning = Color.FromArgb(177, 118, 38),
        Success = Color.FromArgb(56, 122, 77),
        InputBackground = Color.FromArgb(255, 255, 255),
        LogBackground = Color.FromArgb(28, 36, 40),
        LogText = Color.FromArgb(224, 230, 233),
        DisabledBackground = Color.FromArgb(232, 228, 220),
        DisabledText = Color.FromArgb(141, 145, 149),
        ExcludedRowBackground = Color.FromArgb(244, 241, 235),
        ExcludedRowText = Color.FromArgb(135, 133, 126),
        WarningText = Color.FromArgb(154, 92, 18)
    };

    private static readonly UiPalette DarkPalette = new()
    {
        IsDark = true,
        WindowBackground = Color.FromArgb(24, 30, 32),
        Surface = Color.FromArgb(31, 38, 40),
        SurfaceRaised = Color.FromArgb(41, 49, 52),
        SurfaceInset = Color.FromArgb(19, 24, 26),
        Border = Color.FromArgb(66, 78, 82),
        Text = Color.FromArgb(230, 235, 237),
        MutedText = Color.FromArgb(164, 171, 176),
        Accent = Color.FromArgb(73, 177, 168),
        AccentText = Color.FromArgb(16, 25, 26),
        Selection = Color.FromArgb(57, 152, 144),
        Danger = Color.FromArgb(213, 92, 76),
        Warning = Color.FromArgb(227, 176, 87),
        Success = Color.FromArgb(91, 181, 120),
        InputBackground = Color.FromArgb(19, 24, 26),
        LogBackground = Color.FromArgb(14, 18, 20),
        LogText = Color.FromArgb(214, 221, 224),
        DisabledBackground = Color.FromArgb(44, 50, 53),
        DisabledText = Color.FromArgb(126, 134, 139),
        ExcludedRowBackground = Color.FromArgb(39, 44, 47),
        ExcludedRowText = Color.FromArgb(133, 143, 148),
        WarningText = Color.FromArgb(230, 182, 100)
    };

    public static UiPalette GetPalette(bool darkMode) => darkMode ? DarkPalette : LightPalette;

    public static void ApplyForm(Form form, UiPalette palette)
    {
        form.BackColor = palette.WindowBackground;
        form.ForeColor = palette.Text;
    }

    public static void StylePanel(Control control, UiPalette palette, bool raised = false)
    {
        control.BackColor = raised ? palette.SurfaceRaised : palette.Surface;
        control.ForeColor = palette.Text;
    }

    public static void StyleGroupBox(GroupBox groupBox, UiPalette palette)
    {
        groupBox.BackColor = palette.Surface;
        groupBox.ForeColor = palette.Text;
    }

    public static void StyleLabel(Label label, UiPalette palette, bool muted = false)
    {
        label.BackColor = Color.Transparent;
        label.ForeColor = muted ? palette.MutedText : palette.Text;
    }

    public static void StyleButton(Button button, UiPalette palette, AppButtonKind kind)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseDownBackColor = palette.SurfaceInset;
        button.FlatAppearance.MouseOverBackColor = palette.SurfaceRaised;

        switch (kind)
        {
            case AppButtonKind.Primary:
                button.BackColor = button.Enabled ? palette.Accent : palette.DisabledBackground;
                button.ForeColor = button.Enabled ? palette.AccentText : palette.DisabledText;
                button.FlatAppearance.BorderColor = button.Enabled ? palette.Accent : palette.DisabledBackground;
                break;

            case AppButtonKind.Danger:
                button.BackColor = button.Enabled ? palette.Danger : palette.DisabledBackground;
                button.ForeColor = button.Enabled ? Color.White : palette.DisabledText;
                button.FlatAppearance.BorderColor = button.Enabled ? palette.Danger : palette.DisabledBackground;
                break;

            default:
                button.BackColor = button.Enabled ? palette.SurfaceRaised : palette.DisabledBackground;
                button.ForeColor = button.Enabled ? palette.Text : palette.DisabledText;
                button.FlatAppearance.BorderColor = button.Enabled ? palette.Border : palette.DisabledBackground;
                break;
        }
    }

    public static void StyleCheckBox(CheckBox checkBox, UiPalette palette)
    {
        checkBox.BackColor = Color.Transparent;
        checkBox.ForeColor = palette.Text;
    }

    public static void StyleComboBox(ComboBox comboBox, UiPalette palette)
    {
        comboBox.BackColor = comboBox.Enabled ? palette.InputBackground : palette.DisabledBackground;
        comboBox.ForeColor = comboBox.Enabled ? palette.Text : palette.DisabledText;
        comboBox.FlatStyle = FlatStyle.Flat;
    }

    public static void StyleListBox(ListBox listBox, UiPalette palette)
    {
        listBox.BackColor = palette.InputBackground;
        listBox.ForeColor = palette.Text;
        listBox.BorderStyle = BorderStyle.FixedSingle;
    }

    public static void StyleTextBox(TextBox textBox, UiPalette palette)
    {
        textBox.BackColor = textBox.ReadOnly ? palette.SurfaceInset : palette.InputBackground;
        textBox.ForeColor = palette.Text;
        textBox.BorderStyle = BorderStyle.FixedSingle;
    }

    public static void StyleDataGridView(DataGridView grid, UiPalette palette)
    {
        grid.BackgroundColor = palette.SurfaceInset;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = palette.Border;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = palette.SurfaceRaised,
            ForeColor = palette.Text,
            SelectionBackColor = palette.SurfaceRaised,
            SelectionForeColor = palette.Text,
            Padding = new Padding(8, 0, 8, 0)
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = palette.Surface,
            ForeColor = palette.Text,
            SelectionBackColor = palette.Selection,
            SelectionForeColor = palette.AccentText,
            Padding = new Padding(6, 4, 6, 4)
        };
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = palette.SurfaceRaised,
            ForeColor = palette.Text,
            SelectionBackColor = palette.Selection,
            SelectionForeColor = palette.AccentText
        };
    }

    public static void StyleRichTextBox(RichTextBox richTextBox, UiPalette palette)
    {
        richTextBox.BackColor = palette.LogBackground;
        richTextBox.ForeColor = palette.LogText;
        richTextBox.BorderStyle = BorderStyle.None;
    }

    public static void StyleToolStrip(ToolStrip toolStrip, UiPalette palette)
    {
        toolStrip.Renderer = new AppToolStripRenderer(palette);
        toolStrip.BackColor = palette.Surface;
        toolStrip.ForeColor = palette.Text;

        foreach (ToolStripItem item in toolStrip.Items)
            StyleToolStripItem(item, palette);
    }

    private static void StyleToolStripItem(ToolStripItem item, UiPalette palette)
    {
        item.BackColor = palette.Surface;
        item.ForeColor = palette.Text;

        if (item is ToolStripDropDownItem dropDownItem)
        {
            dropDownItem.DropDown.BackColor = palette.SurfaceRaised;
            dropDownItem.DropDown.ForeColor = palette.Text;

            foreach (ToolStripItem child in dropDownItem.DropDownItems)
                StyleToolStripItem(child, palette);
        }
    }
}

internal sealed class AppToolStripRenderer(UiPalette palette) : ToolStripProfessionalRenderer(new AppToolStripColorTable(palette))
{
    protected override void Initialize(ToolStrip toolStrip)
    {
        base.Initialize(toolStrip);
        RoundedEdges = false;
    }
}

internal sealed class AppToolStripColorTable(UiPalette palette) : ProfessionalColorTable
{
    public override Color MenuStripGradientBegin => palette.Surface;
    public override Color MenuStripGradientEnd => palette.Surface;
    public override Color ToolStripDropDownBackground => palette.SurfaceRaised;
    public override Color ImageMarginGradientBegin => palette.SurfaceRaised;
    public override Color ImageMarginGradientMiddle => palette.SurfaceRaised;
    public override Color ImageMarginGradientEnd => palette.SurfaceRaised;
    public override Color MenuItemBorder => palette.Border;
    public override Color MenuItemSelected => palette.SurfaceInset;
    public override Color MenuItemSelectedGradientBegin => palette.SurfaceInset;
    public override Color MenuItemSelectedGradientEnd => palette.SurfaceInset;
    public override Color MenuItemPressedGradientBegin => palette.SurfaceInset;
    public override Color MenuItemPressedGradientEnd => palette.SurfaceInset;
    public override Color ButtonSelectedBorder => palette.Border;
    public override Color ButtonSelectedHighlight => palette.SurfaceInset;
    public override Color ButtonSelectedHighlightBorder => palette.Border;
    public override Color ButtonPressedBorder => palette.Border;
    public override Color ButtonPressedGradientBegin => palette.SurfaceInset;
    public override Color ButtonPressedGradientEnd => palette.SurfaceInset;
    public override Color CheckBackground => palette.Accent;
    public override Color CheckPressedBackground => palette.Accent;
    public override Color CheckSelectedBackground => palette.Accent;
    public override Color SeparatorDark => palette.Border;
    public override Color SeparatorLight => palette.Border;
}