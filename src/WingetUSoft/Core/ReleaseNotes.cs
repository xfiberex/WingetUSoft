using System.Text;
using System.Text.RegularExpressions;

namespace WingetUSoft;

/// <summary>Un bloque de las notas ya convertido: un párrafo, una viñeta o un encabezado.</summary>
/// <param name="Text">Texto plano del bloque, sin marcas de Markdown.</param>
/// <param name="IsHeading">True si en el Markdown era un encabezado (<c>#</c>): el diálogo lo resalta.</param>
public readonly record struct ReleaseNoteBlock(string Text, bool IsHeading);

/// <summary>
/// Conversión de las notas de versión (Markdown de GitHub Releases) a texto plano legible para
/// mostrarlas en el diálogo de novedades. Lógica pura y testeable (sin red ni UI).
/// </summary>
public static partial class ReleaseNotes
{
    [GeneratedRegex(@"^\s{0,3}#{1,6}\s+")]             private static partial Regex HeadingRegex();
    [GeneratedRegex(@"^(\s*)(?:[-*+]|\d{1,3}[.)])\s+")] private static partial Regex ListItemRegex();
    [GeneratedRegex(@"^(\s*)[-*+]\s+")]                 private static partial Regex BulletRegex();
    [GeneratedRegex(@"\[([^\]]+)\]\([^)]*\)")]          private static partial Regex LinkRegex();
    [GeneratedRegex(@"(?<![\w*])\*(?![\s*])([^*\n]+?)(?<![\s*])\*(?![\w*])")] private static partial Regex ItalicRegex();

    /// <summary>
    /// Convierte Markdown a texto plano: quita marcadores de encabezado (<c>#</c>), normaliza viñetas
    /// (<c>-</c>/<c>*</c>/<c>+</c> → <c>•</c>), elimina negritas, cursivas y comillas de código, reduce
    /// enlaces <c>[texto](url)</c> a su texto y colapsa líneas en blanco repetidas.
    /// Devuelve cadena vacía si la entrada es nula o en blanco.
    /// </summary>
    /// <param name="markdown">Cuerpo Markdown de la versión (campo <c>body</c> del release).</param>
    /// <param name="version">Ver <see cref="ToBlocks"/>.</param>
    public static string ToPlainText(string? markdown, string? version = null) =>
        string.Join('\n', ToBlocks(markdown, version).Select(b => b.Text)).Trim();

    /// <summary>
    /// Divide las notas en bloques (párrafos, viñetas y encabezados), con un bloque vacío donde va una línea en
    /// blanco. El diálogo de novedades los usa para resaltar los encabezados.
    /// </summary>
    /// <remarks>
    /// Las líneas consecutivas de un mismo párrafo o de una misma viñeta se <b>vuelven a unir</b>, como hace
    /// cualquier visor de Markdown. Las notas se escriben con el texto partido a ~110 columnas, y conservar esos
    /// saltos en un diálogo de 380 px dejaba frases cortadas a la mitad y sangrías sueltas (crítica de diseño,
    /// 2026-11-20). Un párrafo nuevo empieza solo tras una línea en blanco, un encabezado o una viñeta. La línea
    /// en blanco que sigue a un encabezado se omite: el encabezado va pegado a lo que rotula y separado de lo
    /// anterior.
    /// </remarks>
    /// <param name="markdown">Cuerpo Markdown de la versión (campo <c>body</c> del release).</param>
    /// <param name="version">
    /// Versión que ya rotula el diálogo (p. ej. <c>1.11.0</c>). Si el primer encabezado la repite
    /// («WingetUSoft v1.11.0»), se omite para no decir lo mismo dos veces.
    /// </param>
    public static IReadOnlyList<ReleaseNoteBlock> ToBlocks(string? markdown, string? version = null)
    {
        var blocks = new List<ReleaseNoteBlock>();
        if (string.IsNullOrWhiteSpace(markdown)) return blocks;

        string normalized = markdown.Replace("\r\n", "\n").Replace('\r', '\n');
        var paragraph = new StringBuilder();
        bool firstHeadingSeen = false;

        void Flush()
        {
            if (paragraph.Length > 0) blocks.Add(new ReleaseNoteBlock(paragraph.ToString(), false));
            paragraph.Clear();
        }

        bool LastIsBlankOrHeading() => blocks.Count == 0 || blocks[^1].Text.Length == 0 || blocks[^1].IsHeading;

        foreach (string raw in normalized.Split('\n'))
        {
            string line = Inline(raw);

            if (line.Trim().Length == 0)
            {
                Flush();
                if (!LastIsBlankOrHeading()) blocks.Add(new ReleaseNoteBlock("", false));
                continue;
            }

            if (HeadingRegex().IsMatch(line))
            {
                Flush();
                string heading = HeadingRegex().Replace(line, "").Trim();
                bool repeatsVersion = !firstHeadingSeen && blocks.Count == 0
                    && !string.IsNullOrEmpty(version) && heading.Contains(version, StringComparison.OrdinalIgnoreCase);
                firstHeadingSeen = true;
                if (!repeatsVersion) blocks.Add(new ReleaseNoteBlock(heading, true));
                continue;
            }

            if (ListItemRegex().IsMatch(line))
            {
                Flush();
                paragraph.Append(BulletRegex().Replace(line, "$1• ").TrimEnd());
                continue;
            }

            // Continuación del párrafo o de la viñeta en curso.
            if (paragraph.Length > 0) paragraph.Append(' ').Append(line.Trim());
            else paragraph.Append(line.Trim());
        }

        Flush();
        while (blocks.Count > 0 && blocks[^1].Text.Length == 0) blocks.RemoveAt(blocks.Count - 1);
        return blocks;
    }

    private static string Inline(string line)
    {
        line = LinkRegex().Replace(line, "$1");
        line = line.Replace("**", "").Replace("__", "").Replace("`", "");
        return ItalicRegex().Replace(line, "$1").TrimEnd();
    }
}
