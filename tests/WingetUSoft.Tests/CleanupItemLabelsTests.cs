using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Etiquetas accesibles de la ventana de limpieza (T1-09).
/// </summary>
/// <remarks>
/// Se cubren aquí y no en los UI tests porque <c>CleanupWindow</c> **no es alcanzable** conduciendo la
/// app: solo se abre desde <c>UninstallWindow</c> después de desinstalar un programa de verdad, y
/// ningún test desinstala nada del equipo. Lo que sí se puede fijar es la composición de las etiquetas,
/// que es donde estaba el fallo: la casilla que decide qué carpetas se borran de forma recursiva se
/// anunciaba solo como «casilla de verificación», sin ruta.
/// </remarks>
public sealed class CleanupItemLabelsTests
{
    private static CleanupItemViewModel Item(string path, bool isDirectory = true) => new()
    {
        Path = path,
        IsDirectory = isDirectory,
        DisplaySize = "12,3 MB",
        PackageName = "Un Programa"
    };

    [Fact]
    public void SelectLabel_NamesThePathBeingDeleted()
    {
        string label = Item(@"C:\Users\yo\AppData\Roaming\UnPrograma").SelectLabel;

        Assert.Contains(@"C:\Users\yo\AppData\Roaming\UnPrograma", label, StringComparison.Ordinal);
        Assert.NotEqual(@"C:\Users\yo\AppData\Roaming\UnPrograma", label);   // no es solo la ruta suelta
    }

    /// <summary>La fila se anuncia con ruta, tipo, tamaño y programa — no con el tipo .NET del ViewModel.</summary>
    [Fact]
    public void RowLabel_CarriesPathTypeSizeAndProgram()
    {
        var item = Item(@"C:\Program Files\UnPrograma");
        string label = item.RowLabel;

        Assert.Contains(@"C:\Program Files\UnPrograma", label, StringComparison.Ordinal);
        Assert.Contains(item.TypeLabel, label, StringComparison.Ordinal);
        Assert.Contains("12,3 MB", label, StringComparison.Ordinal);
        Assert.Contains("Un Programa", label, StringComparison.Ordinal);
        Assert.DoesNotContain("CleanupItemViewModel", label, StringComparison.Ordinal);
    }

    /// <summary>Carpeta y archivo no se anuncian igual: uno se borra recursivamente y el otro no.</summary>
    [Fact]
    public void RowLabel_DistinguishesFolderFromFile()
    {
        Assert.NotEqual(
            Item(@"C:\x", isDirectory: true).RowLabel,
            Item(@"C:\x", isDirectory: false).RowLabel);
    }

    /// <summary>
    /// Las etiquetas salen del **diccionario**, no de un literal: así siguen al idioma activo.
    /// </summary>
    /// <remarks>
    /// Se comprueba contra <c>L.Map</c> en vez de mover <c>L.Current</c> con <c>L.Set</c>, por la misma
    /// razón que ya documenta <see cref="LocalizationTests"/>: el idioma es estado global y estático, y
    /// xUnit ejecuta las clases de test en paralelo — cambiarlo aquí haría fallar a otra clase de forma
    /// intermitente.
    /// </remarks>
    [Fact]
    public void Labels_ComeFromTheDictionary_SoTheyFollowTheActiveLanguage()
    {
        int lang = (int)L.Current;
        var item = Item(@"C:\x");

        Assert.Equal(
            string.Format(L.Map["cleanup.selectAccessible"][lang], item.Path),
            item.SelectLabel);

        Assert.Equal(
            string.Format(L.Map["cleanup.rowAccessible"][lang],
                item.Path, item.TypeLabel, item.DisplaySize, item.PackageName),
            item.RowLabel);
    }
}
