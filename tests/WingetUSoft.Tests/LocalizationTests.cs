using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Verifica el comportamiento defensivo del proveedor de localización <see cref="L"/>.
/// </summary>
public sealed class LocalizationTests
{
    [Fact]
    public void T_UnknownKey_ReturnsKeyItself()
        => Assert.Equal("clave.inexistente", L.T("clave.inexistente"));

    [Fact]
    public void T_KnownKey_ReturnsLocalizedText()
    {
        // Ojo al elegir la clave: T() devuelve la propia clave cuando no la conoce, así que una clave
        // que ya no exista pasaría igualmente el "no está vacío". Se comprueba contra el diccionario.
        Assert.True(L.Map.ContainsKey("menu.tools"));
        Assert.Equal(L.Map["menu.tools"][(int)L.Current], L.T("menu.tools"));
    }

    [Fact]
    public void EveryEntry_HasFiveNonEmptyTranslations()
    {
        int langs = Enum.GetValues<AppLang>().Length;   // Es, En, Pt, Fr, It
        Assert.All(L.Map, kv =>
        {
            Assert.Equal(langs, kv.Value.Length);
            Assert.All(kv.Value, s => Assert.False(string.IsNullOrWhiteSpace(s), $"'{kv.Key}' tiene una traducción vacía"));
        });
    }

    [Theory]
    [InlineData("es", AppLang.Es)]
    [InlineData("en", AppLang.En)]
    [InlineData("pt", AppLang.Pt)]
    [InlineData("fr", AppLang.Fr)]
    [InlineData("it", AppLang.It)]
    [InlineData("EN", AppLang.En)]      // sin distinción de mayúsculas
    [InlineData("xx", AppLang.Es)]      // desconocido → Es
    [InlineData(null, AppLang.Es)]
    public void FromCode_MapsLanguage(string? code, AppLang expected)
        => Assert.Equal(expected, L.FromCode(code));

    [Fact]
    public void ToCode_RoundTripsWithFromCode()
        => Assert.All(Enum.GetValues<AppLang>(), lang => Assert.Equal(lang, L.FromCode(L.ToCode(lang))));

    [Theory]
    [InlineData("es-ES", AppLang.Es)]
    [InlineData("en-US", AppLang.En)]
    [InlineData("pt-BR", AppLang.Pt)]
    [InlineData("fr-FR", AppLang.Fr)]
    [InlineData("it-IT", AppLang.It)]
    [InlineData("fr", AppLang.Fr)]        // solo idioma, sin región
    [InlineData("DE-de", AppLang.Es)]     // idioma no soportado → Es
    [InlineData("", AppLang.Es)]
    [InlineData(null, AppLang.Es)]
    public void FromCulture_MapsLanguagePart(string? culture, AppLang expected)
        => Assert.Equal(expected, L.FromCulture(culture));

    /// <summary>
    /// Los botones de los diálogos son las cadenas que más se ven en toda la aplicación: los usan
    /// <c>MainWindow</c>, <c>SearchWindow</c>, <c>UninstallWindow</c> y <c>CleanupWindow</c> a través de
    /// <c>WindowDialogHelper</c>. Hasta la auditoría del 2026-08-20 estaban cableados en español, así que
    /// un usuario con la interfaz en francés confirmaba un borrado pulsando «Sí» — y «No» ni siquiera es
    /// una palabra francesa. Desde F-04 las confirmaciones ya no dicen «Sí / No» en ningún idioma: el primario
    /// es el verbo de la acción y el cierre, «Cancelar».
    /// </summary>
    /// <remarks>
    /// Se comprueba el diccionario directamente en vez de mover <see cref="L.Current"/> con
    /// <see cref="L.Set"/>: el idioma es estado global y estático, y xUnit ejecuta las clases de test en
    /// paralelo, así que cambiarlo aquí volvería intermitentes los tests de otras clases.
    /// </remarks>
    [Theory]
    [InlineData(AppLang.Es, "btn.accept", "Aceptar")]
    [InlineData(AppLang.En, "btn.accept", "OK")]
    [InlineData(AppLang.Fr, "btn.accept", "OK")]
    [InlineData(AppLang.Es, "btn.cancel", "Cancelar")]
    [InlineData(AppLang.En, "btn.cancel", "Cancel")]
    [InlineData(AppLang.Pt, "btn.cancel", "Cancelar")]
    [InlineData(AppLang.Fr, "btn.cancel", "Annuler")]
    [InlineData(AppLang.It, "btn.cancel", "Annulla")]
    [InlineData(AppLang.Es, "uninstall.confirmPrimary", "Desinstalar")]
    [InlineData(AppLang.Fr, "uninstall.confirmPrimary", "Désinstaller")]
    [InlineData(AppLang.Es, "cleanup.confirmPrimary", "Eliminar")]
    [InlineData(AppLang.It, "cleanup.confirmPrimary", "Elimina")]
    public void DialogButtons_AreTranslatedInEveryLanguage(AppLang lang, string key, string expected)
    {
        Assert.True(L.Map.ContainsKey(key), $"Falta la clave '{key}' en el diccionario.");
        Assert.Equal(expected, L.Map[key][(int)lang]);
    }
}
