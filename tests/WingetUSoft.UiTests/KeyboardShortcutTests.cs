using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;

namespace WingetUSoft.UiTests;

/// <summary>Atajos de la ventana principal contra la app real (F-21).</summary>
[Collection(AppCollection.Name)]
public sealed class KeyboardShortcutTests(AppFixture fixture)
{
    /// <summary>Ctrl+F lleva al buscador desde cualquier otro control de la ventana.</summary>
    [Fact]
    public void CtrlF_FocusesTheSearchBox()
    {
        var search = fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("txtBuscar"));
        var elsewhere = fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("btnHerramientas"));
        Assert.NotNull(search);
        Assert.NotNull(elsewhere);

        elsewhere!.Focus();
        Assert.False(search!.Properties.HasKeyboardFocus.Value, "El buscador ya tenía el foco antes del atajo.");

        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_F);

        var focused = Retry.WhileFalse(() => search.Properties.HasKeyboardFocus.Value, TimeSpan.FromSeconds(3));
        Assert.True(focused.Result, "Ctrl+F no llevó el foco al buscador.");
    }

    /// <summary>
    /// Cada atajo se publica en su control: es lo que muestra el tooltip y lo que anuncia un lector de pantalla.
    /// </summary>
    [Theory]
    [InlineData("btnConsultar", "F5")]
    [InlineData("btnCancelar", "Esc")]
    [InlineData("txtBuscar", "Ctrl+F")]
    [InlineData("chkSelectAll", "Ctrl+A")]
    public void Shortcut_IsExposedOnItsControl(string automationId, string expected)
    {
        var control = fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
        Assert.NotNull(control);

        Assert.Contains(expected, control!.Properties.AcceleratorKey.ValueOrDefault ?? "", StringComparison.OrdinalIgnoreCase);
    }
}
