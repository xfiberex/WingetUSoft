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
    [InlineData("btnConsultarDesconocidas")]
    [InlineData("btnActualizarSeleccionados")]
    [InlineData("btnActualizarTodo")]
    [InlineData("btnCancelar")]
    [InlineData("btnOpciones")]
    [InlineData("btnAyuda")]
    public void QuickActionButton_IsPresent(string automationId)
    {
        var button = fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId));

        Assert.NotNull(button);
    }

    [Fact]
    public void PackagesList_IsPresent()
    {
        var list = fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("lvPackages"));

        Assert.NotNull(list);
    }
}
