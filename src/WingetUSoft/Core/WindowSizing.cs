namespace WingetUSoft;

/// <summary>
/// Rectángulo de ventana en píxeles físicos: posición (<see cref="X"/>, <see cref="Y"/>) y
/// tamaño (<see cref="Width"/>, <see cref="Height"/>).
/// </summary>
public readonly record struct WindowBounds(int X, int Y, int Width, int Height);

/// <summary>
/// Matemática pura de dimensionado y centrado de ventana (sin tipos <c>Windows.*</c> ni WinRT),
/// para que sea testeable en xUnit sin dependencias de proyección. Réplica del patrón
/// <c>SizeAndCenterWindow</c> de FormatDiskPro: tamaño de diseño (DIP) escalado por el DPI del
/// monitor y acotado al área de trabajo (<c>DisplayArea.WorkArea</c>), luego centrado en ella.
/// </summary>
public static class WindowSizing
{
    /// <summary>
    /// Calcula el tamaño y la posición centrada de la ventana a partir del tamaño de diseño (DIP),
    /// la escala de DPI del monitor y el área de trabajo (en píxeles físicos).
    /// </summary>
    /// <param name="designWidthDip">Ancho de diseño en píxeles independientes de la resolución.</param>
    /// <param name="designHeightDip">Alto de diseño en píxeles independientes de la resolución.</param>
    /// <param name="scale">Escala de DPI del monitor (p. ej. 1.0 = 100%, 1.5 = 150%, 2.0 = 200%).</param>
    /// <param name="workX">Origen X del área de trabajo del monitor, en píxeles físicos.</param>
    /// <param name="workY">Origen Y del área de trabajo del monitor, en píxeles físicos.</param>
    /// <param name="workWidth">Ancho del área de trabajo del monitor, en píxeles físicos.</param>
    /// <param name="workHeight">Alto del área de trabajo del monitor, en píxeles físicos.</param>
    /// <param name="marginDip">Respiro respecto a los bordes del área de trabajo, en DIP.</param>
    public static WindowBounds ComputeSizeAndCenter(
        int designWidthDip, int designHeightDip, double scale,
        int workX, int workY, int workWidth, int workHeight, int marginDip)
    {
        int margin = (int)Math.Round(marginDip * scale);
        int w = Math.Min((int)Math.Round(designWidthDip * scale), workWidth - margin);
        int h = Math.Min((int)Math.Round(designHeightDip * scale), workHeight - margin);

        int x = workX + (workWidth - w) / 2;
        int y = workY + (workHeight - h) / 2;

        return new WindowBounds(x, y, w, h);
    }

    /// <summary>
    /// Escala un tamaño mínimo de diseño (DIP) a píxeles físicos según el DPI del monitor y lo
    /// acota al área de trabajo menos el margen (igual que <see cref="ComputeSizeAndCenter"/>), para
    /// que en pantallas de baja resolución con DPI alto el mínimo no supere el tamaño de la pantalla
    /// (lo que impediría encoger la ventana o hacer snap a media/cuarto de pantalla).
    /// </summary>
    public static (int Width, int Height) ScaleMinSize(
        int minWidthDip, int minHeightDip, double scale, int workWidth, int workHeight, int marginDip)
    {
        int margin = (int)Math.Round(marginDip * scale);
        int w = Math.Min((int)Math.Round(minWidthDip * scale), workWidth - margin);
        int h = Math.Min((int)Math.Round(minHeightDip * scale), workHeight - margin);
        return (w, h);
    }
}
