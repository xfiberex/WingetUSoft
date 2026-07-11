using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Pruebas de la matemática pura de dimensionado/centrado de ventana (<see cref="WindowSizing"/>):
/// escalado por DPI, acotado al área de trabajo y centrado.
/// </summary>
public sealed class WindowSizingTests
{
    // ── ComputeSizeAndCenter ─────────────────────────────────────

    [Fact]
    public void ComputeSizeAndCenter_Scale100_FitsWithoutClamping()
    {
        // Área de trabajo amplia (1920x1080 a 100%), diseño 1180x820 con margen 16 DIP.
        var b = WindowSizing.ComputeSizeAndCenter(
            designWidthDip: 1180, designHeightDip: 820, scale: 1.0,
            workX: 0, workY: 0, workWidth: 1920, workHeight: 1080, marginDip: 16);

        Assert.Equal(1180, b.Width);
        Assert.Equal(820, b.Height);
        Assert.Equal((1920 - 1180) / 2, b.X);
        Assert.Equal((1080 - 820) / 2, b.Y);
    }

    [Fact]
    public void ComputeSizeAndCenter_Scale150_ScalesDesignSizeByDpi()
    {
        var b = WindowSizing.ComputeSizeAndCenter(
            designWidthDip: 1180, designHeightDip: 820, scale: 1.5,
            workX: 0, workY: 0, workWidth: 2880, workHeight: 1620, marginDip: 16);

        Assert.Equal((int)Math.Round(1180 * 1.5), b.Width);
        Assert.Equal((int)Math.Round(820 * 1.5), b.Height);
    }

    [Fact]
    public void ComputeSizeAndCenter_Scale200_ScalesDesignSizeByDpi()
    {
        var b = WindowSizing.ComputeSizeAndCenter(
            designWidthDip: 900, designHeightDip: 700, scale: 2.0,
            workX: 0, workY: 0, workWidth: 3840, workHeight: 2160, marginDip: 16);

        Assert.Equal((int)Math.Round(900 * 2.0), b.Width);
        Assert.Equal((int)Math.Round(700 * 2.0), b.Height);
    }

    [Fact]
    public void ComputeSizeAndCenter_DesignExceedsWorkArea_ClampsToWorkAreaMinusMargin()
    {
        // WorkArea pequeña (800x600), diseño de 1180x820 (típico de MainWindow) no cabe.
        int margin = (int)Math.Round(16 * 1.0);
        var b = WindowSizing.ComputeSizeAndCenter(
            designWidthDip: 1180, designHeightDip: 820, scale: 1.0,
            workX: 0, workY: 0, workWidth: 800, workHeight: 600, marginDip: 16);

        Assert.Equal(800 - margin, b.Width);
        Assert.Equal(600 - margin, b.Height);
    }

    [Fact]
    public void ComputeSizeAndCenter_CentersWithinNonZeroOriginWorkArea()
    {
        // Monitor secundario con origen no-cero (p. ej. a la izquierda del principal).
        var b = WindowSizing.ComputeSizeAndCenter(
            designWidthDip: 1000, designHeightDip: 600, scale: 1.0,
            workX: -1920, workY: 40, workWidth: 1920, workHeight: 1040, marginDip: 16);

        Assert.Equal(1000, b.Width);
        Assert.Equal(600, b.Height);
        Assert.Equal(-1920 + (1920 - 1000) / 2, b.X);
        Assert.Equal(40 + (1040 - 600) / 2, b.Y);
    }

    [Fact]
    public void ComputeSizeAndCenter_MarginScalesWithDpi()
    {
        // A escala 2.0, un margen de 16 DIP debe restar 32 px físicos del ancho disponible antes
        // del clamp, no 16 px fijos.
        var b = WindowSizing.ComputeSizeAndCenter(
            designWidthDip: 2000, designHeightDip: 2000, scale: 2.0,
            workX: 0, workY: 0, workWidth: 1000, workHeight: 1000, marginDip: 16);

        int expectedMargin = (int)Math.Round(16 * 2.0);
        Assert.Equal(1000 - expectedMargin, b.Width);
        Assert.Equal(1000 - expectedMargin, b.Height);
    }

    // ── ScaleMinSize ─────────────────────────────────────────────

    [Theory]
    [InlineData(900, 600, 1.0, 900, 600)]
    [InlineData(900, 600, 1.5, 1350, 900)]
    [InlineData(720, 520, 2.0, 1440, 1040)]
    public void ScaleMinSize_FitsInWorkArea_ScalesByDpiWithoutClamping(
        int minWidthDip, int minHeightDip, double scale, int expectedWidth, int expectedHeight)
    {
        // Área de trabajo amplia (4K): el mínimo escalado cabe holgado y no se acota.
        var (w, h) = WindowSizing.ScaleMinSize(
            minWidthDip, minHeightDip, scale, workWidth: 3840, workHeight: 2160, marginDip: 16);
        Assert.Equal(expectedWidth, w);
        Assert.Equal(expectedHeight, h);
    }

