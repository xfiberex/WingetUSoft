using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;

namespace WingetUSoft.UiTests;

/// <summary>
/// Ventana "Buscar e instalar" (Tier E). Se comprueba que abre desde el menú y que sus controles están
/// donde deben, **sin lanzar una búsqueda real**: una búsqueda sale a la red y al catálogo de winget, así
/// que meterla aquí haría que un release dependiera de la conectividad de la máquina que lo corta (y
/// `release.ps1` ejecuta estos tests). La búsqueda y la instalación reales se verifican conduciendo la
/// app a mano; la lógica de parseo de la salida de winget está cubierta por `WingetSearchParserTests`.
/// </summary>
[Collection(AppCollection.Name)]
public sealed class SearchWindowTests(AppFixture fixture)
{
    private Window Window => fixture.MainWindow;

    [Fact]
    public void SearchWindow_OpensFromToolsMenuWithItsControls()
    {
        MenuActions.ClickPath(Window, "btnHerramientas", "menuBuscarInstalar");

        var searchWindow = WaitForWindow("btnBuscar");
        try
        {
            Assert.NotNull(searchWindow.FindFirstDescendant(cf => cf.ByAutomationId("txtBuscar")));
            Assert.NotNull(searchWindow.FindFirstDescendant(cf => cf.ByAutomationId("lvResults")));

            // Sin nada seleccionado no hay nada que instalar: el botón arranca deshabilitado. Es lo que
            // evita el error más tonto (pulsar Instalar sin haber elegido paquete).
            var install = searchWindow.FindFirstDescendant(cf => cf.ByAutomationId("btnInstalar"));
            Assert.NotNull(install);
            Assert.False(install!.IsEnabled);
        }
        finally
        {
            searchWindow.AsWindow()?.Close();
        }
    }

    /// <summary>
    /// La ventana es un <c>Window</c> propio (no un ContentDialog), así que se busca en el escritorio
    /// entre las ventanas del proceso de la app, no dentro del árbol de MainWindow.
    /// </summary>
    private AutomationElement WaitForWindow(string childAutomationId)
    {
        var result = Retry.WhileNull(
            () => fixture.App.GetAllTopLevelWindows(fixture.Automation)
                .FirstOrDefault(w => w.FindFirstDescendant(cf => cf.ByAutomationId(childAutomationId)) is not null),
            timeout: TimeSpan.FromSeconds(15),
            interval: TimeSpan.FromMilliseconds(300),
            ignoreException: true);

        Assert.True(result.Success && result.Result is not null, "No se abrió la ventana de búsqueda.");
        return result.Result!;
    }
}
