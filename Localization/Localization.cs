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
        ["menu.options"]      = ["Opciones", "Options", "Opções", "Options", "Opzioni"],
        ["menu.updateMode"]   = ["Modo de actualización", "Update mode", "Modo de atualização", "Mode de mise à jour", "Modalità di aggiornamento"],
        ["menu.silent"]       = ["Silenciosa", "Silent", "Silenciosa", "Silencieux", "Silenzioso"],
        ["menu.interactive"]  = ["Interactiva", "Interactive", "Interativa", "Interactif", "Interattivo"],
        ["menu.theme"]        = ["Tema", "Theme", "Tema", "Thème", "Tema"],
        ["menu.themeSystem"]  = ["Sistema (automático)", "System (automatic)", "Sistema (automático)", "Système (automatique)", "Sistema (automatico)"],
        ["menu.themeLight"]   = ["Claro", "Light", "Claro", "Clair", "Chiaro"],
        ["menu.themeDark"]    = ["Oscuro", "Dark", "Escuro", "Sombre", "Scuro"],
        ["menu.lang"]         = ["Idioma", "Language", "Idioma", "Langue", "Lingua"],
        ["menu.lang.es"]      = ["Español", "Spanish", "Espanhol", "Espagnol", "Spagnolo"],
        ["menu.lang.en"]      = ["Inglés", "English", "Inglês", "Anglais", "Inglese"],
        ["menu.lang.pt"]      = ["Portugués", "Portuguese", "Português", "Portugais", "Portoghese"],
        ["menu.lang.fr"]      = ["Francés", "French", "Francês", "Français", "Francese"],
        ["menu.lang.it"]      = ["Italiano", "Italian", "Italiano", "Italien", "Italiano"],
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

        ["about.title"]         = ["Acerca de WingetUSoft", "About WingetUSoft", "Sobre o WingetUSoft", "À propos de WingetUSoft", "Informazioni su WingetUSoft"],
        ["about.version"]       = ["Versión {0}", "Version {0}", "Versão {0}", "Version {0}", "Versione {0}"],
        ["about.desc"]          = ["Interfaz gráfica para gestionar actualizaciones y desinstalaciones de software mediante winget en Windows.", "A graphical interface to manage software updates and uninstalls via winget on Windows.", "Interface gráfica para gerenciar atualizações e desinstalações de software via winget no Windows.", "Interface graphique pour gérer les mises à jour et désinstallations de logiciels via winget sous Windows.", "Interfaccia grafica per gestire aggiornamenti e disinstallazioni di software tramite winget su Windows."],
        ["about.copyright"]     = ["© 2026 xfiberex. Distribuido bajo la licencia MIT.", "© 2026 xfiberex. Distributed under the MIT license.", "© 2026 xfiberex. Distribuído sob a licença MIT.", "© 2026 xfiberex. Distribué sous licence MIT.", "© 2026 xfiberex. Distribuito con licenza MIT."],
        ["about.privacyHeader"] = ["Privacidad", "Privacy", "Privacidade", "Confidentialité", "Privacy"],
        ["about.privacy"]       = ["WingetUSoft no recopila datos personales ni telemetría. La aplicación se conecta a Internet únicamente para consultar/instalar paquetes vía winget y para comprobar actualizaciones de la propia app en GitHub Releases (HTTPS).", "WingetUSoft does not collect personal data or telemetry. The app connects to the Internet only to query/install packages via winget and to check for app updates on GitHub Releases (HTTPS).", "O WingetUSoft não coleta dados pessoais nem telemetria. O aplicativo se conecta à Internet apenas para consultar/instalar pacotes via winget e para verificar atualizações do próprio app no GitHub Releases (HTTPS).", "WingetUSoft ne collecte aucune donnée personnelle ni télémétrie. L'application se connecte à Internet uniquement pour consulter/installer des paquets via winget et pour vérifier les mises à jour de l'application sur GitHub Releases (HTTPS).", "WingetUSoft non raccoglie dati personali né telemetria. L'app si connette a Internet solo per consultare/installare pacchetti tramite winget e per verificare gli aggiornamenti dell'app su GitHub Releases (HTTPS)."],
        ["about.github"]        = ["Ver en GitHub", "View on GitHub", "Ver no GitHub", "Voir sur GitHub", "Vedi su GitHub"],

        // ── MainWindow ──────────────────────────────────────────────
        ["app.titleBase"]     = ["WingetUSoft - Actualiza tus programas", "WingetUSoft - Update your programs", "WingetUSoft - Atualize seus programas", "WingetUSoft - Mettez à jour vos programmes", "WingetUSoft - Aggiorna i tuoi programmi"],
        ["header.title"]      = ["Actualiza tus programas con winget", "Update your programs with winget", "Atualize seus programas com winget", "Mettez à jour vos programmes avec winget", "Aggiorna i tuoi programmi con winget"],
        ["header.subtitle"]   = ["Consulta, filtra y actualiza paquetes desde una sola vista.", "Check, filter and update packages from a single view.", "Consulte, filtre e atualize pacotes em uma única tela.", "Consultez, filtrez et mettez à jour les paquets depuis une seule vue.", "Consulta, filtra e aggiorna i pacchetti da un'unica schermata."],
        ["header.detailDefault"] = ["Selecciona un programa para ver sus detalles antes de actualizar.", "Select a program to see its details before updating.", "Selecione um programa para ver seus detalhes antes de atualizar.", "Sélectionnez un programme pour voir ses détails avant la mise à jour.", "Seleziona un programma per vedere i dettagli prima di aggiornare."],
        ["header.detailEmpty"]   = ["Todavía no hay datos cargados. Pulsa \"Consultar actualizaciones\" para empezar.", "No data loaded yet. Click \"Check for updates\" to start.", "Ainda não há dados carregados. Clique em \"Consultar atualizações\" para começar.", "Aucune donnée chargée pour l'instant. Cliquez sur \"Rechercher des mises à jour\" pour commencer.", "Nessun dato ancora caricato. Premi \"Cerca aggiornamenti\" per iniziare."],
        ["header.shortcuts"]  = ["Atajos: F5 consultar · Ctrl+A marcar · Supr excluir · Esc cancelar", "Shortcuts: F5 check · Ctrl+A select · Del exclude · Esc cancel", "Atalhos: F5 consultar · Ctrl+A marcar · Del excluir · Esc cancelar", "Raccourcis : F5 vérifier · Ctrl+A sélectionner · Suppr exclure · Échap annuler", "Scorciatoie: F5 verifica · Ctrl+A seleziona · Canc escludi · Esc annulla"],
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
        ["list.colSize"]      = ["Tam.", "Size", "Tam.", "Taille", "Dim."],
        ["list.colSource"]    = ["Fuente", "Source", "Fonte", "Source", "Fonte"],
        ["list.colExcluded"]  = ["Excl.", "Excl.", "Excl.", "Excl.", "Escl."],
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

        // ── Consulta y actualización de paquetes ────────────────────
        ["info.title"]        = ["Información", "Information", "Informação", "Information", "Informazione"],
        ["error.title"]       = ["Error", "Error", "Erro", "Erreur", "Errore"],
        ["error.updateTitle"] = ["Error de actualización", "Update error", "Erro de atualização", "Erreur de mise à jour", "Errore di aggiornamento"],
        ["error.genericPrefix"] = ["Error: {0}", "Error: {0}", "Erro: {0}", "Erreur : {0}", "Errore: {0}"],
        ["msg.noPackagesSelected"] = ["No hay programas seleccionados para actualizar.", "No programs selected to update.", "Nenhum programa selecionado para atualizar.", "Aucun programme sélectionné pour la mise à jour.", "Nessun programma selezionato per l'aggiornamento."],
        ["msg.noPackagesToUpdate"] = ["No hay programas para actualizar.", "No programs to update.", "Nenhum programa para atualizar.", "Aucun programme à mettre à jour.", "Nessun programma da aggiornare."],
        ["msg.noDataToExport"] = ["No hay datos para exportar.", "No data to export.", "Nenhum dado para exportar.", "Aucune donnée à exporter.", "Nessun dato da esportare."],
        ["msg.historySaveError"] = ["No se pudo guardar el historial de actualizaciones.", "Could not save the update history.", "Não foi possível salvar o histórico de atualizações.", "Impossible d'enregistrer l'historique des mises à jour.", "Impossibile salvare la cronologia degli aggiornamenti."],
        ["msg.saveUpdateModeError"] = ["No se pudo guardar el modo de actualización.", "Could not save the update mode.", "Não foi possível salvar o modo de atualização.", "Impossible d'enregistrer le mode de mise à jour.", "Impossibile salvare la modalità di aggiornamento."],
        ["msg.saveThemeError"] = ["No se pudo guardar la configuración visual.", "Could not save the visual settings.", "Não foi possível salvar as configurações visuais.", "Impossible d'enregistrer les paramètres visuels.", "Impossibile salvare le impostazioni visive."],
        ["msg.saveLanguageError"] = ["No se pudo guardar el idioma.", "Could not save the language.", "Não foi possível salvar o idioma.", "Impossible d'enregistrer la langue.", "Impossibile salvare la lingua."],
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
        ["error.cannotUpdateBody"] = ["No se pudo actualizar \"{0}\" (Id: {1}).\n\nMotivo: {2}\n\nPor seguridad, la aplicación no abrirá búsquedas web automáticas para descargas manuales.\nUse el Id del paquete ({1}) para verificar manualmente el sitio oficial del proveedor o revisar el paquete directamente con winget.", "Could not update \"{0}\" (Id: {1}).\n\nReason: {2}\n\nFor safety, the app will not open automatic web searches for manual downloads.\nUse the package Id ({1}) to manually verify the vendor's official site or check the package directly with winget.", "Não foi possível atualizar \"{0}\" (Id: {1}).\n\nMotivo: {2}\n\nPor segurança, o aplicativo não abrirá buscas na web automáticas para downloads manuais.\nUse o Id do pacote ({1}) para verificar manualmente o site oficial do fornecedor ou consultar o pacote diretamente com o winget.", "Impossible de mettre à jour « {0} » (Id : {1}).\n\nMotif : {2}\n\nPar sécurité, l'application n'ouvrira pas de recherches web automatiques pour les téléchargements manuels.\nUtilisez l'Id du paquet ({1}) pour vérifier manuellement le site officiel de l'éditeur ou consulter le paquet directement avec winget.", "Impossibile aggiornare \"{0}\" (Id: {1}).\n\nMotivo: {2}\n\nPer sicurezza, l'app non aprirà ricerche web automatiche per download manuali.\nUsa l'Id del pacchetto ({1}) per verificare manualmente il sito ufficiale del fornitore o controllare il pacchetto direttamente con winget."],

        ["confirm.updateTitle"] = ["Confirmar actualización", "Confirm update", "Confirmar atualização", "Confirmer la mise à jour", "Conferma aggiornamento"],
        ["confirm.updateBody"] = ["Se van a actualizar {0} programa(s):\n\n  • {1}\n\n¿Desea continuar?", "{0} program(s) will be updated:\n\n  • {1}\n\nDo you want to continue?", "{0} programa(s) serão atualizados:\n\n  • {1}\n\nDeseja continuar?", "{0} programme(s) vont être mis à jour :\n\n  • {1}\n\nVoulez-vous continuer ?", "Verranno aggiornati {0} programma/i:\n\n  • {1}\n\nVuoi continuare?"],
        ["confirm.openWingetRunTitle"] = ["Abrir en winget.run", "Open on winget.run", "Abrir no winget.run", "Ouvrir sur winget.run", "Apri su winget.run"],
        ["confirm.openWingetRunBody"] = ["Se abrirá la página del paquete en su navegador:\n\n{0}\n\nVerifique que el paquete es legítimo antes de instalar nada. ¿Desea continuar?", "The package page will open in your browser:\n\n{0}\n\nVerify the package is legitimate before installing anything. Do you want to continue?", "A página do pacote será aberta no seu navegador:\n\n{0}\n\nVerifique se o pacote é legítimo antes de instalar algo. Deseja continuar?", "La page du paquet s'ouvrira dans votre navigateur :\n\n{0}\n\nVérifiez que le paquet est légitime avant d'installer quoi que ce soit. Voulez-vous continuer ?", "La pagina del pacchetto si aprirà nel browser:\n\n{0}\n\nVerifica che il pacchetto sia legittimo prima di installare qualsiasi cosa. Vuoi continuare?"],

        ["export.txtFormat"] = ["Texto", "Text", "Texto", "Texte", "Testo"],
        ["export.colCurrentVersion"] = ["Versión actual", "Current version", "Versão atual", "Version actuelle", "Versione attuale"],

        ["pkg.loading"]    = ["Cargando...", "Loading...", "Carregando...", "Chargement...", "Caricamento..."],
        ["pkg.sizeLabel"]  = ["Tamaño: {0}", "Size: {0}", "Tamanho: {0}", "Taille : {0}", "Dimensione: {0}"],

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
        ["settings.autoCheckTitle"] = ["Consulta automática", "Automatic check", "Consulta automática", "Vérification automatique", "Verifica automatica"],
        ["settings.intervalHeader"] = ["Intervalo de consulta automática", "Automatic check interval", "Intervalo de consulta automática", "Intervalle de vérification automatique", "Intervallo di verifica automatica"],
        ["settings.intervalOff"]  = ["Desactivada", "Disabled", "Desativada", "Désactivé", "Disattivata"],
        ["settings.interval30"]   = ["Cada 30 minutos", "Every 30 minutes", "A cada 30 minutos", "Toutes les 30 minutes", "Ogni 30 minuti"],
        ["settings.interval60"]   = ["Cada 60 minutos", "Every 60 minutes", "A cada 60 minutos", "Toutes les 60 minutes", "Ogni 60 minuti"],
        ["settings.interval120"]  = ["Cada 120 minutos", "Every 120 minutes", "A cada 120 minutos", "Toutes les 120 minutes", "Ogni 120 minuti"],
        ["settings.optionsTitle"] = ["Opciones", "Options", "Opções", "Options", "Opzioni"],
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

        ["update.unsignedInstaller"] = ["El instalador descargado no tiene una firma digital válida y fue eliminado por seguridad.", "The downloaded installer does not have a valid digital signature and was deleted for security.", "O instalador baixado não possui uma assinatura digital válida e foi excluído por segurança.", "Le programme d'installation téléchargé n'a pas de signature numérique valide et a été supprimé par sécurité.", "Il programma di installazione scaricato non ha una firma digitale valida ed è stato eliminato per sicurezza."],

        ["log.packageNotStarted"] = ["[{0}/{1}] Cancelado antes de iniciar: {2} ({3})", "[{0}/{1}] Cancelled before starting: {2} ({3})", "[{0}/{1}] Cancelado antes de iniciar: {2} ({3})", "[{0}/{1}] Annulé avant le démarrage : {2} ({3})", "[{0}/{1}] Annullato prima dell'avvio: {2} ({3})"],
        ["log.resultUnavailable"] = ["[{0}/{1}] Resultado no disponible: {2} ({3})", "[{0}/{1}] Result not available: {2} ({3})", "[{0}/{1}] Resultado não disponível: {2} ({3})", "[{0}/{1}] Résultat non disponible : {2} ({3})", "[{0}/{1}] Risultato non disponibile: {2} ({3})"],
        ["log.packageFinished"] = ["[{0}/{1}] Finalizado: {2} ({3})", "[{0}/{1}] Finished: {2} ({3})", "[{0}/{1}] Finalizado: {2} ({3})", "[{0}/{1}] Terminé : {2} ({3})", "[{0}/{1}] Terminato: {2} ({3})"],
        ["log.packageRunning"] = ["[{0}/{1}] En ejecución: {2} ({3})", "[{0}/{1}] Running: {2} ({3})", "[{0}/{1}] Em execução: {2} ({3})", "[{0}/{1}] En cours : {2} ({3})", "[{0}/{1}] In esecuzione: {2} ({3})"],
        ["log.upgradeSuccess"] = ["  ✔ {0}: actualizado correctamente.", "  ✔ {0}: updated successfully.", "  ✔ {0}: atualizado com sucesso.", "  ✔ {0} : mis à jour avec succès.", "  ✔ {0}: aggiornato correttamente."],
        ["list.andMore"] = ["\n  ... y {0} más", "\n  ... and {0} more", "\n  ... e mais {0}", "\n  ... et {0} de plus", "\n  ... e altri {0}"],
    };
}
