using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Parseo de <c>winget search</c>. Las entradas son **salida real** de winget 1.29.280 (Windows en
/// español), no inventada: la columna "Coincidencia" aparece o no según cómo casó cada paquete, que es
/// justo lo que rompería un parser que mapeara columnas por índice fijo.
/// </summary>
public sealed class WingetSearchParserTests
{
    /// <summary>Búsqueda por Id exacto: 4 columnas, sin "Coincidencia".</summary>
    private const string FourColumnOutput =
        "Nombre Id      Versión  Origen\n" +
        "-------------------------------\n" +
        "Git    Git.Git 2.55.0.2 winget\n";

    /// <summary>Búsqueda libre: winget añade "Coincidencia" (Tag/Moniker) y la tabla pasa a 5 columnas.</summary>
    private const string FiveColumnOutput =
        "Nombre                             Id                        Versión         Coincidencia  Origen\n" +
        "-------------------------------------------------------------------------------------------------\n" +
        "7-Zip                              7zip.7zip                 26.02           Moniker: 7zip winget\n" +
        "NanaZip                            M2Team.NanaZip            6.5.1767.0      Tag: 7zip     winget\n" +
        "7zr                                7zip.7zr                  26.02                         winget\n";

    [Fact]
    public void Parse_FourColumns_MapsSourceFromLastColumn()
    {
        var results = WingetSearchParser.Parse(FourColumnOutput);

        var git = Assert.Single(results);
        Assert.Equal("Git", git.Name);
        Assert.Equal("Git.Git", git.Id);
        Assert.Equal("2.55.0.2", git.Version);
        Assert.Equal("winget", git.Source);
    }

    /// <summary>
    /// El caso que rompe un parser ingenuo: con la columna "Coincidencia" intercalada, el origen ya no
    /// está en el índice 3. Si se mapeara por índice fijo, la fuente saldría "Moniker: 7zip".
    /// </summary>
    [Fact]
    public void Parse_FiveColumns_DoesNotMistakeMatchColumnForSource()
    {
        var results = WingetSearchParser.Parse(FiveColumnOutput);

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.Equal("winget", r.Source));

        var sevenZip = results[0];
        Assert.Equal("7-Zip", sevenZip.Name);
        Assert.Equal("7zip.7zip", sevenZip.Id);
        Assert.Equal("26.02", sevenZip.Version);
    }

    /// <summary>Una fila puede traer la celda "Coincidencia" vacía (7zr): no debe descolocar el resto.</summary>
    [Fact]
    public void Parse_RowWithEmptyMatchCell_StillParses()
    {
        var results = WingetSearchParser.Parse(FiveColumnOutput);

        var sevenZr = Assert.Single(results, r => r.Id == "7zip.7zr");
        Assert.Equal("26.02", sevenZr.Version);
        Assert.Equal("winget", sevenZr.Source);
    }

    /// <summary>Sin coincidencias, winget imprime un texto suelto (traducido) y ninguna tabla.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("No se encontró ningún paquete que coincida con los criterios de entrada.")]
    public void Parse_NoTable_ReturnsEmpty(string? output)
        => Assert.Empty(WingetSearchParser.Parse(output));

    /// <summary>Las barras de progreso y avisos que winget cuela alrededor de la tabla no son paquetes.</summary>
    [Fact]
    public void Parse_IgnoresNonPackageLines()
    {
        string noisy =
            "Nombre Id      Versión  Origen\n" +
            "-------------------------------\n" +
            "Git    Git.Git 2.55.0.2 winget\n" +
            "Se requiere aceptar los términos de la fuente para continuar\n";

        var results = WingetSearchParser.Parse(noisy);

        Assert.Single(results);
        Assert.Equal("Git.Git", results[0].Id);
    }

    /// <summary>
    /// La cabecera va traducida al idioma de Windows, así que el parser no puede buscar "Name"/"Source":
    /// localiza las columnas por posición. Misma tabla en inglés → mismo resultado.
    /// </summary>
    [Fact]
    public void Parse_EnglishHeader_WorksToo()
    {
        string english =
            "Name Id      Version  Source\n" +
            "----------------------------\n" +
            "Git  Git.Git 2.55.0.2 winget\n";

        var git = Assert.Single(WingetSearchParser.Parse(english));
        Assert.Equal("Git.Git", git.Id);
        Assert.Equal("winget", git.Source);
    }
}
