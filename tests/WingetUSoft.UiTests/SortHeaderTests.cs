using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace WingetUSoft.UiTests;

/// <summary>
/// Tier C #6: las cabeceras ordenables eran <c>StackPanel</c> con un <c>Tapped</c>. Funcionaban con el
/// ratón y con nada más: fuera del orden de tabulación, sordas a Espacio/Intro, y un lector de pantalla
/// las anunciaba como un panel cualquiera, sin decir siquiera por qué columna estaba ordenada la tabla.
///
/// Que estos tests puedan pulsarlas por <c>Invoke</c> ES la prueba del arreglo: Invoke es el patrón de
/// UI Automation que usan también el teclado y los lectores de pantalla, y sobre un StackPanel no
/// existe. Ninguno de estos tests toca winget: la cabecera está ahí aunque la tabla esté vacía.
/// </summary>
[Collection(AppCollection.Name)]
public sealed class SortHeaderTests(AppFixture fixture)
{
    private Window Window => fixture.MainWindow;

    private static readonly string[] SortableHeaders =
    [
        "btnColNombre", "btnColId", "btnColVersion", "btnColDisponible", "btnColFuente"
    ];

    [Theory]
    [InlineData("btnColNombre")]
    [InlineData("btnColId")]
    [InlineData("btnColVersion")]
    [InlineData("btnColDisponible")]
    [InlineData("btnColFuente")]
    public void SortableHeader_IsAButton_AndIsKeyboardFocusable(string automationId)
    {
        var header = Window.FindFirstDescendant(cf => cf.ByAutomationId(automationId));

        Assert.NotNull(header);
        Assert.Equal(ControlType.Button, header!.ControlType);
        Assert.True(header.Properties.IsKeyboardFocusable.Value,
            $"'{automationId}' no puede recibir el foco: no se puede ordenar sin ratón.");
    }

    [Fact]
    public void SortableHeaders_AnnounceTheirColumnAndSortState()
    {
        foreach (string id in SortableHeaders)
        {
            var header = Window.FindFirstDescendant(cf => cf.ByAutomationId(id));
            Assert.NotNull(header);

            // El nombre accesible lo compone DescribeSortHeader: "<columna>, <estado>. Actívalo...".
            // Sin el estado, quien no ve el triángulo ▲/▼ no sabe por dónde está ordenada la tabla.
            Assert.False(string.IsNullOrWhiteSpace(header!.Name), $"'{id}' no tiene nombre accesible.");
            Assert.Contains(",", header.Name, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// El ciclo real: sin ordenar -> ascendente -> descendente, activando la cabecera como lo haría el
    /// teclado. Comprueba que el estado nuevo llega al nombre accesible, no solo al triángulo pintado.
    /// </summary>
    [Fact]
    public void InvokingHeader_CyclesSortState_AndAnnouncesIt()
    {
        var header = Window.FindFirstDescendant(cf => cf.ByAutomationId("btnColNombre"))?.AsButton();
        Assert.NotNull(header);

        string initial = header!.Name;

        header.Invoke();
        string afterFirst = WaitForNameChange(header, initial);

        header.Invoke();
        string afterSecond = WaitForNameChange(header, afterFirst);

        // Tres estados distintos, en tres pulsaciones: el orden cambió de verdad y se anunció.
        Assert.NotEqual(initial, afterFirst);
        Assert.NotEqual(afterFirst, afterSecond);
        Assert.All([initial, afterFirst, afterSecond],
            name => Assert.StartsWith(header.Name.Split(',')[0], name, StringComparison.Ordinal));
    }

    private static string WaitForNameChange(AutomationElement element, string previousName)
    {
        var result = FlaUI.Core.Tools.Retry.WhileTrue(
            () => element.Name == previousName,
            timeout: TimeSpan.FromSeconds(5),
            interval: TimeSpan.FromMilliseconds(100),
            ignoreException: true);

        Assert.True(result.Success, $"El nombre accesible no cambió tras activar la cabecera (seguía en '{previousName}').");
        return element.Name;
    }
}
