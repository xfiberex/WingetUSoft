# Contexto del proyecto — WingetUSoft

> **Propósito de este archivo.** Documento de contexto **vivo** que resume el estado del
> proyecto y las decisiones tomadas, para no perder continuidad al cambiar de equipo (PC)
> o de sesión. **Mantenerlo actualizado con cada cambio relevante**: actualizar
> _Estado actual_ y añadir una entrada en el _Registro de cambios_. Usar fechas absolutas.

- **Repositorio:** https://github.com/xfiberex/WingetUSoft
- **Última actualización de este documento:** 2026-07-11
- **Versión actual:** **1.4.0** — Tier B (#1–#6 + #8): UI adaptable por DPI/WorkArea, tamaño mínimo,
  barra de acciones responsiva (WrapPanel), accesibilidad y proyecto de UI tests con FlaUI
  ([release en GitHub](https://github.com/xfiberex/WingetUSoft/releases/tag/v1.4.0), tag `v1.4.0`,
  **sin firmar** — ver Pendientes §6). Versión previa: 1.3.0 (cierre de Tier A). **El arreglo de snap
  layouts (#7, 2026-07-11) está en `main` sin publicar todavía** — entra en la próxima release.
- **Hoja de ruta:** ver [`ROADMAP.md`](ROADMAP.md) — **Tier A COMPLETADO** (2026-07-08/09):
  paridad con FormatDiskPro (proyecto hermano, mismo autor). Las 9 fases (-1 a 8) están
  implementadas y verificadas. **Tier B COMPLETADO** (2026-07-10/11): mejoras visuales/responsivas +
  accesibilidad + proyecto `WingetUSoft.UiTests` con FlaUI, y cierre de #7 (snap layouts de
  Windows 11) — build 0/0, **93/93 unitarios**, **16/16 UI tests**.
- **Stack:** C# / .NET 10 · **WinUI 3** (Windows App SDK 1.8, unpackaged,
  `net10.0-windows10.0.22621.0`, `TargetPlatformMinVersion=10.0.19041.0`) · **xUnit** (migrado
  desde MSTest en Tier A #0) · Inno Setup 6

---

## 1. Qué es

Interfaz gráfica (WinUI 3) para gestionar **actualizaciones y desinstalaciones de software**
mediante **winget** en Windows: consulta y actualiza paquetes (individual o en lote, silencioso
o interactivo), desinstala programas, exporta la lista a CSV/TSV, mantiene un historial de
actualizaciones y se auto-actualiza vía GitHub Releases. No gestiona discos ni almacenamiento
(eso es FormatDiskPro).

## 2. Arquitectura (separación por capas)

```
WingetUSoft/
├─ src/WingetUSoft/             Proyecto de aplicación (WinUI 3)
│  ├─ Program.cs                Punto de entrada
│  ├─ App.xaml / App.xaml.cs    Aplicación WinUI
│  │
│  ├─ Core/                     Lógica de negocio pura (sin UI ni efectos externos)
│  │  ├─ ReleaseNotes.cs        Notas de versión (Markdown de GitHub) → texto plano (diálogo de novedades)
│  │  ├─ Throughput.cs          ETA (tiempo restante) para descargas y operaciones largas
│  │  ├─ WindowSizing.cs        Dimensionado/centrado por DPI acotado a WorkArea (puro, Tier B #1/#2)
│  │  ├─ DelimitedTextExporter.cs  Exportación CSV/TSV segura (neutralización de fórmulas)
│  │  └─ Models/
│  │     ├─ WingetPackage.cs           Paquete con versión disponible/instalada
│  │     ├─ WingetPackageInfo.cs       Metadatos enriquecidos (winget show)
│  │     ├─ WingetProgressInfo.cs      Progreso de descarga/instalación
│  │     ├─ CleanupItemViewModel.cs    ViewModel para la ventana de limpieza
│  │     ├─ UpgradeResult.cs
│  │     └─ UpgradeBatchResult.cs
│  │
│  ├─ Services/                 Operaciones con efectos externos (procesos, red, disco)
│  │  ├─ WingetService.cs       Ejecución de winget, parsing, elevación (worker + named pipe)
│  │  ├─ GitHubUpdateService.cs Auto-actualización desde GitHub Releases (verificación Authenticode, changelog)
│  │  └─ CleanupScanner.cs      Detección de residuos post-desinstalación
│  │
│  ├─ Settings/                 Persistencia y configuración
│  │  ├─ AppSettings.cs         Carga/guardado JSON, paths, log, backup de settings corruptos, idioma
│  │  ├─ HistoryEntry.cs        DTO de entrada de historial
│  │  └─ HistoryFilter.cs       Filtrado del historial por texto y estado (lógica pura)
│  │
│  ├─ Localization/             Cadenas ES/EN/PT/FR/IT (patrón L.T("clave"), ver Tier A #1/#7)
│  │  └─ Localization.cs        enum AppLang + clase L (Map, T, FromCode/ToCode/FromCulture)
│  │
│  ├─ UI/                       Capa de presentación (WinUI 3)
│  │  ├─ MainWindow.xaml/.cs       Ventana principal (actualizaciones, tray icon)
│  │  ├─ SettingsWindow.xaml/.cs   Diálogo de configuración
│  │  ├─ HistoryWindow.xaml/.cs    Vista de historial
│  │  ├─ UninstallWindow.xaml/.cs  Ventana de desinstalación
│  │  ├─ CleanupWindow.xaml/.cs    Ventana de limpieza de residuos
│  │  ├─ WhatsNewDialog.xaml/.cs   ContentDialog de novedades (changelog de la versión instalada)
│  │  ├─ AboutDialog.xaml/.cs      ContentDialog "Acerca de": versión, descripción, licencia MIT, privacidad
│  │  ├─ Notifier.cs               Aviso al terminar: sonido + parpadeo de la barra de tareas (Win32)
│  │  ├─ TaskbarProgress.cs        Progreso en el icono de la barra de tareas (ITaskbarList3, Win32)
│  │  ├─ WindowSizer.cs            Wrapper DPI + WorkArea + PreferredMinimum (usa Core/WindowSizing, Tier B #1/#2)
│  │  ├─ WrapPanel.cs              Panel de envoltura nativo (barra "Acciones rápidas" responsiva, Tier B #3)
│  │  ├─ Converters.cs             Convertidores de valor para XAML
│  │  ├─ TitleBarHelper.cs         Helper compartido para colores del title bar
│  │  └─ WindowDialogHelper.cs     Helper compartido para diálogos modales
│  │
│  └─ installer/                Inno Setup (installer.iss) — único empaquetador
│     ├─ installer.iss             MyAppVersion/SourceDir overridables vía /D (#ifndef)
│     ├─ build-installer.ps1       Publish framework-dependent (win-x64) + ISCC + firma opcional
│     ├─ new-selfsigned-cert.ps1   Certificado de firma autofirmado para pruebas del pipeline
│     └─ Output/                   Instaladores compilados (gitignored)
│
├─ tests/WingetUSoft.Tests/     Tests unitarios (xUnit, migrados desde MSTest en Tier A #0)
│  ├─ AppSettingsTests.cs
│  ├─ CleanupScannerTests.cs
│  ├─ WingetServiceTests.cs
│  ├─ LocalizationTests.cs      Completitud del diccionario L.Map + FromCode/FromCulture/ToCode
│  ├─ ReleaseNotesTests.cs      Markdown → texto plano (encabezados, viñetas, negrita/código, enlaces, saltos)
│  ├─ NotifierTests.cs          Notifier.ShouldNotify (umbral, cancelado, deshabilitado)
│  ├─ ThroughputTests.cs        Eta/FormatEta (casos normales, velocidad cero, formato mm:ss / h:mm:ss)
│  ├─ HistoryFilterTests.cs     Filtro por texto/estado/combinados, casos sin coincidencias
│  └─ WindowSizingTests.cs      Dimensionado por DPI/WorkArea + clamp del mínimo (Tier B #1/#2)
│
├─ tests/WingetUSoft.UiTests/   Tests de UI end-to-end (FlaUI + UIA3, xUnit) — Tier B #8; lanzan la app real
│  ├─ AppFixture.cs             ICollectionFixture: lanza el .exe, obtiene la ventana, respalda settings (sin elevación: app asInvoker)
│  ├─ SettingsBackup.cs         Respalda/restaura %AppData%\WingetUSoft\settings.json + history.log
│  ├─ DialogHelper.cs           Helpers de ContentDialog de WinUI (dismiss de arranque, SafeCloseAnyDialog)
│  ├─ MenuActions.cs            Navegación de DropDownButton + MenuFlyout (Opciones/Ayuda/Idioma)
│  ├─ MonitorInfo.cs            P/Invoke compartido: WorkArea del monitor bajo una ventana (px físicos)
│  ├─ LayoutTests.cs            Regresión Tier B: ventana dentro de WorkArea (#1) + wrap de acciones (#3)
│  ├─ SnapLayoutTests.cs        Regresión Tier B #7: celdas de snap de media y cuarto de pantalla
│  ├─ MainWindowTests.cs        Smoke: ventana abre, botones de acción y lvPackages presentes
│  ├─ MenuDialogsTests.cs       Ayuda → Acerca de abre/cierra ContentDialog
│  └─ SettingsTests.cs          Cambio de idioma en caliente + apertura/cierre de SettingsWindow
│
├─ release.ps1                  Corte de versión en un paso (build + tag + GitHub Release)
└─ LICENSE                      Texto MIT (© 2026 xfiberex)
```

**Regla de oro:** la lógica de negocio pura y testeable vive en `Core` (sin dependencias de
WinUI/Process/HttpClient); las operaciones con efectos externos (winget, red, disco) viven en
`Services`. La UI, `Services` y `Settings` consumen `Core`. Namespace único `WingetUSoft`.

## 3. Estado actual

- ✅ Build: **0 advertencias / 0 errores** (`dotnet build WingetUSoft.slnx`).
- ✅ Tests: **95/95** (`dotnet test`) — 30 migrados de MSTest (Tier A #0) + 21 de `LocalizationTests`
  (Tier A #1) + 8 de `ReleaseNotesTests` (Tier A #2) + 5 de `NotifierTests` (Tier A #3) + 6 de
  `ThroughputTests` (Tier A #4) + 8 de `HistoryFilterTests` (Tier A #5) + **15 de `WindowSizingTests`
  (Tier B #1/#2, +4 del clamp de snap de #7)** + **2 de `GitHubUpdateServiceTests`** (hash SHA-256 con
  el que se verifica el instalador descargado). Las Fases 6 y 7 de Tier A no añadieron tests nuevos
  (UI pura + extracción de strings; `LocalizationTests` ya cubre por completitud las **273 claves** de
  `L.Map` —272 de Tier A + `grid.excludedAccessible` de Tier B #6— sin necesitar un test por clave).
- ✅ **UI tests (FlaUI): 16/16** (`dotnet test tests/WingetUSoft.UiTests`) — proyecto
  `WingetUSoft.UiTests` (Tier B #8): ejercen la app real vía UIA3 (ventana dentro de WorkArea, wrap
  de acciones al angostar, **celdas de snap de media y cuarto de pantalla (#7)**, navegación de
  diálogos, cambio de idioma, apertura de ventanas). **No** los corre `release.ps1` (solo ejecuta el
  proyecto unitario); necesitan sesión de escritorio interactiva pero **no** elevación (la app es
  asInvoker).
- ✅ **Tier B COMPLETADO (2026-07-10/11):** build 0/0, 93/93 unitarios + 16/16 UI, y verificación
  visual del usuario OK (wrap de acciones, mínimos por DPI, DataGrid, accesibilidad, snap layouts).
  Ver Registro de cambios.
- ✅ **Tier A — Paridad con FormatDiskPro, COMPLETADO (2026-07-08/09):**
  Se comparó WingetUSoft con FormatDiskPro (proyecto hermano en el mismo workspace, misma
  arquitectura por capas) para identificar infraestructura de app ya resuelta allí y ausente
  aquí: changelog/novedades en las actualizaciones, aviso al terminar + progreso en la barra de
  tareas, ETA/velocidad en operaciones largas, historial con filtros/exportación, diálogo Acerca
  de con licencia, y pipeline de release en un paso. Plan completo en
  `C:\Users\User\.claude\plans\linear-chasing-thacker.md`. Decisiones tomadas con el usuario:
  localización a **5 idiomas** (ES/EN/PT/FR/IT, como FormatDiskPro), licencia **MIT** (a
  diferencia de FormatDiskPro que usa GPLv3), **migrar tests a xUnit** (mejor compatibilidad con
  FlaUI para futuros tests de UI), **Inno Setup como único empaquetador** (se descarta MSIX o
  cualquier otra vía; el proyecto ya es unpackaged, `WindowsPackageType=None`).
  Fases 0–8 detalladas en [`ROADMAP.md`](ROADMAP.md).
  - **Fase 0 (✅ 2026-07-08):** tests migrados de MSTest a xUnit, 30/30 en verde.
  - **Fase 1 (✅ 2026-07-08):** `Localization/Localization.cs` (`enum AppLang` + clase `L`,
    port literal del patrón de FormatDiskPro) + `AppSettings.Language`/`LoadedFromFile`. Cubre
    por ahora **solo el menú principal** (`DropDownButton "Opciones"` en `MainWindow.xaml`):
    nuevo submenú **Idioma** (5 `RadioMenuFlyoutItem`, mismo patrón que el submenú Tema ya
    existente) + `ApplyLocalizedStrings()` para el cambio en caliente. Detección del idioma del
    sistema (`CultureInfo.CurrentUICulture`) solo cuando `AppSettings.Language` es `null` y no
    había `settings.json` previo (`LoadedFromFile == false`); si ya existía configuración previa
    sin campo `Language` (actualización desde una versión anterior a este cambio), se asume
    español en vez de reinterpretar el sistema. La extracción completa del resto de la UI
    (ventanas de Settings/Uninstall/Cleanup/History) queda para la **Fase 7**.
  - **Fase 2 (✅ 2026-07-08):** `Services/GitHubUpdateService.cs` gana `Notes` (campo `body` del
    release), `GetLatestReleaseAsync` (sin gate de versión) y `GetReleaseByTagAsync(tag)`, ambos
    refactorizados sobre un `FetchReleaseAsync` privado compartido con `CheckForUpdateAsync`.
    **Nuevo** `Core/ReleaseNotes.cs` (port literal de FormatDiskPro, Markdown → texto plano) y
    **nuevo** `UI/WhatsNewDialog.xaml`/`.cs` (ContentDialog con versión + notas + botón "Ver en
    GitHub" vía `Windows.System.Launcher.LaunchUriAsync`, patrón ya usado en
    `CtxBuscarWeb_Click`). `AppSettings.LastVersionSeen` + `MaybeShowWhatsNewAsync()`/
    `ShowWhatsNewAsync()` en `MainWindow`: muestra las novedades una sola vez tras actualizar
    (gateado por `LoadedFromFile` igual que la Fase 1, para no disparar en instalación nueva),
    llamado en el `Loaded` del arranque antes de `CheckForAppUpdateAsync()`; también accesible
    bajo demanda vía nuevo ítem **"Novedades..."** en el dropdown `Opciones` (no se creó un menú
    `Ayuda` dedicado todavía — **desviación deliberada del texto del plan**, que lo menciona en la
    Fase 2 pero lo especifica en detalle recién en la Fase 6; se pospuso la reorganización del
    menú a la Fase 6 para no tocar la disposición dos veces). El changelog (vía
    `BuildChangelogMessage`, trunca a 500 caracteres) se antepone tanto al `InfoBar` de
    actualización como al diálogo de confirmación en `CheckForAppUpdateAsync`/
    `MenuBuscarActualizacion_Click`. **Nuevo test** `ReleaseNotesTests.cs` (port literal, 8 casos).
    **Resultado: 59/59 tests en verde**, build 0/0.
  - **Fase 3 (✅ 2026-07-08):** **Nuevos** `UI/Notifier.cs` (Win32 `FlashWindowEx`/`MessageBeep`,
    `ShouldNotify` puro) y `UI/TaskbarProgress.cs` (COM `ITaskbarList3`), ports literales. Campo
    `_hWnd` añadido a `MainWindow`/`UninstallWindow`/`CleanupWindow` (antes solo variable local en
    el constructor). En `MainWindow.UpdatePackagesAsync`: `Stopwatch` de la operación,
    `TaskbarProgress.SetValue` por paquete en el bucle secuencial (`SetIndeterminate` en el lote
    elevado como administrador), `Notifier.ShouldNotify`/`OperationFinished` al terminar —
    reutiliza el ajuste ya existente `AppSettings.ShowNotifications` (toggle en `SettingsWindow`),
    sin setting nuevo. También en `LnkDescargarUpdate_Click` (descarga del instalador de la propia
    app): progreso real en la taskbar durante la descarga, indeterminado mientras se lanza el
    instalador. Mismo patrón replicado en `UninstallWindow.UninstallSelectedAsync` y
    `CleanupWindow.DeleteSelectedAsync` (desinstalación y limpieza de residuos, ambas pueden ser
    operaciones largas). **Nuevo test** `NotifierTests.cs` (port literal, 5 casos: umbral,
    deshabilitado, cancelado). **Resultado: 64/64 tests en verde**, build 0/0.
  - **Fase 4 (✅ 2026-07-08):** **Nuevo** `Core/Throughput.cs` (`Eta`/`FormatEta`, puro; sin
    `FormatSpeed` — se mantuvo el `FormatBytes` inline ya existente en `MainWindow`). ETA añadido
    a `UpdateLogDownloadLine`, al estado del lote en `UpdatePackagesAsync` y, por extrapolación
    (sin bytes/velocidad reales disponibles en esa API), a la descarga del instalador de la propia
    app en `LnkDescargarUpdate_Click`. **Resultado: 70/70 tests en verde**, build 0/0.
  - **Fase 5 (✅ 2026-07-08):** **Nuevo** `Settings/HistoryFilter.cs` (`HistoryStatusFilter` enum +
    `Apply(entries, texto, estado)`, puro). `UI/HistoryWindow.xaml` gana una barra de
    búsqueda/filtro/exportación (mismo patrón visual que el filtro de `MainWindow`): `TextBox` de
    búsqueda en vivo, `DropDownButton` con `RadioMenuFlyoutItem` Todos/Éxito/Fallido, botón
    "Exportar CSV...". La exportación reutiliza `Core/DelimitedTextExporter.BuildRow` y el patrón
    `FileSavePicker` ya usado en `MainWindow.MenuExportar_Click` (columnas: fecha, nombre, Id,
    versión origen/destino, resultado) — exporta lo **filtrado**, no todo el historial.
    **Resultado: 78/78 tests en verde**, build 0/0.
  - **Fase 6 (✅ 2026-07-08):** **Nuevo** `LICENSE` (MIT, © 2026 xfiberex, decisión del usuario —
    a diferencia de FormatDiskPro que usa GPLv3) en la raíz del repo. **Nuevo**
    `UI/AboutDialog.xaml`/`.cs`: versión, descripción, copyright/licencia, aviso de privacidad y
    botón "Ver en GitHub" (sin disclaimer de uso destructivo ni donaciones — no aplican al
    propósito de esta app, a diferencia del `AboutDialog` de FormatDiskPro). **Nuevo menú Ayuda**
    (`DropDownButton` junto al de `Opciones`): agrupa "Buscar actualización...", "Novedades..."
    (movidos desde `Opciones`, cerrando la desviación anotada en la Fase 2) y el nuevo
    "Acerca de...". README gana una sección de Licencia/Privacidad. Sin tests nuevos (UI pura).
    **Resultado: 78/78 tests en verde** (sin cambios), build 0/0.
  - **Fase 7 (✅ 2026-07-08):** extracción completa de strings a los 5 idiomas — el diccionario
    `L.Map` pasa de ~30 claves (solo menú principal, Fase 1) a **272 claves**. Cubre las 5
    ventanas (`MainWindow`, `SettingsWindow`, `UninstallWindow`, `CleanupWindow`,
    `HistoryWindow`) por completo: cada `TextBlock`/`Content`/`PlaceholderText` sin nombre ganó
    `x:Name` para poder localizarlo desde código, y cada ventana secundaria ganó su propio
    `ApplyLocalizedStrings()` (llamado una vez en el constructor — a diferencia de `MainWindow`,
    no tienen menú de idioma propio: heredan el idioma vigente de `L.Current` al abrirse).
    `Converters.cs` no tenía strings que extraer. También se localizaron mensajes de `Core/`
    que llegan directo a diálogos: `UpgradeResult.GetFailureReason()` (con test existente que
    sigue en verde porque `L.Current` es `Es` por defecto durante los tests),
    `GitHubUpdateService` (mensaje de instalador sin firma) y las cadenas de progreso/error del
    modo administrador en `WingetService` (solicitudes de elevación, resultado del lote elevado).
    **Límite deliberado**: se dejaron sin traducir (documentado, no es un olvido) (1) las claves
    de parseo de la salida de `winget` (`"Homepage:"`, `"Description:"`, etc. — deben coincidir
    literalmente con el CLI, no son texto de UI); (2) los mensajes de protocolo interno del
    worker elevado en rutas de error muy profundas (fallos de negociación de la sesión IPC,
    "no debería pasar nunca"); (3) los mensajes de `AppSettings.LastLoadError`/`LastSaveError`,
    porque `AppSettings.Load()` se ejecuta *antes* de que el idioma se determine (el propio
    `Language` viene del archivo que se está cargando) — traducirlos no funcionaría de verdad.
    **Resultado: 78/78 tests en verde** (sin cambios; ninguna fase de UI pura necesitaba tests
    nuevos), build 0/0.

## 4. Decisiones y convenciones clave

- **Namespace único** `WingetUSoft` para toda la app y los tests.
- **Elevación de permisos:** worker interno con comunicación por named pipe (sin scripts
  temporales en disco) — ver `Services/WingetService.cs`.
- **Exportación CSV/TSV:** `Core/DelimitedTextExporter.cs` neutraliza fórmulas (prefijo `'` ante
  `=`/`+`/`-`/`@`) para que sea seguro abrir en Excel/Calc.
- **Actualizaciones de la app:** `Services/GitHubUpdateService.cs` **exige firma Authenticode
  válida** en el instalador descargado (`VerifyAuthenticodeSignature`) — lo borra si no está
  firmado. Cualquier pipeline de release que publique sin firmar rompe la auto-actualización.
- **Publicación (verificado en Tier A #8):** `dotnet publish -r win-x64 --self-contained false`
  (**framework-dependent**, no self-contained pese a `WindowsAppSDKSelfContained=true` en el
  .csproj — esa propiedad solo empaqueta el Windows App SDK, no el propio runtime de .NET). El
  instalador de Inno Setup descarga VC++ Redist / Windows App Runtime / .NET 10 Desktop Runtime
  si faltan (ver `installer/installer.iss` → `[Code]`) — ese código de detección/descarga es lo
  que hace que el modelo framework-dependent funcione sin fricción para el usuario final. Ver
  `installer/build-installer.ps1`.
- **Instalador (Inno Setup):** `AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}` — **no cambiar
  nunca** (permite actualización in-place). `PrivilegesRequired=admin`,
  `CloseApplications=yes`. **Único empaquetador del proyecto** (Tier A, decisión explícita).
  `MyAppVersion`/`SourceDir` en `installer.iss` van envueltos en `#ifndef` para poder
  sobrescribirlos vía `/D` desde `build-installer.ps1` sin romper `iscc installer.iss` a mano.
- **Versionado:** fuente única en `WingetUSoft.csproj` `<Version>` (hoy `1.2.0`).
- **Tests:** **xUnit** (migrado desde MSTest en Tier A #0) — no reintroducir MSTest/NUnit/TUnit.
- **Scripts PowerShell con acentos/`—`:** guardar siempre con **BOM UTF-8**
  (`[System.Text.UTF8Encoding($true)]`, no `Set-Content -Encoding UTF8` a secas si el archivo
  se creó sin BOM antes). Windows PowerShell 5.1 asume el codepage ANSI del sistema para `.ps1`
  sin BOM; un guion largo `—` o una tilde sin BOM puede generar bytes que rompen el tokenizer
  del parser (visto en vivo: `release.ps1` fallaba con "Falta el paréntesis de cierre" hasta
  reescribirlo con BOM). Mismo hallazgo que documenta `FormatDiskPro/CONTEXT.md`.
- **Localización (Tier A #1 en adelante):** patrón `L.T("clave", args...)` con diccionario por
  clave → `string[5]` (ES/EN/PT/FR/IT), idéntico al de FormatDiskPro. Detección del idioma del
  sistema solo en el primer arranque (sin `settings.json` previo); después manda la elección
  manual persistida. Todo string nuevo en la UI a partir de la Fase 1 debe usar `L.T()`, no texto
  literal — evita duplicar trabajo en la Fase 7.

## 5. Tareas comunes

| Tarea | Comando |
|-------|---------|
| Compilar | `dotnet build WingetUSoft.slnx` |
| Ejecutar | `dotnet run --project src/WingetUSoft/WingetUSoft.csproj` |
| Pruebas | `dotnet test tests/WingetUSoft.Tests/WingetUSoft.Tests.csproj` |
| Generar instalador | `installer\build-installer.ps1` (añade `-CertThumbprint <huella>` para firmar) |
| Crear certificado de prueba | `installer\new-selfsigned-cert.ps1` |
| Publicar versión | `.\release.ps1 -Version X.Y.Z` (usa `-DryRun` para simular) |

`release.ps1` hace: validar → tests → bump `<Version>` → build instalador → commit + tag `vX.Y.Z`
→ push → `gh release create` con el instalador. Flags: `-DryRun`, `-SkipTests`, `-AllowDirty`,
`-NotesFile`, y los de firma (`-CertThumbprint`/`-CertFile`/`-CertPassword`/`-TimestampUrl`,
reenviados a `build-installer.ps1`).

## 6. Pendientes / ideas

- **Hoja de ruta de características:** [`ROADMAP.md`](ROADMAP.md) — **Tier A COMPLETADO**, 9/9
  fases (-1 a 8) implementadas y verificadas. **Tier B COMPLETADO** (2026-07-10/11), 8/8 ítems:
  build 0/0, 93/93 unitarios + 16/16 UI (`WingetUSoft.UiTests` con FlaUI) y verificación visual del
  usuario OK. No hay ninguna tier en curso.
- **Publicar la 1.4.1:** el `.csproj` ya está en **1.4.1** y el instalador está construido y probado en
  local, pero **sin publicar en GitHub**. Contiene el arreglo de snap layouts (#7) y los tres arreglos
  del updater (ver Registro de cambios). Publicar con `release.ps1 -Version 1.4.1` — ahora sube dos
  assets: el `.exe` y su `.sha256` (**imprescindible**: sin el `.sha256`, la app no puede verificar un
  instalador sin firmar y rechaza la actualización).
- Conseguir un certificado de firma de código real (OV/EV). **Ya no bloquea la auto-actualización**
  (desde 2026-07-11 se verifica por SHA-256 cuando no hay firma), pero sigue siendo deseable por dos
  motivos: (a) SmartScreen muestra "editor desconocido" en cada instalación, y (b) una firma es una
  garantía más fuerte que el hash — el hash y el `.exe` salen del mismo release, así que detecta
  manipulación en tránsito pero no protegería frente a un compromiso de la cuenta de GitHub. El
  código ya prefiere la firma si existe: `build-installer.ps1 -CertThumbprint ...` y listo, sin más
  cambios.

## 7. Cómo mantener este documento

1. Tras un cambio relevante, añadir una entrada en el **Registro de cambios** (fecha absoluta).
2. Actualizar **Estado actual** (versión, tests, lo publicado, pendientes).
3. Si cambia una convención o decisión, reflejarlo en la sección 4.
4. Marcar el ítem correspondiente como ✅ en [`ROADMAP.md`](ROADMAP.md) cuando una fase quede
   completa y verificada (build + tests + prueba manual, no solo código escrito).
5. Commitear este archivo junto con el cambio para que viaje entre equipos.

---

## Registro de cambios

### 2026-07-11 — fix: el flujo instalar / actualizar / desinstalar vía GitHub (3 bugs)

Reporte del usuario: al actualizar, la app mostraba **una ventana pidiendo descargar una librería que
ya estaba instalada**, y después **no volvía a ejecutarse** (había que cerrar la ventana y abrirla a
mano). Al auditar el flujo aparecieron **tres bugs**, los tres confirmados con datos del equipo real.

**Bug 1 — el Windows App Runtime se descargaba sin necesitarlo.** `installer.iss` comprobaba
`HKLM\SOFTWARE\Microsoft\WindowsAppRuntime\1.8`, que no existe, así que siempre concluía "falta" y se
bajaba ~40 MB. Pero la app **no lo necesita**: el `.csproj` usa `WindowsAppSDKSelfContained=true` y el
runtime viaja dentro de la carpeta de la app (`Microsoft.WindowsAppRuntime.dll`, `Microsoft.ui.xaml.dll`,
el bootstrap...; se verificó en el output de publish y en el propio log de instalación). Comprobación y
descarga **eliminadas**.

**Bug 2 — el .NET siempre se daba por ausente.** La comprobación buscaba subclaves de
`...\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App`, y era **doblemente incorrecta**:
(a) esa rama `sharedfx` **no existe** (bajo `InstalledVersions\x64` solo hay `sharedhost`), y (b) el
framework es el equivocado — `WingetUSoft.runtimeconfig.json` pide **`Microsoft.NETCore.App`**, no
`Microsoft.WindowsDesktop.App` (WinUI 3 no es una app WindowsDesktop; eso es WPF/WinForms). Resultado:
daba "falta .NET" **en cualquier máquina**, y cada instalación/actualización rebajaba y reinstalaba el
runtime que el usuario ya tenía. Ahora se comprueba la carpeta del framework compartido
(`{commonpf64}\dotnet\shared\Microsoft.NETCore.App\10.*`), que es lo que hostfxr resuelve de verdad y
lo mismo que lista `dotnet --list-runtimes`.

Esos dos falsos positivos son la "ventana de la librería": en una instalación **interactiva** (descarga
manual desde Releases, que es como se actualizaba en la práctica — ver Bug 3) la `TDownloadWizardPage`
de Inno **sí se muestra**. Se comprobó en un `.iss` de laboratorio que en `/VERYSILENT` esa página
**no** aparece (`WizardForm.Visible=0`) y que el `[Run]` con `Check: IsAutoUpdate` **sí** se ejecuta —
lo que descartó la primera hipótesis y obligó a buscar la causa real. Además los instaladores de
dependencias se ejecutaban con `SW_SHOW` y `ewWaitUntilTerminated` (podían abrir ventana y bloquear
Setup): ahora `SW_HIDE`.

**Bug 3 — la auto-actualización estaba muerta.** `GitHubUpdateService` exigía una firma Authenticode
válida y **borraba** el instalador si no la tenía... pero los releases se publican **sin firmar** (no
hay certificado): `Get-AuthenticodeSignature` sobre el instalador 1.4.0 publicado devuelve `NotSigned`.
O sea que la actualización *desde dentro de la app* fallaba **siempre**, antes de ejecutar nada. Por eso
el usuario acababa instalando a mano. **Decidido con el usuario:** verificar por **SHA-256** publicado
como asset del release, manteniendo la firma como vía preferente si algún día hay certificado
(`VerifyInstallerAsync`: firma válida → OK; si no, hash contra el asset `...exe.sha256`; sin ninguna de
las dos, se borra y se aborta). `build-installer.ps1` genera el `.sha256` (después de firmar, porque
firmar cambia el binario) y `release.ps1` lo sube como segundo asset. **Alcance honesto:** el hash y el
`.exe` salen del mismo release, así que esto detecta corrupción/manipulación en tránsito pero no
protegería un compromiso de la cuenta de GitHub; es el compromiso habitual sin certificado.

**Extra:** el `[Run]` relanzaba la app heredando el token de administrador de Setup. Con
`runasoriginaluser` vuelve a arrancar como el usuario normal, que es como debe correr (la app es
asInvoker y eleva bajo demanda con un worker por named pipe).

**Verificado de punta a punta en el equipo real** (instalador 1.4.1, ejecutado exactamente como lo hace
la auto-actualización: `/VERYSILENT /NORESTART /autoinstall=1` con `/LOG`): actualiza **1.3.0 → 1.4.1**,
**0 descargas** y **0 instaladores de runtime** en el log, **ninguna ventana**, la app **se relanza sola**
y el log confirma `Run as: Original user` (proceso no elevado). Entrada de desinstalación correcta y
única (el `AppId` fijo actualiza en sitio, no duplica). Build 0/0, **95/95 unitarios** (+2 de
`GitHubUpdateServiceTests`, que fijan el formato hex/mayúsculas del hash: si cambiara, la app
rechazaría su propio instalador).

### 2026-07-11 — fix: Tier B #7 — snap layouts de Windows 11 — **TIER B COMPLETADO**

#7 estaba planteado como "verificación manual" y se esperaba que fuera una consecuencia gratuita de
#1/#2. Al automatizarlo con FlaUI aparecieron **dos bugs reales**, ambos arreglados y con test de
regresión. Ninguno era visible en una ventana de tamaño normal, que es por lo que sobrevivieron a la
verificación visual de #1–#6.

**Bug 1 — el mínimo de la ventana impedía el snap.** `WindowSizing.ScaleMinSize` solo acotaba el
mínimo de diseño (900×600 DIP) a "WorkArea menos el margen", un techo demasiado alto: una celda de
snap mide **la mitad** de la WorkArea en uno o ambos ejes. En un 1920×1080 corriente (WorkArea
~1920×1040) la celda de cuarto es 960×520 y el mínimo de alto (600) ya no cabía, así que Windows no
podía encoger la ventana lo suficiente para completar el snap. Ahora `ScaleMinSize` acota además cada
eje a `workWidth/2` / `workHeight/2`. No hace falta ningún número fijo: en monitores grandes la mitad
de la WorkArea es mucho mayor que el mínimo de diseño y este se conserva **intacto**; solo se relaja
en pantallas pequeñas, que es justo donde estorbaba. En la práctica este clamp domina siempre al del
margen (la mitad de cualquier resolución real es más restrictiva que "menos un puñado de DIP"), pero
se conserva el otro como red de seguridad — `Math.Min` escoge el menor sin coste.

**Bug 2 — la tabla desaparecía de la pantalla.** Con la ventana ya encogida a un cuarto (960×510), las
tres tarjetas superiores (cabecera, acciones, filtros) consumían los ~510 px de alto **enteros**: el
volcado del árbol de UIA mostraba el DataGrid, el log y la barra de estado con `BoundingRectangle`
**0×0 y `IsOffscreen=True`**. No estaban "bajo el pliegue": estaban recortados fuera de la ventana, sin
barra de scroll con la que llegar a ellos, porque **la página no tenía scroll vertical**. Ahora todo el
contenido de `MainWindow.xaml` vive en un `ScrollViewer` (`ContentScroller`) cuyo Grid usa
**`MinHeight` (no `Height`) atado al `ViewportHeight`** del propio scroller. Esa sola regla da los dos
comportamientos: si sobra alto, `MinHeight` estira el Grid hasta el viewport y la fila `*` rellena como
siempre, **sin barra** (media pantalla se ve idéntica a una ventana normal); si falta, `MinHeight` es
solo un mínimo, el Grid conserva su alto natural (mayor que el viewport) y el ScrollViewer desplaza la
página. Es el mismo patrón que ya usaba `DataGridScroller` en horizontal (`MinWidth` + `ViewportWidth`).
Además, `MinHeight` en la fila de tabla+log (300, y 160/120 dentro) pone un piso: por debajo, la página
se desplaza en vez de seguir encogiendo la tabla hasta hacerla inservible.

**Método (por qué importa):** el primer intento de #2 fue un ScrollViewer que envolvía **solo**
tabla+log+pie, sobre la teoría de que un scroller de toda la página dejaría el DataGrid sin viewport.
El volcado del árbol de UIA en la celda real lo refutó: el scroller acotado tenía él mismo viewport
**0 px de alto**, así que no había ni barra que arrastrar. Se descartó y se sustituyó por el
`ContentScroller` de página completa, confirmado con el patrón `Scroll` de UIA
(`VerticallyScrollable=True`, `VerticalViewSize=50.2 %` → contenido ~916 px en un viewport de 460).

**Tests:** +4 unitarios en `WindowSizingTests` (celda de cuarto en 1920×1080, celda de media en un
portátil 1366×768, monitor QHD donde el mínimo se conserva intacto, y clamp en píxeles físicos a
150 % de DPI) y +2 UI en el nuevo **`SnapLayoutTests`** (`MonitorInfo.cs` extrae el P/Invoke de
WorkArea que `LayoutTests` ya tenía, para compartirlo). El test de la celda de cuarto **no** exige ver
la tabla nada más encoger —a esa altura es correcto que quede bajo el pliegue—, exige lo que de verdad
importa: que la página sea desplazable y que, al bajar del todo, la tabla esté visible y en pantalla.

**Nota de proceso:** el XAML en curso traía un `--` dentro de un comentario `<!-- -->`, ilegal en XML.
El XamlCompiler de WinUI fallaba con **exit 1 y cero diagnóstico** (`MSB3073`, sin línea ni archivo);
el error solo salió al validar el XAML como XML a mano. Si el markup compiler vuelve a fallar mudo,
ese es el primer sitio donde mirar.

**Resultado: build 0/0, 93/93 unitarios (89 + 4) y 16/16 UI tests (14 + 2, estables en 3 corridas)**,
más captura de la app real en ambas celdas de snap. **Con esto Tier B queda completo (8/8).** El
arreglo está en `main` **sin publicar**: al ser corrección de bug, la próxima release sería 1.4.1.

### 2026-07-10 — release: v1.4.0 — Tier B (#1–#6 + #8)

Ejecutado `release.ps1 -Version 1.4.0` (bump `1.3.0` → `1.4.0`, MINOR por semver: Tier B añadió
funcionalidad retrocompatible —UI adaptable, tamaño mínimo, wrap responsivo, accesibilidad— más el
proyecto de UI tests con FlaUI; ningún cambio de ruptura). Antes de correrlo se hizo `git add`
explícito de los archivos nuevos sin rastrear (`Core/WindowSizing.cs`, `UI/WindowSizer.cs`,
`UI/WrapPanel.cs`, `tests/WingetUSoft.Tests/WindowSizingTests.cs` y todo `tests/WingetUSoft.UiTests/`)
porque `release.ps1` solo hace `git add -u` (rastreados) — mismo cuidado que documentó el release de
1.3.0.

Resultado: 89/89 tests unitarios en verde (el release **no** corre los 14 UI tests de FlaUI, que
necesitan sesión de escritorio interactiva), instalador `WingetUSoft-Setup-1.4.0.exe` (34.1 MB)
compilado **sin firmar** (no hay certificado de código real disponible — ver Pendientes §6; la
auto-actualización desde 1.3.0 rechazará este instalador, la descarga manual sí funciona), commit
`c9acf6c` ("release: v1.4.0", 28 archivos), tag `v1.4.0` y push a `origin/main`, GitHub Release
publicado: https://github.com/xfiberex/WingetUSoft/releases/tag/v1.4.0.

### 2026-07-10 — test: Tier B #8 — proyecto de UI tests con FlaUI (`WingetUSoft.UiTests`)

**Nuevo proyecto `tests/WingetUSoft.UiTests/`** (FlaUI.Core + FlaUI.UIA3 5.0.0, xUnit, TFM
`net10.0-windows10.0.19041.0`, **sin `ProjectReference` a la app** — lanza el `.exe` compilado). Porta
el patrón ya probado de `FormatDiskPro.UiTests`. La implementación la ejecutó un subagente **Sonnet 5**
(esfuerzo alto, workflow por pasos); Opus orquestó, revisó y verificó de forma independiente.
Añadido a `WingetUSoft.slnx`.

**Diferencia clave con FormatDiskPro (bien resuelta):** WingetUSoft corre **`asInvoker`** (winget se
eleva bajo demanda vía worker + named pipe), así que el `AppFixture` **elimina el `EnsureElevated()`**
de FDP — el proceso de test **no** necesita terminal elevada. No se cubren operaciones reales de winget
(upgrade/uninstall disparan UAC en escritorio seguro, inautomatizable con FlaUI).

**Infra portada:** `AppFixture` (`ICollectionFixture`, lanza el exe, `GetMainWindow`, descarta los
diálogos de arranque Novedades/Actualización), `SettingsBackup` (respalda/restaura
`%AppData%\WingetUSoft\settings.json` + `history.log` para no filtrar cambios de prueba a la instalación
real), `DialogHelper` (ContentDialog de WinUI: `PrimaryButton`/`SecondaryButton`/`CloseButton` por
AutomationId, `SafeCloseAnyDialog`, `DismissStartupDialogs`), `MenuActions` (navega los `DropDownButton`
+ `MenuFlyout` de Opciones/Ayuda; con fallback a `SelectionItem` para los `RadioMenuFlyoutItem` de
idioma/tema).

**Tests (14, todos verdes):** `MainWindowTests` (la ventana abre y no está offscreen; los 7 botones de
"Acciones rápidas" y `lvPackages` presentes por AutomationId), **`LayoutTests`** (el corazón del Tier B:
(a) `MainWindow.BoundingRectangle` cabe en la `WorkArea` del monitor — #1 DPI/WorkArea; (b) redimensiona
la ventana a un ancho estrecho vía `TransformPattern.Resize` y verifica que los 7 botones siguen visibles
con `BoundingRectangle` no vacío — #3 wrap), `MenuDialogsTests` (Ayuda → Acerca de abre un ContentDialog
con versión y se cierra), `SettingsTests` (cambio de idioma en caliente ES↔EN restaurado, y apertura/
cierre de la ventana real `SettingsWindow`).

**Dos hallazgos reales durante el endurecimiento** (no artefactos de test): (1) el proceso de test no era
DPI-aware, así que `GetMonitorInfo` devolvía coordenadas virtualizadas mientras UIA reporta píxeles
físicos → se añadió un `[ModuleInitializer]` que llama `SetProcessDpiAwarenessContext(PER_MONITOR_AWARE_V2)`
para igualar el manifest de la app; (2) el `WrapPanel` necesita su propio pase de Measure/Arrange tras el
resize → se añadió un settle + `Retry.WhileNull` por botón. **No** hubo que añadir ningún `x:Name` a la app
(todos los AutomationId usados ya existían). **Resultado: build 0/0, 89/89 unitarios + 14/14 UI en verde**
(estables en 5 corridas consecutivas, sin flakiness ni procesos `WingetUSoft.exe` colgados). Con esto el
Tier B queda completo salvo #7 (verificación manual de snap layouts).

### 2026-07-10 — feat: Tier B — Fase 1 (#1–#6) mejoras visuales, responsivas y accesibilidad

Implementados los ítems #1–#6 del Tier B (arreglos de layout + accesibilidad). Origen: reporte del
usuario con el botón "Cancelar" recortado contra el borde de la ventana. La implementación la ejecutó
un subagente **Sonnet 5** (esfuerzo alto, workflow por pasos con gates build+tests); Opus orquestó,
revisó y detectó dos refinamientos aplicados en una segunda pasada (ver más abajo). Plan aprobado en
`C:\Users\User\.claude\plans\encapsulated-waddling-wilkinson.md`.

**Decisiones tomadas con el usuario (2026-07-10):**
- **#3 barra responsiva:** WrapPanel **nativo propio** (cero dependencias nuevas), no CommunityToolkit
  ni CommandBar — mantiene la filosofía minimalista (solo 3 NuGets) y el estilo visual actual.
- **#1/#2 redimensionado:** las 5 ventanas **siguen redimensionables**; se les añade dimensionado por
  DPI + acotado a WorkArea y `PreferredMinimumWidth/Height`. No se copia el modelo de "ventana fija"
  de FormatDiskPro.
- **Alcance:** esta ronda solo #1–#6. #7 (snap layouts, manual) y #8 (FlaUI) quedan aparte.

**Nuevos archivos:**
- `Core/WindowSizing.cs` — matemática **pura** y testeable (sin tipos `Windows.*`): `ComputeSizeAndCenter`
  (réplica de `SizeAndCenterWindow` de FormatDiskPro: diseño en DIP × escala DPI, acotado a WorkArea,
  centrado) y `ScaleMinSize` (mínimo escalado **y acotado** a la WorkArea — ver refinamiento 2).
- `UI/WindowSizer.cs` — wrapper UI delgado reutilizado por las 5 ventanas: `GetDpiForWindow` (P/Invoke) +
  `DisplayArea.WorkArea` + `WindowSizing` → `Resize`/`Move` + `OverlappedPresenter.PreferredMinimum*`.
  No fija `IsResizable`/`IsMaximizable`.
- `UI/WrapPanel.cs` — `Panel` nativo con `Measure/ArrangeOverride` + `HorizontalSpacing`/`VerticalSpacing`.
- `tests/WingetUSoft.Tests/WindowSizingTests.cs` — 11 casos (escalas 100/150/200 %, clamp de tamaño
  y de mínimo contra pantallas pequeñas, centrado con origen no-cero, margen escalado por DPI).

**Modificados:** los 5 constructores de ventana (`MainWindow`/`Settings`/`Uninstall`/`Cleanup`/`History`
`.xaml.cs`) reemplazan el `Resize(SizeInt32 fijo)` por `WindowSizer.Apply(...)` (#1/#2). `MainWindow.xaml`:
`StackPanel Horizontal`→`local:WrapPanel` en "Acciones rápidas" (#3, arregla el recorte); cabecera + `ListView`
del DataGrid envueltos en un `ScrollViewer` horizontal (`MinWidth=860` + `Width` enlazado al `ViewportWidth`)
para que las columnas rellenen si caben y hagan scroll conjunto si no (#4); `TextTrimming`+tooltip en cabeceras
de columna de ancho fijo (#5); `AutomationProperties.Name`+tooltip localizados en el `FontIcon` "Excluido"
vía `PackageViewModel.ExcludedLabel` (#6). `TextTrimming` defensivo también en `HistoryWindow`/`UninstallWindow`/
`CleanupWindow`. `Localization/Localization.cs`: nueva clave `grid.excludedAccessible` (ES/EN/PT/FR/IT).

**Dos refinamientos detectados en revisión (Opus) y corregidos por Sonnet 5:**
1. La columna "Nombre" (`*`) no rellenaba en ventanas anchas porque el `ScrollViewer` horizontal mide el
   contenido con ancho **infinito** (el `*` colapsa a `Auto`). Se enlazó el `Width` del Grid interno al
   `ViewportWidth` del ScrollViewer (con `MinWidth=860` de piso): rellena si el viewport ≥ 860, hace scroll
   horizontal si < 860.
2. El mínimo escalado por DPI podía **superar la pantalla** en portátiles de baja resolución con DPI alto
   (900×600 @150 % = 1350×900 físico en 1366×768), impidiendo encoger la ventana o hacer snap (#7). Se acotó
   el mínimo a la WorkArea menos el margen, en la lógica pura y con test.

**Auditoría de accesibilidad (#6):** salvo el icono "Excluido", no había ningún control **solo-icono** sin
etiqueta en las 5 ventanas ni en `AboutDialog`/`WhatsNewDialog` (los demás `FontIcon` son `MenuFlyoutItem.Icon`
con `Text` adyacente). No se tocó `TabIndex` (no se detectó ningún orden de tabulación claramente incorrecto).

**Resultado: build 0/0, 89/89 tests en verde, verificación visual del usuario OK** (captura con el
WrapPanel repartiendo los 7 botones de "Acciones rápidas" en 2 filas sin recortes, DataGrid y
dimensionado correctos). Queda **#7** (snap layouts, verificación manual del usuario) antes de pasar a
**#8** (proyecto `WingetUSoft.UiTests` con FlaUI, su propia fase).

### 2026-07-09 — release: v1.3.0 — primera publicación real en GitHub

Ejecutado `release.ps1 -Version 1.3.0` (bump `1.2.0` → `1.3.0`, MINOR por semver: Tier A añadió
funcionalidad nueva y retrocompatible, sin cambios de ruptura). Antes de correrlo se corrigió un
bug en `release.ps1`: el bump de versión solo sustituía `<Version>` en el `.csproj`, dejando
`<AssemblyVersion>`/`<FileVersion>` obsoletos — y tanto el título de la ventana
(`MainWindow.xaml.cs`, `Assembly.GetName().Version`) como la comparación de auto-actualización
(`GitHubUpdateService.cs:47`) leen `AssemblyVersion`, no `<Version>`. Ahora el script actualiza
las tres etiquetas a la vez (`X.Y.Z.0` para las de 4 partes).

Se añadieron a git (`git add`) los ~20 archivos nuevos de Tier A que quedaron sin rastrear en la
sesión anterior (`CONTEXT.md`, `ROADMAP.md`, `LICENSE`, `Localization/`, `Core/ReleaseNotes.cs`,
`Core/Throughput.cs`, `Settings/HistoryFilter.cs`, diálogos `AboutDialog`/`WhatsNewDialog`,
`UI/Notifier.cs`, `UI/TaskbarProgress.cs`, los 5 archivos de test nuevos, y los 3 scripts de
`installer/`+`release.ps1`) — sin esto, `git add -u` los habría dejado fuera del commit de release
y el tag habría apuntado a un build incompleto.

Resultado: 78/78 tests en verde, instalador `WingetUSoft-Setup-1.3.0.exe` (34.2 MB) compilado
**sin firmar** (no hay certificado de código real disponible — ver Pendientes §6), commit
`d2e4c4c` ("release: v1.3.0", 44 archivos), tag `v1.3.0` y push a `origin/main`, GitHub Release
publicado: https://github.com/xfiberex/WingetUSoft/releases/tag/v1.3.0.

### 2026-07-09 — docs: Tier B propuesto en ROADMAP.md (mejoras visuales y responsivas)

A partir de un reporte del usuario (captura con el botón "Cancelar" recortado contra el borde de
la ventana), se investigó la causa raíz en código y se añadió una tabla de 7 ítems a
[`ROADMAP.md`](ROADMAP.md) bajo "Tier B — Mejoras visuales y revisión", todos ⏳ pendientes (solo
documentación, sin implementar nada):

1. Ventanas adaptadas a DPI/área de trabajo — las 5 ventanas usan `AppWindow.Resize(SizeInt32(...))`
   fijo en píxeles, sin escalar por DPI ni acotar a `DisplayArea.WorkArea`; FormatDiskPro ya
   resolvió esto con `SizeAndCenterWindow()` (`FormatDiskPro/UI/MainWindow.xaml.cs:139-168`),
   patrón candidato a portar.
2. Tamaño mínimo de ventana (`OverlappedPresenter.PreferredMinimumWidth/Height`, sin definir hoy).
3. Barra "Acciones rápidas" sin wrap (`MainWindow.xaml:96`) — causa directa del recorte reportado.
4. Columnas del DataGrid con ancho fijo en píxeles (`MainWindow.xaml:332-341` y `:392-401`).
5. Revisión de longitud de texto por idioma (FR/IT ~20-30% más largos que ES) tras las 272 claves
   de la Fase 7 de Tier A.
6. Accesibilidad: `AutomationProperties.Name` en controles solo-icono, orden de tabulación.
7. Verificación manual de snap layouts de Windows 11 en pantallas de baja resolución.

### 2026-07-09 — feat: pipeline de release con Inno Setup (Tier A #8) — Tier A COMPLETADO

**Nuevo** `installer/build-installer.ps1` (adaptado de FormatDiskPro): lee versión y TFM del
`.csproj`, publica con `dotnet publish -r win-x64 --self-contained false` (**framework-dependent**
— decisión distinta a FormatDiskPro, ver más abajo), firma opcionalmente el `.exe`/`.dll`
publicados y compila `installer.iss` con ISCC pasando `/DMyAppVersion`/`/DSourceDir`, firmando
también el instalador resultante si hay certificado. **Nuevo** `release.ps1` en la raíz: valida
versión/árbol git → tests (`WingetUSoft.Tests.csproj`, no hay `.slnx`) → bump `<Version>` → build
instalador → commit + tag `vX.Y.Z` → push → `gh release create`. **Nuevo**
`installer/new-selfsigned-cert.ps1` para generar un certificado de prueba.

`installer/installer.iss`: `MyAppVersion` y `SourceDir` (antes `#define` fijos) pasan a
`#ifndef`/`#endif` para poder sobrescribirse vía `/D` desde el script sin romper `iscc
installer.iss` ejecutado a mano (el valor por defecto de `SourceDir`, `..\publish`, no cambia).
De paso, `ArchitecturesAllowed`/`ArchitecturesInstallIn64BitMode` pasan de `x64` (deprecado en
Inno Setup 6.7.2) a `x64compatible`, quitando un warning del compilador — hallado al verificar el
build real, no estaba en el plan original.

**Decisión de diseño (desviación deliberada de FormatDiskPro):** el publish es
**framework-dependent** (`--self-contained false`), no self-contained. `WindowsAppSDKSelfContained
=true` en `WingetUSoft.csproj` solo empaqueta el Windows App SDK, no el runtime de .NET — y
`installer.iss` ya traía código `[Code]` preexistente (de antes de este Tier A) que detecta y
descarga VC++ Redist / Windows App Runtime / .NET 10 Desktop Runtime si faltan. Forzar
self-contained como FormatDiskPro habría hecho ese código redundante y duplicado ~150 MB de
runtime en el instalador sin necesidad; se respetó el modelo de despliegue que el propio
`installer.iss` ya asumía.

**Verificado con hardware real (no solo revisado, ejecutado de punta a punta):**
- `installer\build-installer.ps1` sin firmar: publish + ISCC compilan limpio, sin advertencias,
  `WingetUSoft-Setup-1.2.0.exe` (34.2 MB) generado en `installer\Output\`.
- `installer\new-selfsigned-cert.ps1` + `build-installer.ps1 -CertThumbprint <huella>`: signtool
  firma `.exe`, `.dll` y el instalador correctamente (certificado de prueba creado y **eliminado
  del almacén al terminar** — no se dejó estado persistente en el equipo).
- `Get-AuthenticodeSignature` sobre el instalador firmado con el cert autofirmado confirma
  `Status=UnknownError` / "cadena termina en un certificado raíz no confiable" — el mismo
  resultado que `GitHubUpdateService.VerifyAuthenticodeSignature` (`WinVerifyTrust`) usaría para
  **rechazar** el instalador. Confirma que el gate de firma de la auto-actualización funciona
  como se espera: un cert real de una CA reconocida pasaría, uno autofirmado no.
- `release.ps1 -Version 1.3.0 -DryRun -AllowDirty`: muestra el plan completo sin tocar nada y
  **corre las 78 pruebas inline** (verde) — confirma que el flujo de validación previo al build
  funciona antes de comprometerse a publicar.

**Hallazgo de plataforma (no estaba en el plan, encontrado al ejecutar los scripts):** los tres
`.ps1` se habían guardado sin BOM UTF-8; Windows PowerShell 5.1 los interpretó con el codepage
ANSI del sistema, y los acentos/guion largo (`—`) de `release.ps1` generaron bytes que rompían el
tokenizer del parser ("Falta el paréntesis de cierre"). Reescritos los tres con BOM UTF-8
(`System.Text.UTF8Encoding($true)`); mismo problema que ya documenta `FormatDiskPro/CONTEXT.md`
para sus propios scripts — añadida la misma nota a la sección 4 de este documento para que no se
repita en scripts futuros.

`.gitignore` gana `*.pfx` (ya tenía `publish/` e `installer/Output/`) para no arriesgar subir una
clave privada de firma por accidente.

**Resultado: 78/78 tests en verde, build 0/0.** Con esto se cierran las 9 fases del Tier A
(-1 a 8): WingetUSoft tiene paridad de infraestructura de app con FormatDiskPro dentro de su
propio propósito (gestión de software vía winget), sin haber tocado nada fuera de alcance.

### 2026-07-08 — feat: extracción completa de strings a 5 idiomas (Tier A #7)

El diccionario `L.Map` pasa de ~30 claves (solo menú principal, Fase 1) a **272 claves**. Cubre
las 5 ventanas (`MainWindow`, `SettingsWindow`, `UninstallWindow`, `CleanupWindow`,
`HistoryWindow`) por completo: cada `TextBlock`/`Content`/`PlaceholderText` sin nombre ganó
`x:Name` para poder localizarlo desde código; cada ventana secundaria ganó su propio
`ApplyLocalizedStrings()` llamado una vez en el constructor (a diferencia de `MainWindow`, no
tienen menú de idioma propio — heredan `L.Current` vigente al abrirse). `Converters.cs` no tenía
strings que extraer.

También se localizaron mensajes de `Core/` que llegan directo a diálogos o al log de actividad:
`UpgradeResult.GetFailureReason()` (los tests existentes siguen en verde porque `L.Current` es
`Es` por defecto durante toda la corrida de tests — ningún test cambia el idioma), mensaje de
instalador sin firma en `GitHubUpdateService`, y las cadenas de progreso/error del modo
administrador en `WingetService` (solicitud de elevación, resultado del lote elevado).

**Límite deliberado (documentado, no un olvido):** se dejaron sin traducir (1) las claves de
parseo de la salida de `winget` (`"Homepage:"`, `"Description:"`, etc. — deben coincidir
literalmente con el texto que devuelve el CLI, no son texto de UI); (2) los mensajes de protocolo
interno del worker elevado en rutas de error muy profundas (fallos de negociación de la sesión
IPC — "no debería pasar nunca"); (3) `AppSettings.LastLoadError`/`LastSaveError`, porque
`AppSettings.Load()` corre *antes* de que el idioma se determine (el propio `Language` viene del
archivo que se está cargando) — traducirlos ahí no funcionaría de verdad.

Sin tests nuevos (extracción de strings existentes, no lógica nueva); `LocalizationTests` ya
cubre por completitud las 272 claves sin necesitar un test por clave. **Resultado: 78/78 tests en
verde** (sin cambios respecto a la Fase 6), build 0/0.

### 2026-07-08 — feat: aviso al terminar + progreso en la barra de tareas (Tier A #3)

Ports literales desde FormatDiskPro: `UI/Notifier.cs` (`ShouldNotify(elapsed, enabled, cancelled,
threshold)` puro y testeable + `OperationFinished(hwnd)` con Win32 `FlashWindowEx`/`MessageBeep`,
nunca lanza) y `UI/TaskbarProgress.cs` (`SetValue`/`SetIndeterminate`/`Clear` sobre
`ITaskbarList3` vía COM, no-op si el shell no lo soporta). Se colocaron en `UI/` en vez de un
`Services/` que no existe en este proyecto (WingetUSoft usa `Core/`+`Settings/`+`UI/`, no la
carpeta `Services/` de FormatDiskPro).

Cada ventana con operaciones potencialmente largas gana un campo `_hWnd` (antes era solo una
variable local en el constructor, descartada después de construir `AppWindow`):
`MainWindow`, `UninstallWindow`, `CleanupWindow`.

- **`MainWindow.UpdatePackagesAsync`** (actualización en lote): `Stopwatch` de la operación
  completa; `TaskbarProgress.SetValue` por paquete en el bucle secuencial silencioso,
  `SetIndeterminate` en el lote elevado como administrador (progreso real no disponible ahí,
  igual que ya ocurría con el progreso in-app); `Clear` en el `finally`. Al final,
  `Notifier.ShouldNotify`/`OperationFinished` — **reutiliza** el ajuste ya existente
  `AppSettings.ShowNotifications` (el mismo toggle "Mostrar notificaciones al completar
  actualizaciones" de `SettingsWindow`, que antes solo controlaba el texto de `ShowUpdateNotification`)
  en vez de introducir un setting nuevo, tal como especificaba el plan.
- **`LnkDescargarUpdate_Click`** (descarga del instalador de la propia app): progreso real en la
  taskbar durante la descarga (mismo `Progress<double>` que ya alimentaba la barra in-app),
  indeterminado mientras se lanza el instalador, limpiado si falla.
- **`UninstallWindow.UninstallSelectedAsync`** y **`CleanupWindow.DeleteSelectedAsync`**: mismo
  patrón (`Stopwatch` + `TaskbarProgress` + `Notifier` al final, gateado por el `ShowNotifications`
  de cada ventana vía `_settings` compartido) — no estaban explícitos en el ejemplo del plan pero
  sí en su alcance ("Integrar igualmente el aviso al terminar en desinstalaciones largas y
  limpieza"). En `CleanupWindow` el `foreach` de borrado se cambió a `for` indexado para poder
  reportar progreso por elemento sin alterar el resto de la lógica.

**Nuevo test** `NotifierTests.cs` (port literal, 5 casos: umbral cumplido/no cumplido,
deshabilitado, cancelado). El efecto real (Win32) no se cubre con pruebas unitarias, igual que en
FormatDiskPro. **Resultado: 64/64 tests en verde** (59 previos + 5 nuevos), build 0/0.

### 2026-07-08 — feat: velocidad y ETA en operaciones largas (Tier A #4)

**Nuevo** `Core/Throughput.cs`: port de `Eta(remainingBytes, bytesPerSec)` y `FormatEta(TimeSpan?)`
(`mm:ss` o `h:mm:ss` si supera la hora), lógica pura. **Se omitió `FormatSpeed`** (a diferencia de
FormatDiskPro) porque WingetUSoft ya tenía `FormatBytes` inline en `MainWindow` reutilizado en
varios sitios — introducir un duplicado en `Core` habría sido la opción B del plan, se eligió la A
(mantener `FormatBytes` donde está).

- **`UpdateLogDownloadLine`** (línea de progreso de descarga bajo el log): añade `ETA mm:ss` junto
  a la velocidad ya existente, calculado con `Throughput.Eta(TotalBytes - DownloadedBytes,
  SpeedBytesPerSecond)`.
- **`UpdatePackagesAsync`** (bucle secuencial del lote): el texto de estado pasa de
  `"Actualizando (i/N): pkg..."` a incluir velocidad y ETA cuando hay progreso de bytes:
  `"Actualizando (i/N): pkg  ·  4,2 MB/s  ·  02:10 restante"`.
- **`LnkDescargarUpdate_Click`** (descarga del instalador de la propia app): el `IProgress<double>`
  de `GitHubUpdateService.DownloadInstallerAsync` solo reporta la **fracción** completada (0–1), no
  bytes ni velocidad — no se tocó esa API pública. En su lugar, se **extrapola** el ETA a partir del
  tiempo transcurrido (`Stopwatch`) y la propia fracción (`tiempo_total_estimado = transcurrido / p`),
  mostrado en el `InfoBar`. Es una aproximación (mejora conforme avanza la descarga), documentada
  como tal en el código — decisión pragmática para no ampliar la superficie pública de
  `GitHubUpdateService` solo para este dato.

**Nuevo test** `ThroughputTests.cs` (port parcial: `Eta`/`FormatEta`, 6 casos; sin los casos de
`FormatSpeed` que no aplican). **Resultado: 70/70 tests en verde** (64 previos + 6 nuevos), build 0/0.

### 2026-07-08 — feat: historial con búsqueda, filtros y exportación (Tier A #5)

**Nuevo** `Settings/HistoryFilter.cs` (no existía un `HistoryFilter` en FormatDiskPro con ese
nombre exacto — se diseñó desde cero siguiendo la idea del plan, `Apply(entries, texto, estado)`
puro): `enum HistoryStatusFilter { All, Success, Failed }` + filtrado por nombre/Id (contiene,
insensible a mayúsculas) combinable con el filtro de estado.

`UI/HistoryWindow.xaml`: nueva barra entre el header y la lista (fila `Grid` añadida, filas
siguientes desplazadas) con `TextBox` de búsqueda en vivo, `DropDownButton`+`RadioMenuFlyoutItem`
para el estado (mismo patrón que el filtro "Excluidos" de `MainWindow`) y botón "Exportar CSV...".
`HistoryWindow.xaml.cs`: `_allHistory` guarda el historial completo sin filtrar; `ApplyFilter()`
recalcula la vista con `HistoryFilter.Apply` en cada cambio de texto/estado; el resumen distingue
"N registro(s) cargados" de "N de M registro(s)" cuando el filtro reduce la lista. La exportación
reutiliza `Core/DelimitedTextExporter.BuildRow` (mismo helper que ya usaba `MainWindow` para
exportar la lista de paquetes) y el patrón `FileSavePicker`/`InitializeWithWindow` — exporta lo
**filtrado**, no el historial completo. Columnas: Fecha, Nombre, Id, Version origen, Version
destino, Resultado.

**Nuevo test** `HistoryFilterTests.cs` (8 casos: sin filtros, solo éxito, solo fallido, búsqueda
por nombre/Id insensible a mayúsculas, combinación búsqueda+estado, búsqueda en blanco ignorada,
sin coincidencias). **Resultado: 78/78 tests en verde** (70 previos + 8 nuevos), build 0/0.

### 2026-07-08 — feat: diálogo Acerca de + licencia MIT + menú Ayuda (Tier A #6)

**Nuevo** `LICENSE` en la raíz del repo: texto MIT estándar, © 2026 xfiberex (decisión del usuario
— WingetUSoft usa MIT, a diferencia de FormatDiskPro que usa GPLv3; no hay conflicto porque son
proyectos independientes).

**Nuevo** `UI/AboutDialog.xaml`/`.cs` (adaptado de FormatDiskPro, simplificado): título, versión
(vía `Assembly.GetName().Version`, mismo patrón que `MaybeShowWhatsNewAsync`), descripción,
copyright/licencia, sección de privacidad y botón "Ver en GitHub" (`Windows.System.Launcher`,
sin cerrar el diálogo — `args.Cancel = true`). **Se omitieron el disclaimer de uso destructivo y
el botón de donación** que sí tiene el `AboutDialog` de FormatDiskPro: no aplican al propósito de
WingetUSoft (no hay operaciones irreversibles sobre datos del usuario más allá de desinstalar
software, que winget ya confirma) y no se pidió donación en el plan aprobado.

**Nuevo menú Ayuda**: `DropDownButton` "Ayuda" añadido junto al de "Opciones" en la barra de
acciones. Agrupa "Buscar actualización de WingetUSoft...", "Novedades..." (**movidos** desde el
dropdown `Opciones`, donde habían quedado provisionalmente en la Fase 2) y el nuevo
"Acerca de...". Esto cierra la desviación documentada en la entrada de la Fase 2: en vez de tocar
la disposición del menú dos veces, se dejó la reorganización completa para esta fase, tal como
especificaba el plan aprobado.

`README.md` gana una sección **Licencia** (enlaza `LICENSE`, menciona *Ayuda → Acerca de*) y
**Privacidad** (sin telemetría, conexiones solo a winget y GitHub Releases).

Sin tests nuevos: toda la fase es UI (`ContentDialog` + reorganización de menú), sin lógica nueva
en `Core`/`Settings`. **Resultado: 78/78 tests en verde** (sin cambios respecto a la Fase 5),
build 0/0.

### 2026-07-08 — feat: changelog en actualizaciones + diálogo "Novedades" (Tier A #2)

`Services/GitHubUpdateService.cs` refactorizado: la lógica de fetch+parse de un release se extrajo a
`FetchReleaseAsync(Uri, ct)` privado, compartida por `CheckForUpdateAsync` (gatea por versión),
`GetLatestReleaseAsync` (sin gate, para el fallback de "novedades") y el nuevo
`GetReleaseByTagAsync(tag, ct)` (endpoint `releases/tags/{tag}`, para las notas de la versión
instalada). `GitHubReleaseInfo` gana el campo `Notes` (poblado desde `body` en el JSON).

Port literal de `Core/ReleaseNotes.cs` desde FormatDiskPro (regex-based, Markdown → texto plano:
encabezados, viñetas, negrita/código, enlaces, colapso de saltos). Nuevo `UI/WhatsNewDialog.xaml`/
`.cs`: `ContentDialog` con la versión, las notas y un botón "Ver en GitHub" que abre la URL del
release con `Windows.System.Launcher.LaunchUriAsync` (mismo mecanismo que ya usaba
`CtxBuscarWeb_Click` para abrir winget.run — no hizo falta introducir un helper nuevo).

`AppSettings.LastVersionSeen` (versión cuyas novedades ya se mostraron) + `MaybeShowWhatsNewAsync()`
en `MainWindow`: se dispara una sola vez tras detectar un cambio de versión respecto a
`LastVersionSeen`, con el mismo gate `LoadedFromFile` que ya usa la detección de idioma de la
Fase 1 (evita mostrarlo en una instalación nueva). Se llama en el `Loaded` del arranque, antes de
`CheckForAppUpdateAsync()`. También accesible bajo demanda vía un nuevo ítem **"Novedades..."**
añadido al dropdown `Opciones` existente.

**Desviación deliberada del plan aprobado:** el texto de la Fase 2 menciona un menú "Ayuda →
Novedades…", pero la Fase 6 es la que especifica en detalle la creación de ese menú dedicado
(agrupando Buscar actualizaciones / Novedades / Acerca de). Para no reorganizar el menú dos veces
en fases consecutivas, el ítem "Novedades..." se añadió por ahora al dropdown `Opciones` ya
existente; la Fase 6 hará la migración a un menú `Ayuda` propio moviendo los tres ítems juntos.

El changelog del release (vía nuevo `BuildChangelogMessage`, trunca a 500 caracteres) se antepone
tanto al `InfoBar` de actualización (`CheckForAppUpdateAsync`) como al diálogo de confirmación
(`MenuBuscarActualizacion_Click`), usando las nuevas claves de localización `update.*`.

**Nuevo test** `ReleaseNotesTests.cs` (port literal desde FormatDiskPro, 8 casos: blank/null,
encabezados, viñetas, negrita/código, enlaces, colapso de saltos). **Resultado: 59/59 tests en
verde** (51 previos + 8 nuevos), build 0/0.

### 2026-07-08 — feat: infraestructura de localización + menú Idioma (Tier A #1)

Port literal de `Localization.cs` desde FormatDiskPro: `enum AppLang { Es, En, Pt, Fr, It }` +
clase estática `L` (`Map`, `T(key, args...)` con fallback defensivo a la propia clave, `FromCode`/
`ToCode`/`FromCulture`). `Settings/AppSettings.cs` gana `Language` (código ISO persistido, `null`
= sin elegir todavía) y `LoadedFromFile` (`true` solo cuando `Load()` deserializa un
`settings.json` ya existente con éxito — usado para no reinterpretar el idioma del sistema en una
actualización desde una versión sin este campo).

`UI/MainWindow.xaml`: nuevo `MenuFlyoutSubItem` **Idioma** junto al de **Tema** existente en el
`DropDownButton "Opciones"`, con 5 `RadioMenuFlyoutItem` (mismo patrón exacto que el submenú
Tema: `GroupName`, un `Click` por opción). `UI/MainWindow.xaml.cs`: handlers `MenuIdioma{Es,En,Pt,
Fr,It}_Click` → `SetLanguage(AppLang)` (aplica `L.Set`, persiste `_settings.Language`, actualiza
los radio-checks y llama `ApplyLocalizedStrings()`); en el constructor, si `_settings.Language` es
`null` se siembra desde `CultureInfo.CurrentUICulture.Name` vía `L.FromCulture` (o español si ya
había `settings.json` previo sin ese campo) y se guarda en silencio (sin diálogo de error — el
`XamlRoot` aún no está listo antes de `Loaded`).

`ApplyLocalizedStrings()` cubre **solo los textos del menú principal** en esta fase (según el
plan aprobado): el propio botón "Opciones", los submenús Modo de actualización/Tema/Idioma, las
5 etiquetas de idioma y los ítems Exportar/Configuración/Ver historial/Desinstalar/Buscar
actualización. El resto de ventanas (Settings, Uninstall, Cleanup, History) sigue en español
literal — se extrae por completo en la **Fase 7**.

**Nuevo test** `LocalizationTests.cs` (port de FormatDiskPro, adaptado a las claves de este
proyecto): completitud de `L.Map` (toda clave tiene 5 traducciones no vacías), `FromCode`,
`FromCulture`, round-trip `ToCode`↔`FromCode`. **Resultado: 51/51 tests en verde** (30 previos +
21 nuevos), build 0/0.

### 2026-07-08 — test: migración de `WingetUSoft.Tests` de MSTest a xUnit (Tier A #0)

`WingetUSoft.Tests.csproj` cambia `MSTest.TestAdapter`/`MSTest.TestFramework` por `xunit` +
`xunit.runner.visualstudio`. Los 3 archivos existentes (`AppSettingsTests.cs`,
`CleanupScannerTests.cs`, `WingetServiceTests.cs`) se convirtieron a sintaxis xUnit:
`[TestClass]` eliminado, `[TestMethod]` → `[Fact]`, `[TestInitialize]`/`[TestCleanup]` →
constructor/`IDisposable.Dispose()` en `AppSettingsTests`, `Assert.AreEqual`/`IsTrue`/`IsFalse`/
`IsNull`/`IsNotNull` → `Assert.Equal`/`True`/`False`/`Null`/`NotNull`, `CollectionAssert.AreEqual`
→ `Assert.Equal` (xUnit compara colecciones out-of-the-box), `StringAssert.Contains`/`StartsWith`
→ `Assert.Contains`/`StartsWith` (orden de argumentos invertido: en xUnit el esperado va primero),
`Assert.ThrowsExceptionAsync` → `Assert.ThrowsAsync`. Los `delta:` de `Assert.AreEqual` numérico
(sin equivalente directo en xUnit) se reescribieron como `Assert.InRange(actual, esperado-delta,
esperado+delta)`. **Resultado: 30/30 tests en verde**, mismo número que antes de la migración
(verificado con `dotnet test`). Motivo (decisión del usuario): mejor compatibilidad de xUnit con
FlaUI para futuros tests de UI, y consistencia con FormatDiskPro (que ya usa xUnit).

### 2026-07-08 — docs: creación de `ROADMAP.md` y `CONTEXT.md` (Fase -1 del Tier A)

Antes de empezar a portar código desde FormatDiskPro, se replicó su patrón de documentos vivos
(descrito en `FormatDiskPro/CONTEXT.md`, sección 7) para dar continuidad al trabajo del Tier A
entre sesiones y equipos. `ROADMAP.md` lista las 9 fases (0–8) del plan aprobado con el usuario;
este archivo resume arquitectura, estado y convenciones actuales de WingetUSoft antes de que
empiecen los cambios. Ningún código tocado todavía en esta entrada.
