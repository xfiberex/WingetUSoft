# WingetUSoft — Hoja de ruta

> **Qué hay aquí:** las características agrupadas por **tiers**, con dónde vive cada una en la
> arquitectura por capas (`Core` lógica pura testeable · `Services` efectos externos · `Settings`
> persistencia · `UI` WinUI 3 · `Localization`).
>
> **Qué NO hay aquí:** el detalle de cómo se resolvió cada cosa y por qué — eso vive en
> [`CONTEXT.md`](CONTEXT.md) (§4 *Decisiones* y el *Registro de cambios*).
>
> **Propósito del proyecto:** GUI para **gestionar software con winget** — buscar e instalar, actualizar,
> desinstalar, y exportar/importar la lista de paquetes. *(Ampliado en el Tier E; hasta la v1.7.0 era solo
> "actualizaciones y desinstalaciones".)*

## Estado

| Tier | Tema | Estado | Versión |
|---|---|---|---|
| **A** | Paridad de infraestructura con FormatDiskPro | ✅ Completado | 1.3.0 |
| **B** | Layout adaptable, accesibilidad y UI tests | ✅ Completado | 1.4.0 / 1.4.1 |
| **C** | Auditoría de UI/UX (flujo, datos, color, accesibilidad) | ✅ Completado | 1.5.0 / 1.6.0 |
| **D** | Cara pública (licencia in-app, README, capturas) | ✅ Completado | 1.7.0 |
| **E** | Gestión completa de software ⚠️ *cambio de alcance* | ✅ Completado | *(sin publicar)* |

No hay ninguna tier en curso ni ideas abiertas: las decisiones ya tomadas (y lo que se descartó a
propósito) están al final.

---

## 🔄 Tier A — Paridad con FormatDiskPro *(v1.3.0)*

Nace de comparar WingetUSoft con su proyecto hermano **FormatDiskPro** (misma arquitectura, mismo autor) y
portar la infraestructura de app que faltaba, sin salirse del propósito.

| # | Característica | Dónde |
|---|----------------|-------|
| 0 | Migración de tests **MSTest → xUnit** | `tests/WingetUSoft.Tests/` |
| 1 | **Localización** (`L`/`AppLang`, detección del idioma del sistema) — 272 claves × 5 idiomas | `Localization/`, `Settings/AppSettings.cs` |
| 2 | **Changelog** en las actualizaciones + diálogo **Novedades** | `Services/GitHubUpdateService.cs`, `Core/ReleaseNotes.cs`, `UI/WhatsNewDialog` |
| 3 | **Aviso al terminar** + progreso en la barra de tareas | `UI/Notifier.cs`, `UI/TaskbarProgress.cs` |
| 4 | **Velocidad y ETA** en operaciones largas | `Core/Throughput.cs` |
| 5 | **Historial**: búsqueda, filtros y exportación | `Settings/HistoryFilter.cs`, `UI/HistoryWindow` |
| 6 | Diálogo **Acerca de** + licencia **MIT** + menú Ayuda | `LICENSE`, `UI/AboutDialog` |
| 7 | Extracción completa de strings (5 idiomas) | Todas las ventanas de `UI/` |
| 8 | **Pipeline de release** (Inno Setup) | `installer/build-installer.ps1`, `release.ps1` |

**Decisiones con el usuario:** licencia **MIT** (FormatDiskPro usa GPLv3) · **xUnit** (mejor encaje con
FlaUI) · **Inno Setup como único empaquetador** · publish **framework-dependent** (no self-contained).

---

## 🎨 Tier B — Layout, accesibilidad y UI tests *(v1.4.0 / v1.4.1)*

Nace de un reporte del usuario: el botón "Cancelar" **recortado** contra el borde de la ventana.

