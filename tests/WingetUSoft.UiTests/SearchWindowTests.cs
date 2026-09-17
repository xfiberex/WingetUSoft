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
    /// T3-07: el nombre accesible del cuadro de búsqueda estaba **dos veces** —cableado en español en el
    /// XAML y puesto otra vez, ya traducido, desde <c>ApplyLocalizedStrings</c>—. Se quedó el segundo,
    /// que es el que sigue al idioma; este test es el que garantiza que al quitar el primero el control
    /// no se quedara sin nombre.
    /// </summary>
    [Fact]
    public void SearchBox_HasAnAccessibleName()
    {
        MenuActions.ClickPath(Window, "btnHerramientas", "menuBuscarInstalar");

        var searchWindow = WaitForWindow("btnBuscar");
        try
        {
            var box = searchWindow.FindFirstDescendant(cf => cf.ByAutomationId("txtBuscar"));

            Assert.NotNull(box);

            // Se fija la cadena exacta, y no un "no está vacío", porque eso último **no** detectaba nada:
            // sin nombre propio WinUI deduce uno del PlaceholderText y el control reporta "Nombre o
            // Id...", que no es una etiqueta sino un ejemplo de lo que escribir. Comprobado quitando el
            // SetName. La cadena es la de `search.catalogLabel` en español, el idioma con el que arranca la
            // suite; si se retoca la traducción, hay que retocarla aquí.
            Assert.Equal("Buscar en el catálogo de winget", box!.Name);

            // Desde F-15 ese nombre sale de una etiqueta que se ve (LabeledBy), no de un SetName invisible.
            var label = searchWindow.FindFirstDescendant(cf => cf.ByAutomationId("txtBuscarLabel"));
            Assert.NotNull(label);
            Assert.Equal(label!.Name, box.Name);
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
