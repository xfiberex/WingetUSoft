using FlaUI.Core.AutomationElements;

namespace WingetUSoft.UiTests;

/// <summary>
/// Cubre la navegación al diálogo estático/de solo lectura accesible desde Ayuda -> Acerca de -- ningún
/// test de este proyecto toca una operación real de winget (upgrade/uninstall disparan UAC en el
/// escritorio seguro, inautomatizable con FlaUI; ver ROADMAP.md #8). El test envuelve el diálogo en
/// try/finally con <see cref="DialogHelper.SafeCloseAnyDialog"/>: un assert fallido nunca debe dejar un
/// ContentDialog abierto (WinUI solo permite uno a la vez; el siguiente test intentaría abrir un
/// segundo y tumbaría el proceso entero).
/// </summary>
[Collection(AppCollection.Name)]
public sealed class MenuDialogsTests(AppFixture fixture)
{
    private Window Window => fixture.MainWindow;

    [Fact]
    public void AboutDialog_OpensWithVersionAndCloses()
    {
        MenuActions.ClickPath(Window, "btnAyuda", "menuAcercaDe");
        var dialog = DialogHelper.WaitForDialog(fixture);
        try
        {
            var versionText = DialogHelper.WaitForChild(dialog, "VersionText");
            Assert.False(string.IsNullOrWhiteSpace(DialogHelper.ReadText(versionText)));
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
        }
    }
}
