namespace WingetUSoft;

/// <summary>
/// Etiquetas con las que <c>winget show</c> rotula los campos que necesitamos, en todos los idiomas
/// a los que winget las traduce. winget imprime su salida en el idioma de Windows y no permite
/// forzar el inglés (<c>--locale</c> elige el idioma del instalador, no el de la CLI), así que el
/// parser las compara todas: buscar solo la etiqueta inglesa dejaba el panel de detalle sin
/// descripción ni enlaces en cualquier Windows que no estuviera en inglés.
/// </summary>
/// <remarks>
/// Extraídas de los recursos oficiales del paquete Microsoft.DesktopAppInstaller (winget v1.29.280):
/// claves <c>ShowLabelDescription</c>, <c>ShowLabelPackageUrl</c> y <c>ShowLabelReleaseNotesUrl</c>.
/// winget solo traduce estas cadenas a 10 idiomas y en el resto cae al inglés, de modo que la tabla
/// cubre todas las salidas posibles.
/// <para>
/// Cuidado al editarlas: el francés lleva un espacio duro antes de los dos puntos, el chino
/// tradicional usa dos puntos de ancho completo y el coreano no lleva ninguno. Esos caracteres van
/// escapados a propósito, porque son invisibles o fáciles de romper al 'corregir' el espaciado.
/// </para>
/// </remarks>
internal static class WingetShowLabels
{
    public static readonly string[] Description =
    [
        "Description:",                                  // inglés (y todo idioma que winget no traduzca)
        "Beschreibung:",                                 // alemán
        "Descripción:",                                  // español
        "Description\u00A0:",                            // francés
        "Descrizione:",                                  // italiano
        "説明:",                                           // japonés
        "설명:",                                           // coreano
        "Descrição:",                                    // portugués
        "Описание:",                                     // ruso
        "描述:",                                           // chino simplificado
        "描述\uFF1A",                                      // chino tradicional
    ];

    public static readonly string[] Homepage =
    [
        "Homepage:",                                     // inglés (y todo idioma que winget no traduzca)
        "Startseite:",                                   // alemán
        "Página principal:",                             // español
        "Page d\u2019accueil :",                         // francés
        "Home page:",                                    // italiano
        "ホーム ページ:",                                      // japonés
        "홈페이지",                                          // coreano
        "Página inicial:",                               // portugués
        "Домашняя страница:",                            // ruso
        "主页:",                                           // chino simplificado
        "首頁\uFF1A",                                      // chino tradicional
    ];

    public static readonly string[] ReleaseNotesUrl =
    [
        "Release Notes Url:",                            // inglés (y todo idioma que winget no traduzca)
        "URL der Versionshinweise:",                     // alemán
        "Dirección URL de notas de la versión:",         // español
        "URL des notes de publication\u00A0:",           // francés
        "URL note sulla versione:",                      // italiano
        "リリース ノート URL:",                                 // japonés
        "릴리스 정보 URL:",                                   // coreano
        "URL de Notas de Versão:",                       // portugués
        "URL-адрес заметок о выпуске:",                  // ruso
        "发行说明 URL:",                                     // chino simplificado
        "版本資訊 Url\uFF1A",                                // chino tradicional
    ];
}
