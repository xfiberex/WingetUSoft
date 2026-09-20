using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace WingetUSoft;

/// <summary>
/// Rotula un botón de icono y texto (F-17).
/// </summary>
/// <remarks>
/// Con un <c>StackPanel</c> por contenido, WinUI ya no saca el nombre accesible del texto del botón, así que un
/// lector de pantalla lo anunciaría como «botón» a secas; y los rótulos cambian en ejecución (idioma, contador de
/// seleccionados, variante de la consulta). Por eso el texto y el nombre se ponen siempre juntos, aquí.
/// </remarks>
internal static class CommandLabel
{
    internal static void Set(Control button, TextBlock label, string text)
    {
        label.Text = text;
        AutomationProperties.SetName(button, text);
    }
}
