namespace WingetUSoft;

/// <summary>
/// Etiquetas con las que <c>winget show</c> rotula los campos que necesitamos, en todos los idiomas
/// a los que winget las traduce. winget imprime su salida en el idioma de Windows y no permite
/// forzar el ingles (<c>--locale</c> elige el idioma del instalador, no el de la CLI), asi que el
/// parser las compara todas: buscar solo la etiqueta inglesa dejaba el panel de detalle sin
/// descripcion ni enlaces en cualquier Windows que no estuviera en ingles.
/// </summary>
/// <remarks>
/// Extraidas de los recursos oficiales del paquete Microsoft.DesktopAppInstaller (winget v1.29.280):
/// claves <c>ShowLabelDescription</c>, <c>ShowLabelPackageUrl</c> y <c>ShowLabelReleaseNotesUrl</c>.
/// winget solo traduce estas cadenas a 10 idiomas y en el resto cae al ingles, de modo que la tabla
/// cubre todas las salidas posibles.
/// <para>
/// Cuidado al editarlas: el frances lleva un espacio duro antes de los dos puntos, el chino
/// tradicional usa dos puntos de ancho completo y el coreano no lleva ninguno. Esos caracteres van
/// escapados a proposito, porque son invisibles o faciles de romper al 'corregir' el espaciado.
/// </para>
/// </remarks>
internal static class WingetShowLabels
{
    public static readonly string[] Description =
    [
        "Description:",                                  // ingles (y todo idioma que winget no traduzca)
        "Beschreibung:",                                 // aleman
        "Descripción:",                                  // espanol
        "Description\u00A0:",                            // frances
        "Descrizione:",                                  // italiano
        "説明:",                                           // japones
        "설명:",                                           // coreano
        "Descrição:",                                    // portugues
        "Описание:",                                     // ruso
        "描述:",                                           // chino simplificado
        "描述\uFF1A",                                      // chino tradicional
    ];

    public static readonly string[] Homepage =
    [
        "Homepage:",                                     // ingles (y todo idioma que winget no traduzca)
        "Startseite:",                                   // aleman
        "Página principal:",                             // espanol
        "Page d\u2019accueil :",                         // frances
        "Home page:",                                    // italiano
        "ホーム ページ:",                                      // japones
        "홈페이지",                                          // coreano
        "Página inicial:",                               // portugues
        "Домашняя страница:",                            // ruso
        "主页:",                                           // chino simplificado
        "首頁\uFF1A",                                      // chino tradicional
    ];

    public static readonly string[] ReleaseNotesUrl =
    [
        "Release Notes Url:",                            // ingles (y todo idioma que winget no traduzca)
        "URL der Versionshinweise:",                     // aleman
        "Dirección URL de notas de la versión:",         // espanol
        "URL des notes de publication\u00A0:",           // frances
        "URL note sulla versione:",                      // italiano
        "リリース ノート URL:",                                 // japones
        "릴리스 정보 URL:",                                   // coreano
        "URL de Notas de Versão:",                       // portugues
        "URL-адрес заметок о выпуске:",                  // ruso
        "发行说明 URL:",                                     // chino simplificado
        "版本資訊 Url\uFF1A",                                // chino tradicional
    ];
}
