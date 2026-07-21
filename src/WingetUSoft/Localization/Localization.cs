namespace WingetUSoft;

public enum AppLang { Es, En, Pt, Fr, It }

/// <summary>
/// Proveedor de cadenas localizadas (ES/EN/PT/FR/IT). Uso: L.T("clave") o L.T("clave", arg0, ...).
/// Cada entrada del diccionario es un arreglo indexado por <see cref="AppLang"/> (orden Es, En, Pt, Fr, It).
/// </summary>
public static class L
{
    public static AppLang Current { get; private set; } = AppLang.Es;

    public static void Set(AppLang lang)
    {
        if (Current == lang) return;
        Current = lang;
    }

    /// <summary>
    /// Idioma a partir del nombre de una cultura .NET (p. ej. <c>"es-ES"</c>, <c>"pt-BR"</c>, <c>"fr"</c>):
    /// toma la parte de idioma de dos letras (antes de <c>-</c>/<c>_</c>) y la mapea con <see cref="FromCode"/>.
    /// Desconocido o vacío → Es. Lógica pura; se usa para sembrar el idioma en el primer arranque.
    /// </summary>
    /// <param name="cultureName">Nombre de la cultura, p. ej. <see cref="System.Globalization.CultureInfo.Name"/>.</param>
    public static AppLang FromCulture(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName)) return AppLang.Es;
        string lang = cultureName.Trim();
        int sep = lang.IndexOfAny(['-', '_']);
        if (sep > 0) lang = lang[..sep];
        return FromCode(lang);
    }

    /// <summary>Convierte un código ISO (<c>"es"/"en"/"pt"/"fr"/"it"</c>) al idioma; desconocido → Es.</summary>
    public static AppLang FromCode(string? code) => code?.Trim().ToLowerInvariant() switch
    {
        "en" => AppLang.En,
        "pt" => AppLang.Pt,
        "fr" => AppLang.Fr,
        "it" => AppLang.It,
        _    => AppLang.Es,
    };

    /// <summary>Código ISO del idioma (<c>"es"/"en"/"pt"/"fr"/"it"</c>).</summary>
    public static string ToCode(AppLang lang) => lang switch
    {
        AppLang.En => "en",
        AppLang.Pt => "pt",
        AppLang.Fr => "fr",
        AppLang.It => "it",
        _          => "es",
    };

    public static string T(string key)
    {
        if (Map.TryGetValue(key, out var arr))
        {
            int i = (int)Current;
            return i >= 0 && i < arr.Length && !string.IsNullOrEmpty(arr[i]) ? arr[i] : arr[0];
        }
        return key; // defensivo: nunca lanza
    }

    public static string T(string key, params object[] args) => string.Format(T(key), args);

    /// <summary>
    /// Diccionario de traducciones. Orden de cada arreglo: <c>[Es, En, Pt, Fr, It]</c>.
    /// En esta fase (Tier A #1) solo cubre el menú principal; la extracción completa del resto
    /// de la UI se hace en el Tier A #7 (ver ROADMAP.md).
    /// </summary>
    internal static readonly Dictionary<string, string[]> Map = new()
    {
        // El menú de la barra de acciones ya solo contiene ACCIONES: las preferencias (modo, tema,
        // idioma) viven en la ventana de Configuración, con el resto de ajustes (Tier C #5). Por eso
        // estas claves son "pref.*" y no "menu.*": las rotula SettingsWindow, no un MenuFlyout.
        ["menu.tools"]        = ["Herramientas", "Tools", "Ferramentas", "Outils", "Strumenti"],
        ["pref.updateMode"]   = ["Modo de actualización", "Update mode", "Modo de atualização", "Mode de mise à jour", "Modalità di aggiornamento"],
        ["pref.silent"]       = ["Silenciosa", "Silent", "Silenciosa", "Silencieux", "Silenzioso"],
        ["pref.interactive"]  = ["Interactiva", "Interactive", "Interativa", "Interactif", "Interattivo"],
        ["pref.theme"]        = ["Tema", "Theme", "Tema", "Thème", "Tema"],
        ["pref.themeSystem"]  = ["Sistema (automático)", "System (automatic)", "Sistema (automático)", "Système (automatique)", "Sistema (automatico)"],
        ["pref.themeLight"]   = ["Claro", "Light", "Claro", "Clair", "Chiaro"],
        ["pref.themeDark"]    = ["Oscuro", "Dark", "Escuro", "Sombre", "Scuro"],
        ["pref.lang"]         = ["Idioma", "Language", "Idioma", "Langue", "Lingua"],
        ["pref.lang.es"]      = ["Español", "Spanish", "Espanhol", "Espagnol", "Spagnolo"],
        ["pref.lang.en"]      = ["Inglés", "English", "Inglês", "Anglais", "Inglese"],
        ["pref.lang.pt"]      = ["Portugués", "Portuguese", "Português", "Portugais", "Portoghese"],
        ["pref.lang.fr"]      = ["Francés", "French", "Francês", "Français", "Francese"],
        ["pref.lang.it"]      = ["Italiano", "Italian", "Italiano", "Italien", "Italiano"],
        ["menu.export"]       = ["Exportar lista...", "Export list...", "Exportar lista...", "Exporter la liste...", "Esporta elenco..."],
        ["menu.settings"]     = ["Configuración...", "Settings...", "Configurações...", "Paramètres...", "Impostazioni..."],
        ["menu.history"]      = ["Ver historial", "View history", "Ver histórico", "Voir l'historique", "Visualizza cronologia"],
        ["menu.uninstall"]    = ["Desinstalar programas...", "Uninstall programs...", "Desinstalar programas...", "Désinstaller des programmes...", "Disinstalla programmi..."],
        ["menu.checkUpdate"]  = ["Buscar actualización de WingetUSoft...", "Check for WingetUSoft update...", "Verificar atualização do WingetUSoft...", "Rechercher une mise à jour de WingetUSoft...", "Cerca aggiornamenti di WingetUSoft..."],
        ["menu.whatsnew"]     = ["Novedades...", "What's new...", "Novidades...", "Nouveautés...", "Novità..."],

        ["btn.close"]              = ["Cerrar", "Close", "Fechar", "Fermer", "Chiudi"],
        ["whatsnew.title"]         = ["Novedades de WingetUSoft", "What's new in WingetUSoft", "Novidades do WingetUSoft", "Nouveautés de WingetUSoft", "Novità di WingetUSoft"],
        ["whatsnew.version"]       = ["Versión {0}", "Version {0}", "Versão {0}", "Version {0}", "Versione {0}"],
        ["whatsnew.viewOnGitHub"]  = ["Ver en GitHub", "View on GitHub", "Ver no GitHub", "Voir sur GitHub", "Vedi su GitHub"],
        ["whatsnew.empty"]         = ["No se pudieron cargar las novedades. Puedes verlas en GitHub.", "Could not load the release notes. You can view them on GitHub.", "Não foi possível carregar as novidades. Você pode vê-las no GitHub.", "Impossible de charger les nouveautés. Vous pouvez les consulter sur GitHub.", "Impossibile caricare le novità. Puoi vederle su GitHub."],
        ["update.availTitle"]      = ["Actualización disponible", "Update available", "Atualização disponível", "Mise à jour disponible", "Aggiornamento disponibile"],
        ["update.availBody"]       = ["Hay una nueva versión disponible: {0}", "A new version is available: {0}", "Há uma nova versão disponível: {0}", "Une nouvelle version est disponible : {0}", "È disponibile una nuova versione: {0}"],
        ["update.changelog"]       = ["Novedades:", "What's new:", "Novidades:", "Nouveautés :", "Novità:"],
        ["update.confirmInstall"]  = ["¿Descargar e instalar automáticamente?", "Download and install automatically?", "Baixar e instalar automaticamente?", "Télécharger et installer automatiquement ?", "Scaricare e installare automaticamente?"],

        ["menu.help"]         = ["Ayuda", "Help", "Ajuda", "Aide", "Aiuto"],
        ["menu.about"]        = ["Acerca de...", "About...", "Sobre...", "À propos...", "Informazioni..."],
        ["menu.license"]      = ["Licencia", "License", "Licença", "Licence", "Licenza"],
        ["menu.thirdParty"]   = ["Avisos de terceros", "Third-party notices", "Avisos de terceiros", "Avis de tiers", "Note di terze parti"],
        ["legal.unavailable"] = ["Texto no disponible.", "Text not available.", "Texto indisponível.", "Texte indisponible.", "Testo non disponibile."],

        ["about.title"]         = ["Acerca de WingetUSoft", "About WingetUSoft", "Sobre o WingetUSoft", "À propos de WingetUSoft", "Informazioni su WingetUSoft"],
        ["about.version"]       = ["Versión {0}", "Version {0}", "Versão {0}", "Version {0}", "Versione {0}"],
        ["about.desc"]          = ["Interfaz gráfica para gestionar tu software con winget en Windows: buscar e instalar programas, actualizarlos, desinstalarlos y exportar o importar tu lista de paquetes.", "A graphical interface to manage your software with winget on Windows: search and install programs, update them, uninstall them, and export or import your package list.", "Interface gráfica para gerenciar seu software com winget no Windows: buscar e instalar programas, atualizá-los, desinstalá-los e exportar ou importar sua lista de pacotes.", "Interface graphique pour gérer vos logiciels avec winget sous Windows : rechercher et installer des programmes, les mettre à jour, les désinstaller et exporter ou importer votre liste de paquets.", "Interfaccia grafica per gestire il tuo software con winget su Windows: cercare e installare programmi, aggiornarli, disinstallarli ed esportare o importare l'elenco dei pacchetti."],
        ["about.copyright"]     = ["© 2026 Ricky Angel Jiménez Bueno. Distribuido bajo la licencia MIT.", "© 2026 Ricky Angel Jiménez Bueno. Distributed under the MIT license.", "© 2026 Ricky Angel Jiménez Bueno. Distribuído sob a licença MIT.", "© 2026 Ricky Angel Jiménez Bueno. Distribué sous licence MIT.", "© 2026 Ricky Angel Jiménez Bueno. Distribuito con licenza MIT."],
        ["about.privacyHeader"] = ["Privacidad", "Privacy", "Privacidade", "Confidentialité", "Privacy"],
        ["about.privacy"]       = ["WingetUSoft no recopila datos personales ni telemetría. La aplicación se conecta a Internet únicamente para consultar/instalar paquetes vía winget y para comprobar actualizaciones de la propia app en GitHub Releases (HTTPS).", "WingetUSoft does not collect personal data or telemetry. The app connects to the Internet only to query/install packages via winget and to check for app updates on GitHub Releases (HTTPS).", "O WingetUSoft não coleta dados pessoais nem telemetria. O aplicativo se conecta à Internet apenas para consultar/instalar pacotes via winget e para verificar atualizações do próprio app no GitHub Releases (HTTPS).", "WingetUSoft ne collecte aucune donnée personnelle ni télémétrie. L'application se connecte à Internet uniquement pour consulter/installer des paquets via winget et pour vérifier les mises à jour de l'application sur GitHub Releases (HTTPS).", "WingetUSoft non raccoglie dati personali né telemetria. L'app si connette a Internet solo per consultare/installare pacchetti tramite winget e per verificare gli aggiornamenti dell'app su GitHub Releases (HTTPS)."],
        ["about.github"]        = ["Ver en GitHub", "View on GitHub", "Ver no GitHub", "Voir sur GitHub", "Vedi su GitHub"],

        // ── Tier E: omitir versión ──────────────────────────────────
        ["ctx.skipVersion"]     = ["Omitir esta versión", "Skip this version", "Ignorar esta versão", "Ignorer cette version", "Salta questa versione"],
        ["ctx.unskipVersion"]   = ["Dejar de omitir esta versión", "Stop skipping this version", "Deixar de ignorar esta versão", "Ne plus ignorer cette version", "Non saltare più questa versione"],
        ["grid.skippedAccessible"] = ["Versión {0} omitida", "Version {0} skipped", "Versão {0} ignorada", "Version {0} ignorée", "Versione {0} saltata"],
        ["grid.rowAccessible"]  = ["{0}, versión instalada {1}, disponible {2}", "{0}, installed version {1}, available {2}", "{0}, versão instalada {1}, disponível {2}", "{0}, version installée {1}, disponible {2}", "{0}, versione installata {1}, disponibile {2}"],
        ["status.versionSkipped"]   = ["Versión {0} de {1} omitida. Volverá a aparecer cuando salga una nueva.", "Version {0} of {1} skipped. It will reappear when a newer one is released.", "Versão {0} de {1} ignorada. Reaparecerá quando sair uma nova.", "Version {0} de {1} ignorée. Elle réapparaîtra à la sortie d'une nouvelle.", "Versione {0} di {1} saltata. Riapparirà quando ne uscirà una nuova."],
        ["status.versionUnskipped"] = ["{0} vuelve a estar disponible para actualizar.", "{0} is available to update again.", "{0} está novamente disponível para atualizar.", "{0} est de nouveau disponible pour la mise à jour.", "{0} è di nuovo disponibile per l'aggiornamento."],
        ["msg.saveSkippedError"]    = ["No se pudieron guardar las versiones omitidas: {0}", "Could not save the skipped versions: {0}", "Não foi possível salvar as versões ignoradas: {0}", "Impossible d'enregistrer les versions ignorées : {0}", "Impossibile salvare le versioni saltate: {0}"],

        // ── Tier E: exportar / importar (winget) ────────────────────
        ["menu.exportWinget"]   = ["Exportar paquetes (winget)...", "Export packages (winget)...", "Exportar pacotes (winget)...", "Exporter les paquets (winget)...", "Esporta pacchetti (winget)..."],
        ["menu.importWinget"]   = ["Importar paquetes (winget)...", "Import packages (winget)...", "Importar pacotes (winget)...", "Importer des paquets (winget)...", "Importa pacchetti (winget)..."],
        ["export.wingetTitle"]  = ["Exportar paquetes instalados", "Export installed packages", "Exportar pacotes instalados", "Exporter les paquets installés", "Esporta pacchetti installati"],
        ["export.wingetBody"]   = ["Se guardará la lista de programas instalados en el formato JSON de winget. Sirve para reinstalarlos todos en otro equipo (o tras formatear) con Importar paquetes.", "The list of installed programs will be saved in winget's JSON format. Use it to reinstall everything on another PC (or after formatting) with Import packages.", "A lista de programas instalados será salva no formato JSON do winget. Serve para reinstalar tudo em outro PC (ou após formatar) com Importar pacotes.", "La liste des programmes installés sera enregistrée au format JSON de winget. Elle permet de tout réinstaller sur un autre PC (ou après un formatage) avec Importer des paquets.", "L'elenco dei programmi installati verrà salvato nel formato JSON di winget. Serve per reinstallare tutto su un altro PC (o dopo una formattazione) con Importa pacchetti."],
        ["export.includeVersions"] = ["Incluir las versiones exactas", "Include exact versions", "Incluir as versões exatas", "Inclure les versions exactes", "Includi le versioni esatte"],
        ["export.includeVersionsHint"] = ["Sin marcar (recomendado) se instalará la última versión de cada programa. Marcado, se fija la versión que tienes hoy: la importación fallará para los programas cuya versión exacta ya no esté en el catálogo.", "Unchecked (recommended) installs the latest version of each program. Checked pins today's versions: the import will fail for programs whose exact version is no longer in the catalog.", "Desmarcado (recomendado) instala a versão mais recente de cada programa. Marcado, fixa as versões de hoje: a importação falhará para programas cuja versão exata não esteja mais no catálogo.", "Décoché (recommandé), la dernière version de chaque programme sera installée. Coché, les versions actuelles sont figées : l'importation échouera pour les programmes dont la version exacte n'est plus au catalogue.", "Deselezionato (consigliato) installa l'ultima versione di ogni programma. Selezionato, fissa le versioni odierne: l'importazione fallirà per i programmi la cui versione esatta non è più nel catalogo."],
        ["export.wingetContinue"] = ["Elegir archivo...", "Choose file...", "Escolher arquivo...", "Choisir un fichier...", "Scegli file..."],
        ["status.exportingWinget"] = ["Exportando paquetes...", "Exporting packages...", "Exportando pacotes...", "Exportation des paquets...", "Esportazione pacchetti..."],
        ["status.wingetExported"]  = ["Paquetes exportados a {0}", "Packages exported to {0}", "Pacotes exportados para {0}", "Paquets exportés vers {0}", "Pacchetti esportati in {0}"],
        ["status.wingetExportFailed"] = ["No se pudieron exportar los paquetes.", "Could not export the packages.", "Não foi possível exportar os pacotes.", "Impossible d'exporter les paquets.", "Impossibile esportare i pacchetti."],
        ["log.exportingWinget"]  = ["[Exportando paquetes instalados a {0}]", "[Exporting installed packages to {0}]", "[Exportando pacotes instalados para {0}]", "[Exportation des paquets installés vers {0}]", "[Esportazione pacchetti installati in {0}]"],
        ["log.exportWingetDone"] = ["  ✔ Exportación completada: {0}", "  ✔ Export completed: {0}", "  ✔ Exportação concluída: {0}", "  ✔ Exportation terminée : {0}", "  ✔ Esportazione completata: {0}"],
        ["log.exportWingetFailed"] = ["  ✖ winget export falló (código {0})", "  ✖ winget export failed (code {0})", "  ✖ winget export falhou (código {0})", "  ✖ Échec de winget export (code {0})", "  ✖ winget export non riuscito (codice {0})"],
        ["msg.exportWingetError"] = ["winget no pudo exportar la lista de paquetes (código {0}).", "winget could not export the package list (code {0}).", "O winget não conseguiu exportar a lista de pacotes (código {0}).", "winget n'a pas pu exporter la liste des paquets (code {0}).", "winget non è riuscito a esportare l'elenco dei pacchetti (codice {0})."],
        ["import.confirmTitle"]  = ["Importar e instalar paquetes", "Import and install packages", "Importar e instalar pacotes", "Importer et installer des paquets", "Importa e installa pacchetti"],
        ["import.confirmBody"]   = ["Se instalarán en este equipo los programas listados en «{0}». Los que ya tengas instalados se ACTUALIZARÁN si hay una versión más reciente. Puede tardar bastante y algunos instaladores pedirán permiso de administrador. ¿Continuar?", "The programs listed in \"{0}\" will be installed on this PC. Those you already have will be UPDATED if a newer version exists. It may take a while and some installers will ask for administrator permission. Continue?", "Serão instalados neste PC os programas listados em «{0}». Os que você já tiver serão ATUALIZADOS se houver uma versão mais recente. Pode demorar e alguns instaladores pedirão permissão de administrador. Continuar?", "Les programmes listés dans « {0} » seront installés sur ce PC. Ceux que vous avez déjà seront MIS À JOUR s'il existe une version plus récente. Cela peut prendre du temps et certains installateurs demanderont une autorisation d'administrateur. Continuer ?", "Verranno installati su questo PC i programmi elencati in «{0}». Quelli che hai già verranno AGGIORNATI se esiste una versione più recente. Può richiedere tempo e alcuni installer chiederanno l'autorizzazione di amministratore. Continuare?"],
        ["status.importing"]     = ["Importando paquetes...", "Importing packages...", "Importando pacotes...", "Importation des paquets...", "Importazione pacchetti..."],
        ["status.importDone"]    = ["Importación completada.", "Import completed.", "Importação concluída.", "Importation terminée.", "Importazione completata."],
        ["status.importPartial"] = ["Importación terminada con incidencias (ver registro).", "Import finished with issues (see log).", "Importação concluída com problemas (ver registro).", "Importation terminée avec des problèmes (voir le journal).", "Importazione terminata con problemi (vedi registro)."],
        ["status.importCancelled"] = ["Importación cancelada.", "Import cancelled.", "Importação cancelada.", "Importation annulée.", "Importazione annullata."],
        ["status.importFailed"]  = ["No se pudo importar.", "Could not import.", "Não foi possível importar.", "Impossible d'importer.", "Impossibile importare."],
        ["log.importing"]        = ["[Importando paquetes desde {0}]", "[Importing packages from {0}]", "[Importando pacotes de {0}]", "[Importation des paquets depuis {0}]", "[Importazione pacchetti da {0}]"],
        ["log.importDone"]       = ["  ✔ Importación completada", "  ✔ Import completed", "  ✔ Importação concluída", "  ✔ Importation terminée", "  ✔ Importazione completata"],
        ["log.importPartial"]    = ["  ! winget import terminó con código {0}: algún paquete no se pudo instalar", "  ! winget import finished with code {0}: some package could not be installed", "  ! winget import terminou com código {0}: algum pacote não pôde ser instalado", "  ! winget import s'est terminé avec le code {0} : un paquet n'a pas pu être installé", "  ! winget import terminato con codice {0}: qualche pacchetto non è stato installato"],
        ["log.importCancelled"]  = ["  ! Importación cancelada por el usuario", "  ! Import cancelled by the user", "  ! Importação cancelada pelo usuário", "  ! Importation annulée par l'utilisateur", "  ! Importazione annullata dall'utente"],
        ["msg.importPartial"]    = ["La importación terminó, pero algún paquete no se pudo instalar (por ejemplo, porque ya no está en el catálogo). Revisa el registro de actividad para ver cuáles.", "The import finished, but some package could not be installed (for example, because it is no longer in the catalog). Check the activity log to see which ones.", "A importação terminou, mas algum pacote não pôde ser instalado (por exemplo, porque não está mais no catálogo). Verifique o registro de atividade.", "L'importation est terminée, mais un paquet n'a pas pu être installé (par exemple, parce qu'il n'est plus au catalogue). Consultez le journal d'activité.", "L'importazione è terminata, ma qualche pacchetto non è stato installato (ad esempio perché non è più nel catalogo). Controlla il registro attività."],

        // ── Tier E: buscar e instalar ───────────────────────────────
        ["menu.searchInstall"]   = ["Buscar e instalar programas...", "Search and install programs...", "Buscar e instalar programas...", "Rechercher et installer des programmes...", "Cerca e installa programmi..."],
        ["search.windowTitle"]   = ["Buscar e instalar", "Search and install", "Buscar e instalar", "Rechercher et installer", "Cerca e installa"],
        ["search.header"]        = ["Buscar e instalar programas", "Search and install programs", "Buscar e instalar programas", "Rechercher et installer des programmes", "Cerca e installa programmi"],
        ["search.subtitle"]      = ["Busca en el catálogo de winget e instala lo que necesites.", "Search the winget catalog and install what you need.", "Busque no catálogo do winget e instale o que precisar.", "Cherchez dans le catalogue winget et installez ce dont vous avez besoin.", "Cerca nel catalogo di winget e installa ciò che ti serve."],
        ["search.placeholder"]   = ["Nombre, Id o palabra clave...", "Name, Id or keyword...", "Nome, Id ou palavra-chave...", "Nom, Id ou mot-clé...", "Nome, Id o parola chiave..."],
        ["search.placeholderAccessible"] = ["Buscar en el catálogo de winget", "Search the winget catalog", "Buscar no catálogo do winget", "Rechercher dans le catalogue winget", "Cerca nel catalogo di winget"],
        ["search.search"]        = ["Buscar", "Search", "Buscar", "Rechercher", "Cerca"],
        ["search.install"]       = ["Instalar seleccionado", "Install selected", "Instalar selecionado", "Installer la sélection", "Installa selezionato"],
        ["search.results"]       = ["Resultados", "Results", "Resultados", "Résultats", "Risultati"],
        ["search.colState"]      = ["Estado", "State", "Estado", "État", "Stato"],
        ["search.installed"]     = ["Instalado", "Installed", "Instalado", "Installé", "Installato"],
        ["search.rowAccessible"] = ["{0}, versión {1}, origen {2}", "{0}, version {1}, source {2}", "{0}, versão {1}, fonte {2}", "{0}, version {1}, source {2}", "{0}, versione {1}, origine {2}"],
        ["search.rowAccessibleInstalled"] = ["{0}, versión {1}, origen {2}, ya instalado", "{0}, version {1}, source {2}, already installed", "{0}, versão {1}, fonte {2}, já instalado", "{0}, version {1}, source {2}, déjà installé", "{0}, versione {1}, origine {2}, già installato"],
        ["search.ready"]         = ["Escribe qué buscas y pulsa Buscar.", "Type what you are looking for and press Search.", "Digite o que procura e pressione Buscar.", "Saisissez votre recherche et appuyez sur Rechercher.", "Scrivi cosa cerchi e premi Cerca."],
        ["search.emptyQuery"]    = ["Escribe algo que buscar.", "Type something to search for.", "Digite algo para buscar.", "Saisissez quelque chose à rechercher.", "Scrivi qualcosa da cercare."],
        ["search.searching"]     = ["Buscando «{0}»...", "Searching for \"{0}\"...", "Buscando «{0}»...", "Recherche de « {0} »...", "Ricerca di «{0}»..."],
        ["search.found"]         = ["{0} paquete(s) encontrado(s).", "{0} package(s) found.", "{0} pacote(s) encontrado(s).", "{0} paquet(s) trouvé(s).", "{0} pacchetto/i trovato/i."],
        ["search.countLabel"]    = ["{0} resultado(s)", "{0} result(s)", "{0} resultado(s)", "{0} résultat(s)", "{0} risultato/i"],
        ["search.noResults"]     = ["No se encontró ningún paquete para «{0}».", "No package found for \"{0}\".", "Nenhum pacote encontrado para «{0}».", "Aucun paquet trouvé pour « {0} ».", "Nessun pacchetto trovato per «{0}»."],
        ["search.cancelled"]     = ["Búsqueda cancelada.", "Search cancelled.", "Busca cancelada.", "Recherche annulée.", "Ricerca annullata."],
        ["search.error"]         = ["No se pudo completar la búsqueda.", "The search could not be completed.", "Não foi possível concluir a busca.", "La recherche n'a pas pu aboutir.", "Impossibile completare la ricerca."],
        ["search.confirmInstallTitle"] = ["Instalar programa", "Install program", "Instalar programa", "Installer le programme", "Installa programma"],
        ["search.confirmInstallBody"]  = ["Se instalará {0} ({1}), versión {2}, en este equipo. El instalador puede pedir permiso de administrador. ¿Continuar?", "{0} ({1}), version {2}, will be installed on this PC. The installer may ask for administrator permission. Continue?", "Será instalado {0} ({1}), versão {2}, neste PC. O instalador pode pedir permissão de administrador. Continuar?", "{0} ({1}), version {2}, sera installé sur ce PC. L'installateur peut demander une autorisation d'administrateur. Continuer ?", "Verrà installato {0} ({1}), versione {2}, su questo PC. L'installer potrebbe chiedere l'autorizzazione di amministratore. Continuare?"],
        ["search.installing"]    = ["Instalando {0}...", "Installing {0}...", "Instalando {0}...", "Installation de {0}...", "Installazione di {0}..."],
        ["search.installOk"]     = ["{0} instalado correctamente.", "{0} installed successfully.", "{0} instalado com sucesso.", "{0} installé avec succès.", "{0} installato correttamente."],
        ["search.installFailed"] = ["No se pudo instalar {0}.", "Could not install {0}.", "Não foi possível instalar {0}.", "Impossible d'installer {0}.", "Impossibile installare {0}."],
        ["search.installFailedBody"] = ["winget no pudo instalar {0} (código {1}). Revisa el registro de actividad para ver el detalle.", "winget could not install {0} (code {1}). Check the activity log for details.", "O winget não conseguiu instalar {0} (código {1}). Verifique o registro de atividade.", "winget n'a pas pu installer {0} (code {1}). Consultez le journal d'activité.", "winget non è riuscito a installare {0} (codice {1}). Controlla il registro attività."],
        ["search.installCancelled"] = ["Instalación cancelada.", "Installation cancelled.", "Instalação cancelada.", "Installation annulée.", "Installazione annullata."],
        ["search.logInstalling"] = ["[Instalando {0} ({1})]", "[Installing {0} ({1})]", "[Instalando {0} ({1})]", "[Installation de {0} ({1})]", "[Installazione di {0} ({1})]"],
        ["search.logInstallOk"]  = ["  ✔ {0} instalado", "  ✔ {0} installed", "  ✔ {0} instalado", "  ✔ {0} installé", "  ✔ {0} installato"],
        ["search.logInstallFailed"] = ["  ✖ {0}: winget devolvió el código {1}", "  ✖ {0}: winget returned code {1}", "  ✖ {0}: o winget retornou o código {1}", "  ✖ {0} : winget a renvoyé le code {1}", "  ✖ {0}: winget ha restituito il codice {1}"],
        ["status.listMayBeStale"] = ["Has instalado software: vuelve a consultar para ver la lista al día.", "You installed software: check again to see an up-to-date list.", "Você instalou software: consulte novamente para ver a lista atualizada.", "Vous avez installé un logiciel : relancez la recherche pour une liste à jour.", "Hai installato software: riesegui la ricerca per vedere l'elenco aggiornato."],

        // ── MainWindow ──────────────────────────────────────────────
        ["app.titleBase"]     = ["WingetUSoft", "WingetUSoft", "WingetUSoft", "WingetUSoft", "WingetUSoft"],
        ["header.title"]      = ["Actualiza tus programas con winget", "Update your programs with winget", "Atualize seus programas com winget", "Mettez à jour vos programmes avec winget", "Aggiorna i tuoi programmi con winget"],
        ["header.subtitle"]   = ["Consulta, filtra y actualiza paquetes desde una sola vista.", "Check, filter and update packages from a single view.", "Consulte, filtre e atualize pacotes em uma única tela.", "Consultez, filtrez et mettez à jour les paquets depuis une seule vue.", "Consulta, filtra e aggiorna i pacchetti da un'unica schermata."],
        ["header.detailDefault"] = ["Selecciona un programa para ver sus detalles antes de actualizar.", "Select a program to see its details before updating.", "Selecione um programa para ver seus detalhes antes de atualizar.", "Sélectionnez un programme pour voir ses détails avant la mise à jour.", "Seleziona un programma per vedere i dettagli prima di aggiornare."],
        ["header.detailEmpty"]   = ["Todavía no hay datos cargados. Pulsa \"Consultar actualizaciones\" para empezar.", "No data loaded yet. Click \"Check for updates\" to start.", "Ainda não há dados carregados. Clique em \"Consultar atualizações\" para começar.", "Aucune donnée chargée pour l'instant. Cliquez sur \"Rechercher des mises à jour\" pour commencer.", "Nessun dato ancora caricato. Premi \"Cerca aggiornamenti\" per iniziare."],
        ["header.shortcuts"]  = ["Atajos: F5 consultar · Ctrl+A marcar todo · Supr excluir · Esc cancelar", "Shortcuts: F5 check · Ctrl+A select all · Del exclude · Esc cancel", "Atalhos: F5 consultar · Ctrl+A marcar tudo · Del excluir · Esc cancelar", "Raccourcis : F5 vérifier · Ctrl+A tout sélectionner · Suppr exclure · Échap annuler", "Scorciatoie: F5 verifica · Ctrl+A seleziona tutto · Canc escludi · Esc annulla"],
        ["btn.installNow"]    = ["Instalar ahora", "Install now", "Instalar agora", "Installer maintenant", "Installa ora"],
        ["actions.title"]     = ["Acciones rápidas", "Quick actions", "Ações rápidas", "Actions rapides", "Azioni rapide"],
        ["btn.checkUpdates"]  = ["Consultar actualizaciones", "Check for updates", "Consultar atualizações", "Rechercher des mises à jour", "Cerca aggiornamenti"],
        ["btn.checkUnknown"]  = ["Consultar con desconocidas", "Check including unknown", "Consultar incluindo desconhecidas", "Rechercher, y compris inconnues", "Cerca includendo sconosciuti"],
        ["btn.updateSelected"] = ["Actualizar seleccionados", "Update selected", "Atualizar selecionados", "Mettre à jour la sélection", "Aggiorna selezionati"],
        ["btn.updateAll"]     = ["Actualizar todo", "Update all", "Atualizar tudo", "Tout mettre à jour", "Aggiorna tutto"],
        ["btn.cancel"]        = ["Cancelar", "Cancel", "Cancelar", "Annuler", "Annulla"],
        ["filter.allSources"] = ["Todas las fuentes", "All sources", "Todas as fontes", "Toutes les sources", "Tutte le fonti"],
        ["filter.sourceLabel"] = ["Fuente:", "Source:", "Fonte:", "Source :", "Fonte:"],
        ["filter.excludedLabel"] = ["Excluidos:", "Excluded:", "Excluídos:", "Exclus :", "Esclusi:"],
        ["filter.all"]        = ["Todos", "All", "Todos", "Tous", "Tutti"],
        ["filter.notExcluded"] = ["No excluidos", "Not excluded", "Não excluídos", "Non exclus", "Non esclusi"],
        ["filter.onlyExcluded"] = ["Solo excluidos", "Only excluded", "Somente excluídos", "Exclus uniquement", "Solo esclusi"],
        ["search.label"]      = ["Buscar:", "Search:", "Buscar:", "Rechercher :", "Cerca:"],
        ["search.placeholder"] = ["Nombre o Id...", "Name or Id...", "Nome ou Id...", "Nom ou Id...", "Nome o Id..."],
        ["info.homepage"]     = ["Página web", "Homepage", "Página web", "Site web", "Sito web"],
        ["info.releaseNotes"] = ["Notas de versión", "Release notes", "Notas de versão", "Notes de version", "Note di rilascio"],
        ["list.header"]       = ["Actualizaciones disponibles", "Available updates", "Atualizações disponíveis", "Mises à jour disponibles", "Aggiornamenti disponibili"],
        ["list.colSelect"]    = ["Sel.", "Sel.", "Sel.", "Sél.", "Sel."],
        ["list.colName"]      = ["Nombre", "Name", "Nome", "Nom", "Nome"],
        ["list.colId"]        = ["Id", "Id", "Id", "Id", "Id"],
        ["list.colVersion"]   = ["Versión", "Version", "Versão", "Version", "Versione"],
        ["list.colAvailable"] = ["Disponible", "Available", "Disponível", "Disponible", "Disponibile"],
        ["list.colSource"]    = ["Fuente", "Source", "Fonte", "Source", "Fonte"],
        ["list.colExcluded"]  = ["Excl.", "Excl.", "Excl.", "Excl.", "Escl."],
        // Nombre accesible de las cabeceras ordenables (Tier C #6). Un lector de pantalla anuncia el
        // botón con {0} = columna y {1} = uno de los tres estados de orden de abajo.
        ["list.sortHeaderAccessible"] = ["{0}, {1}. Actívalo para reordenar.", "{0}, {1}. Activate to reorder.", "{0}, {1}. Ative para reordenar.", "{0}, {1}. Activez pour réordonner.", "{0}, {1}. Attivalo per riordinare."],
        ["list.sortAscending"]  = ["orden ascendente", "sorted ascending", "ordem crescente", "tri croissant", "ordine crescente"],
        ["list.sortDescending"] = ["orden descendente", "sorted descending", "ordem decrescente", "tri décroissant", "ordine decrescente"],
        ["list.sortNone"]       = ["sin ordenar", "not sorted", "sem ordenação", "non trié", "non ordinato"],
        ["grid.excludedAccessible"] = ["Paquete excluido", "Excluded package", "Pacote excluído", "Paquet exclu", "Pacchetto escluso"],
        ["grid.selectAccessible"]   = ["Marcar {0} para actualizar", "Select {0} for update", "Marcar {0} para atualizar", "Sélectionner {0} pour la mise à jour", "Seleziona {0} per l'aggiornamento"],
        ["grid.selectAllAccessible"] = ["Marcar o desmarcar todos los programas visibles", "Select or clear all visible programs", "Marcar ou desmarcar todos os programas visíveis", "Sélectionner ou désélectionner tous les programmes visibles", "Seleziona o deseleziona tutti i programmi visibili"],
        ["btn.updateSelectedCount"] = ["Actualizar seleccionados ({0})", "Update selected ({0})", "Atualizar selecionados ({0})", "Mettre à jour la sélection ({0})", "Aggiorna selezionati ({0})"],

        // ── Estados de la tabla (panel superpuesto cuando no hay filas que mostrar) ──
        ["list.stateLoadingTitle"]   = ["Consultando winget...", "Querying winget...", "Consultando o winget...", "Interrogation de winget...", "Interrogazione di winget..."],
        ["list.stateLoadingBody"]    = ["Esto puede tardar unos segundos.", "This may take a few seconds.", "Isso pode levar alguns segundos.", "Cela peut prendre quelques secondes.", "L'operazione può richiedere alcuni secondi."],
        ["list.stateInitialTitle"]   = ["Todavía no hay datos", "No data yet", "Ainda não há dados", "Aucune donnée pour l'instant", "Ancora nessun dato"],
        ["list.stateInitialBody"]    = ["Pulsa \"Consultar actualizaciones\" para ver qué programas tienen una versión nueva.", "Click \"Check for updates\" to see which programs have a new version.", "Clique em \"Consultar atualizações\" para ver quais programas têm uma versão nova.", "Cliquez sur « Rechercher des mises à jour » pour voir quels programmes ont une nouvelle version.", "Premi \"Cerca aggiornamenti\" per vedere quali programmi hanno una nuova versione."],
        ["list.stateUpToDateTitle"]  = ["Todo está al día", "Everything is up to date", "Está tudo em dia", "Tout est à jour", "È tutto aggiornato"],
        ["list.stateUpToDateBody"]   = ["winget no encontró actualizaciones pendientes para tus programas.", "winget found no pending updates for your programs.", "O winget não encontrou atualizações pendentes para seus programas.", "winget n'a trouvé aucune mise à jour en attente pour vos programmes.", "winget non ha trovato aggiornamenti in sospeso per i tuoi programmi."],
        ["list.stateNoMatchTitle"]   = ["Sin coincidencias", "No matches", "Sem correspondências", "Aucun résultat", "Nessuna corrispondenza"],
        ["list.stateNoMatchSearch"]  = ["Ningún programa coincide con \"{0}\".", "No program matches \"{0}\".", "Nenhum programa corresponde a \"{0}\".", "Aucun programme ne correspond à « {0} ».", "Nessun programma corrisponde a \"{0}\"."],
        ["list.stateNoMatchFilters"] = ["Ningún programa coincide con los filtros activos.", "No program matches the active filters.", "Nenhum programa corresponde aos filtros ativos.", "Aucun programme ne correspond aux filtres actifs.", "Nessun programma corrisponde ai filtri attivi."],
        ["list.stateCancelledTitle"] = ["Consulta cancelada", "Query cancelled", "Consulta cancelada", "Recherche annulée", "Verifica annullata"],
        ["list.stateCancelledBody"]  = ["Pulsa \"Consultar actualizaciones\" para volver a intentarlo.", "Click \"Check for updates\" to try again.", "Clique em \"Consultar atualizações\" para tentar novamente.", "Cliquez sur « Rechercher des mises à jour » pour réessayer.", "Premi \"Cerca aggiornamenti\" per riprovare."],
        ["list.stateErrorTitle"]     = ["No se pudo consultar winget", "Could not query winget", "Não foi possível consultar o winget", "Impossible d'interroger winget", "Impossibile interrogare winget"],
        ["list.stateErrorBody"]      = ["Revisa el registro de actividad para ver el detalle del error.", "Check the activity log for the error details.", "Verifique o registro de atividade para ver os detalhes do erro.", "Consultez le journal d'activité pour le détail de l'erreur.", "Controlla il registro attività per i dettagli dell'errore."],

        ["log.header"]        = ["Actividad y resultados", "Activity and results", "Atividade e resultados", "Activité et résultats", "Attività e risultati"],
        ["status.ready"]      = ["Listo.", "Ready.", "Pronto.", "Prêt.", "Pronto."],
        ["status.readyToStart"] = ["Listo. Pulsa 'Consultar actualizaciones' para comenzar.", "Ready. Click 'Check for updates' to start.", "Pronto. Clique em 'Consultar atualizações' para começar.", "Prêt. Cliquez sur « Rechercher des mises à jour » pour commencer.", "Pronto. Premi 'Cerca aggiornamenti' per iniziare."],
        ["status.cancelling"] = ["Cancelando...", "Cancelling...", "Cancelando...", "Annulation...", "Annullamento..."],
        ["status.cancellingAfterCurrent"] = ["Cancelando después de la operación actual...", "Cancelling after the current operation...", "Cancelando após a operação atual...", "Annulation après l'opération en cours...", "Annullamento dopo l'operazione corrente..."],
        ["pkg.excluded"]      = ["Excluido de actualizaciones automáticas", "Excluded from automatic updates", "Excluído das atualizações automáticas", "Exclu des mises à jour automatiques", "Escluso dagli aggiornamenti automatici"],
        ["pkg.readyToUpdate"] = ["Listo para actualizar", "Ready to update", "Pronto para atualizar", "Prêt à mettre à jour", "Pronto per l'aggiornamento"],
        ["winget.unavailableStatus"] = ["winget no está disponible. Instálalo desde Microsoft Store.", "winget is not available. Install it from the Microsoft Store.", "O winget não está disponível. Instale-o pela Microsoft Store.", "winget n'est pas disponible. Installez-le depuis le Microsoft Store.", "winget non è disponibile. Installalo dal Microsoft Store."],
        ["winget.unavailableDetail"]  = ["La aplicación necesita App Installer o una versión reciente de Windows.", "The app needs App Installer or a recent version of Windows.", "O aplicativo precisa do App Installer ou de uma versão recente do Windows.", "L'application nécessite App Installer ou une version récente de Windows.", "L'app richiede App Installer o una versione recente di Windows."],
        ["winget.unavailableTitle"]   = ["winget no disponible", "winget not available", "winget não disponível", "winget non disponible", "winget non disponibile"],
        ["winget.unavailableBody"]    = ["No se encontró winget en el sistema.\n\nInstala 'App Installer' desde la Microsoft Store o actualiza Windows.", "winget was not found on this system.\n\nInstall 'App Installer' from the Microsoft Store or update Windows.", "O winget não foi encontrado no sistema.\n\nInstale o 'App Installer' pela Microsoft Store ou atualize o Windows.", "winget est introuvable sur ce système.\n\nInstallez « App Installer » depuis le Microsoft Store ou mettez à jour Windows.", "winget non è stato trovato nel sistema.\n\nInstalla 'App Installer' dal Microsoft Store o aggiorna Windows."],

        // ── Context menu (fila de paquete) ─────────────────────────
        ["ctx.update"]        = ["Actualizar este programa", "Update this program", "Atualizar este programa", "Mettre à jour ce programme", "Aggiorna questo programma"],
        ["ctx.copyName"]      = ["Copiar nombre", "Copy name", "Copiar nome", "Copier le nom", "Copia nome"],
        ["ctx.copyId"]        = ["Copiar Id", "Copy Id", "Copiar Id", "Copier l'Id", "Copia Id"],
        ["ctx.viewOnWingetRun"] = ["Ver en winget.run", "View on winget.run", "Ver no winget.run", "Voir sur winget.run", "Vedi su winget.run"],
        ["ctx.exclude"]       = ["Excluir de actualizaciones", "Exclude from updates", "Excluir das atualizações", "Exclure des mises à jour", "Escludi dagli aggiornamenti"],
        ["ctx.include"]       = ["Volver a incluir en actualizaciones", "Include in updates again", "Voltar a incluir nas atualizações", "Réinclure dans les mises à jour", "Includi di nuovo negli aggiornamenti"],

        // ── Consulta y actualización de paquetes ────────────────────
        ["info.title"]        = ["Información", "Information", "Informação", "Information", "Informazione"],
        ["error.title"]       = ["Error", "Error", "Erro", "Erreur", "Errore"],
        ["error.updateTitle"] = ["Error de actualización", "Update error", "Erro de atualização", "Erreur de mise à jour", "Errore di aggiornamento"],
        ["error.genericPrefix"] = ["Error: {0}", "Error: {0}", "Erro: {0}", "Erreur : {0}", "Errore: {0}"],
        ["msg.noPackagesSelected"] = ["No hay programas seleccionados para actualizar.", "No programs selected to update.", "Nenhum programa selecionado para atualizar.", "Aucun programme sélectionné pour la mise à jour.", "Nessun programma selezionato per l'aggiornamento."],
        ["msg.noPackagesToUpdate"] = ["No hay programas para actualizar.", "No programs to update.", "Nenhum programa para atualizar.", "Aucun programme à mettre à jour.", "Nessun programma da aggiornare."],
        ["msg.noDataToExport"] = ["No hay datos para exportar.", "No data to export.", "Nenhum dado para exportar.", "Aucune donnée à exporter.", "Nessun dato da esportare."],
        ["msg.historySaveError"] = ["No se pudo guardar el historial de actualizaciones.", "Could not save the update history.", "Não foi possível salvar o histórico de atualizações.", "Impossible d'enregistrer l'historique des mises à jour.", "Impossibile salvare la cronologia degli aggiornamenti."],
        // Un único mensaje para toda la ventana de Configuración: desde el Tier C #5 todas las
        // preferencias se guardan de una vez al pulsar "Guardar", así que no hay un fallo por ajuste.
        ["msg.saveSettingsError"] = ["No se pudieron guardar los cambios.", "Could not save the changes.", "Não foi possível salvar as alterações.", "Impossible d'enregistrer les modifications.", "Impossibile salvare le modifiche."],
        ["msg.saveExclusionsError"] = ["No se pudieron guardar las exclusiones.", "Could not save the exclusions.", "Não foi possível salvar as exclusões.", "Impossible d'enregistrer les exclusions.", "Impossibile salvare le esclusioni."],

        ["admin.confirmTitle"] = ["Confirmar modo administrador", "Confirm administrator mode", "Confirmar modo administrador", "Confirmer le mode administrateur", "Conferma modalità amministratore"],
        ["admin.confirmSingleBody"] = ["La actualización se ejecutará con permisos de administrador.\n\nWindows mostrará el aviso de UAC y el progreso detallado se reemplazará por un indicador general.\n\n¿Desea continuar?", "The update will run with administrator permissions.\n\nWindows will show the UAC prompt and the detailed progress will be replaced by a general indicator.\n\nDo you want to continue?", "A atualização será executada com permissões de administrador.\n\nO Windows mostrará o aviso de UAC e o progresso detalhado será substituído por um indicador geral.\n\nDeseja continuar?", "La mise à jour s'exécutera avec des droits d'administrateur.\n\nWindows affichera l'invite UAC et la progression détaillée sera remplacée par un indicateur général.\n\nVoulez-vous continuer ?", "L'aggiornamento verrà eseguito con i permessi di amministratore.\n\nWindows mostrerà la richiesta UAC e il progresso dettagliato sarà sostituito da un indicatore generale.\n\nVuoi continuare?"],
        ["admin.confirmBatchBody"] = ["Las {0} actualizaciones se ejecutarán con permisos de administrador.\n\nWindows pedirá confirmación de UAC una sola vez para todo el lote y el progreso detallado se reemplazará por un indicador general.\n\n¿Desea continuar?", "The {0} updates will run with administrator permissions.\n\nWindows will ask for UAC confirmation once for the whole batch and the detailed progress will be replaced by a general indicator.\n\nDo you want to continue?", "As {0} atualizações serão executadas com permissões de administrador.\n\nO Windows pedirá confirmação de UAC uma única vez para todo o lote e o progresso detalhado será substituído por um indicador geral.\n\nDeseja continuar?", "Les {0} mises à jour s'exécuteront avec des droits d'administrateur.\n\nWindows demandera une confirmation UAC une seule fois pour tout le lot et la progression détaillée sera remplacée par un indicateur général.\n\nVoulez-vous continuer ?", "I {0} aggiornamenti verranno eseguiti con i permessi di amministratore.\n\nWindows chiederà la conferma UAC una sola volta per l'intero lotto e il progresso dettagliato sarà sostituito da un indicatore generale.\n\nVuoi continuare?"],

        ["status.updating"]         = ["Actualizando ({0}/{1}): {2}...", "Updating ({0}/{1}): {2}...", "Atualizando ({0}/{1}): {2}...", "Mise à jour ({0}/{1}) : {2}...", "Aggiornamento ({0}/{1}): {2}..."],
        ["status.updatingProgress"] = ["Actualizando ({0}/{1}): {2}", "Updating ({0}/{1}): {2}", "Atualizando ({0}/{1}): {2}", "Mise à jour ({0}/{1}) : {2}", "Aggiornamento ({0}/{1}): {2}"],
        ["status.cancelledCompleted"] = ["Cancelado. Completados: {0}, Fallidos: {1}.", "Cancelled. Completed: {0}, Failed: {1}.", "Cancelado. Concluídos: {0}, Falharam: {1}.", "Annulé. Terminés : {0}, Échoués : {1}.", "Annullato. Completati: {0}, Falliti: {1}."],
        ["status.completedSuccessReload"] = ["Completada. Éxito: {0}, Fallidos: {1}. Actualizando lista...", "Completed. Success: {0}, Failed: {1}. Refreshing list...", "Concluída. Sucesso: {0}, Falharam: {1}. Atualizando lista...", "Terminé. Réussites : {0}, Échecs : {1}. Actualisation de la liste...", "Completato. Riusciti: {0}, Falliti: {1}. Aggiornamento elenco..."],
        ["status.updateCompleted"] = ["Actualización completada. Éxito: {0}, Fallidos: {1}.", "Update completed. Success: {0}, Failed: {1}.", "Atualização concluída. Sucesso: {0}, Falharam: {1}.", "Mise à jour terminée. Réussites : {0}, Échecs : {1}.", "Aggiornamento completato. Riusciti: {0}, Falliti: {1}."],
        ["status.updateError"] = ["Error al actualizar programas.", "Error updating programs.", "Erro ao atualizar programas.", "Erreur lors de la mise à jour des programmes.", "Errore nell'aggiornamento dei programmi."],
        ["status.adminUpdatingSingle"] = ["Actualizando en modo administrador: {0}...", "Updating in administrator mode: {0}...", "Atualizando em modo administrador: {0}...", "Mise à jour en mode administrateur : {0}...", "Aggiornamento in modalità amministratore: {0}..."],
        ["status.adminUpdatingBatch"] = ["Actualizando {0} programas en modo administrador...", "Updating {0} programs in administrator mode...", "Atualizando {0} programas em modo administrador...", "Mise à jour de {0} programmes en mode administrateur...", "Aggiornamento di {0} programmi in modalità amministratore..."],
        ["status.adminUpdatingProgress"] = ["Actualizando ({0}/{1}) en modo administrador: {2}...", "Updating ({0}/{1}) in administrator mode: {2}...", "Atualizando ({0}/{1}) em modo administrador: {2}...", "Mise à jour ({0}/{1}) en mode administrateur : {2}...", "Aggiornamento ({0}/{1}) in modalità amministratore: {2}..."],
        ["status.processingResult"] = ["Procesando resultado ({0}/{1}): {2}...", "Processing result ({0}/{1}): {2}...", "Processando resultado ({0}/{1}): {2}...", "Traitement du résultat ({0}/{1}) : {2}...", "Elaborazione risultato ({0}/{1}): {2}..."],
        ["status.listExported"] = ["Lista exportada: {0}", "List exported: {0}", "Lista exportada: {0}", "Liste exportée : {0}", "Elenco esportato: {0}"],

        ["log.adminSingleSession"] = ["Ejecutando la actualización en una única sesión de administrador.", "Running the update in a single administrator session.", "Executando a atualização em uma única sessão de administrador.", "Exécution de la mise à jour dans une seule session administrateur.", "Esecuzione dell'aggiornamento in un'unica sessione amministratore."],
        ["log.adminBatchSession"] = ["Ejecutando {0} actualizaciones en una única sesión de administrador.", "Running {0} updates in a single administrator session.", "Executando {0} atualizações em uma única sessão de administrador.", "Exécution de {0} mises à jour dans une seule session administrateur.", "Esecuzione di {0} aggiornamenti in un'unica sessione amministratore."],
        ["log.adminCancelledBeforeStart"] = ["La operación se canceló antes de iniciar el lote elevado.", "The operation was cancelled before starting the elevated batch.", "A operação foi cancelada antes de iniciar o lote elevado.", "L'opération a été annulée avant le démarrage du lot élevé.", "L'operazione è stata annullata prima di avviare il lotto elevato."],
        ["log.preparingElevatedBatch"] = ["Preparando lote elevado...", "Preparing elevated batch...", "Preparando lote elevado...", "Préparation du lot élevé...", "Preparazione del lotto elevato..."],
        ["log.cancellingAfterCurrent"] = ["Cancelando después del paquete actual...", "Cancelling after the current package...", "Cancelando após o pacote atual...", "Annulation après le paquet actuel...", "Annullamento dopo il pacchetto corrente..."],
        ["log.elevatedBatchFinished"] = ["Lote elevado finalizado. Procesando resultados...", "Elevated batch finished. Processing results...", "Lote elevado finalizado. Processando resultados...", "Lot élevé terminé. Traitement des résultats...", "Lotto elevato terminato. Elaborazione dei risultati..."],

        ["msg.noElevatedResult"] = ["No se recibió un resultado de la actualización elevada.", "No result was received from the elevated update.", "Nenhum resultado recebido da atualização elevada.", "Aucun résultat reçu de la mise à jour élevée.", "Nessun risultato ricevuto dall'aggiornamento elevato."],

        // Resumen único al terminar el lote (antes: un diálogo modal por cada paquete fallido).
        ["error.failedSummarySingle"] = ["No se pudo actualizar 1 programa:", "1 program could not be updated:", "Não foi possível atualizar 1 programa:", "1 programme n'a pas pu être mis à jour :", "Non è stato possibile aggiornare 1 programma:"],
        ["error.failedSummaryHeader"] = ["No se pudieron actualizar {0} programas:", "{0} programs could not be updated:", "Não foi possível atualizar {0} programas:", "{0} programmes n'ont pas pu être mis à jour :", "Non è stato possibile aggiornare {0} programmi:"],
        ["error.failedSummaryFooter"] = ["Por seguridad, la aplicación no abre búsquedas web automáticas para descargas manuales. Usa el Id del paquete para verificar el sitio oficial del proveedor o revisarlo directamente con winget.", "For safety, the app does not open automatic web searches for manual downloads. Use the package Id to verify the vendor's official site or check it directly with winget.", "Por segurança, o aplicativo não abre buscas na web automáticas para downloads manuais. Use o Id do pacote para verificar o site oficial do fornecedor ou consultá-lo diretamente com o winget.", "Par sécurité, l'application n'ouvre pas de recherches web automatiques pour les téléchargements manuels. Utilisez l'Id du paquet pour vérifier le site officiel de l'éditeur ou le consulter directement avec winget.", "Per sicurezza, l'app non apre ricerche web automatiche per download manuali. Usa l'Id del pacchetto per verificare il sito ufficiale del fornitore o controllarlo direttamente con winget."],
        ["error.cannotUpdateBody"] = ["No se pudo actualizar \"{0}\" (Id: {1}).\n\nMotivo: {2}\n\nPor seguridad, la aplicación no abrirá búsquedas web automáticas para descargas manuales.\nUse el Id del paquete ({1}) para verificar manualmente el sitio oficial del proveedor o revisar el paquete directamente con winget.", "Could not update \"{0}\" (Id: {1}).\n\nReason: {2}\n\nFor safety, the app will not open automatic web searches for manual downloads.\nUse the package Id ({1}) to manually verify the vendor's official site or check the package directly with winget.", "Não foi possível atualizar \"{0}\" (Id: {1}).\n\nMotivo: {2}\n\nPor segurança, o aplicativo não abrirá buscas na web automáticas para downloads manuais.\nUse o Id do pacote ({1}) para verificar manualmente o site oficial do fornecedor ou consultar o pacote diretamente com o winget.", "Impossible de mettre à jour « {0} » (Id : {1}).\n\nMotif : {2}\n\nPar sécurité, l'application n'ouvrira pas de recherches web automatiques pour les téléchargements manuels.\nUtilisez l'Id du paquet ({1}) pour vérifier manuellement le site officiel de l'éditeur ou consulter le paquet directement avec winget.", "Impossibile aggiornare \"{0}\" (Id: {1}).\n\nMotivo: {2}\n\nPer sicurezza, l'app non aprirà ricerche web automatiche per download manuali.\nUsa l'Id del pacchetto ({1}) per verificare manualmente il sito ufficiale del fornitore o controllare il pacchetto direttamente con winget."],

        ["confirm.updateTitle"] = ["Confirmar actualización", "Confirm update", "Confirmar atualização", "Confirmer la mise à jour", "Conferma aggiornamento"],
        ["confirm.updateBody"] = ["Se van a actualizar {0} programa(s):\n\n  • {1}\n\n¿Desea continuar?", "{0} program(s) will be updated:\n\n  • {1}\n\nDo you want to continue?", "{0} programa(s) serão atualizados:\n\n  • {1}\n\nDeseja continuar?", "{0} programme(s) vont être mis à jour :\n\n  • {1}\n\nVoulez-vous continuer ?", "Verranno aggiornati {0} programma/i:\n\n  • {1}\n\nVuoi continuare?"],
        ["confirm.openWingetRunTitle"] = ["Abrir en winget.run", "Open on winget.run", "Abrir no winget.run", "Ouvrir sur winget.run", "Apri su winget.run"],
        ["confirm.openWingetRunBody"] = ["Se abrirá la página del paquete en su navegador:\n\n{0}\n\nVerifique que el paquete es legítimo antes de instalar nada. ¿Desea continuar?", "The package page will open in your browser:\n\n{0}\n\nVerify the package is legitimate before installing anything. Do you want to continue?", "A página do pacote será aberta no seu navegador:\n\n{0}\n\nVerifique se o pacote é legítimo antes de instalar algo. Deseja continuar?", "La page du paquet s'ouvrira dans votre navigateur :\n\n{0}\n\nVérifiez que le paquet est légitime avant d'installer quoi que ce soit. Voulez-vous continuer ?", "La pagina del pacchetto si aprirà nel browser:\n\n{0}\n\nVerifica che il pacchetto sia legittimo prima di installare qualsiasi cosa. Vuoi continuare?"],

        ["export.txtFormat"] = ["Texto", "Text", "Texto", "Texte", "Testo"],
        ["export.colCurrentVersion"] = ["Versión actual", "Current version", "Versão atual", "Version actuelle", "Versione attuale"],

        ["pkg.loading"]    = ["Cargando...", "Loading...", "Carregando...", "Chargement...", "Caricamento..."],

        ["error.saveConfigStatus"] = ["No se pudo guardar la configuración.", "Could not save the settings.", "Não foi possível salvar a configuração.", "Impossible d'enregistrer la configuration.", "Impossibile salvare la configurazione."],
        ["error.configTitle"] = ["Error de configuración", "Configuration error", "Erro de configuração", "Erreur de configuration", "Errore di configurazione"],
        ["msg.configResetTitle"] = ["Configuración restablecida", "Settings reset", "Configuração restaurada", "Configuration réinitialisée", "Configurazione ripristinata"],

        ["update.newVersionTitle"] = ["Nueva versión {0} disponible", "New version {0} available", "Nova versão {0} disponível", "Nouvelle version {0} disponible", "Nuova versione {0} disponibile"],
        ["update.pressInstallNow"] = ["Pulsa 'Instalar ahora' para descargar e instalar automáticamente.", "Click 'Install now' to download and install automatically.", "Clique em 'Instalar agora' para baixar e instalar automaticamente.", "Cliquez sur « Installer maintenant » pour télécharger et installer automatiquement.", "Premi 'Installa ora' per scaricare e installare automaticamente."],
        ["btn.downloading"] = ["Descargando...", "Downloading...", "Baixando...", "Téléchargement...", "Download in corso..."],
        ["update.downloadingProgress"] = ["Descargando... {0}{1}", "Downloading... {0}{1}", "Baixando... {0}{1}", "Téléchargement... {0}{1}", "Download in corso... {0}{1}"],
        ["update.installingRestart"] = ["Instalando... La aplicación se reiniciará automáticamente.", "Installing... The app will restart automatically.", "Instalando... O aplicativo será reiniciado automaticamente.", "Installation... L'application redémarrera automatiquement.", "Installazione... L'app si riavvierà automaticamente."],
        ["update.checking"] = ["Comprobando...", "Checking...", "Verificando...", "Vérification...", "Verifica in corso..."],
        ["update.noUpdatesTitle"] = ["Sin actualizaciones", "No updates", "Sem atualizações", "Aucune mise à jour", "Nessun aggiornamento"],
        ["update.noUpdatesBody"] = ["WingetUSoft está actualizado. No hay versiones nuevas disponibles.", "WingetUSoft is up to date. No new versions available.", "O WingetUSoft está atualizado. Não há novas versões disponíveis.", "WingetUSoft est à jour. Aucune nouvelle version disponible.", "WingetUSoft è aggiornato. Nessuna nuova versione disponibile."],
        ["menu.installVersion"] = ["⬆ Instalar WingetUSoft {0}...", "⬆ Install WingetUSoft {0}...", "⬆ Instalar WingetUSoft {0}...", "⬆ Installer WingetUSoft {0}...", "⬆ Installa WingetUSoft {0}..."],

        ["notif.updatedSuccess"] = ["Se actualizaron {0} programa(s) correctamente.", "{0} program(s) were updated successfully.", "{0} programa(s) foram atualizados com sucesso.", "{0} programme(s) mis à jour avec succès.", "{0} programma/i aggiornati correttamente."],
        ["notif.updatedMixed"] = ["Actualizados: {0}. Fallidos: {1}.", "Updated: {0}. Failed: {1}.", "Atualizados: {0}. Falharam: {1}.", "Mis à jour : {0}. Échoués : {1}.", "Aggiornati: {0}. Falliti: {1}."],

        ["status.queryingUpdates"] = ["Consultando actualizaciones disponibles...", "Checking for available updates...", "Consultando atualizações disponíveis...", "Recherche des mises à jour disponibles...", "Verifica degli aggiornamenti disponibili..."],
        ["status.queryingUpdatesUnknown"] = ["Consultando actualizaciones (incluidas desconocidas)...", "Checking for updates (including unknown)...", "Consultando atualizações (incluindo desconhecidas)...", "Recherche des mises à jour (y compris inconnues)...", "Verifica degli aggiornamenti (incluse sconosciute)..."],
        ["list.headerUnknown"] = ["Actualizaciones disponibles (incluidas desconocidas)", "Available updates (including unknown)", "Atualizações disponíveis (incluindo desconhecidas)", "Mises à jour disponibles (y compris inconnues)", "Aggiornamenti disponibili (incluse sconosciute)"],
        ["list.suffixUnknown"] = [" (incluidas desconocidas)", " (including unknown)", " (incluindo desconhecidas)", " (y compris inconnues)", " (incluse sconosciute)"],
        ["status.noUpdatesFound"] = ["No se encontraron actualizaciones disponibles{0}.", "No available updates were found{0}.", "Nenhuma atualização disponível encontrada{0}.", "Aucune mise à jour disponible trouvée{0}.", "Nessun aggiornamento disponibile trovato{0}."],
        ["status.updatesFound"] = ["Se encontraron {0} actualización(es) disponible(s){1}.", "{0} available update(s) were found{1}.", "{0} atualização(ões) disponível(is) encontrada(s){1}.", "{0} mise(s) à jour disponible(s) trouvée(s){1}.", "Trovati {0} aggiornamento/i disponibile/i{1}."],
        ["status.queryCancelled"] = ["Consulta cancelada.", "Query cancelled.", "Consulta cancelada.", "Recherche annulée.", "Verifica annullata."],
        ["status.queryError"] = ["Error al consultar actualizaciones.", "Error checking for updates.", "Erro ao consultar atualizações.", "Erreur lors de la recherche de mises à jour.", "Errore nella verifica degli aggiornamenti."],
        ["eta.remaining"] = ["  ·  {0} restante", "  ·  {0} remaining", "  ·  {0} restante", "  ·  {0} restant", "  ·  {0} rimanente"],
        ["eta.label"] = ["  ·  ETA {0}", "  ·  ETA {0}", "  ·  ETA {0}", "  ·  ETA {0}", "  ·  ETA {0}"],

        // ── SettingsWindow ──────────────────────────────────────────
        ["settings.title"]        = ["Configuración", "Settings", "Configurações", "Paramètres", "Impostazioni"],
        ["settings.subtitle"]     = ["Ajusta las opciones generales de la aplicación.", "Adjust the app's general options.", "Ajuste as opções gerais do aplicativo.", "Ajustez les options générales de l'application.", "Regola le opzioni generali dell'app."],
        ["settings.appearanceTitle"] = ["Apariencia", "Appearance", "Aparência", "Apparence", "Aspetto"],
        ["settings.updatesTitle"]   = ["Actualizaciones", "Updates", "Atualizações", "Mises à jour", "Aggiornamenti"],
        ["settings.logTitle"]       = ["Registro de actividad", "Activity log", "Registro de atividade", "Journal d'activité", "Registro attività"],
        ["settings.intervalHeader"] = ["Intervalo de consulta automática", "Automatic check interval", "Intervalo de consulta automática", "Intervalle de vérification automatique", "Intervallo di verifica automatica"],
        ["settings.intervalOff"]  = ["Desactivada", "Disabled", "Desativada", "Désactivé", "Disattivata"],
        ["settings.interval30"]   = ["Cada 30 minutos", "Every 30 minutes", "A cada 30 minutos", "Toutes les 30 minutes", "Ogni 30 minuti"],
        ["settings.interval60"]   = ["Cada 60 minutos", "Every 60 minutes", "A cada 60 minutos", "Toutes les 60 minutes", "Ogni 60 minuti"],
        ["settings.interval120"]  = ["Cada 120 minutos", "Every 120 minutes", "A cada 120 minutos", "Toutes les 120 minutes", "Ogni 120 minuti"],
        ["settings.logToFile"]    = ["Guardar registro de actividad en archivo", "Save activity log to file", "Salvar registro de atividade em arquivo", "Enregistrer le journal d'activité dans un fichier", "Salva il registro attività su file"],
        ["settings.runAsAdmin"]   = ["Ejecutar actualizaciones como administrador", "Run updates as administrator", "Executar atualizações como administrador", "Exécuter les mises à jour en tant qu'administrateur", "Esegui gli aggiornamenti come amministratore"],
        ["settings.logDirLabel"]  = ["Directorio de registros:", "Log directory:", "Diretório de registros:", "Répertoire des journaux :", "Cartella dei log:"],
        ["settings.notifTrayTitle"] = ["Notificaciones y bandeja", "Notifications and tray", "Notificações e bandeja", "Notifications et barre d'état", "Notifiche e area di notifica"],
        ["settings.showNotifications"] = ["Mostrar notificaciones al completar actualizaciones", "Show notifications when updates complete", "Mostrar notificações ao concluir atualizações", "Afficher des notifications à la fin des mises à jour", "Mostra notifiche al completamento degli aggiornamenti"],
        ["settings.minimizeToTray"] = ["Minimizar a la bandeja del sistema al cerrar", "Minimize to the system tray on close", "Minimizar para a bandeja do sistema ao fechar", "Réduire dans la barre d'état système à la fermeture", "Riduci a icona nell'area di notifica alla chiusura"],
        ["settings.excludedTitle"] = ["Paquetes excluidos", "Excluded packages", "Pacotes excluídos", "Paquets exclus", "Pacchetti esclusi"],
        ["settings.excludedSubtitle"] = ["Estos paquetes no se incluirán en las actualizaciones.", "These packages will not be included in updates.", "Esses pacotes não serão incluídos nas atualizações.", "Ces paquets ne seront pas inclus dans les mises à jour.", "Questi pacchetti non saranno inclusi negli aggiornamenti."],
        ["btn.removeSelected"]    = ["Quitar seleccionado", "Remove selected", "Remover selecionado", "Retirer la sélection", "Rimuovi selezionato"],
        ["btn.clearList"]         = ["Limpiar lista", "Clear list", "Limpar lista", "Effacer la liste", "Cancella elenco"],
        ["btn.save"]              = ["Guardar", "Save", "Salvar", "Enregistrer", "Salva"],

        // ── UninstallWindow ─────────────────────────────────────────
        ["uninstall.windowTitle"] = ["WingetUSoft - Desinstalar programas", "WingetUSoft - Uninstall programs", "WingetUSoft - Desinstalar programas", "WingetUSoft - Désinstaller des programmes", "WingetUSoft - Disinstalla programmi"],
        ["uninstall.titleBar"]    = ["Desinstalar programas", "Uninstall programs", "Desinstalar programas", "Désinstaller des programmes", "Disinstalla programmi"],
        ["uninstall.headerTitle"] = ["Desinstalar programas instalados", "Uninstall installed programs", "Desinstalar programas instalados", "Désinstaller les programmes installés", "Disinstalla i programmi installati"],
        ["uninstall.headerSubtitle"] = ["Selecciona un programa de la lista y pulsa Desinstalar.", "Select a program from the list and click Uninstall.", "Selecione um programa da lista e clique em Desinstalar.", "Sélectionnez un programme dans la liste et cliquez sur Désinstaller.", "Seleziona un programma dall'elenco e premi Disinstalla."],
        ["btn.refreshList"]       = ["Actualizar lista", "Refresh list", "Atualizar lista", "Actualiser la liste", "Aggiorna elenco"],
        ["uninstall.uninstallSelected"] = ["Desinstalar seleccionado", "Uninstall selected", "Desinstalar selecionado", "Désinstaller la sélection", "Disinstalla selezionato"],
        ["uninstall.listHeader"]  = ["Programas instalados", "Installed programs", "Programas instalados", "Programmes installés", "Programmi installati"],
        ["log.activity"]          = ["Actividad", "Activity", "Atividade", "Activité", "Attività"],
        ["uninstall.loadingList"] = ["Cargando lista de programas instalados...", "Loading list of installed programs...", "Carregando lista de programas instalados...", "Chargement de la liste des programmes installés...", "Caricamento elenco programmi installati..."],
        ["uninstall.foundCount"]  = ["Se encontraron {0} programa(s) instalado(s).", "{0} installed program(s) were found.", "{0} programa(s) instalado(s) encontrado(s).", "{0} programme(s) installé(s) trouvé(s).", "Trovati {0} programma/i installato/i."],
        ["uninstall.loadCancelled"] = ["Carga cancelada.", "Loading cancelled.", "Carregamento cancelado.", "Chargement annulé.", "Caricamento annullato."],
        ["uninstall.loadError"]   = ["Error al cargar la lista.", "Error loading the list.", "Erro ao carregar a lista.", "Erreur lors du chargement de la liste.", "Errore nel caricamento dell'elenco."],
        ["uninstall.countAll"]    = ["{0} programa(s)", "{0} program(s)", "{0} programa(s)", "{0} programme(s)", "{0} programma/i"],
        ["uninstall.countFiltered"] = ["{0} de {1}", "{0} of {1}", "{0} de {1}", "{0} sur {1}", "{0} di {1}"],
        ["uninstall.confirmTitle"] = ["Confirmar desinstalación", "Confirm uninstall", "Confirmar desinstalação", "Confirmer la désinstallation", "Conferma disinstallazione"],
        ["uninstall.confirmBody"] = ["¿Desea desinstalar \"{0}\" ({1})?\n\nEsta acción no se puede deshacer.", "Do you want to uninstall \"{0}\" ({1})?\n\nThis action cannot be undone.", "Deseja desinstalar \"{0}\" ({1})?\n\nEsta ação não pode ser desfeita.", "Voulez-vous désinstaller « {0} » ({1}) ?\n\nCette action est irréversible.", "Vuoi disinstallare \"{0}\" ({1})?\n\nQuesta azione non può essere annullata."],
        ["uninstall.uninstalling"] = ["Desinstalando: {0}...", "Uninstalling: {0}...", "Desinstalando: {0}...", "Désinstallation : {0}...", "Disinstallazione: {0}..."],
        ["uninstall.startingLog"] = ["Iniciando desinstalación: {0} ({1})", "Starting uninstall: {0} ({1})", "Iniciando desinstalação: {0} ({1})", "Démarrage de la désinstallation : {0} ({1})", "Avvio disinstallazione: {0} ({1})"],
        ["uninstall.successLog"]  = ["  ✔ {0}: desinstalado correctamente.", "  ✔ {0}: uninstalled successfully.", "  ✔ {0}: desinstalado com sucesso.", "  ✔ {0} : désinstallé avec succès.", "  ✔ {0}: disinstallato correttamente."],
        ["uninstall.successStatus"] = ["Desinstalado correctamente: {0}", "Successfully uninstalled: {0}", "Desinstalado com sucesso: {0}", "Désinstallé avec succès : {0}", "Disinstallato correttamente: {0}"],
        ["uninstall.errorLog"]    = ["  ✖ {0}: {1}", "  ✖ {0}: {1}", "  ✖ {0}: {1}", "  ✖ {0} : {1}", "  ✖ {0}: {1}"],
        ["uninstall.errorStatus"] = ["Error al desinstalar.", "Error uninstalling.", "Erro ao desinstalar.", "Erreur lors de la désinstallation.", "Errore durante la disinstallazione."],
        ["uninstall.errorTitle"]  = ["Error de desinstalación", "Uninstall error", "Erro de desinstalação", "Erreur de désinstallation", "Errore di disinstallazione"],
        ["uninstall.errorBody"]   = ["No se pudo desinstalar \"{0}\".\n\nMotivo: {1}", "Could not uninstall \"{0}\".\n\nReason: {1}", "Não foi possível desinstalar \"{0}\".\n\nMotivo: {1}", "Impossible de désinstaller « {0} ».\n\nMotif : {1}", "Impossibile disinstallare \"{0}\".\n\nMotivo: {1}"],
        ["uninstall.cancelledLog"] = ["Desinstalación cancelada.", "Uninstall cancelled.", "Desinstalação cancelada.", "Désinstallation annulée.", "Disinstallazione annullata."],
        ["status.cancelled"]      = ["Cancelado.", "Cancelled.", "Cancelado.", "Annulé.", "Annullato."],
        ["log.genericError"]      = ["  ✖ Error: {0}", "  ✖ Error: {0}", "  ✖ Erro: {0}", "  ✖ Erreur : {0}", "  ✖ Errore: {0}"],

        // ── CleanupWindow ───────────────────────────────────────────
        ["cleanup.windowTitle"]  = ["WingetUSoft - Limpieza de residuos", "WingetUSoft - Leftover cleanup", "WingetUSoft - Limpeza de resíduos", "WingetUSoft - Nettoyage des résidus", "WingetUSoft - Pulizia dei residui"],
        ["cleanup.titleBar"]     = ["Limpieza de residuos", "Leftover cleanup", "Limpeza de resíduos", "Nettoyage des résidus", "Pulizia dei residui"],
        ["cleanup.headerTitle"]  = ["Limpieza de residuos tras la desinstalación", "Cleanup of leftovers after uninstalling", "Limpeza de resíduos após a desinstalação", "Nettoyage des résidus après la désinstallation", "Pulizia dei residui dopo la disinstallazione"],
        ["cleanup.scanning"]     = ["Buscando residuos...", "Searching for leftovers...", "Procurando resíduos...", "Recherche de résidus...", "Ricerca dei residui..."],
        ["cleanup.warning"]      = ["⚠ Verifica cuidadosamente cada elemento antes de eliminar. Esta acción no se puede deshacer.", "⚠ Carefully check each item before deleting. This action cannot be undone.", "⚠ Verifique cuidadosamente cada item antes de excluir. Esta ação não pode ser desfeita.", "⚠ Vérifiez soigneusement chaque élément avant de supprimer. Cette action est irréversible.", "⚠ Verifica attentamente ogni elemento prima di eliminare. Questa azione non può essere annullata."],
        ["btn.rescan"]           = ["Volver a escanear", "Scan again", "Escanear novamente", "Analyser à nouveau", "Ripeti scansione"],
        ["btn.deleteSelected"]   = ["Eliminar seleccionados", "Delete selected", "Excluir selecionados", "Supprimer la sélection", "Elimina selezionati"],
        ["btn.selectAll"]        = ["Seleccionar todo", "Select all", "Selecionar tudo", "Tout sélectionner", "Seleziona tutto"],
        ["btn.deselectAll"]      = ["Deseleccionar todo", "Deselect all", "Desmarcar tudo", "Tout désélectionner", "Deseleziona tutto"],
        ["btn.yesDelete"]        = ["Sí, eliminar", "Yes, delete", "Sim, excluir", "Oui, supprimer", "Sì, elimina"],
        ["cleanup.listHeader"]   = ["Residuos encontrados", "Leftovers found", "Resíduos encontrados", "Résidus trouvés", "Residui trovati"],
        ["cleanup.colPath"]      = ["Ruta", "Path", "Caminho", "Chemin", "Percorso"],
        ["cleanup.colType"]      = ["Tipo", "Type", "Tipo", "Type", "Tipo"],
        ["cleanup.colSize"]      = ["Tamaño", "Size", "Tamanho", "Taille", "Dimensione"],
        ["cleanup.colProgram"]   = ["Programa", "Program", "Programa", "Programme", "Programma"],
        ["cleanup.typeFolder"]   = ["Carpeta", "Folder", "Pasta", "Dossier", "Cartella"],
        ["cleanup.typeFile"]     = ["Archivo", "File", "Arquivo", "Fichier", "File"],
        ["cleanup.scanningStatus"] = ["Escaneando residuos...", "Scanning for leftovers...", "Escaneando resíduos...", "Analyse des résidus...", "Scansione dei residui..."],
        ["cleanup.noResiduesFound"] = ["No se encontraron residuos de: {0}.", "No leftovers were found for: {0}.", "Nenhum resíduo encontrado para: {0}.", "Aucun résidu trouvé pour : {0}.", "Nessun residuo trovato per: {0}."],
        ["cleanup.noResiduesStatus"] = ["No se encontraron residuos.", "No leftovers were found.", "Nenhum resíduo encontrado.", "Aucun résidu trouvé.", "Nessun residuo trovato."],
        ["cleanup.potentialResidues"] = ["Residuos potenciales de: {0}. Verifica cada elemento antes de eliminar.", "Potential leftovers from: {0}. Check each item before deleting.", "Resíduos potenciais de: {0}. Verifique cada item antes de excluir.", "Résidus potentiels de : {0}. Vérifiez chaque élément avant de supprimer.", "Residui potenziali di: {0}. Verifica ogni elemento prima di eliminare."],
        ["cleanup.foundResidues"] = ["Se encontraron {0} residuo(s) potencial(es).", "{0} potential leftover(s) were found.", "{0} resíduo(s) potencial(is) encontrado(s).", "{0} résidu(s) potentiel(s) trouvé(s).", "Trovati {0} residuo/i potenziale/i."],
        ["cleanup.scanCancelled"] = ["Escaneo cancelado.", "Scan cancelled.", "Escaneamento cancelado.", "Analyse annulée.", "Scansione annullata."],
        ["cleanup.scanError"]    = ["Error durante el escaneo.", "Error during the scan.", "Erro durante o escaneamento.", "Erreur pendant l'analyse.", "Errore durante la scansione."],
        ["cleanup.noSelectionTitle"] = ["Sin selección", "No selection", "Sem seleção", "Aucune sélection", "Nessuna selezione"],
        ["cleanup.noSelectionBody"] = ["No hay elementos seleccionados para eliminar.", "No items selected to delete.", "Nenhum item selecionado para excluir.", "Aucun élément sélectionné à supprimer.", "Nessun elemento selezionato da eliminare."],
        ["cleanup.confirmDeleteTitle"] = ["Confirmar eliminación", "Confirm deletion", "Confirmar exclusão", "Confirmer la suppression", "Conferma eliminazione"],
        ["cleanup.confirmDeleteBody"] = ["¿Eliminar {0} elemento(s) seleccionado(s)?\n\nEsta acción no se puede deshacer.", "Delete {0} selected item(s)?\n\nThis action cannot be undone.", "Excluir {0} item(ns) selecionado(s)?\n\nEsta ação não pode ser desfeita.", "Supprimer {0} élément(s) sélectionné(s) ?\n\nCette action est irréversible.", "Eliminare {0} elemento/i selezionato/i?\n\nQuesta azione non può essere annullata."],
        ["cleanup.deletedLog"]   = ["  ✔ Eliminado: {0}", "  ✔ Deleted: {0}", "  ✔ Excluído: {0}", "  ✔ Supprimé : {0}", "  ✔ Eliminato: {0}"],
        ["cleanup.noPermissionLog"] = ["  ✖ Sin permisos: {0}", "  ✖ No permission: {0}", "  ✖ Sem permissão: {0}", "  ✖ Autorisation refusée : {0}", "  ✖ Permessi insufficienti: {0}"],
        ["cleanup.deleteErrorLog"] = ["  ✖ Error en {0}: {1}", "  ✖ Error in {0}: {1}", "  ✖ Erro em {0}: {1}", "  ✖ Erreur dans {0} : {1}", "  ✖ Errore in {0}: {1}"],
        ["cleanup.completedStatus"] = ["Completado — Eliminados: {0} | Errores: {1}", "Completed — Deleted: {0} | Errors: {1}", "Concluído — Excluídos: {0} | Erros: {1}", "Terminé — Supprimés : {0} | Erreurs : {1}", "Completato — Eliminati: {0} | Errori: {1}"],

        // ── HistoryWindow ───────────────────────────────────────────
        ["history.titleBar"]     = ["Historial de actualizaciones", "Update history", "Histórico de atualizações", "Historique des mises à jour", "Cronologia aggiornamenti"],
        ["history.headerTitle"]  = ["Historial reciente", "Recent history", "Histórico recente", "Historique récent", "Cronologia recente"],
        ["history.noEntriesYet"] = ["Aún no se ha registrado ninguna actualización desde la aplicación.", "No updates have been recorded from the app yet.", "Ainda não foi registrada nenhuma atualização pelo aplicativo.", "Aucune mise à jour n'a encore été enregistrée depuis l'application.", "Nessun aggiornamento ancora registrato dall'app."],
        ["history.noEntriesRow"] = ["No hay entradas en el historial.", "There are no entries in the history.", "Não há entradas no histórico.", "Aucune entrée dans l'historique.", "Nessuna voce nella cronologia."],
        ["history.summaryAll"]   = ["{0} registro(s) cargados. Éxito: {1}. Fallidos: {2}.", "{0} record(s) loaded. Success: {1}. Failed: {2}.", "{0} registro(s) carregado(s). Sucesso: {1}. Falharam: {2}.", "{0} enregistrement(s) chargé(s). Réussites : {1}. Échecs : {2}.", "{0} record caricati. Riusciti: {1}. Falliti: {2}."],
        ["history.summaryFiltered"] = ["{0} de {1} registro(s). Éxito: {2}. Fallidos: {3}.", "{0} of {1} record(s). Success: {2}. Failed: {3}.", "{0} de {1} registro(s). Sucesso: {2}. Falharam: {3}.", "{0} sur {1} enregistrement(s). Réussites : {2}. Échecs : {3}.", "{0} di {1} record. Riusciti: {2}. Falliti: {3}."],
        ["history.exportedTo"]   = ["Historial exportado: {0}", "History exported: {0}", "Histórico exportado: {0}", "Historique exporté : {0}", "Cronologia esportata: {0}"],
        ["history.colDate"]      = ["Fecha", "Date", "Data", "Date", "Data"],
        ["history.colVersionFrom"] = ["Versión origen", "From version", "Versão origem", "Version d'origine", "Versione di origine"],
        ["history.colVersionTo"] = ["Versión destino", "To version", "Versão destino", "Version cible", "Versione di destinazione"],
        ["history.colStatus"]    = ["Estado", "Status", "Status", "État", "Stato"],
        ["history.statusSuccess"] = ["Éxito", "Success", "Sucesso", "Réussite", "Riuscito"],
        ["history.statusFailed"] = ["Fallido", "Failed", "Falhou", "Échec", "Fallito"],
        ["history.stateLabel"]   = ["Estado:", "Status:", "Status:", "État :", "Stato:"],
        ["btn.exportCsv"]        = ["Exportar CSV...", "Export CSV...", "Exportar CSV...", "Exporter en CSV...", "Esporta CSV..."],

        // ── WingetService (modo administrador) ──────────────────────
        ["worker.noResult"]      = ["No se recibió resultado de la actualización elevada.", "No result was received from the elevated update.", "Nenhum resultado recebido da atualização elevada.", "Aucun résultat reçu de la mise à jour élevée.", "Nessun risultato ricevuto dall'aggiornamento elevato."],
        ["worker.requestingAdminSingle"] = ["Solicitando permisos de administrador...", "Requesting administrator permissions...", "Solicitando permissões de administrador...", "Demande des droits d'administrateur...", "Richiesta dei permessi di amministratore..."],
        ["worker.requestingAdminBatch"]  = ["Solicitando permisos de administrador para el lote seleccionado...", "Requesting administrator permissions for the selected batch...", "Solicitando permissões de administrador para o lote selecionado...", "Demande des droits d'administrateur pour le lot sélectionné...", "Richiesta dei permessi di amministratore per il lotto selezionato..."],
        ["worker.elevatedInProgressSingle"] = ["Actualización elevada en curso...", "Elevated update in progress...", "Atualização elevada em andamento...", "Mise à jour élevée en cours...", "Aggiornamento elevato in corso..."],
        ["worker.elevatedInProgressBatch"]  = ["Lote elevado en curso. Se usará una sola elevación para todas las actualizaciones seleccionadas.", "Elevated batch in progress. A single elevation will be used for all selected updates.", "Lote elevado em andamento. Será usada uma única elevação para todas as atualizações selecionadas.", "Lot élevé en cours. Une seule élévation sera utilisée pour toutes les mises à jour sélectionnées.", "Lotto elevato in corso. Verrà utilizzata un'unica elevazione per tutti gli aggiornamenti selezionati."],
        ["worker.sessionNoResults"] = ["La sesión elevada finalizó sin enviar resultados válidos.", "The elevated session ended without sending valid results.", "A sessão elevada terminou sem enviar resultados válidos.", "La session élevée s'est terminée sans envoyer de résultats valides.", "La sessione elevata è terminata senza inviare risultati validi."],
        ["worker.sessionReadError"] = ["No se pudo leer el resultado de la sesión elevada: {0}", "Could not read the elevated session result: {0}", "Não foi possível ler o resultado da sessão elevada: {0}", "Impossible de lire le résultat de la session élevée : {0}", "Impossibile leggere il risultato della sessione elevata: {0}"],
        ["worker.batchExitCode"]  = ["El lote elevado finalizó con el código {0}.", "The elevated batch finished with code {0}.", "O lote elevado terminou com o código {0}.", "Le lot élevé s'est terminé avec le code {0}.", "Il lotto elevato è terminato con il codice {0}."],
        ["worker.elevationCancelled"] = ["La elevación de permisos fue cancelada por el usuario.", "The permission elevation was cancelled by the user.", "A elevação de permissões foi cancelada pelo usuário.", "L'élévation des droits a été annulée par l'utilisateur.", "L'elevazione dei permessi è stata annullata dall'utente."],

        // ── UpgradeResult.GetFailureReason ───────────────────────────
        ["reason.userCancelled"]  = ["Se canceló la elevación de permisos. La actualización no se inició.", "The permission elevation was cancelled. The update did not start.", "A elevação de permissões foi cancelada. A atualização não foi iniciada.", "L'élévation des droits a été annulée. La mise à jour n'a pas démarré.", "L'elevazione dei permessi è stata annullata. L'aggiornamento non è stato avviato."],
        ["reason.noApplicableUpdate"] = ["No se encontró una actualización aplicable para este paquete.", "No applicable update was found for this package.", "Nenhuma atualização aplicável foi encontrada para este pacote.", "Aucune mise à jour applicable n'a été trouvée pour ce paquet.", "Nessun aggiornamento applicabile trovato per questo pacchetto."],
        ["reason.noApplicableInstaller"] = ["No se encontró un instalador compatible para este sistema.", "No compatible installer was found for this system.", "Nenhum instalador compatível foi encontrado para este sistema.", "Aucun programme d'installation compatible n'a été trouvé pour ce système.", "Nessun programma di installazione compatibile trovato per questo sistema."],
        ["reason.hashMismatch"]   = ["El hash del instalador no coincide. El paquete podría haber sido modificado por el proveedor.", "The installer hash does not match. The package may have been modified by the vendor.", "O hash do instalador não corresponde. O pacote pode ter sido modificado pelo fornecedor.", "Le hachage du programme d'installation ne correspond pas. Le paquet a peut-être été modifié par l'éditeur.", "L'hash del programma di installazione non corrisponde. Il pacchetto potrebbe essere stato modificato dal fornitore."],
        ["reason.needsAdmin"]     = ["Se requieren permisos de administrador adicionales para este instalador específico.", "Additional administrator permissions are required for this specific installer.", "São necessárias permissões de administrador adicionais para este instalador específico.", "Des droits d'administrateur supplémentaires sont requis pour ce programme d'installation spécifique.", "Sono richiesti permessi di amministratore aggiuntivi per questo specifico programma di installazione."],
        ["reason.blocked"]        = ["La instalación fue bloqueada por una directiva del sistema.", "The installation was blocked by a system policy.", "A instalação foi bloqueada por uma diretiva do sistema.", "L'installation a été bloquée par une stratégie système.", "L'installazione è stata bloccata da un criterio di sistema."],
        ["reason.currentlyRunning"] = ["El programa está actualmente en ejecución. Ciérrelo e intente de nuevo.", "The program is currently running. Close it and try again.", "O programa está em execução no momento. Feche-o e tente novamente.", "Le programme est en cours d'exécution. Fermez-le et réessayez.", "Il programma è attualmente in esecuzione. Chiudilo e riprova."],
        ["reason.notFound"]       = ["No se encontró el paquete en los orígenes configurados.", "The package was not found in the configured sources.", "O pacote não foi encontrado nas fontes configuradas.", "Le paquet n'a pas été trouvé dans les sources configurées.", "Il pacchetto non è stato trovato nelle origini configurate."],
        ["reason.networkError"]   = ["Error de red. Verifique su conexión a internet.", "Network error. Check your internet connection.", "Erro de rede. Verifique sua conexão com a internet.", "Erreur réseau. Vérifiez votre connexion Internet.", "Errore di rete. Verifica la connessione a Internet."],
        ["reason.unknownError"]   = ["Error desconocido (código de salida: {0}).", "Unknown error (exit code: {0}).", "Erro desconhecido (código de saída: {0}).", "Erreur inconnue (code de sortie : {0}).", "Errore sconosciuto (codice di uscita: {0})."],

        ["update.unverifiableInstaller"] = ["El instalador descargado no se pudo verificar (sin firma digital válida ni hash SHA-256 publicado) y fue eliminado por seguridad.", "The downloaded installer could not be verified (no valid digital signature and no published SHA-256 hash) and was deleted for security.", "Não foi possível verificar o instalador baixado (sem assinatura digital válida nem hash SHA-256 publicado) e ele foi excluído por segurança.", "Le programme d'installation téléchargé n'a pas pu être vérifié (pas de signature numérique valide ni de hachage SHA-256 publié) et a été supprimé par sécurité.", "Non è stato possibile verificare il programma di installazione scaricato (nessuna firma digitale valida né hash SHA-256 pubblicato) ed è stato eliminato per sicurezza."],
        ["update.checksumMismatch"] = ["El instalador descargado no coincide con el hash SHA-256 publicado y fue eliminado por seguridad.", "The downloaded installer does not match the published SHA-256 hash and was deleted for security.", "O instalador baixado não corresponde ao hash SHA-256 publicado e foi excluído por segurança.", "Le programme d'installation téléchargé ne correspond pas au hachage SHA-256 publié et a été supprimé par sécurité.", "Il programma di installazione scaricato non corrisponde all'hash SHA-256 pubblicato ed è stato eliminato per sicurezza."],

        ["log.packageNotStarted"] = ["[{0}/{1}] Cancelado antes de iniciar: {2} ({3})", "[{0}/{1}] Cancelled before starting: {2} ({3})", "[{0}/{1}] Cancelado antes de iniciar: {2} ({3})", "[{0}/{1}] Annulé avant le démarrage : {2} ({3})", "[{0}/{1}] Annullato prima dell'avvio: {2} ({3})"],
        ["log.resultUnavailable"] = ["[{0}/{1}] Resultado no disponible: {2} ({3})", "[{0}/{1}] Result not available: {2} ({3})", "[{0}/{1}] Resultado não disponível: {2} ({3})", "[{0}/{1}] Résultat non disponible : {2} ({3})", "[{0}/{1}] Risultato non disponibile: {2} ({3})"],
        ["log.packageFinished"] = ["[{0}/{1}] Finalizado: {2} ({3})", "[{0}/{1}] Finished: {2} ({3})", "[{0}/{1}] Finalizado: {2} ({3})", "[{0}/{1}] Terminé : {2} ({3})", "[{0}/{1}] Terminato: {2} ({3})"],
        ["log.packageRunning"] = ["[{0}/{1}] En ejecución: {2} ({3})", "[{0}/{1}] Running: {2} ({3})", "[{0}/{1}] Em execução: {2} ({3})", "[{0}/{1}] En cours : {2} ({3})", "[{0}/{1}] In esecuzione: {2} ({3})"],
        ["log.upgradeSuccess"] = ["  ✔ {0}: actualizado correctamente.", "  ✔ {0}: updated successfully.", "  ✔ {0}: atualizado com sucesso.", "  ✔ {0} : mis à jour avec succès.", "  ✔ {0}: aggiornato correttamente."],
        ["list.andMore"] = ["\n  ... y {0} más", "\n  ... and {0} more", "\n  ... e mais {0}", "\n  ... et {0} de plus", "\n  ... e altri {0}"],
    };
}
