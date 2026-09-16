using System.Text.RegularExpressions;
using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Todo diálogo de la app pasa por <c>WindowDialogHelper.Prepare</c> (F-03).
/// </summary>
/// <remarks>
/// <para>
/// Un <c>ContentDialog</c> no hereda el <c>RequestedTheme</c> de la ventana. Tres diálogos lo copiaban a
/// mano y el resto —todas las confirmaciones y errores de <c>WindowDialogHelper</c>— no: con la app en Claro
/// sobre Windows oscuro salían oscuros (comprobado en la app real el 2026-09-14). El fallo no era de un
/// diálogo sino de que cada sitio que crea uno tenía que acordarse; por eso se fija la regla y no un caso.
/// </para>
/// <para>
/// Lee el código fuente, como <see cref="LocalizationUsageTests"/>: «este diálogo se preparó» solo existe
/// en el texto de la llamada.
/// </para>
/// </remarks>
public sealed class DialogPreparationTests
{
    [Fact]
    public void EveryDialog_IsCreatedInsidePrepare()
    {
        var creation = new Regex(@"new\s+(?<type>\w*Dialog)\s*[({]");
        var unprepared = new List<string>();
        int found = 0;

        foreach (var (file, code) in SourceFiles())
        {
            foreach (Match m in creation.Matches(code))
            {
                found++;
                if (!code[..m.Index].TrimEnd().EndsWith("Prepare(", StringComparison.Ordinal))
                    unprepared.Add($"{m.Groups["type"].Value} en {file}:{LineOf(code, m.Index)}");
            }
        }

        // Acerca de, Licencia, Novedades, exportar a winget, error al guardar ajustes y los dos del helper.
        Assert.True(found >= 7, $"Solo se encontraron {found} creaciones de diálogo: el escaneo no está viendo el código.");
        Assert.True(
            unprepared.Count == 0,
            "Diálogos creados sin WindowDialogHelper.Prepare (no seguirían el tema elegido): "
                + string.Join(", ", unprepared));
    }

    /// <summary>
    /// Anclar un diálogo a mano (<c>XamlRoot = ...</c>) es la forma de saltarse <c>Prepare</c> sin crear
    /// uno nuevo, p. ej. reutilizando una instancia. La única asignación está dentro de <c>Prepare</c>.
    /// </summary>
    [Fact]
    public void XamlRoot_IsOnlyAssignedInsidePrepare()
    {
        var assignment = new Regex(@"\bXamlRoot\s*=[^=]");

        var assignments = SourceFiles()
            .SelectMany(f => assignment.Matches(f.Code).Select(m => $"{f.File}:{LineOf(f.Code, m.Index)}"))
            .ToList();

        Assert.Single(assignments);
        Assert.StartsWith("WindowDialogHelper.cs:", assignments[0], StringComparison.Ordinal);
    }

    private static int LineOf(string code, int index) => code.AsSpan(0, index).Count('\n') + 1;

    private static IEnumerable<(string File, string Code)> SourceFiles()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WingetUSoft.slnx")))
            dir = dir.Parent;

        Assert.True(dir is not null, "No se encontró la raíz del repositorio (WingetUSoft.slnx) desde " + AppContext.BaseDirectory);

        string src = Path.Combine(dir!.FullName, "src", "WingetUSoft");
        char sep = Path.DirectorySeparatorChar;

        return Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{sep}obj{sep}", StringComparison.Ordinal)
                     && !f.Contains($"{sep}bin{sep}", StringComparison.Ordinal))
            .Select(f => (Path.GetFileName(f), File.ReadAllText(f)));
    }
}
