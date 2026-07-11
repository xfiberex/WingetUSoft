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
    /// Escala un tamaño mínimo de diseño (DIP) a píxeles físicos según el DPI del monitor y lo acota
    /// tanto al área de trabajo menos el margen (igual que <see cref="ComputeSizeAndCenter"/>) como,
    /// además, a <b>la mitad del área de trabajo en cada eje</b>.
    ///
    /// Ese segundo clamp es lo que hace que el mínimo sea "snap-aware": una celda de snap layout de
    /// Windows 11 mide la mitad del área de trabajo en un eje (media pantalla) o en ambos ejes (cuarto
    /// de pantalla). Si el mínimo escalado superase el tamaño de esa celda, <c>OverlappedPresenter</c>
    /// le impediría a Windows encoger la ventana lo suficiente y el snap fallaría o dejaría la ventana
    /// más grande que su celda (recortada visualmente contra las vecinas). Acotando cada eje a
    /// <c>workWidth/2</c> / <c>workHeight/2</c>, el mínimo nunca bloquea ni el snap a media pantalla ni
    /// el snap a cuarto, en cualquier monitor y a cualquier DPI, sin necesidad de números fijos: en
    /// monitores grandes el mínimo cómodo se conserva intacto (la mitad del área de trabajo es mucho
    /// mayor que el mínimo de diseño), y solo se relaja lo justo en pantallas pequeñas.
    ///
    /// En la práctica, la mitad del área de trabajo es siempre más restrictiva que "área de trabajo
    /// menos margen" (el margen son un puñado de DIP, muy por debajo de la mitad de cualquier
    /// resolución real), así que ese clamp por mitad es el que termina dominando el resultado. Aun así
    /// se conserva el clamp por margen como red de seguridad para casos extremos: <see cref="Math.Min(int, int)"/>
    /// ya escoge el más pequeño de los dos sin coste adicional.
    /// </summary>
    public static (int Width, int Height) ScaleMinSize(
        int minWidthDip, int minHeightDip, double scale, int workWidth, int workHeight, int marginDip)
    {
        int margin = (int)Math.Round(marginDip * scale);
        int w = Math.Min((int)Math.Round(minWidthDip * scale), Math.Min(workWidth - margin, workWidth / 2));
        int h = Math.Min((int)Math.Round(minHeightDip * scale), Math.Min(workHeight - margin, workHeight / 2));
        return (w, h);
    }
}
