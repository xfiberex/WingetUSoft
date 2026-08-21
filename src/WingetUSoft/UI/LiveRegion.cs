using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace WingetUSoft;

/// <summary>
/// Convierte la barra de estado de una ventana en una <b>región activa</b> de UI Automation
/// (WCAG 2.2 AA, criterio 4.1.3 "Mensajes de estado").
/// </summary>
/// <remarks>
/// El problema que resuelve: la barra de estado es lo único que el usuario mira durante una operacion
/// larga ("Actualizando 3 de 8", "Completado", "Error"), pero cambiar <c>Text</c> no notifica nada al
/// árbol de automatización. Un lector de pantalla solo lo leería si el foco estuviera encima, y el foco
/// nunca está ahí: está en el botón que lanzó la operacion.
///
/// Dos piezas hacen falta, y las dos son necesarias:
/// <list type="number">
///   <item><c>LiveSetting = Polite</c> marca el elemento como región activa (se anuncia sin robar el
///   turno al usuario, a diferencia de <c>Assertive</c>).</item>
///   <item>El evento <c>LiveRegionChanged</c> es lo que dispara el anuncio. Sin él, la marca del punto
///   anterior no hace nada por sí sola.</item>
/// </list>
///
/// Se engancha por <see cref="DependencyObject.RegisterPropertyChangedCallback"/> en vez de envolver
/// cada asignación en un ayudante <c>SetStatus(...)</c>: hay más de 60 asignaciones a <c>txtEstado</c>
/// repartidas por cuatro ventanas, y por esta vía <b>toda</b> asignación queda cubierta, también las
/// que se escriban en el futuro.
///
/// <see cref="FrameworkElementAutomationPeer.FromElement"/> devuelve <c>null</c> cuando no hay ningún
/// par de automatización creado, es decir, cuando no hay ningún lector de pantalla escuchando. Ese
/// <c>null</c> es el caso normal y correcto: sin cliente de UIA no hay nada que anunciar.
/// </remarks>
internal static class LiveRegion
{
    /// <summary>
    /// Marca <paramref name="statusText"/> como región activa y anuncia cada cambio de su texto.
    /// Idempotente: llamar dos veces sobre el mismo elemento registraría el aviso dos veces, así que
    /// se invoca una sola vez, desde el constructor de la ventana.
    /// </summary>
    public static void TrackStatusText(TextBlock statusText)
    {
        AutomationProperties.SetLiveSetting(statusText, AutomationLiveSetting.Polite);
        statusText.RegisterPropertyChangedCallback(TextBlock.TextProperty, (sender, _) => Announce(sender));
    }

    /// <summary>Emite <c>LiveRegionChanged</c> si hay algún cliente de UIA escuchando.</summary>
    private static void Announce(DependencyObject sender)
    {
        if (sender is not UIElement element) return;
        FrameworkElementAutomationPeer.FromElement(element)
            ?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }
}
