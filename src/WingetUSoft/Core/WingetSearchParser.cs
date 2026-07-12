namespace WingetUSoft;

/// <summary>
/// Convierte la salida de <c>winget search</c> en resultados. Lógica pura y testeable.
/// </summary>
public static class WingetSearchParser
{
    /// <summary>
    /// Parsea la tabla de resultados. Las columnas se mapean por **posición relativa**, no por índice
    /// absoluto: winget intercala una columna "Coincidencia" (Tag/Moniker) solo cuando el paquete casó
    /// por ahí, así que la tabla tiene 4 o 5 columnas según la consulta. Lo estable es que el nombre, el
    /// Id y la versión son las tres primeras y el **origen es siempre la última**.
    /// </summary>
    public static List<WingetSearchResult> Parse(string? output)
    {
        var results = new List<WingetSearchResult>();

        foreach (string[] cells in WingetTable.Parse(output))
        {
            if (cells.Length < 4)
                continue;

            string name = cells[0];
            string id = cells[1];
            string version = cells[2];
            string source = cells[^1];

            if (!LooksLikeResult(name, id))
                continue;

            results.Add(new WingetSearchResult
            {
                Name = name,
                Id = id,
                Version = version,
                Source = source,
            });
        }

        return results;
    }

    /// <summary>
    /// Descarta las líneas que no son un paquete (barras de progreso, avisos que winget cuela bajo la
    /// tabla…). Un Id de winget nunca lleva espacios, y ese es el filtro que de verdad discrimina.
    /// </summary>
    private static bool LooksLikeResult(string name, string id) =>
        !string.IsNullOrWhiteSpace(name)
        && !string.IsNullOrWhiteSpace(id)
        && !id.Contains(' ', StringComparison.Ordinal);
}
