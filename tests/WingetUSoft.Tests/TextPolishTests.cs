using System.Xml.Linq;
using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Pulido de textos (F-20): plurales reales, un solo tratamiento en español, versiones desconocidas traducidas
/// y valores truncados alcanzables.
/// </summary>
/// <remarks>
/// La aplicación decía «Se encontraron 26 actualización(es) disponible(s)», «113 programa(s)» y «Unknown», y
/// mezclaba el tú del resto de la interfaz con el «¿Desea continuar?» de los diálogos.
/// </remarks>
public sealed class TextPolishTests
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>Ninguna traducción resuelve el plural con «(s)», «(es)» o «/i».</summary>
    [Fact]
    public void NoTranslation_FakesThePluralWithParentheses()
    {
        var offenders = L.Map
            .Where(kv => kv.Value.Any(v => v.Contains("(s)", StringComparison.Ordinal)
                                        || v.Contains("(es)", StringComparison.Ordinal)
                                        || v.Contains("(ns)", StringComparison.Ordinal)
                                        || v.Contains("(is)", StringComparison.Ordinal)
                                        || v.Contains("(ões)", StringComparison.Ordinal)
                                        || v.Contains("/i ", StringComparison.Ordinal)))
            .Select(kv => kv.Key)
            .ToList();

        Assert.True(offenders.Count == 0, "Claves con plural falso: " + string.Join(", ", offenders));
    }

    /// <summary>Cada clave con recuento tiene sus dos formas en los cinco idiomas.</summary>
    [Theory]
    [InlineData("search.found")]
    [InlineData("search.countLabel")]
    [InlineData("confirm.updateBody")]
    [InlineData("notif.updatedSuccess")]
    [InlineData("status.updatesFound")]
    [InlineData("settings.excludedCount")]
    [InlineData("uninstall.foundCount")]
    [InlineData("uninstall.countAll")]
    [InlineData("cleanup.foundResidues")]
    [InlineData("cleanup.confirmDeleteBody")]
    [InlineData("history.summaryAll")]
    [InlineData("history.summaryFiltered")]
    public void EveryCountingKey_HasBothForms(string key)
    {
        Assert.True(L.Map.ContainsKey(key + ".one"), key + ".one no existe");
        Assert.True(L.Map.ContainsKey(key + ".other"), key + ".other no existe");
        Assert.False(L.Map.ContainsKey(key), key + " debería haberse partido en .one y .other");
    }

    /// <summary>
    /// Reglas CLDR: en francés y en portugués de Brasil el cero va en singular; en español, inglés e italiano, no.
    /// </summary>
    [Theory]
    [InlineData(AppLang.Es, 0, false)]
    [InlineData(AppLang.Es, 1, true)]
    [InlineData(AppLang.Es, 2, false)]
    [InlineData(AppLang.En, 0, false)]
    [InlineData(AppLang.It, 0, false)]
    [InlineData(AppLang.Fr, 0, true)]
    [InlineData(AppLang.Fr, 1, true)]
    [InlineData(AppLang.Fr, 2, false)]
    [InlineData(AppLang.Pt, 0, true)]
    [InlineData(AppLang.Pt, 2, false)]
    public void PluralRule_FollowsCldr(AppLang lang, int count, bool singular)
        => Assert.Equal(singular, L.UsesSingular(lang, count));

    [Fact]
    public void P_PicksTheFormThatMatchesTheCount()
    {
        var original = L.Current;
        try
        {
            L.Set(AppLang.Es);
            Assert.Equal("1 programa", L.P("uninstall.countAll", 1, 1));
            Assert.Equal("113 programas", L.P("uninstall.countAll", 113, 113));
        }
        finally { L.Set(original); }
    }

    /// <summary>El español tutea en toda la interfaz, también en los diálogos.</summary>
    [Fact]
    public void Spanish_UsesASingleRegister()
    {
        string[] formal = ["Desea", "desea ", "Verifique", "Seleccione", "Pulse ", "Compruebe su", "Elija ", "Asegúrese"];

        var offenders = L.Map
            .Where(kv => formal.Any(f => kv.Value[(int)AppLang.Es].Contains(f, StringComparison.Ordinal)))
            .Select(kv => kv.Key)
            .ToList();

        Assert.True(offenders.Count == 0, "Claves en español con tratamiento de usted: " + string.Join(", ", offenders));
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData("   ")]
    public void UnknownVersion_IsTranslated(string raw)
        => Assert.Equal(L.T("version.unknown"), L.VersionText(raw));

    [Fact]
    public void RealVersion_IsLeftAlone() => Assert.Equal("1.9.0", L.VersionText("1.9.0"));

    /// <summary>Nombre e Id se recortan en las tres tablas, así que el valor entero está en el tooltip.</summary>
    [Theory]
    [InlineData("MainWindow.xaml")]
    [InlineData("UninstallWindow.xaml")]
    [InlineData("SearchWindow.xaml")]
    public void TruncatedCells_ShowTheWholeValueInATooltip(string file)
    {
        var doc = XDocument.Load(Path.Combine(UiDirectory(), file));

        foreach (string binding in new[] { "Name", "Id" })
        {
            var cell = doc.Descendants(Presentation + "TextBlock")
                .FirstOrDefault(e => (string?)e.Attribute("Text") == "{Binding " + binding + "}");

            Assert.True(cell is not null, $"{file}: no se encontró la celda {binding}");
            Assert.Equal("{Binding " + binding + "}", (string?)cell!.Attribute("ToolTipService.ToolTip"));
        }
    }

    /// <summary>La columna de estado ya no se abrevia: es un icono con su nombre completo.</summary>
    [Fact]
    public void StatusColumnHeader_IsAnIcon_NotAnAbbreviation()
    {
        var doc = XDocument.Load(Path.Combine(UiDirectory(), "MainWindow.xaml"));

        var header = doc.Descendants(Presentation + "FontIcon")
            .SingleOrDefault(e => (string?)e.Attribute(Xaml + "Name") == "colExcl");

        Assert.True(header is not null, "colExcl debería ser un FontIcon.");
        Assert.DoesNotContain("Excl.", L.Map["list.colExcluded"]);
    }

    private static string UiDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WingetUSoft.slnx")))
            dir = dir.Parent;

        Assert.True(dir is not null, "No se encontró la raíz del repositorio (WingetUSoft.slnx) desde " + AppContext.BaseDirectory);
        return Path.Combine(dir!.FullName, "src", "WingetUSoft", "UI");
    }
}
