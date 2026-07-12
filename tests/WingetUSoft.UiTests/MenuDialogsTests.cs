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

    /// <summary>
    /// Ayuda -> Licencia / Avisos de terceros muestran los textos legales <b>embebidos</b> en el .exe.
    /// El assert que importa es que el cuerpo no venga vacío: <see cref="LegalText"/> es defensivo, así
    /// que un recurso mal embebido no lanzaría — pintaría "Texto no disponible" y el diálogo seguiría
    /// abriéndose igual. Leer el cuerpo real es lo único que distingue un caso del otro desde la UI.
    /// </summary>
    [Theory]
    [InlineData("menuLicencia")]
    [InlineData("menuAvisosTerceros")]
    public void LegalDialog_ShowsEmbeddedTextAndCloses(string menuItemId)
    {
        MenuActions.ClickPath(Window, "btnAyuda", menuItemId);
        var dialog = DialogHelper.WaitForDialog(fixture);
        try
        {
            var body = DialogHelper.WaitForChild(dialog, "BodyText");
            string text = DialogHelper.ReadText(body);
            Assert.False(string.IsNullOrWhiteSpace(text));
            Assert.DoesNotContain("no disponible", text, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
        }
    }
}
