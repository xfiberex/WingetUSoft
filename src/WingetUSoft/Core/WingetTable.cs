namespace WingetUSoft;

/// <summary>
/// Parseo genérico de las tablas de ancho fijo que imprime winget (<c>search</c>, <c>list</c>,
/// <c>upgrade</c>…). Lógica pura y testeable.
/// </summary>
/// <remarks>
/// <para>
/// <b>No se puede parsear por nombre de columna:</b> winget traduce sus cabeceras al idioma de Windows
/// ("Nombre/Id/Versión/Origen" en español, "Name/Id/Version/Source" en inglés) — la misma trampa que
/// destapó el Tier C con las etiquetas de <c>winget show</c>. Por eso las columnas se localizan por
/// <b>posición</b>: se busca la línea de guiones que separa la cabecera de los datos, y las columnas
/// arrancan donde arranca cada palabra de la cabecera. Eso funciona en cualquier idioma.
/// </para>
/// <para>
/// Tampoco se puede asumir un <b>número fijo de columnas</b>: <c>winget search</c> añade la columna
/// "Coincidencia" solo cuando el paquete casó por tag o moniker, así que la misma consulta devuelve
/// tablas de 4 o 5 columnas según los resultados. Quien consuma esto debe mapear por posición relativa
/// (la primera y la última son estables), no por índice absoluto.
/// </para>
/// </remarks>
public static class WingetTable
{
    /// <summary>
    /// Divide la salida en filas de celdas. Devuelve lista vacía si no hay tabla (p. ej. "no se
    /// encontró ningún paquete", que winget imprime como texto suelto y también traduce).
    /// </summary>
    public static List<string[]> Parse(string? output)
    {
        var rows = new List<string[]>();
        if (string.IsNullOrWhiteSpace(output))
            return rows;

        string[] lines = output.Replace("\r", "").Split('\n');

        int separatorIndex = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].Trim();
            if (trimmed.StartsWith("--", StringComparison.Ordinal) && trimmed.Length > 10)
            {
                separatorIndex = i;
                break;
            }
        }

        if (separatorIndex < 1)
            return rows;

        List<int> starts = GetColumnStarts(lines[separatorIndex - 1]);
        if (starts.Count < 2)
            return rows;

        for (int i = separatorIndex + 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var cells = new string[starts.Count];
            for (int c = 0; c < starts.Count; c++)
            {
                int start = starts[c];
                int end = c + 1 < starts.Count ? starts[c + 1] : line.Length;
                cells[c] = Cut(line, start, end);
            }
            rows.Add(cells);
        }

        return rows;
    }

    /// <summary>Posición donde empieza cada columna de la cabecera (primer carácter tras un espacio).</summary>
    private static List<int> GetColumnStarts(string header)
    {
        var starts = new List<int>();
        for (int i = 0; i < header.Length; i++)
        {
            if (!char.IsWhiteSpace(header[i]) && (i == 0 || char.IsWhiteSpace(header[i - 1])))
                starts.Add(i);
        }
        return starts;
    }

    /// <summary>Recorta [start, end) tolerando líneas más cortas que la cabecera (celdas finales vacías).</summary>
    private static string Cut(string line, int start, int end)
    {
        if (start >= line.Length) return "";
        if (end > line.Length) end = line.Length;
        if (end <= start) return "";
        return line[start..end].Trim();
    }
}
