using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;

namespace WingetUSoft.UiTests;

/// <summary>
/// Cambio de idioma en caliente y ventana de Configuración -- ninguno de estos tests toca winget. Cada
/// test deja la app en el estado con el que la encontró (español) para no afectar a otros tests que
/// dependen de texto en español (<see cref="SettingsBackup"/> ya protege el settings.json real del
/// usuario aparte de esto).
///
/// Desde el Tier C #5 el idioma ya NO se cambia desde el menú: vive en la ventana de Configuración,
/// junto al resto de preferencias. Por eso estos tests abren la ventana, eligen y guardan.
/// </summary>
[Collection(AppCollection.Name)]
public sealed class SettingsTests(AppFixture fixture)
{
    private const int LangSpanish = 0;
    private const int LangEnglish = 1;

    private Window Window => fixture.MainWindow;

    /// <summary>
    /// Cubre Tier B #5 (revisión de longitud de texto por idioma) y el cableado nuevo del Tier C #5:
    /// que guardar en Configuración repinte de verdad los controles ya visibles de la ventana
    /// principal, que es el paso que antes hacía el menú y ahora hace <c>MenuConfiguracion_Click</c>.
    /// </summary>
    [Fact]
    public void LanguageSwitch_FromSettingsWindow_UpdatesButtonText_ThenRestoresSpanish()
    {
        try
        {
            SelectLanguageAndSave(LangEnglish);

            var button = Window.FindFirstDescendant(cf => cf.ByAutomationId("btnConsultar"))?.AsButton();
            Assert.NotNull(button);
            Assert.Equal("Check for updates", button!.Name);
        }
        finally
        {
            // Siempre se intenta volver a español, incluso si el Assert de arriba falló, para no dejar
            // el idioma cambiado de cara al resto de tests de esta corrida.
            SelectLanguageAndSave(LangSpanish);
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
        var settingsWindow = OpenSettingsWindow();

        try
        {
            var cancelButton = settingsWindow.FindFirstDescendant(cf => cf.ByAutomationId("btnCancelar"))?.AsButton();
            Assert.NotNull(cancelButton);
            cancelButton!.Invoke();
        }
        finally
        {
            WaitUntilSettingsWindowIsGone();
        }
    }

    /// <summary>
    /// Tier C #5: las preferencias que vivían en el menú (tema, idioma, modo de actualización) tienen
    /// que estar TODAS en Configuración. Si alguien se deja una por el camino, este test la echa en falta.
    /// </summary>
    [Fact]
    public void SettingsWindow_HostsEveryPreferenceThatUsedToLiveInTheMenu()
    {
        var settingsWindow = OpenSettingsWindow();

        try
        {
            Assert.NotNull(settingsWindow.FindFirstDescendant(cf => cf.ByAutomationId("rbTema")));
            Assert.NotNull(settingsWindow.FindFirstDescendant(cf => cf.ByAutomationId("cmbIdioma")));
            Assert.NotNull(settingsWindow.FindFirstDescendant(cf => cf.ByAutomationId("rbModo")));
        }
        finally
        {
            settingsWindow.FindFirstDescendant(cf => cf.ByAutomationId("btnCancelar"))?.AsButton()?.Invoke();
            WaitUntilSettingsWindowIsGone();
        }
    }

    private void SelectLanguageAndSave(int languageIndex)
    {
        var settingsWindow = OpenSettingsWindow();

        var combo = settingsWindow.FindFirstDescendant(cf => cf.ByAutomationId("cmbIdioma"))?.AsComboBox();
        Assert.NotNull(combo);
        // Por índice y no por texto: los propios elementos del ComboBox están traducidos, así que su
        // rótulo depende del idioma en el que esté la app justo ahora.
        combo!.Select(languageIndex);

        var save = settingsWindow.FindFirstDescendant(cf => cf.ByAutomationId("btnGuardar"))?.AsButton();
        Assert.NotNull(save);
        save!.Invoke();

        WaitUntilSettingsWindowIsGone();
    }

    private Window OpenSettingsWindow()
    {
        MenuActions.ClickPath(Window, "btnHerramientas", "menuConfiguracion");

        var result = Retry.WhileNull(
            () => FindSettingsWindow(),
            timeout: TimeSpan.FromSeconds(10),
            interval: TimeSpan.FromMilliseconds(250),
            ignoreException: true);

        Assert.True(result.Success && result.Result is not null, "No se abrió la ventana de Configuración a tiempo.");
        return result.Result!;
    }

    private void WaitUntilSettingsWindowIsGone() =>
        Retry.WhileTrue(
            () => FindSettingsWindow() is not null,
            timeout: TimeSpan.FromSeconds(10),
            interval: TimeSpan.FromMilliseconds(250),
            ignoreException: true);

    /// <summary>Se identifica por su botón Guardar: es el único control que solo existe en esa ventana.</summary>
    private Window? FindSettingsWindow() =>
        fixture.App.GetAllTopLevelWindows(fixture.Automation)
            .FirstOrDefault(w =>
            {
                try { return w.FindFirstDescendant(cf => cf.ByAutomationId("btnGuardar")) is not null; }
                catch { return false; }
            });
}
