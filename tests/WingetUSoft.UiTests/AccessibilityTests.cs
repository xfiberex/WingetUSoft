using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;

namespace WingetUSoft.UiTests;

/// <summary>
/// Comprueba contra la app real lo que la auditoría (T1-07, T1-08) daba por **no automatizable**.
/// </summary>
/// <remarks>
/// Y en buena parte lo es: lo que no se puede automatizar es *oír* al Narrador, pero eso no es lo que
/// falla. Lo que falla es que las propiedades de UI Automation no estén ahí — que la barra de estado no
/// sea región activa, que no emita el evento, que un control no tenga nombre — y eso es exactamente lo
/// que un cliente UIA como FlaUI ve igual que lo vería un lector de pantalla.
///
/// Lo que estos tests NO cubren, y sigue siendo verificación manual: cómo suena el anuncio, si llega en
/// buen momento y si el texto se entiende de oído.
/// </remarks>
[Collection(AppCollection.Name)]
public sealed class AccessibilityTests(AppFixture fixture)
{
    private const int LangSpanish = 0;
    private const int LangEnglish = 1;

    private Window Window => fixture.MainWindow;

    /// <summary>
    /// T1-07: la barra de estado tiene que estar marcada como región activa **educada**.
    /// </summary>
    /// <remarks>
    /// Antes del arreglo, <c>grep -rn "LiveSetting" src</c> daba cero resultados: la barra cambiaba
    /// —«Actualizando 3 de 8», «Completado», «Error»— sin notificar nada al árbol de automatización, y
    /// el foco nunca está encima, sino en el botón que lanzó la operación.
    /// </remarks>
    [Fact]
    public void StatusBar_IsAPoliteLiveRegion()
    {
        var status = Window.FindFirstDescendant(cf => cf.ByAutomationId("txtEstado"));

        Assert.NotNull(status);
        Assert.Equal(LiveSetting.Polite, status!.Properties.LiveSetting.Value);
    }

    /// <summary>
    /// T1-07, la otra mitad: marcar el elemento no anuncia nada por sí solo. Lo que dispara el anuncio
    /// es el evento <c>LiveRegionChanged</c>, y eso solo se comprueba provocando un cambio real.
    /// </summary>
    /// <remarks>
    /// Se cambia el idioma porque es la vía más barata que reescribe la barra sin invocar a winget:
    /// <c>ApplyLocalizedStrings</c> la repinta («Listo.» → «Ready.»). El anuncio se emite desde un
    /// callback sobre la propiedad Text, así que sirve igual para cualquier otra escritura.
    /// </remarks>
    [Fact]
    public void StatusBar_RaisesLiveRegionChanged_WhenItsTextChanges()
    {
        var status = Window.FindFirstDescendant(cf => cf.ByAutomationId("txtEstado"));
        Assert.NotNull(status);

        using var announced = new ManualResetEventSlim(false);
        var registration = status!.RegisterAutomationEvent(
            fixture.Automation.EventLibrary.Element.LiveRegionChangedEvent,
            TreeScope.Element,
            (_, _) => announced.Set());

        try
        {
            SelectLanguageAndSave(LangEnglish);

            Assert.True(announced.Wait(TimeSpan.FromSeconds(10)),
                "La barra de estado cambió de texto sin emitir LiveRegionChanged: un lector de pantalla "
                + "no anunciaría el progreso ni el resultado de ninguna operación.");
        }
        finally
        {
            try { registration.Dispose(); } catch { /* el registro muere con la ventana */ }
            SelectLanguageAndSave(LangSpanish);
        }
    }

    /// <summary>T1-07: el registro de actividad tiene nombre accesible propio (su encabezado).</summary>
    [Fact]
    public void ActivityLog_HasAnAccessibleName()
    {
        var log = Window.FindFirstDescendant(cf => cf.ByAutomationId("rtbLog"));

        Assert.NotNull(log);
        Assert.False(string.IsNullOrWhiteSpace(log!.Name),
            "El registro no tiene nombre accesible: al recorrer la ventana se anuncia como un bloque de texto sin identificar.");
    }

