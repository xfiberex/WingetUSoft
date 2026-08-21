namespace WingetUSoft;

/// <summary>
/// Un mensaje para el usuario que se guarda **sin traducir**: la clave y sus argumentos, no el texto ya
/// formateado. Se resuelve con <see cref="Text"/> en el momento de mostrarlo.
/// </summary>
/// <remarks>
/// Existe por el orden de arranque de la aplicación: <see cref="AppSettings.Load"/> corre **antes** de
/// <c>L.Set(...)</c>, porque el idioma elegido es justamente uno de los ajustes que hay que leer. Un
/// mensaje de error de configuración resuelto ahí mismo saldría siempre en el idioma por defecto, aunque
/// el usuario tenga la aplicación en italiano — y son mensajes que se le muestran en un diálogo.
///
/// Aplazar la traducción también la deja correcta si el idioma cambia entre que el mensaje se produce y
/// que se muestra.
/// </remarks>
/// <param name="Key">Clave del diccionario de <see cref="L"/>.</param>
/// <param name="Args">Argumentos de formato de esa clave.</param>
public sealed record DeferredMessage(string Key, params object?[] Args)
{
    /// <summary>El mensaje en el idioma activo **ahora**.</summary>
    public string Text => Args.Length == 0 ? L.T(Key) : L.T(Key, Args!);

    public override string ToString() => Text;
}
