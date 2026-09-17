using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace WingetUSoft.UiTests;

/// <summary>
/// Estado inicial de la tabla principal contra la app real (F-18).
/// </summary>
/// <remarks>
/// No se pulsa el botón: lanzaría una consulta real a winget, y estas pruebas no dependen de winget ni de la red.
/// Que la consulta arranque de verdad se comprueba conduciendo la app a mano.
/// </remarks>
[Collection(AppCollection.Name)]
public sealed class EmptyStateTests(AppFixture fixture)
{
    [Fact]
    public void EmptyTable_OffersTheActionThatFillsIt()
    {
        var button = fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("btnEstadoAccion"));

        Assert.NotNull(button);
        Assert.False(button!.IsOffscreen);
        Assert.Equal("Consultar actualizaciones", button.Name);
    }

    /// <summary>
    /// La instrucción del arranque aparece una sola vez: hasta F-18 la repetían la línea de detalle, el panel
    /// de la tabla y la barra de estado.
    /// </summary>
    [Fact]
    public void StartupInstruction_AppearsOnlyOnce()
    {
        var texts = fixture.MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.Text))
            .Select(e => e.Properties.Name.ValueOrDefault ?? "")   // algún elemento del árbol no expone Name
            .Where(t => t.Contains("Consultar actualizaciones", StringComparison.OrdinalIgnoreCase)
                     && t.Contains("Pulsa", StringComparison.OrdinalIgnoreCase)).ToList();

        Assert.True(texts.Count == 1, "Textos con la instrucción: " + string.Join(" | ", texts));
    }
}
