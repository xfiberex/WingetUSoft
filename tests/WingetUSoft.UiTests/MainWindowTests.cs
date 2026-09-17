using FlaUI.Core.AutomationElements;

namespace WingetUSoft.UiTests;

[Collection(AppCollection.Name)]
public sealed class MainWindowTests(AppFixture fixture)
{
    [Fact]
    public void MainWindow_Opens()
    {
        Assert.False(fixture.MainWindow.IsOffscreen);
    }

    [Theory]
    [InlineData("btnConsultar")]
    [InlineData("btnActualizarSeleccionados")]
    [InlineData("btnActualizarTodo")]
    [InlineData("btnCancelar")]
    [InlineData("btnHerramientas")]
    [InlineData("btnAyuda")]
    public void QuickActionButton_IsPresent(string automationId)
    {
        var button = fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId));

        Assert.NotNull(button);
    }

    /// <summary>
    /// «Consultar» es una sola acción con su variante en el desplegable (F-23): el botón se puede pulsar y
    /// desplegar. No se elige ninguna variante aquí, porque lanzaría una consulta real a winget.
    /// </summary>
    [Fact]
    public void CheckForUpdates_IsASplitButton()
    {
        var button = fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("btnConsultar"));

        Assert.NotNull(button);
        Assert.NotNull(button!.Patterns.ExpandCollapse.PatternOrDefault);
        Assert.Null(fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("btnConsultarDesconocidas")));
    }

    [Fact]
    public void PackagesList_IsPresent()
    {
        var list = fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("lvPackages"));

        Assert.NotNull(list);
    }
}
