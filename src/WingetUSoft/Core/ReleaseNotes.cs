using System.Text;
using System.Text.RegularExpressions;

namespace WingetUSoft;

/// <summary>
/// Conversión de las notas de versión (Markdown de GitHub Releases) a texto plano legible para
/// mostrarlas en el diálogo de novedades. Lógica pura y testeable (sin red ni UI).
/// </summary>
public static partial class ReleaseNotes
{
    [GeneratedRegex(@"^\s{0,3}#{1,6}\s*")]              private static partial Regex HeadingRegex();
    [GeneratedRegex(@"^(\s*)[-*+]\s+")]                 private static partial Regex BulletRegex();
    [GeneratedRegex(@"\[([^\]]+)\]\([^)]*\)")]          private static partial Regex LinkRegex();
    [GeneratedRegex(@"\n{3,}")]                         private static partial Regex BlankLinesRegex();

    /// <summary>
    /// Convierte Markdown a texto plano: quita marcadores de encabezado (<c>#</c>), normaliza viñetas
    /// (<c>-</c>/<c>*</c>/<c>+</c> → <c>•</c>), elimina negritas (<c>**</c>/<c>__</c>) y comillas de
    /// código, reduce enlaces <c>[texto](url)</c> a su texto y colapsa líneas en blanco repetidas.
    /// Devuelve cadena vacía si la entrada es nula o en blanco.
    /// </summary>
    /// <param name="markdown">Cuerpo Markdown de la versión (campo <c>body</c> del release).</param>
    public static string ToPlainText(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return "";

        string normalized = markdown.Replace("\r\n", "\n").Replace("\r", "\n");
        var sb = new StringBuilder();

        foreach (string raw in normalized.Split('\n'))
        {
            string line = HeadingRegex().Replace(raw, "");
            line = BulletRegex().Replace(line, "$1• ");
            line = LinkRegex().Replace(line, "$1");
            line = line.Replace("**", "").Replace("__", "").Replace("`", "");
            sb.Append(line.TrimEnd()).Append('\n');
        }

        // Colapsar 3+ saltos consecutivos a un máximo de dos (una línea en blanco) y recortar extremos.
        return BlankLinesRegex().Replace(sb.ToString(), "\n\n").Trim();
    }
}
