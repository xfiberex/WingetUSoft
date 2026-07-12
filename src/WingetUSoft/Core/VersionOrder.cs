namespace WingetUSoft;

/// <summary>
/// Comparación de versiones tal y como las imprime winget. Ordenarlas como texto da resultados
/// falsos ("1.10.0" &lt; "1.9.0", porque '1' &lt; '9'), así que se comparan segmento a segmento y los
/// segmentos numéricos se comparan como números. Lógica pura y testeable (sin estado ni efectos).
/// </summary>
public static class VersionOrder
{
    /// <summary>
    /// Compara dos versiones. Devuelve &lt;0 si <paramref name="a"/> es anterior, &gt;0 si es posterior,
    /// 0 si son equivalentes.
    /// </summary>
    /// <remarks>
    /// winget no siempre devuelve una versión limpia:
    /// <list type="bullet">
    ///   <item>"&lt; 13.5.0.359" cuando solo conoce una cota superior.</item>
    ///   <item>"Unknown" / cadena vacía cuando no la conoce.</item>
    ///   <item>Sufijos no numéricos ("1.2.3-beta", "2.55.0.windows.2").</item>
    /// </list>
    /// Las versiones desconocidas ordenan al final; los prefijos "&lt;" se ignoran para comparar el
    /// número, y solo desempatan si el resto es idéntico (una cota es "menor" que la versión exacta).
    /// </remarks>
    public static int Compare(string? a, string? b)
    {
        bool aUnknown = IsUnknown(a);
        bool bUnknown = IsUnknown(b);
        if (aUnknown || bUnknown)
            return aUnknown && bUnknown ? 0 : aUnknown ? 1 : -1;   // lo desconocido, al final

        string va = Strip(a!, out bool aBounded);
        string vb = Strip(b!, out bool bBounded);

        string[] pa = va.Split('.');
        string[] pb = vb.Split('.');

        for (int i = 0; i < Math.Max(pa.Length, pb.Length); i++)
        {
            // "1.2" frente a "1.2.1": el que se queda sin segmentos es el menor.
            string sa = i < pa.Length ? pa[i] : "";
            string sb = i < pb.Length ? pb[i] : "";

            int cmp = CompareSegment(sa, sb);
            if (cmp != 0) return cmp;
        }

        // Mismo número: "< 1.2" es una cota, no la versión exacta, así que va antes.
        return aBounded == bBounded ? 0 : aBounded ? -1 : 1;
    }

    /// <summary>Comparador listo para <c>OrderBy</c>.</summary>
    public static IComparer<string?> Comparer { get; } = Comparer<string?>.Create(Compare);

    private static int CompareSegment(string a, string b)
    {
        bool aNum = long.TryParse(a, out long na);
        bool bNum = long.TryParse(b, out long nb);

        if (aNum && bNum) return na.CompareTo(nb);

        // Un segmento numérico va después de uno que no lo es ("1.2" > "1.2-beta"), como en SemVer:
        // una preliberación precede a la versión final.
        if (aNum != bNum) return aNum ? 1 : -1;

        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnknown(string? v) =>
        string.IsNullOrWhiteSpace(v) || v.Trim().Equals("Unknown", StringComparison.OrdinalIgnoreCase);

    /// <summary>Quita el prefijo "&lt;" que winget antepone cuando solo conoce una cota superior.</summary>
    private static string Strip(string v, out bool bounded)
    {
        string t = v.Trim();
        bounded = t.StartsWith('<');
        return bounded ? t[1..].Trim() : t;
    }
}