    /// <summary>
    /// T1-08: los dos interruptores de Configuración se anunciaban como «interruptor, desactivado», sin
    /// decir de qué eran — su etiqueta vivía en un TextBlock aparte, sin asociar.
    /// </summary>
    /// <remarks>
    /// Desde F-16 cada opción es una <c>SettingsCard</c>, que nombra su control desde el título visible; el título
    /// expone el AutomationId «nombre de la fila + Header». Se comprueban todas las filas, no solo los interruptores.
    /// Los botones quedan fuera a propósito: conservan su propio texto (ver <see cref="SettingsRowButtons_KeepTheirOwnText"/>).
    /// </remarks>
    [Theory]
    [InlineData("cmbTema", "cardThemeHeader")]
    [InlineData("cmbIdioma", "cardLanguageHeader")]
    [InlineData("cmbModo", "cardModeHeader")]
    [InlineData("tsAdministrador", "cardAdminHeader")]
    [InlineData("cmbIntervalo", "cardIntervalHeader")]
    [InlineData("tsLogArchivo", "cardLogToFileHeader")]
    [InlineData("tsShowNotifications", "cardNotificationsHeader")]
    [InlineData("tsMinimizeToTray", "cardTrayHeader")]
    public void SettingsRows_NameTheirControlAfterTheVisibleTitle(string toggleId, string labelId)
    {
        var settingsWindow = OpenSettingsWindow();

        try
        {
            var toggle = settingsWindow.FindFirstDescendant(cf => cf.ByAutomationId(toggleId));
            var label = settingsWindow.FindFirstDescendant(cf => cf.ByAutomationId(labelId));

            Assert.NotNull(toggle);
            Assert.NotNull(label);
            Assert.False(string.IsNullOrWhiteSpace(toggle!.Name),
                $"'{toggleId}' no tiene nombre accesible: se anuncia solo como 'interruptor'.");

            // El nombre sale de la etiqueta visible (LabeledBy), así que sigue al idioma sin claves nuevas.
            Assert.Equal(label!.Name, toggle.Name);
        }
        finally
        {
            settingsWindow.FindFirstDescendant(cf => cf.ByAutomationId("btnCancelar"))?.AsButton()?.Invoke();
            WaitUntilSettingsWindowIsGone();
        }
    }

    /// <summary>
    /// F-16: un botón dentro de una fila de Configuración se anuncia con su texto, no con el título de la fila.
    /// </summary>
    /// <remarks>
    /// En la primera versión de <c>SettingsCard</c>, «Limpiar lista» se anunciaba «Estos paquetes no se incluirán en
    /// las actualizaciones.»: el <c>LabeledBy</c> de la fila tapaba el texto del botón. Lo detectó la prueba en la app.
    /// </remarks>
    [Theory]
    [InlineData("btnAbrirCarpeta", "Abrir carpeta")]
    [InlineData("btnLimpiar", "Limpiar lista")]
    public void SettingsRowButtons_KeepTheirOwnText(string buttonId, string expectedName)
    {
        var settingsWindow = OpenSettingsWindow();

        try
        {
            var button = settingsWindow.FindFirstDescendant(cf => cf.ByAutomationId(buttonId));

            Assert.NotNull(button);
            Assert.Equal(expectedName, button!.Name);
        }
        finally
        {
            settingsWindow.FindFirstDescendant(cf => cf.ByAutomationId("btnCancelar"))?.AsButton()?.Invoke();
            WaitUntilSettingsWindowIsGone();
        }
    }

