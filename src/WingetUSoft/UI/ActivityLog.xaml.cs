using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace WingetUSoft;

/// <summary>
/// El registro de actividad de una ventana: pinta cada línea según su tipo, recorta por antigüedad,
/// sigue el scroll al final y se repinta cuando cambia el tema.
/// </summary>
/// <remarks>
/// <para>
/// Había **cuatro** copias de esto, una por ventana, con tres estrategias de color distintas y tres
/// límites de recorte (400 / 200 / 400 / 200). Las diferencias no eran decisiones: la ventana de
/// búsqueda dejaba sin colorear las líneas normales, dos ventanas se quedaron con RGB cableados que el
/// Tier C ya había retirado de la principal por ilegibles, y solo la principal repintaba lo ya escrito
/// al cambiar de tema. Eso es lo que pasa con un bloque copiado: cada copia envejece por su cuenta.
/// </para>
/// <para>
/// Lo que **sigue siendo de cada ventana** es el límite de líneas (<see cref="MaxLines"/>) y qué tipo
/// tiene cada línea: son decisiones suyas, no del widget.
/// </para>
/// </remarks>
public sealed partial class ActivityLog : UserControl
{
    /// <summary>
    /// Tipo de cada línea ya pintada, en paralelo a <c>rtbLog.Blocks</c> (mismo índice, se recortan a
    /// la vez). Un <c>Run</c> no guarda de qué tipo era, y sin eso <see cref="Recolor"/> no podría
    /// devolverle su color al cambiar el tema.
    /// </summary>
    private readonly List<LogLineKind> _kinds = [];

    public ActivityLog() => InitializeComponent();

    /// <summary>Líneas que se conservan antes de empezar a tirar las más viejas.</summary>
    public int MaxLines { get; set; } = 400;

    /// <summary>
    /// El tema se lee del propio control y no de <c>Application.Current</c>: el tema se fuerza por
    /// elemento, así que con "Claro" elegido sobre un Windows oscuro la aplicación seguiría diciendo
    /// "oscuro" y el registro saldría con los colores del tema contrario (Tier C #4).
    /// </summary>
    private bool IsDark => rtbLog.ActualTheme == ElementTheme.Dark;

    private SolidColorBrush BrushFor(LogLineKind kind) => new(LogPalette.For(kind, IsDark));

    /// <summary>
    /// Nombre accesible del registro. Lo pone la ventana con su encabezado ya traducido.
    /// </summary>
    /// <remarks>
    /// Se nombra el <c>RichTextBlock</c> y no el control: el bloque de texto es donde aterriza un
    /// lector de pantalla, y al mover el registro aquí se quedó sin nombre — lo cazó
    /// <c>AccessibilityTests.ActivityLog_HasAnAccessibleName</c>. Se hace por código y no con
    /// <c>LabeledBy</c> desde el XAML de la ventana porque la etiqueta vive en otro control y el
    /// vínculo no llegaba al bloque interior.
    /// </remarks>
    internal void SetAccessibleName(string name) =>
        AutomationProperties.SetName(rtbLog, name);

    /// <summary>Añade una línea y deja el scroll al final.</summary>
    internal void Append(string text, LogLineKind kind = LogLineKind.Normal)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var paragraph = new Paragraph();
        paragraph.Inlines.Add(new Run { Text = text, Foreground = BrushFor(kind) });
        rtbLog.Blocks.Add(paragraph);
        _kinds.Add(kind);

        while (rtbLog.Blocks.Count > MaxLines && rtbLog.Blocks.Count > 1)
        {
            rtbLog.Blocks.RemoveAt(0);
            _kinds.RemoveAt(0);
        }

        ScrollToEnd();
    }

    internal void Clear()
    {
        rtbLog.Blocks.Clear();
        _kinds.Clear();
    }

    /// <summary>Repinta lo ya escrito; sin esto el registro conservaría los colores del tema anterior.</summary>
    internal void Recolor()
    {
        int count = Math.Min(rtbLog.Blocks.Count, _kinds.Count);
        for (int i = 0; i < count; i++)
        {
            if (rtbLog.Blocks[i] is Paragraph { Inlines: [Run run, ..] })
                run.Foreground = BrushFor(_kinds[i]);
        }
    }

    /// <summary>
    /// Reescribe la última línea si empieza por <paramref name="prefix"/>; si no, añade una nueva.
    /// </summary>
    /// <remarks>
    /// Es lo que hace que la barra de descarga avance **en su sitio** en vez de dejar un rastro de
    /// cientos de líneas casi iguales.
    /// </remarks>
    internal void AppendOrReplaceLast(string prefix, string text, LogLineKind kind = LogLineKind.Normal)
    {
        if (rtbLog.Blocks.Count > 0
            && rtbLog.Blocks[^1] is Paragraph { Inlines: [Run last, ..] }
            && last.Text.StartsWith(prefix, StringComparison.Ordinal))
        {
            last.Text = text;
            ScrollToEnd();
            return;
        }

        Append(text, kind);
    }

    /// <summary>
    /// <c>UpdateLayout</c> antes de mover el scroll: sin él, <c>ScrollableHeight</c> es todavía el de
    /// antes de añadir la línea y el registro se queda una línea corto justo al final.
    /// </summary>
    private void ScrollToEnd()
    {
        scrollLog.UpdateLayout();
        scrollLog.ChangeView(null, scrollLog.ScrollableHeight, null);
    }
}
