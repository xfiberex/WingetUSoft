using Windows.UI;

namespace WingetUSoft;

/// <summary>Qué es cada línea del registro de actividad. Decide su color, no su formato.</summary>
internal enum LogLineKind { Normal, Success, Error, Warning, Accent }

/// <summary>
/// Colores del registro de actividad, uno por tema.
///
/// Hasta v1.5.0 había UN solo juego de RGB cableado en <c>AppendLog</c>, el mismo para claro y para
/// oscuro. Esos colores estaban elegidos para fondo claro, así que sobre la tarjeta oscura el verde
/// (#387A4D) y el rojo (#BA4636) caían muy por debajo del contraste mínimo legible: los aciertos y
/// los fallos —justo lo que el usuario busca en el registro— eran lo peor de leer en tema oscuro.
///
/// Los tonos salen de la paleta de Windows (SystemFillColorSuccess/Critical/Caution), que ya trae una
/// variante por tema. <c>LogPaletteTests</c> mide el contraste real de cada combinación contra el
/// fondo de su tarjeta y exige el 4.5:1 de WCAG AA, así que un color mal elegido rompe el build.
/// </summary>
internal static class LogPalette
{
    // Fondo de la tarjeta del registro (CardBackgroundFillColorDefault ya resuelto sobre el fondo de
    // página). Es la referencia contra la que se mide el contraste.
    internal static readonly Color LightBackground = Color.FromArgb(255, 251, 251, 251);
    internal static readonly Color DarkBackground = Color.FromArgb(255, 43, 43, 43);

    internal static Color For(LogLineKind kind, bool darkTheme) => (kind, darkTheme) switch
    {
        (LogLineKind.Success, false) => Color.FromArgb(255, 0x0F, 0x7B, 0x0F),
        (LogLineKind.Success, true) => Color.FromArgb(255, 0x6C, 0xCB, 0x5F),
        (LogLineKind.Error, false) => Color.FromArgb(255, 0xC4, 0x2B, 0x1C),
        (LogLineKind.Error, true) => Color.FromArgb(255, 0xFF, 0x99, 0xA4),
        (LogLineKind.Warning, false) => Color.FromArgb(255, 0x9D, 0x5D, 0x00),
        (LogLineKind.Warning, true) => Color.FromArgb(255, 0xFC, 0xE1, 0x00),
        // Marcadores estructurales ("[12:00:00]", "[1/8] ..."): ni error ni acierto, solo separan el
        // registro. En teal, que no compite con el rojo de los fallos.
        (LogLineKind.Accent, false) => Color.FromArgb(255, 0x12, 0x6D, 0x6F),
        (LogLineKind.Accent, true) => Color.FromArgb(255, 0x4C, 0xC2, 0xC4),
        (_, false) => Color.FromArgb(255, 0x1B, 0x1B, 0x1B),
        (_, true) => Color.FromArgb(255, 0xFF, 0xFF, 0xFF),
    };

    internal static Color Background(bool darkTheme) => darkTheme ? DarkBackground : LightBackground;

    /// <summary>Razón de contraste WCAG 2.x entre dos colores opacos (1:1 = idénticos, 21:1 = negro sobre blanco).</summary>
    internal static double ContrastRatio(Color a, Color b)
    {
        double la = RelativeLuminance(a);
        double lb = RelativeLuminance(b);
        (double lighter, double darker) = la >= lb ? (la, lb) : (lb, la);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(Color c) =>
        (0.2126 * Linearize(c.R)) + (0.7152 * Linearize(c.G)) + (0.0722 * Linearize(c.B));

    private static double Linearize(byte channel)
    {
        double s = channel / 255.0;
        return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
    }
}