| # | Característica | Dónde |
|---|----------------|-------|
| 1–2 | **Ventanas por DPI y WorkArea** + **tamaño mínimo** (sustituyen los `Resize()` fijos en píxeles) | `Core/WindowSizing.cs`, `UI/WindowSizer.cs` (5 ventanas) |
| 3 | **Barra de acciones responsiva** (WrapPanel nativo propio) — arregla el recorte reportado | `UI/WrapPanel.cs` |
| 4 | **Columnas de la tabla** en un `ScrollViewer` horizontal: rellenan si caben, hacen scroll si no | `UI/MainWindow.xaml` |
| 5 | Revisión de **longitud de texto por idioma** (FR/IT son 20–30 % más largos que ES) | `UI/` (4 ventanas) |
| 6 | **Accesibilidad**: nombre accesible en los controles solo-icono | `UI/MainWindow.xaml` |
| 7 | **Snap layouts de Windows 11**: la ventana encaja en las celdas, y lo que no cabe es alcanzable | `Core/WindowSizing.cs`, `UI/MainWindow.xaml` (`ContentScroller`) |
| 8 | **Proyecto de UI tests con FlaUI** (paridad con `FormatDiskPro.UiTests`, **sin elevación**) | `tests/WingetUSoft.UiTests/` |

> **#7 no salió gratis:** al automatizarlo aparecieron **dos bugs reales** (el mínimo de la ventana impedía
> el snap; la tabla quedaba recortada *fuera* de la ventana sin scroll con el que llegar a ella). Ver
> `CONTEXT.md`.

---

## 🧭 Tier C — Auditoría de UI/UX *(v1.5.0 / v1.6.0)*

A diferencia del Tier B (**layout**: que la ventana quepa), ataca **flujo y feedback**: que el usuario
entienda qué pasa y que los datos no le mientan.

| # | Bloque | Dónde |
|---|--------|-------|
| 1 | **Flujo y feedback**: un único resumen de fallos (fin del modal por paquete), barra de estado anclada + progreso real, estados de la tabla | `UI/MainWindow` |
| 2 | **Modelo de selección**: la selección sobrevive a buscar/ordenar/filtrar, tri-estado, contador, `Ctrl+A` estándar | `UI/MainWindow` |
| 3 | **Datos que mentían**: orden semántico de versiones, columna "Tam." imposible (winget no emite ese dato) y parser de `winget show` **multi-idioma** | `Core/VersionOrder.cs`, `Services/WingetShowLabels.cs` |
| 4 | **Color**: el rojo hacía cuatro trabajos; ahora solo significa peligro. Paleta del registro por tema con **contraste WCAG AA testeado** | `Core/LogPalette.cs`, `UI/` |
| 5 | **Preferencias en un solo sitio**: se mudan al *Configuración*; el menú pasa a ser *Herramientas* (solo acciones) | `UI/SettingsWindow`, `UI/MainWindow` |
| 6 | **Accesibilidad de la tabla**: las cabeceras ordenables pasan a ser botones enfocables que anuncian el orden | `UI/MainWindow.xaml` |

> Los bloques 1–3 destaparon **tres bugs de fondo** y uno de ellos invalidó una premisa que el propio
> `CONTEXT.md` daba por buena: **winget traduce la salida de `winget show`** al idioma de Windows.

---

## 📣 Tier D — Cara pública *(v1.7.0)*

El fondo ya estaba hecho, pero la **presentación** seguía siendo la de un repo para compilar, no para
instalar.

| # | Ítem | Dónde |
|---|------|-------|
| 1 | **README para el usuario final**: badges, *Instalación*, *Actualizaciones* con el modelo de confianza (Authenticode → SHA-256), enlace a esta hoja de ruta | `README.md` |
| 2 | **Capturas reproducibles**: script que conduce la app real por UI Automation y las regenera | `tools/capture-screenshots.ps1`, `docs/screenshots/` |
| 3 | **Licencia y avisos de terceros dentro de la app**, embebidos en el `.exe` (*Ayuda → Licencia* / *Avisos de terceros*) | `THIRD-PARTY-NOTICES.txt`, `Core/LegalText.cs`, `UI/LegalTextDialog` |
| 4 | Refresco de `CONTEXT.md` (decía versión 1.2.0; listaba pendientes ya publicados) | `CONTEXT.md` |

