using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Fechas y nombres de archivo que deben seguir al idioma elegido en la app (T2-09, T2-10), no a la
/// configuración regional de Windows. Se usan las sobrecargas puras con <see cref="AppLang"/> explícito:
/// <see cref="L.Current"/> es estado estático global y xUnit ejecuta las clases en paralelo.
/// </summary>
public sealed class CultureFormattingTests
{
    [Theory]
    [InlineData(AppLang.Es, "es-ES")]
    [InlineData(AppLang.En, "en-US")]
    [InlineData(AppLang.Pt, "pt-BR")]
    [InlineData(AppLang.Fr, "fr-FR")]
    [InlineData(AppLang.It, "it-IT")]
    public void CultureFor_MapsEachLanguageToItsCulture(AppLang lang, string expected)
        => Assert.Equal(expected, L.CultureFor(lang).Name);

    /// <summary>
    /// El caso que motivó la tarea: 03/07 no significa lo mismo en español que en inglés. Con la app en
    /// inglés el mes va primero, y con el resto de idiomas el día.
    /// </summary>
    [Fact]
    public void FormatDateTime_PutsTheMonthFirstOnlyInEnglish()
    {
        var date = new DateTime(2026, 7, 3, 14, 5, 0);

        string english = L.FormatDateTime(date, AppLang.En);
        Assert.StartsWith("07/03/2026", english);

        foreach (var lang in new[] { AppLang.Es, AppLang.Pt, AppLang.Fr, AppLang.It })
            Assert.StartsWith("03/07/2026", L.FormatDateTime(date, lang));
    }

    /// <summary>La hora también forma parte del formato: si desapareciera, el historial perdería información.</summary>
    [Theory]
    [InlineData(AppLang.Es)]
    [InlineData(AppLang.En)]
    [InlineData(AppLang.Pt)]
    [InlineData(AppLang.Fr)]
    [InlineData(AppLang.It)]
    public void FormatDateTime_IncludesTheTime(AppLang lang)
    {
        string text = L.FormatDateTime(new DateTime(2026, 7, 3, 14, 5, 0), lang);
        Assert.Matches(@"(14|02)[:.]05", text);   // 24 h en la mayoría, 02:05 PM en en-US
    }

    [Theory]
    [InlineData("export.fileUpdates")]
    [InlineData("export.filePackages")]
    [InlineData("export.fileHistory")]
    public void ExportFileName_IsSafeAndDateSuffixedInEveryLanguage(string key)
    {
        Assert.True(L.Map.ContainsKey(key), $"'{key}' ya no existe en el diccionario");

        foreach (var lang in Enum.GetValues<AppLang>())
        {
            string name = L.ExportFileName(key, new DateTime(2026, 7, 3), lang);

            // Sin acentos, espacios ni caracteres prohibidos en Windows: es un nombre de fichero.
            Assert.Matches(@"^[a-z0-9\-]+_2026-07-03$", name);
        }
    }

    /// <summary>
    /// La fecha del nombre va en formato invariante y ordenable, no en el de la cultura: si siguiera a la
    /// cultura, las barras de <c>dd/MM/yyyy</c> serían separadores de ruta.
    /// </summary>
    [Fact]
    public void ExportFileName_UsesTheSameSortableDateInEveryLanguage()
    {
        var date = new DateTime(2026, 7, 3);
        var suffixes = Enum.GetValues<AppLang>()
            .Select(lang => L.ExportFileName("export.fileHistory", date, lang).Split('_')[^1])
            .Distinct();

        Assert.Equal("2026-07-03", Assert.Single(suffixes));
    }

    /// <summary>Los tres prefijos son distintos entre sí dentro de cada idioma: si no, exportar el historial
    /// propondría el mismo nombre que exportar las actualizaciones.</summary>
    [Fact]
    public void ExportFileName_PrefixesAreDistinctWithinEachLanguage()
    {
        string[] keys = ["export.fileUpdates", "export.filePackages", "export.fileHistory"];

        foreach (var lang in Enum.GetValues<AppLang>())
        {
            var names = keys.Select(k => L.ExportFileName(k, new DateTime(2026, 7, 3), lang)).ToArray();
            Assert.Equal(keys.Length, names.Distinct().Count());
        }
    }
}