    /// <summary>
    /// T2-08: los tres filtros de la ventana principal tenían su etiqueta en un TextBlock suelto, sin
    /// asociar, así que se anunciaban como «cuadro combinado», «botón» y «cuadro de edición» a secas.
    /// </summary>
    /// <remarks>
    /// El nombre sale de la etiqueta visible vía <c>LabeledBy</c>, no de una cadena nueva: por eso basta
    /// con comparar los dos, y por eso sigue al idioma en los cinco sin claves de traducción adicionales.
    /// </remarks>
    [Theory]
    [InlineData("cmbFuente", "txtFuenteLabel")]
    [InlineData("btnFiltroExcluidos", "txtExcluidosLabel")]
    [InlineData("txtBuscar", "txtBuscarLabel")]
    public void MainWindowFilters_AreNamedAfterTheirVisibleLabel(string controlId, string labelId)
    {
        var control = Window.FindFirstDescendant(cf => cf.ByAutomationId(controlId));
        var label = Window.FindFirstDescendant(cf => cf.ByAutomationId(labelId));

        Assert.NotNull(control);
        Assert.NotNull(label);
        Assert.False(string.IsNullOrWhiteSpace(control!.Name),
            $"'{controlId}' no tiene nombre accesible: se anuncia solo por su tipo de control.");
        Assert.Equal(label!.Name, control.Name);
    }

    /// <summary>
    /// F-15: el patrón de arriba, en Historial. Hasta F-15 el buscador se anunciaba con su placeholder
    /// («Nombre o Id...») y el filtro de estado con el valor elegido («Todos»).
    /// </summary>
    /// <remarks>
    /// Historial se abre sin winget ni red, así que se conduce de verdad. Desinstalar lanza <c>winget list</c> al
    /// abrirse y lo cubre <c>XamlAccessibilityTests.SearchAndFilterControls_AreLabeledByTheirVisibleLabel</c>.
    /// </remarks>
    [Fact]
    public void HistoryFilters_AreNamedAfterTheirVisibleLabel()
    {
        MenuActions.ClickPath(Window, "btnHerramientas", "menuVerHistorial");

        var result = Retry.WhileNull(
            () => fixture.App.GetAllTopLevelWindows(fixture.Automation)
                .FirstOrDefault(w => w.FindFirstDescendant(cf => cf.ByAutomationId("lvHistory")) is not null),
            timeout: TimeSpan.FromSeconds(10),
            interval: TimeSpan.FromMilliseconds(250),
            ignoreException: true);
        Assert.True(result.Success && result.Result is not null, "No se abrió la ventana de Historial.");
        var history = result.Result!;

        try
        {
            foreach (var (controlId, labelId) in new[] { ("txtBuscar", "txtBuscarLabel"), ("btnFiltroEstado", "txtEstadoLabel") })
            {
                var control = history.FindFirstDescendant(cf => cf.ByAutomationId(controlId));
                var label = history.FindFirstDescendant(cf => cf.ByAutomationId(labelId));

                Assert.NotNull(control);
                Assert.NotNull(label);
                Assert.Equal(label!.Name, control!.Name);
            }
        }
        finally
        {
            history.Close();
            Retry.WhileTrue(
                () => fixture.App.GetAllTopLevelWindows(fixture.Automation)
                    .Any(w => w.FindFirstDescendant(cf => cf.ByAutomationId("lvHistory")) is not null),
                timeout: TimeSpan.FromSeconds(10),
                interval: TimeSpan.FromMilliseconds(250),
                ignoreException: true);
        }
    }

    // ── Apoyo (mismo enfoque que SettingsTests) ────────────────────────────

    private void SelectLanguageAndSave(int languageIndex)
    {
        var settingsWindow = OpenSettingsWindow();

        var combo = settingsWindow.FindFirstDescendant(cf => cf.ByAutomationId("cmbIdioma"))?.AsComboBox();
        Assert.NotNull(combo);
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

    private Window? FindSettingsWindow() =>
        fixture.App.GetAllTopLevelWindows(fixture.Automation)
            .FirstOrDefault(w =>
            {
                try { return w.FindFirstDescendant(cf => cf.ByAutomationId("btnGuardar")) is not null; }
                catch { return false; }
            });
}