    [Fact]
    public void ScaleMinSize_ExceedsSmallWorkArea_ClampsToHalfWorkArea()
    {
        // Portátil de baja resolución con DPI alto: min 900x600 a 150% = 1350x900 físico, que no
        // cabe en 1366x728. El clamp que termina dominando ya no es "workWidth/Height - margen"
        // (1342x704) sino la mitad del área de trabajo (683x364, snap a media pantalla): el snap-aware
        // clamp de ScaleMinSize es más restrictivo que el clamp por margen en cualquier resolución real.
        double scale = 1.5;
        var (w, h) = WindowSizing.ScaleMinSize(
            minWidthDip: 900, minHeightDip: 600, scale: scale, workWidth: 1366, workHeight: 728, marginDip: 16);

        Assert.Equal(1366 / 2, w);
        Assert.Equal(728 / 2, h);
    }

    [Fact]
    public void ScaleMinSize_MarginScalesWithDpi_ButHalfWorkAreaClampDominates()
    {
        // A escala 2.0 el margen de 16 DIP resta 32 px físicos antes del clamp (1000-32=968), pero el
        // clamp por mitad de área de trabajo (500x500) es más restrictivo todavía y es el que gana:
        // exactamente el comportamiento "snap-aware" que arregla el bug de #7 (Tier B).
        var (w, h) = WindowSizing.ScaleMinSize(
            minWidthDip: 2000, minHeightDip: 2000, scale: 2.0, workWidth: 1000, workHeight: 1000, marginDip: 16);

        Assert.Equal(1000 / 2, w);
        Assert.Equal(1000 / 2, h);
    }

    [Fact]
    public void ScaleMinSize_QuarterSnapCell_RelaxesHeightOnly_FullHdMonitor()
    {
        // Bug real de #7 (Tier B): 1920x1080 físico con WorkArea ~1920x1040 a 100%. La celda de
        // cuarto de pantalla mide 960x520. El mínimo de MainWindow (900x600 DIP) ya cabía en ancho
        // (900 < 960) pero NO en alto (600 > 520) -- antes del fix, esto bloqueaba el snap a cuarto.
        // Tras el fix, el alto se relaja exactamente a la mitad del work area (520) y el ancho se
        // conserva (900, sigue siendo más chico que la mitad de 1920).
        var (w, h) = WindowSizing.ScaleMinSize(
            minWidthDip: 900, minHeightDip: 600, scale: 1.0, workWidth: 1920, workHeight: 1040, marginDip: 16);

        Assert.Equal(900, w);
        Assert.Equal(520, h);
    }

    [Fact]
    public void ScaleMinSize_HalfSnapCell_RelaxesBothAxes_LowResLaptop()
    {
        // Bug real de #7 (Tier B): portátil 1366x768 con WorkArea ~1366x728 a 100%. La celda de media
        // pantalla mide 683x728. El mínimo de MainWindow (900x600 DIP) ni siquiera cabía en ancho
        // (900 > 683) -- antes del fix, esto bloqueaba el snap a media pantalla. Tras el fix, ambos
        // ejes se relajan a la mitad del work area (683x364).
        var (w, h) = WindowSizing.ScaleMinSize(
            minWidthDip: 900, minHeightDip: 600, scale: 1.0, workWidth: 1366, workHeight: 728, marginDip: 16);

        Assert.Equal(683, w);
        Assert.Equal(364, h);
    }

    [Fact]
    public void ScaleMinSize_LargeMonitor_MinFitsWithinHalfWorkArea_PreservesMinIntact()
    {
        // Monitor QHD (2560x1440): la mitad del área de trabajo (1280x720) es mucho mayor que el
        // mínimo escalado (900x600 a 100%), así que el snap-aware clamp no relaja nada -- el mínimo
        // cómodo de diseño se mantiene intacto, igual que antes del fix.
        var (w, h) = WindowSizing.ScaleMinSize(
            minWidthDip: 900, minHeightDip: 600, scale: 1.0, workWidth: 2560, workHeight: 1440, marginDip: 16);

        Assert.Equal(900, w);
        Assert.Equal(600, h);
    }

    [Fact]
    public void ScaleMinSize_QuarterSnapCell_ClampsInPhysicalPixels_AtHighDpi()
    {
        // A 150% de DPI, el clamp por mitad de área de trabajo debe operar sobre los píxeles físicos
        // del work area (que ya vienen en físico desde DisplayArea.WorkArea), no sobre DIP: con
        // workWidth=1920 / workHeight=1000 físicos, la mitad es 960x500 -- el ancho lo fija el propio
        // mínimo escalado (900*1.5=1350, pero 1350 > 960 así que también se acota) y el alto se acota
        // a 500 en vez de a 900 (600 DIP * 1.5), demostrando que el clamp no reintroduce DIP a mitad
        // de camino.
        var (w, h) = WindowSizing.ScaleMinSize(
            minWidthDip: 900, minHeightDip: 600, scale: 1.5, workWidth: 1920, workHeight: 1000, marginDip: 16);

        Assert.Equal(960, w);
        Assert.Equal(500, h);
    }
}