> **#3 no era solo una carencia:** el README **afirmaba en falso** que la licencia se podía consultar en
> *Acerca de*, que solo muestra una línea de copyright.

---

## 🧩 Tier E — Gestión completa de software ⚠️ *cambio de alcance* *(sin publicar)*

Hasta la v1.7.0 la app solo tocaba **lo ya instalado**. El Tier E **amplía el propósito del producto**
—decisión explícita del usuario (2026-07-12)— a *gestionar tu software con winget*. README, `CONTEXT.md` y
el diálogo *Acerca de* se reescribieron en consecuencia.

| # | Característica | Dónde |
|---|----------------|-------|
| 1 | **Exportar / importar la lista de paquetes** (JSON nativo de winget): migrar de equipo o restaurar tras formatear | `Services/WingetService.cs`, `UI/MainWindow` |
| 2 | **Omitir esta versión**: descartar *una versión concreta*; el paquete reaparece cuando salga otra | `Core/SkippedVersions.cs`, `Settings/AppSettings.cs`, `UI/MainWindow` |
| 3 | **Buscar e instalar software nuevo** — ⚠️ el cambio de alcance | `Core/WingetTable.cs`, `Core/WingetSearchParser.cs`, `Services/WingetService.cs`, `UI/SearchWindow` |

> **`winget pin` no sirve para "omitir esta versión":** sus anclajes congelan el paquete **también para las
> futuras**. Se resuelve en la app, y la omisión **caduca sola**.
>
> Verificar conduciendo la app real destapó **dos bugs de accesibilidad**: el menú contextual de la tabla
> era **inalcanzable con teclado** (`RightTapped` solo dispara con ratón) y las filas se anunciaban a un
> lector de pantalla como `WingetUSoft.PackageViewModel`. Ambos arreglados.

---

## 🚫 Decisiones cerradas (no reabrir)

- **Empaquetador:** Inno Setup es la única vía. Nada de MSIX/ClickOnce — el proyecto es y seguirá siendo
  unpackaged (`WindowsPackageType=None`).
- **Instalador framework-dependent** (no self-contained): descarga los runtimes que falten, verificado de
  punta a punta.
- **Licencia MIT** y **sin donaciones** (a diferencia de FormatDiskPro, GPLv3 + PayPal).
- **CI con GitHub Actions — descartado (2026-07-12):** `release.ps1` ya corre unitarios **y** UI tests antes
  de cada corte, y un runner hospedado **no puede** correr los UI tests (necesitan escritorio interactivo);
  solo duplicaría lo ya cubierto, con menos cobertura.
- **Certificado de firma de código (OV/EV) — descartado (2026-07-12):** consecuencia asumida — SmartScreen
  dirá "editor desconocido", y las actualizaciones se verifican por **SHA-256**. El soporte de firma sigue en
  el pipeline por si algún día hay certificado.
- **Fuera del propósito del producto:** gestión de discos (S.M.A.R.T. / benchmark / chkdsk — territorio de
  **FormatDiskPro**), presets (la lista de exclusiones cubre ese rol) y autorefresco por `WM_DEVICECHANGE`
  (específico de unidades extraíbles).
- **Notificación toast de Windows — descartada (2026-07-12):** era la cuarta idea del Tier E (avisar con un
  toast, con acción *Actualizar ahora*, cuando la auto-comprobación encuentra actualizaciones). **No se
  hará:** el aviso ya existe (sonido + parpadeo de la barra de tareas + progreso en el icono), y en una app
  **unpackaged** el toast exige registrar un servidor COM del `AppNotificationManager` — bastante fontanería
  de plataforma para un beneficio marginal sobre lo que ya se avisa.
