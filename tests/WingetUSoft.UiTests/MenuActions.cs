using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;

namespace WingetUSoft.UiTests;

/// <summary>
/// Navega la barra de "Acciones rápidas" de <c>MainWindow.xaml</c> por AutomationId (= x:Name del
/// XAML). Adaptado de <c>MainWindowActions.ClickMenuPath</c> de FormatDiskPro.UiTests: la diferencia
/// estructural es que aquí el primer nivel ("Opciones"/"Ayuda") es un <c>DropDownButton</c> con un
/// <c>MenuFlyout</c> propio en vez de un <c>MenuBar</c> clásico -- su automation peer expone
/// ExpandCollapse igual que un MenuBarItem, así que la misma cadena de patrones UIA sirve para ambos.
/// Se evita <c>.Click()</c> por coordenadas de ratón salvo como último recurso: depende de la posición
/// real del cursor/foco en pantalla, frágil en la práctica tras el primer clic de la sesión.
/// </summary>
public static class MenuActions
{
    public static void ClickPath(Window window, params string[] automationIds)
    {
        for (int i = 0; i < automationIds.Length; i++)
        {
            string id = automationIds[i];
            bool isLast = i == automationIds.Length - 1;

            var result = Retry.WhileNull(
                () => window.FindFirstDescendant(cf => cf.ByAutomationId(id)),
                timeout: TimeSpan.FromSeconds(10),
                interval: TimeSpan.FromMilliseconds(200),
                ignoreException: true);

            if (!result.Success || result.Result is null)
                throw new InvalidOperationException($"No se encontró el ítem de menú '{id}'.");

            var element = result.Result;

            if (!isLast && element.Patterns.ExpandCollapse.IsSupported)
                element.Patterns.ExpandCollapse.Pattern.Expand();
            else if (element.Patterns.Invoke.IsSupported)
                element.Patterns.Invoke.Pattern.Invoke();
            // Los RadioMenuFlyoutItem/ToggleMenuFlyoutItem (idioma/tema) no siempre exponen Invoke: su
            // automation peer expone SelectionItem (radio) o Toggle, que disparan el mismo evento Click
            // subyacente en el que engancha MainWindow.
            else if (element.Patterns.SelectionItem.IsSupported)
                element.Patterns.SelectionItem.Pattern.Select();
            else if (element.Patterns.Toggle.IsSupported)
                element.Patterns.Toggle.Pattern.Toggle();
            else if (element.Patterns.ExpandCollapse.IsSupported)
                element.Patterns.ExpandCollapse.Pattern.Expand();
            else
                element.Click();

            // Pequeño respiro tras cada paso: la animación de apertura/cierre del flyout necesita un
            // instante para asentarse antes de que el siguiente paso lea el árbol de automatización.
            Thread.Sleep(150);
        }
    }
}
