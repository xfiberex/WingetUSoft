using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;

namespace WingetUSoft.UiTests;

/// <summary>
/// Cambio de idioma en caliente y ventana de Configuración -- ninguno de estos tests toca winget. Cada
/// test deja la app en el estado con el que la encontró (español) para no afectar a otros tests que
/// dependen de texto en español (<see cref="SettingsBackup"/> ya protege el settings.json real del
/// usuario aparte de esto).
/// </summary>
[Collection(AppCollection.Name)]
public sealed class SettingsTests(AppFixture fixture)
{
    private Window Window => fixture.MainWindow;

    /// <summary>
    /// Cubre Tier B #5 (revisión de longitud de texto por idioma): comprueba que el cambio de idioma
    /// realmente refresca el texto de los controles ya visibles, no solo el que se renderiza al
    /// arrancar.
    /// </summary>
    [Fact]
    public void LanguageSwitch_UpdatesButtonText_ThenRestoresSpanish()
    {
        try
        {
            MenuActions.ClickPath(Window, "btnOpciones", "menuIdioma", "menuIdiomaEn");
            var button = Window.FindFirstDescendant(cf => cf.ByAutomationId("btnConsultar"))?.AsButton();
            Assert.NotNull(button);
            Assert.Equal("Check for updates", button!.Name);
        }
        finally
        {
            // Siempre se intenta volver a español, incluso si el Assert de arriba falló, para no dejar
            // el idioma cambiado de cara al resto de tests de esta corrida.
            MenuActions.ClickPath(Window, "btnOpciones", "menuIdioma", "menuIdiomaEs");
        }

        var restoredButton = Window.FindFirstDescendant(cf => cf.ByAutomationId("btnConsultar"))?.AsButton();
        Assert.NotNull(restoredButton);
        Assert.Equal("Consultar actualizaciones", restoredButton!.Name);
    }

    /// <summary>
    /// A diferencia de los diálogos Acerca de/Novedades (ContentDialog dentro del árbol de MainWindow),
    /// "Configuración..." abre una <c>SettingsWindow</c> real (HWND top-level propio, ver
    /// <c>MainWindow.xaml.cs:MenuConfiguracion_Click</c>) -- se localiza vía
    /// <c>Application.GetAllTopLevelWindows</c> en vez de <see cref="DialogHelper"/>.
    /// </summary>
    [Fact]
    public void SettingsWindow_OpensAndCloses()
    {
        MenuActions.ClickPath(Window, "btnOpciones", "menuConfiguracion");

        var result = Retry.WhileNull(
            () => fixture.App.GetAllTopLevelWindows(fixture.Automation)
                .FirstOrDefault(w =>
                {
                    try { return w.FindFirstDescendant(cf => cf.ByAutomationId("btnGuardar")) is not null; }
                    catch { return false; }
                }),
            timeout: TimeSpan.FromSeconds(10),
            interval: TimeSpan.FromMilliseconds(250),
            ignoreException: true);

        Assert.True(result.Success && result.Result is not null, "No se abrió la ventana de Configuración a tiempo.");
        var settingsWindow = result.Result!;

        try
        {
            var cancelButton = settingsWindow.FindFirstDescendant(cf => cf.ByAutomationId("btnCancelar"))?.AsButton();
            Assert.NotNull(cancelButton);
            cancelButton!.Invoke();
        }
        finally
        {
            Retry.WhileTrue(
                () => fixture.App.GetAllTopLevelWindows(fixture.Automation)
                    .Any(w =>
                    {
                        try { return w.FindFirstDescendant(cf => cf.ByAutomationId("btnGuardar")) is not null; }
                        catch { return false; }
                    }),
                timeout: TimeSpan.FromSeconds(10),
                interval: TimeSpan.FromMilliseconds(250),
                ignoreException: true);
        }
    }
}
