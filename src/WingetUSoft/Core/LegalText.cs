namespace WingetUSoft;

/// <summary>
/// Acceso a los textos legales <b>embebidos</b> en el ejecutable (licencia MIT y avisos de terceros),
/// para mostrarlos dentro de la app sin depender de archivos sueltos junto al .exe. Defensivo: ante
/// cualquier problema devuelve cadena vacía (el diálogo muestra entonces "texto no disponible").
/// </summary>
public static class LegalText
{
    /// <summary>Texto completo de la licencia (MIT).</summary>
    public static string License() => Read("WingetUSoft.LICENSE.txt");

    /// <summary>Avisos y atribuciones de componentes de terceros.</summary>
    public static string ThirdParty() => Read("WingetUSoft.THIRD-PARTY-NOTICES.txt");

    private static string Read(string resource)
    {
        try
        {
            using var s = typeof(LegalText).Assembly.GetManifestResourceStream(resource);
            if (s is null) return "";
            using var r = new StreamReader(s);
            return r.ReadToEnd();
        }
        catch
        {
            return "";
        }
    }
}
