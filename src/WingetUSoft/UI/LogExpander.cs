using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WingetUSoft;

/// <summary>
/// El registro de actividad plegable de las ventanas con lista (F-17).
/// </summary>
/// <remarks>
/// <para>
/// El registro reservaba un tercio del alto aunque estuviera vacío, y casi siempre lo está: solo escribe durante
/// una operación. Ahora vive en un <see cref="Expander"/> plegado, y su fila del <c>Grid</c> es <c>Auto</c>
/// mientras lo está y <c>*</c> al desplegarlo; con la fila siempre en <c>*</c>, plegar dejaría el hueco vacío.
/// </para>
/// <para>
/// Se despliega solo en dos casos: cuando la ventana empieza una operación que cambia el equipo (<see
/// cref="Reveal"/>) y cuando se escribe un error. Una consulta o una carga no lo abren: su resultado está en la
/// lista y en la barra de estado. Si el usuario lo pliega, se queda plegado hasta el siguiente de esos dos casos.
/// </para>
/// </remarks>
internal static class LogExpander
{
    /// <param name="expander">El <see cref="Expander"/> que envuelve el registro.</param>
    /// <param name="row">La fila del <c>Grid</c> que ocupa.</param>
    /// <param name="log">El registro, para desplegarse ante un error.</param>
    /// <param name="expandedMinHeight">Alto mínimo de la fila con el registro desplegado.</param>
    internal static void Attach(Expander expander, RowDefinition row, ActivityLog log, double expandedMinHeight)
    {
        void Apply()
        {
            row.Height = expander.IsExpanded ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
            row.MinHeight = expander.IsExpanded ? expandedMinHeight : 0;
        }

        expander.RegisterPropertyChangedCallback(Expander.IsExpandedProperty, (_, _) => Apply());
        log.LineAppended += kind =>
        {
            if (kind == LogLineKind.Error) Reveal(expander);
        };
        Apply();
    }

    /// <summary>Despliega el registro: la ventana va a escribir en él algo que el usuario querrá seguir.</summary>
    internal static void Reveal(Expander expander) => expander.IsExpanded = true;
}
