using System.Text.RegularExpressions;
using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Comprueba que **toda clave usada en el código existe en el diccionario**.
/// </summary>
/// <remarks>
/// Nace de un bug real cometido en el Tier E: se añadió <c>L.T("ctx.include")</c> a un menú sin dar de
/// alta la clave. No rompió el build ni ningún test — <see cref="L.T"/> devuelve **la propia clave**
/// cuando no la conoce, así que el usuario habría visto el literal "ctx.include" en el menú, y solo se
/// habría descubierto abriendo esa ventana a mano. <c>LocalizationTests</c> no podía verlo: comprueba
/// que las claves *dadas de alta* tengan sus 5 traducciones, no que las *usadas* estén dadas de alta.
///
/// Por eso este test lee el código fuente en vez de reflexionar sobre el ensamblado: la relación
/// "clave usada" solo existe en el texto de las llamadas.
/// </remarks>
public sealed class LocalizationUsageTests
{
    /// <summary>Claves construidas en tiempo de ejecución (no literales), que este test no puede resolver.</summary>
    private static readonly HashSet<string> DynamicKeysAllowed = [];

    [Fact]
    public void EveryKeyUsedInCode_ExistsInTheDictionary()
    {
        string sourceRoot = FindSourceRoot();
        var used = new SortedDictionary<string, string>(StringComparer.Ordinal);

        // Captura L.T("clave") y L.T("clave", args...) — el literal siempre va primero.
        var callPattern = new Regex(@"L\.T\(\s*""([^""]+)""", RegexOptions.Compiled);

        foreach (string file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            // El propio diccionario no "usa" claves: las declara.
            if (Path.GetFileName(file) == "Localization.cs") continue;
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)) continue;
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)) continue;

            string code = File.ReadAllText(file);
            foreach (Match m in callPattern.Matches(code))
                used[m.Groups[1].Value] = Path.GetFileName(file);
        }

        Assert.NotEmpty(used);   // si el escaneo no encuentra nada, el test no está probando nada

        var missing = used
            .Where(kv => !DynamicKeysAllowed.Contains(kv.Key) && !L.Map.ContainsKey(kv.Key))
            .Select(kv => $"{kv.Key}  (usada en {kv.Value})")
            .ToList();

        Assert.True(
            missing.Count == 0,
            "Claves usadas con L.T() que NO están en L.Map (se mostrarían literalmente al usuario):\n  "
                + string.Join("\n  ", missing));
    }

    /// <summary>
    /// Ningún texto de botón de <c>ContentDialog</c> puede ser un literal en el código.
    /// </summary>
    /// <remarks>
    /// El test de arriba comprueba que las claves usadas existan; este comprueba lo contrario, que es como
    /// se coló el bug real: <c>WindowDialogHelper</c> no usaba ninguna clave para «Aceptar», «Sí» y «No»
    /// — los tenía cableados en español. No falta ninguna clave, así que nada lo delataba, y sin embargo
    /// esos tres botones aparecían en español en los cinco idiomas y en prácticamente todos los diálogos
    /// de la aplicación.
    /// </remarks>
    [Fact]
    public void NoDialogButtonText_IsAHardcodedLiteral()
    {
        string uiDirectory = Path.Combine(FindSourceRoot(), "WingetUSoft", "UI");
        Assert.True(Directory.Exists(uiDirectory), "No existe el directorio de UI: " + uiDirectory);

        // Asignaciones del tipo  CloseButtonText = "Aceptar"  (con L.T(...) el siguiente carácter es 'L').
        var literalPattern = new Regex(
            @"(Primary|Secondary|Close)ButtonText\s*=\s*""", RegexOptions.Compiled);

        var offenders = new List<string>();
        int scanned = 0;

        foreach (string file in Directory.EnumerateFiles(uiDirectory, "*.cs", SearchOption.AllDirectories))
        {
            string code = File.ReadAllText(file);
            if (!code.Contains("ButtonText", StringComparison.Ordinal)) continue;

            scanned++;
            foreach (Match m in literalPattern.Matches(code))
                offenders.Add($"{Path.GetFileName(file)}: {m.Value.Trim()}…");
        }

        Assert.True(scanned >= 4, $"El escaneo solo encontró {scanned} archivos con botones de diálogo; se esperaban al menos 4.");
        Assert.True(offenders.Count == 0,
            "Los textos de los botones deben salir de L.T(), no de un literal (se verían en español en los "
                + "cinco idiomas):\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>
    /// Sube desde el directorio del test hasta la raíz del repo (la que tiene <c>WingetUSoft.slnx</c>) y
    /// devuelve <c>src</c>. Falla con un mensaje claro si no la encuentra: un test que "pasa" porque no
    /// pudo leer el código no probaría nada.
    /// </summary>
    private static string FindSourceRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WingetUSoft.slnx")))
            dir = dir.Parent;

        Assert.True(dir is not null, "No se encontró la raíz del repositorio (WingetUSoft.slnx) desde " + AppContext.BaseDirectory);

        string src = Path.Combine(dir!.FullName, "src");
        Assert.True(Directory.Exists(src), "No existe el directorio de código: " + src);
        return src;
    }
}
