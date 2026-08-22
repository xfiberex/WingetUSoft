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
>
> 📐 **Este archivo tiene dos partes.** La **Parte I** (abajo) es el histórico de características por
> Tiers A–E: qué se construyó y dónde vive. La **[Parte II](#parte-ii--plan-de-acción-de-auditoría-t0t4)**
> es el plan de acción ejecutable salido de la auditoría técnica del **2026-08-20**, organizado en
> Tiers T0–T4 con tareas marcables. Las dos numeraciones son independientes y no se solapan.

## Estado

| Tier | Tema | Estado | Versión |
|---|---|---|---|
| **A** | Paridad de infraestructura con FormatDiskPro | ✅ Completado | 1.3.0 |
| **B** | Layout adaptable, accesibilidad y UI tests | ✅ Completado | 1.4.0 / 1.4.1 |
| **C** | Auditoría de UI/UX (flujo, datos, color, accesibilidad) | ✅ Completado | 1.5.0 / 1.6.0 |
| **D** | Cara pública (licencia in-app, README, capturas) | ✅ Completado | 1.7.0 |
| **E** | Gestión completa de software ⚠️ *cambio de alcance* | ✅ Completado | 1.8.0 |

No hay ninguna tier en curso ni ideas abiertas: las decisiones ya tomadas (y lo que se descartó a
propósito) están al final.

---

## 🔄 Tier A — Paridad con FormatDiskPro *(v1.3.0)*

Nace de comparar WingetUSoft con su proyecto hermano **FormatDiskPro** (misma arquitectura, mismo autor) y
portar la infraestructura de app que faltaba, sin salirse del propósito.

| # | Característica | Dónde |
|---|----------------|-------|
| 0 | Migración de tests **MSTest → xUnit** | `tests/WingetUSoft.Tests/` |
| 1 | **Localización** (`L`/`AppLang`, detección del idioma del sistema) — 363 claves × 5 idiomas | `Localization/`, `Settings/AppSettings.cs` |
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

## 🧩 Tier E — Gestión completa de software ⚠️ *cambio de alcance* *(v1.8.0)*

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
- **CI con GitHub Actions — descartado (2026-07-12; reafirmado 2026-08-20):** `release.ps1` ya corre
  unitarios **y** UI tests antes de cada corte, y un runner hospedado **no puede** correr los UI tests
  (necesitan escritorio interactivo); solo duplicaría lo ya cubierto, con menos cobertura. **Nada de
  workflows, GitHub Actions ni runners hospedados: todo el testing profesional se hace en local.** La
  auditoría del 2026-08-20 cuestionó el alcance de esta decisión y se reafirmó sin matices; el plan de
  la Parte II la respeta y canaliza la automatización por scripts locales (T2-12).
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

---
---

# Parte II — Plan de acción de auditoría (T0–T4)

> **Origen:** auditoría técnica completa del repositorio, **2026-08-20** (12 áreas: código, seguridad,
> rendimiento, SEO, accesibilidad, responsive/UX/i18n, arquitectura, QA, refactorización, redacción,
> documentación y DevOps).
>
> **Qué es esto y qué NO es.** Esto es un **plan ejecutable**: cada punto es una tarea marcable,
> independiente y verificable. El diagnóstico —el porqué de cada tarea— vive en el informe de auditoría,
> no aquí. La **Parte I** (arriba) sigue siendo el histórico de características por Tiers A–E y no la
> sustituye este plan: son numeraciones distintas (`A`–`E` frente a `T0`–`T4`) y conviven.
>
> **Estado base verificado el 2026-08-20:** `dotnet test` unitarios **162/162 correctas** (260 ms) ·
> `dotnet list package --vulnerable` → **0 vulnerabilidades** · versión en `.csproj`: **1.8.2**.
> Los UI tests (FlaUI) **no se ejecutaron** en la auditoría: requieren escritorio interactivo.

## Índice

| Tier | Qué entra aquí | Tareas | Esfuerzo bajo | medio | alto |
|---|---|---:|---:|---:|---:|
| **T0** — Crítico / bloqueante | Pérdida de datos y borrado de archivos fuera de ámbito | **2** | 2 | 0 | 0 |
| **T1** — Alta prioridad | Bugs de cara al usuario, barreras de accesibilidad, i18n rota, doc falsa | **22** | 16 | 6 | 0 |
| **T2** — Mejoras sustanciales | Rendimiento, refactorización, cobertura, responsive, verificación local | **23** | 17 | 5 | 1 |
| **T3** — Pulido y mantenimiento | Código muerto, redacción, consistencia de estilo, UX menor | **17** | 17 | 0 | 0 |
| **T4** — Futuro / opcional | Evolución del stack y de la arquitectura. Fuera del alcance inmediato | **6** | 3 | 0 | 3 |
| | **Total** | **70** | **55** | **11** | **4** |

**Orden de ejecución recomendado:** T0 completo → los 12 *quick wins* de T1 (todos de esfuerzo bajo y
sin dependencias) → el resto de T1 → T2 por bloques temáticos → T3 en cualquier hueco → T4 solo con
decisión explícita.

**Progreso (2026-08-22): 46 de 70.** ✅ **T0, T1 y T2 completos** (2 + 22 + 23). Siguiente frente:
**T3** (17 tareas de pulido y mantenimiento) y **T4** (6, solo con decisión explícita).

---

## 🔴 Tier T0 — Crítico / Bloqueante

> Se atiende **antes que cualquier otra cosa**. Ambas tareas son de esfuerzo bajo y sin dependencias:
> se pueden cerrar las dos en una sola sesión.

- [x] **[T0-01] Validar que toda ruta candidata de limpieza quede dentro de su directorio base**
  - **Área:** Seguridad
  - **Ubicación:** `src/WingetUSoft/Services/CleanupScanner.cs:50-82` (consumida por `src/WingetUSoft/UI/CleanupWindow.xaml.cs:166`)
  - **Qué hacer:** `GetCandidatePaths` compone rutas con `Path.Combine(baseDir, term)` donde `term` sale de
    `package.Name` / `package.Id`. `Path.Combine` **no normaliza `..`** y **descarta el primer argumento si
    el segundo tiene raíz**, y `SanitizeName` solo recorta sufijos entre paréntesis. Como los paquetes vienen
    de `winget list` (que incluye entradas ARP del registro local, escritas por instaladores de terceros),
    el término no es dato de confianza. Añadir dos defensas: (1) descartar todo término que contenga
    `Path.GetInvalidFileNameChars()`, separadores de ruta, `:` o `..`; (2) tras componer, comprobar
    contención con `Path.GetFullPath` antes de devolver el candidato.
    ```csharp
    private static bool IsInside(string baseDir, string candidate)
    {
        string root = Path.GetFullPath(baseDir).TrimEnd(Path.DirectorySeparatorChar);
        string full = Path.GetFullPath(candidate);
        return full.Length > root.Length
            && full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
    ```
  - **Criterio de aceptación:** un `WingetPackage` con `Name = @"..\..\..\Windows"` o `Name = @"C:\Windows"`
    no produce ningún candidato; los nombres normales siguen produciendo exactamente los mismos candidatos
    que hoy (los 6 tests de `CleanupScannerTests` siguen en verde).
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T0-02] Escritura atómica de `settings.json`**
  - **Área:** Seguridad / integridad de datos
  - **Ubicación:** `src/WingetUSoft/Settings/AppSettings.cs:100-116`
  - **Qué hacer:** `Save()` usa `File.WriteAllText` sobre el archivo definitivo, que trunca antes de escribir.
    Una interrupción a mitad deja JSON parcial y `Load()` responde restaurando **valores por defecto**: se
    pierden hasta 500 entradas de historial, la lista de exclusiones, las versiones omitidas, el idioma y el
    tema. Escribir a temporal en el mismo volumen y sustituir con `File.Replace` (atómico en NTFS y deja
    copia de respaldo por sí solo).
    ```csharp
    string tmp = SettingsFilePath + ".tmp";
    File.WriteAllText(tmp, JsonSerializer.Serialize(this, JsonOptions));
    if (File.Exists(SettingsFilePath)) File.Replace(tmp, SettingsFilePath, SettingsFilePath + ".bak");
    else File.Move(tmp, SettingsFilePath);
    ```
  - **Criterio de aceptación:** tras un `Save()` correcto no queda ningún `.tmp` en el directorio de datos;
    si se simula un fallo entre la escritura del temporal y el reemplazo, `settings.json` conserva íntegro
    el contenido anterior y `Load()` no reporta `LastLoadError`.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

---

## 🟠 Tier T1 — Alta prioridad

> Errores funcionales visibles, barreras graves de accesibilidad, internacionalización rota y
> documentación que afirma cosas falsas. **T1-01, T1-03, T1-04, T1-05, T1-08, T1-09, T1-10, T1-12,
> T1-15, T1-16, T1-18 y T1-22 son los *quick wins*:** esfuerzo bajo y sin dependencias.

### Corrección de errores

- [x] **[T1-01] Coincidencia por palabra completa en la clasificación de fallos**
  - **Área:** Código
  - **Ubicación:** `src/WingetUSoft/Core/Models/UpgradeResult.cs:43`
  - **Qué hacer:** `combined.Contains("red", …)` casa dentro de `requi-red`, `sha-red`, `expi-red`,
    `configu-red`. Un fallo real como *«A required file is missing»* se le presenta al usuario como
    «Error de red». Exigir límite de palabra en todos los términos cortos de la cadena de `if`, con un
    ayudante `HasWord(text, word)` basado en `Regex.IsMatch(text, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase)`.
  - **Criterio de aceptación:** test nuevo — `ErrorOutput = "A required file is missing"` **no** devuelve
    `reason.networkError`; `ErrorOutput = "Error de red al descargar"` **sí** lo devuelve. Los 5 tests
    existentes de `GetFailureReason` siguen en verde.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T1-02] Clasificar los fallos por código de salida, no por texto traducido**
  - **Área:** Código / i18n
  - **Ubicación:** `src/WingetUSoft/Core/Models/UpgradeResult.cs:17-44`
  - **Qué hacer:** la cadena de `if` solo reconoce español e inglés, contradiciendo la lección que el propio
    proyecto documenta en `Core/WingetTable.cs:9-13` y resolvió con `Services/WingetShowLabels.cs`: winget
    **traduce su salida** al idioma de Windows. En un Windows en FR/IT/DE/PT ninguna rama casa y todo cae al
    *fallback* de «última línea con sentido». Reordenar para que los códigos hexadecimales de winget
    (independientes del idioma) se comprueben primero, y dejar el texto solo como último recurso.
  - **Criterio de aceptación:** un `ErrorOutput` con solo el código de winget (sin texto) devuelve el
    motivo correcto; existe un test por cada código soportado (12 en total).
  - **Corrección sobre el enunciado:** el criterio original decía que `0x8A150011` debía devolver
    `reason.noApplicableUpdate`, que es lo que hacía el código — y **es incorrecto**. Según la tabla
    oficial de `winget-cli` (`doc/windows/package-manager/winget/returnCodes.md`), `0x8A150011` es
    *«el hash del instalador no coincide con el manifiesto»* y «no hay actualización aplicable» es
    `0x8A15002B`. Igual con `0x8A150014`, que es *«no se encontró el paquete»* y no «ningún instalador
    aplicable» (`0x8A150010`). Se implementa la asignación correcta y hay un test que la fija.
  - **Esfuerzo:** medio
  - **Depende de:** T1-01

- [x] **[T1-12] Dejar de tragar en silencio todas las excepciones no controladas**
  - **Área:** Código / observabilidad
  - **Ubicación:** `src/WingetUSoft/App.xaml.cs:12-25` (y el `catch` equivalente en `src/WingetUSoft/Program.cs:25-36`)
  - **Qué hacer:** el manejador hace `e.Handled = true` incondicional y escribe `crash.log` con
    `File.WriteAllText`, que **pisa el fallo anterior**. La app sigue viva en estado desconocido sin decirle
    nada al usuario y la segunda excepción borra la evidencia de la primera. Cambiar a `File.AppendAllText`
    con marca de tiempo (y recorte por tamaño), mostrar un aviso al usuario, y reservar `Handled = true`
    para tipos concretos y recuperables.
  - **Criterio de aceptación:** dos excepciones consecutivas dejan **dos** entradas fechadas en `crash.log`;
    una excepción no prevista produce un aviso visible en lugar de un fallo silencioso.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

### Accesibilidad

- [x] **[T1-04] `CleanupWindow` debe usar `LogPalette` en vez de RGB cableado**
  - **Área:** Accesibilidad (WCAG 2.2 AA · 1.4.3 Contraste mínimo)
  - **Ubicación:** `src/WingetUSoft/UI/CleanupWindow.xaml.cs:255-279`
  - **Qué hacer:** los colores `#387A4D` (éxito) y `#BA4636` (error) son **exactamente** los que
    `Core/LogPalette.cs:11-14` documenta como el bug corregido en la v1.5.0. Medidos sobre el fondo de tarjeta
    oscura `#2B2B2B` dan **2,74:1** y **2,71:1**, por debajo del mínimo AA de 4,5:1. Además el caso `Normal`
    lee `Application.Current.Resources["TextFillColorPrimaryBrush"]`, un recurso de nivel de aplicación que
    **no sigue** el `RequestedTheme` forzado por elemento. Sustituir por
    `LogPalette.For(kind, rtbLog.ActualTheme == ElementTheme.Dark)`, como ya hace `SearchWindow`.
  - **Criterio de aceptación:** ningún `SolidColorBrush` con literales RGB en el archivo; el registro de la
    ventana de limpieza se lee correctamente en tema oscuro y todos los tipos superan 4,5:1.
  - **Esfuerzo:** bajo
  - **Depende de:** T2-06

- [x] **[T1-05] `UninstallWindow` debe usar `LogPalette` en vez de RGB cableado**
  - **Área:** Accesibilidad (WCAG 2.2 AA · 1.4.3)
  - **Ubicación:** `src/WingetUSoft/UI/UninstallWindow.xaml.cs:257-281`
  - **Qué hacer:** mismo problema y misma corrección que T1-04, con los mismos tres colores.
  - **Criterio de aceptación:** idéntico a T1-04, aplicado a la ventana de desinstalación.
  - **Esfuerzo:** bajo
  - **Depende de:** T2-06

- [x] **[T1-06] Test estructural que impida volver a cablear colores de registro**
  - **Área:** QA / Accesibilidad
  - **Ubicación:** `tests/WingetUSoft.Tests/LogPaletteTests.cs`
  - **Qué hacer:** `LogPaletteTests` mide el contraste de `LogPalette` y rompe el build si baja de 4,5:1 —
    hace exactamente lo que promete. El problema es que dos ventanas **no usan `LogPalette`**, así que el
    test pasa en verde mientras esas ventanas incumplen. Añadir un test que escanee el código fuente de
    `src/WingetUSoft/UI/` (al estilo de `LocalizationUsageTests`) y falle si algún archivo con `rtbLog`
    construye `Color.FromArgb` con literales.
  - **Criterio de aceptación:** el test falla si se revierte T1-04 o T1-05, y pasa con el código corregido.
  - **Esfuerzo:** medio
  - **Depende de:** T1-04, T1-05

- [x] **[T1-07] Región activa (`LiveSetting`) en la barra de estado y el registro**
  - **Área:** Accesibilidad (WCAG 2.2 AA · 4.1.3 Mensajes de estado)
  - **Ubicación:** `src/WingetUSoft/UI/MainWindow.xaml:584` (`txtEstado`) y `:555` (`rtbLog`); equivalentes en
    `CleanupWindow.xaml`, `UninstallWindow.xaml`, `SearchWindow.xaml`, `HistoryWindow.xaml`
  - **Qué hacer:** `grep -rn "LiveSetting" src` devuelve **cero resultados**. La barra de estado —que el
    propio comentario del XAML llama «lo único que el usuario mira» durante una operación larga— cambia sin
    notificar al árbol de automatización, así que un lector de pantalla nunca anuncia «Actualizando 3 de 8»,
    «Completado» ni «Error». Añadir `AutomationProperties.LiveSetting="Polite"` en cada `txtEstado`, y para
    el `RichTextBlock` emitir el evento con
    `FrameworkElementAutomationPeer.FromElement(txtEstado)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged)`.
  - **Criterio de aceptación:** con el Narrador activo, iniciar una consulta anuncia el cambio de estado sin
    que el foco esté en la barra.
  - **Verificado (2026-08-21):** ~~no automatizable~~ — **sí lo es en su mayor parte**. Lo que no se puede
    automatizar es *oír* al Narrador; lo que falla en la práctica es que las propiedades de UI Automation
    no estén, y eso un cliente UIA lo ve igual que un lector de pantalla. `AccessibilityTests`
    (UI tests) comprueba el `LiveSetting=Polite` y **se suscribe a `LiveRegionChanged`** para exigir que
    el evento se emita de verdad al cambiar el texto. Queda como manual solo *cómo suena* el anuncio.
  - **Desviación al implementarlo:** el `RichTextBlock` del registro **no** se convierte en región activa.
    Una región activa se anuncia leyendo *todo* su contenido, y el registro llega a 400 líneas: cada línea
    nueva releería el bloque entero. En su lugar el registro recibe un **nombre accesible**
    (`LabeledBy` → `txtLogHeader`) para que sea identificable al recorrer la ventana, y el anuncio de
    progreso se queda donde procede, en la barra de estado.
  - **Esfuerzo:** medio
  - **Depende de:** ninguna

- [x] **[T1-08] Nombre accesible para los `ToggleSwitch` de Configuración**
  - **Área:** Accesibilidad (WCAG 2.2 AA · 4.1.2 Nombre, función, valor)
  - **Ubicación:** `src/WingetUSoft/UI/SettingsWindow.xaml:137-148`
  - **Qué hacer:** `tsShowNotifications` y `tsMinimizeToTray` llevan su etiqueta en un `TextBlock`
    independiente, sin `Header`, sin `AutomationProperties.Name` y sin `LabeledBy` (el proyecto no usa
    `LabeledBy` en ningún sitio: 0 apariciones). El lector anuncia «interruptor, desactivado» sin decir de
    qué. Asociar con `AutomationProperties.LabeledBy="{Binding ElementName=txtShowNotifLabel}"` o mover el
    texto al `Header` del propio control.
  - **Criterio de aceptación:** en Accessibility Insights, ambos interruptores exponen un `Name` no vacío que
    coincide con su etiqueta visible en los 5 idiomas.
  - **Verificado (2026-08-21):** automatizado en `AccessibilityTests` — abre Configuración y exige que el
    `Name` de cada interruptor sea **igual** al de su etiqueta visible, que es lo que garantiza que siga
    al idioma sin claves nuevas.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T1-09] Nombre accesible en las filas y casillas de `CleanupWindow`**
  - **Área:** Accesibilidad (WCAG 2.2 AA · 4.1.2)
  - **Ubicación:** `src/WingetUSoft/UI/CleanupWindow.xaml:174-190` y `src/WingetUSoft/Core/Models/CleanupItemViewModel.cs`
  - **Qué hacer:** `MainWindow` y `SearchWindow` exponen `RowLabel`/`SelectLabel` localizados (trabajo nacido
    de conducir la app real); `CleanupWindow` no. La casilla que decide **qué carpetas se borran de forma
    recursiva** se anuncia solo como «casilla de verificación», sin ruta ni contexto. Añadir a
    `CleanupItemViewModel` las mismas dos propiedades, con claves nuevas en `Localization.cs`.
  - **Criterio de aceptación:** cada fila se anuncia con ruta, tipo y tamaño; la casilla se anuncia como
    «Seleccionar *ruta* para eliminar». `LocalizationTests` sigue en verde (5 traducciones por clave nueva).
  - **Verificado (2026-08-21):** en `CleanupItemLabelsTests` (unitario) y **no** en los UI tests, porque
    `CleanupWindow` no es alcanzable conduciendo la app: solo se abre desde `UninstallWindow` tras
    desinstalar un programa de verdad, y ningún test desinstala nada del equipo.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

### Seguridad y confirmaciones

- [x] **[T1-10] Listar las rutas en la confirmación de borrado**
  - **Área:** Seguridad / UX
  - **Ubicación:** `src/WingetUSoft/UI/CleanupWindow.xaml.cs:146-149` y `src/WingetUSoft/Localization/Localization.cs:433`
  - **Qué hacer:** `cleanup.confirmDeleteBody` dice solo «¿Eliminar {0} elemento(s) seleccionado(s)?». Para una
    operación irreversible sobre el sistema de archivos, la última pantalla antes de ejecutar no nombra ni una
    ruta. Listar hasta 10 rutas con «…y N más», igual que ya se hace en `MainWindow.xaml.cs:982-983`.
  - **Criterio de aceptación:** el diálogo muestra las rutas concretas; con más de 10 seleccionadas aparece el
    sufijo de recuento. Las 5 traducciones actualizadas.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T1-11] Cerrar la ventana TOCTOU entre verificar y ejecutar el instalador**
  - **Área:** Seguridad
  - **Ubicación:** `src/WingetUSoft/Services/GitHubUpdateService.cs:104,121-133` → `src/WingetUSoft/UI/MainWindow.xaml.cs:1931`
  - **Qué hacer:** el instalador se descarga a una ruta **fija y predecible**
    (`%TEMP%\WingetUSoft_Update.exe`), se cierra el `FileStream` (necesario desde el arreglo de la v1.4.1), se
    verifica, y `Process.Start` ocurre después sin ningún handle que impida sustituirlo entre medias. Como el
    instalador es `PrivilegesRequired=admin`, quien lo reemplace obtiene administrador a través de un UAC que
    el usuario reconoce como legítimo. Descargar a un subdirectorio recién creado con nombre aleatorio dentro
    de `%TEMP%` (elimina la ruta predecible) y mantener un handle con `FileShare.Read` desde antes de
    verificar hasta después de lanzar.
  - **Criterio de aceptación:** dos ejecuciones consecutivas usan rutas distintas; mientras la app tiene el
    instalador verificado, otro proceso no puede escribir sobre él. Los 2 tests de `DownloadInstallerAsync`
    siguen en verde.
  - **Esfuerzo:** medio
  - **Depende de:** ninguna

- [x] **[T1-18] Sacar la contraseña del `.pfx` de la línea de comandos**
  - **Área:** Seguridad / DevOps
  - **Ubicación:** `src/WingetUSoft/installer/build-installer.ps1:90-101` (parámetro en `:48`)
  - **Qué hacer:** el bloque que añade `/p $CertPassword` pone la contraseña de la clave privada de firma en
    los argumentos del proceso, legibles por cualquier usuario de la máquina mientras `signtool` corre
    (`Get-CimInstance Win32_Process`). Además el parámetro es `[string]`, así que queda en el historial de
    PowerShell. Cambiar a `[SecureString]` y firmar por `/sha1 <huella>` contra un certificado importado en
    el almacén del usuario.
  - **Criterio de aceptación:** `build-installer.ps1 -CertFile …` ya no expone la contraseña en la línea de
    comandos de ningún proceso hijo; la firma sigue funcionando end-to-end.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

### Internacionalización

- [x] **[T1-03] Localizar los botones de `WindowDialogHelper`**
  - **Área:** i18n
  - **Ubicación:** `src/WingetUSoft/UI/WindowDialogHelper.cs:14,24-25`
  - **Qué hacer:** los textos «Aceptar», «Sí» y «No» están cableados en español. Este helper lo usan
    `MainWindow`, `CleanupWindow`, `UninstallWindow` y `SearchWindow`, es decir, **prácticamente todos los
    diálogos de la app**: con la interfaz en inglés, francés o italiano el usuario ve «Aceptar / Sí / No», y
    en francés «No» ni siquiera es una palabra válida. Añadir `btn.accept`, `btn.yes` y `btn.no`
    (× 5 idiomas) y usarlas como valores por defecto.
  - **Criterio de aceptación:** con la app en francés, un diálogo de confirmación muestra «Oui / Non» y uno
    informativo «OK». `LocalizationUsageTests` y `LocalizationTests` en verde.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T1-15] Extraer las cadenas en español cableadas de `MainWindow`**
  - **Área:** i18n
  - **Ubicación:** `src/WingetUSoft/UI/MainWindow.xaml.cs:720`, `:1668`, `:1802`
  - **Qué hacer:** tres cadenas visibles se construyen en español fijo: el prefijo «Iniciando:» del registro
    de cada lote, «¡Nuevas actualizaciones disponibles!» de la auto-comprobación, y el aviso «No se pudo
    escribir el archivo de log». Pasar por `L.T`.
  - **Criterio de aceptación:** con la app en inglés, un lote completo no imprime ni una palabra en español en
    el registro.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T1-16] Extraer las cadenas en español cableadas de `AppSettings`**
  - **Área:** i18n
  - **Ubicación:** `src/WingetUSoft/Settings/AppSettings.cs:88,96,112,147`
  - **Qué hacer:** los cuatro mensajes de error de configuración están en español fijo y **se le muestran al
    usuario** vía `ShowSettingsLoadWarningIfNeeded` y `TrySaveSettings`. Ojo con el orden de arranque:
    `AppSettings.Load()` corre antes de `L.Set(...)`, así que la resolución debe hacerse en el punto de
    presentación (guardar la clave y los argumentos, no el texto ya formateado).
  - **Criterio de aceptación:** con `settings.json` corrupto y la app en italiano, el diálogo de aviso sale
    en italiano.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T1-17] Extraer las cadenas en español cableadas de `WingetService`**
  - **Área:** i18n
  - **Ubicación:** `src/WingetUSoft/Services/WingetService.cs:53,106,133,709,722,775,830,1208-1209,1252,1344`
  - **Qué hacer:** `BuildWingetCommandErrorMessage` compone «winget no pudo {acción}…» con los verbos
    «consultar actualizaciones», «listar programas instalados» y «buscar paquetes» en español fijo; hay
    además 5 mensajes de excepción que acaban en diálogos. Todos por `L.T`.
  - **Criterio de aceptación:** provocar un fallo de winget con la app en portugués produce un mensaje
    íntegramente en portugués. `WingetServiceTests.BuildWingetCommandErrorMessage_*` adaptado y en verde.
  - **Desviación al implementarlo:** las tres `ArgumentException` de `ParseElevatedWorkerOptions`
    (`:709`, `:722`, `:730`) se quedan **sin traducir**, documentado en el propio método. Corren en el
    proceso worker, que arranca directo desde `Program.Main` sin leer los ajustes —así que no hay idioma
    elegido— y saltan *antes* de que exista la tubería, de modo que su texto nunca vuelve al proceso
    principal. Solo se disparan si la propia app compone mal la invocación: son diagnóstico de protocolo.
  - **Esfuerzo:** medio
  - **Depende de:** ninguna

### Pruebas de respaldo

- [x] **[T1-13] Tests de contención de rutas para `CleanupScanner`**
  - **Área:** QA
  - **Ubicación:** `tests/WingetUSoft.Tests/CleanupScannerTests.cs`
  - **Qué hacer:** los 6 tests actuales cubren deduplicación, cancelación y que nada venga preseleccionado,
    pero ninguno comprueba que un candidato quede dentro de los directorios base. Añadir casos con nombres
    que contengan `..`, una ruta con raíz (`C:\Windows`), separadores (`a/b`) y un `Id` con travesía.
  - **Criterio de aceptación:** los tests nuevos fallan contra el código previo a T0-01 y pasan con la
    corrección.
  - **Esfuerzo:** medio
  - **Depende de:** T0-01

- [x] **[T1-14] Tests de atomicidad de `AppSettings.Save()`**
  - **Área:** QA
  - **Ubicación:** `tests/WingetUSoft.Tests/AppSettingsTests.cs`
  - **Qué hacer:** usar `AppSettings.DataDirectoryPath` (ya existe con ese propósito) para apuntar a un
    directorio temporal y verificar que un `Save()` correcto no deja `.tmp`, y que partir de un
    `settings.json` válido y forzar un fallo de escritura conserva el contenido anterior.
  - **Criterio de aceptación:** los tests nuevos fallan contra el código previo a T0-02 y pasan con la
    corrección.
  - **Esfuerzo:** bajo
  - **Depende de:** T0-02

### Documentación desfasada

- [x] **[T1-19] Poner `CONTEXT.md` §3, §4 y §6 al día (1.8.2)**
  - **Área:** Documentación
  - **Ubicación:** `CONTEXT.md:14-15`, `:92`, `:102`, `:138`, `:170`
  - **Qué hacer:** el documento que el propio proyecto designa para que «el contexto viaje entre equipos»
    (§7) va **3 releases por detrás**: dice «Versión publicada: 1.7.0», «Tier E está en `main`, pendiente de
    la 1.8.0», «Versionado: hoy 1.7.0» y lista «Publicar la 1.8.0» como pendiente. La realidad es
    `<Version>1.8.2</Version>` y `git log` → `release: v1.8.2`. Actualizar las cinco referencias y vaciar el
    pendiente ya resuelto de §6.
  - **Criterio de aceptación:** ninguna aparición de «1.7.0» en `CONTEXT.md` describe el estado actual;
    §6 no lista tareas ya completadas.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T1-20] Añadir las entradas de changelog de 1.8.0, 1.8.1 y 1.8.2**
  - **Área:** Documentación
  - **Ubicación:** `CONTEXT.md:196-210` (tabla del Registro de cambios y secciones fechadas)
  - **Qué hacer:** la última entrada es «*(sin publicar)* — Tier E». Faltan las tres versiones publicadas
    desde entonces. Reconstruir desde `git log` (el commit `b2724c4` «fix(ui): estado vacio real en Historial
    y pulido de layout» documenta la 1.8.1).
  - **Criterio de aceptación:** la tabla del registro llega hasta la 1.8.2 y cada versión tiene fecha.
  - **Esfuerzo:** bajo
  - **Depende de:** T1-19

- [x] **[T1-21] Corregir la afirmación falsa del README sobre contraste WCAG**
  - **Área:** Documentación
  - **Ubicación:** `README.md:102-103`
  - **Qué hacer:** «los colores del registro de actividad cumplen WCAG AA (4.5:1) en tema claro y oscuro,
    comprobado por tests» es cierto en 2 de las 4 ventanas con registro y falso en `CleanupWindow` y
    `UninstallWindow`. La corrección es del código (T1-04/T1-05), no del texto; esta tarea solo verifica que
    la afirmación pase a ser cierta y, si fuera necesario, la matiza.
  - **Criterio de aceptación:** la afirmación es verificable por `LogPaletteTests` + T1-06 para las 4 ventanas.
  - **Esfuerzo:** bajo
  - **Depende de:** T1-04, T1-05, T1-06

- [x] **[T1-22] Corregir el nombre del instalador en el README**
  - **Área:** Documentación
  - **Ubicación:** `README.md:29`
  - **Qué hacer:** dice `WingetUSoft-x.y.z-setup.exe`; el nombre real es `WingetUSoft-Setup-x.y.z.exe`
    (`installer/installer.iss:41`, confirmado en `installer/Output/`). Quien busque el archivo en Releases no
    lo reconoce.
  - **Criterio de aceptación:** el nombre del README coincide literalmente con `OutputBaseFilename` del `.iss`.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

---

## 🟡 Tier T2 — Mejoras sustanciales

> Rendimiento, refactorización, ampliación de cobertura, responsividad, documentación técnica y DevOps.
> El bloque de refactorización (T2-04 a T2-07) es el de mayor retorno: elimina la **causa** de que un
> arreglo se aplique a una ventana y no a las demás, que es el patrón de fallo más repetido del proyecto.

### Rendimiento

- [x] **[T2-01] Sacar el logging a archivo del hilo de UI**
  - **Área:** Rendimiento
  - **Ubicación:** `src/WingetUSoft/UI/MainWindow.xaml.cs:1784-1804`
  - **Qué hacer:** `AppendLogFile` hace `Directory.CreateDirectory` + `File.AppendAllText` (abrir / escribir /
    cerrar) **por cada línea**, síncronamente en el hilo de UI y dentro de un `lock`. Durante una
    actualización, `RunWingetStreamingAsync` retransmite todas las líneas de stdout de winget. Abrir un único
    `StreamWriter` con `AutoFlush = false` mientras dure la operación, o encolar y escribir desde una tarea
    en segundo plano.
  - **Criterio de aceptación:** un lote de 10 paquetes produce una sola apertura de archivo, no una por línea;
    el contenido del `.log` es idéntico al actual.
  - **Esfuerzo:** medio
  - **Depende de:** ninguna

- [x] **[T2-02] Retención de los logs diarios**
  - **Área:** Rendimiento / mantenimiento
  - **Ubicación:** `src/WingetUSoft/Settings/AppSettings.cs:66` y `src/WingetUSoft/UI/MainWindow.xaml.cs:1793`
  - **Qué hacer:** se escribe un `.log` por día en `%LocalAppData%\WingetUSoft\logs\`, sin límite de tamaño ni
    de antigüedad, y con `LogToFile = true` por defecto. Crecimiento indefinido. Purgar al arrancar los
    archivos con más de N días (30 como valor razonable), y documentarlo en el README junto a las rutas de
    datos de usuario.
  - **Criterio de aceptación:** al arrancar con logs de 40 días, solo quedan los de los últimos 30.
  - **Nota:** la primera implementación conservaba **31** archivos — «los últimos 30 días» incluye hoy,
    así que el corte es hace 29 días, no hace 30. Lo destapó el test al escribir el criterio tal cual.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T2-03] Investigar la exclusión del runtime de IA del paquete publicado**
  - **Área:** Rendimiento / tamaño de distribución
  - **Ubicación:** `src/WingetUSoft/WingetUSoft.csproj:12` (`WindowsAppSDKSelfContained`)
  - **Qué hacer:** medido sobre `src/WingetUSoft/publish` (142 MB → instalador de 35,8 MB): `onnxruntime.dll`
    21,5 MB + `DirectML.dll` 18,5 MB + `Microsoft.Windows.AI.*` ≈ 5 MB. La app **no referencia ni una** de
    esas APIs (verificado por grep). Son ~40 MB de peso muerto que el usuario descarga en cada actualización.
    Investigar si el SDK 1.8 expone una propiedad para excluirlos sin romper la activación COM de WinUI 3
    *unpackaged*; si no, dejar constancia y enlazar con T4-01.
  - **Criterio de aceptación:** o bien el `publish` baja de 142 MB manteniendo la app funcional (arranque
    verificado + UI tests en verde), o bien queda documentado en `CONTEXT.md` §4 por qué no se puede.
  - **Resultado:** **sí se puede.** La SDK compone su carga útil en dos targets
    (`AddMicrosoftWindowsAppSDKPayloadFiles*`), y basta un target propio detrás que retire por nombre
    `onnxruntime`, `DirectML` y `Microsoft.Windows.AI.*`. **`publish`: 142 MB → 101 MB; el instalador que descarga el usuario, 34,2 MB → 21,6 MB (−37 %).** No se usa el
    gancho «oficial» (`MicrosoftWindowsAppSDKFilesExcluded`) porque hay que rellenarlo con rutas
    derivadas de `WindowsAppSdkComponentPackages`, que la SDK define dentro de sus propios targets y no
    existe al evaluar el proyecto. Quedan 7 `*.Projection.dll` (~0,5 MB) que llegan por otra vía
    (`ReferenceCopyLocalPaths`) y no se tocan: son ensamblados gestionados del cierre de referencias.
  - **Esfuerzo:** medio
  - **Depende de:** ninguna

- [x] **[T2-21] Cachear los ids instalados en la ventana de búsqueda**
  - **Área:** Rendimiento
  - **Ubicación:** `src/WingetUSoft/UI/SearchWindow.xaml.cs:156`
  - **Qué hacer:** `GetInstalledIdsAsync` lanza un `winget list` completo (proceso externo) en **cada**
    búsqueda. Cachear el resultado mientras viva la ventana e invalidarlo solo tras una instalación con éxito.
  - **Criterio de aceptación:** tres búsquedas consecutivas ejecutan `winget list` una sola vez; la columna
    «Instalado» sigue siendo correcta tras instalar algo.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T2-23] Borrar archivos en segundo plano en la ventana de limpieza**
  - **Área:** Rendimiento
  - **Ubicación:** `src/WingetUSoft/UI/CleanupWindow.xaml.cs:168`
  - **Qué hacer:** los directorios van a `Task.Run` pero `File.Delete` se ejecuta síncronamente en el hilo de
    UI. Unificar en la rama asíncrona.
  - **Criterio de aceptación:** ninguna llamada de E/S de archivos síncrona en el bucle de borrado.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

### Refactorización estructural

- [x] **[T2-06] Unificar `LogLineKind` en una sola declaración**
  - **Área:** Arquitectura
  - **Ubicación:** `src/WingetUSoft/Core/LogPalette.cs:6` (internal, 5 valores),
    `src/WingetUSoft/UI/CleanupWindow.xaml.cs:247` (private, 4), `src/WingetUSoft/UI/UninstallWindow.xaml.cs:249` (private, 4)
  - **Qué hacer:** el enum está declarado tres veces; las copias privadas carecen de `Accent`. Es la causa
    **mecánica** de que esas dos ventanas no puedan usar `LogPalette`. Borrar las dos privadas y usar el de
    `Core`.
  - **Criterio de aceptación:** una sola definición de `LogLineKind` en todo el repositorio; compila sin
    advertencias.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T2-05] Extraer un `ActivityLog` compartido**
  - **Área:** Refactorización
  - **Ubicación:** `MainWindow.xaml.cs:1689`, `CleanupWindow.xaml.cs:255`, `UninstallWindow.xaml.cs:257`, `SearchWindow.xaml.cs:307`
  - **Qué hacer:** hay **cuatro** implementaciones de `AppendLog` con tres estrategias de color distintas y
    límites de recorte divergentes (400 / 200 / 400). Extraer un `UserControl` con `Append(text, kind)`,
    recorte, autoscroll y repintado al cambiar de tema, y que las cuatro ventanas lo consuman.
  - **Criterio de aceptación:** una sola implementación de `AppendLog`; el comportamiento visible de las
    cuatro ventanas no cambia (mismo recorte, mismo autoscroll) y T1-06 sigue en verde.
  - **Cómo quedó:** el control (`UI/ActivityLog.xaml`) se queda con pintar, recortar, autoscroll y
    repintado por tema; cada ventana conserva su `MaxLines` (400/200/200/400) porque es decisión suya.
    `MainWindow` mantiene un `AppendLog` propio de dos cosas que tampoco son del widget: deducir el tipo
    de línea por su prefijo (es la única que retransmite la salida cruda de winget) y volcar a disco.
  - **Dos divergencias se unificaron a propósito**, ambas invisibles: la ventana de búsqueda no coloreaba
    las líneas `Normal` (y `LogPalette` devuelve para `Normal` justo el color de texto por defecto), y solo
    ella hacía `UpdateLayout()` antes de mover el scroll — que es lo correcto, porque si no
    `ScrollableHeight` es todavía el de antes de añadir la línea. Ahora lo hacen las cuatro.
  - **T1-06 se reescribió, no se relajó:** guardaba «las 4 ventanas usan `LogPalette`», premisa que este
    refactor elimina. Ahora exige que el registro se pinte en **un solo sitio** y que ningún archivo de UI
    vuelva a colorear líneas por su cuenta. Verificado saboteando: reintroducir un RGB cableado en una
    ventana lo hace fallar.
  - **Esfuerzo:** medio
  - **Depende de:** T2-06, T1-04, T1-05

- [x] **[T2-04] `ParseUpgradeOutput` debe consumir `WingetTable`**
  - **Área:** Refactorización
  - **Ubicación:** `src/WingetUSoft/Services/WingetService.cs:1054-1129` frente a `src/WingetUSoft/Core/WingetTable.cs:28-83`
  - **Qué hacer:** `GetColumnStarts` está escrito dos veces, idéntico, y la búsqueda de la línea de guiones y
    el recorte por posición también. Un cambio de formato de winget (que ya ocurrió una vez) obliga a tocar
    dos sitios. Dejar en `WingetService` solo el mapeo de celdas a `WingetPackage`.
  - **Criterio de aceptación:** `GetColumnStarts` existe una sola vez; los 8 tests de `ParseUpgradeOutput`
    (inglés, español, cabeceras personalizadas, versión desconocida, nombre que empieza por dígito, línea de
    resumen, cadena vacía) siguen en verde sin modificarlos.
  - **Esfuerzo:** medio
  - **Depende de:** ninguna

- [x] **[T2-07] Extraer un `WindowChrome` compartido**
  - **Área:** Refactorización
  - **Ubicación:** `MainWindow`, `CleanupWindow`, `UninstallWindow`, `SearchWindow`, `SettingsWindow`, `HistoryWindow`
  - **Qué hacer:** icono, `ExtendsContentIntoTitleBar`, `MicaBackdrop`, el `switch` sobre `ThemeMode`,
    `WindowSizer.Apply` y la suscripción a `ActualThemeChanged` están copiados en las seis ventanas. Extraer a
    `WindowChrome.Apply(window, appWindow, hWnd, settings, designW, designH, minW, minH)`.
  - **Criterio de aceptación:** el bloque de arranque de cada ventana se reduce a una llamada; los UI tests de
    layout y snap siguen en verde.
  - **Esfuerzo:** medio
  - **Depende de:** ninguna

### Accesibilidad, i18n y responsive

- [x] **[T2-08] Asociar las etiquetas de los filtros de la ventana principal**
  - **Área:** Accesibilidad
  - **Ubicación:** `src/WingetUSoft/UI/MainWindow.xaml:258-286`
  - **Qué hacer:** «Fuente:», «Excluidos:» y «Buscar:» son `TextBlock` sueltos sin `LabeledBy`, así que
    `cmbFuente`, `btnFiltroExcluidos` y `txtBuscar` no tienen nombre accesible fiable. Asociarlos.
  - **Criterio de aceptación:** los tres controles exponen un `Name` que coincide con su etiqueta visible, en
    los 5 idiomas.
  - **Verificado (2026-08-22):** en `AccessibilityTests.MainWindowFilters_AreNamedAfterTheirVisibleLabel`,
    un `[Theory]` con los tres pares control/etiqueta contra la app real. El nombre sale de `LabeledBy`, no de
    una cadena nueva, así que sigue al idioma en los cinco sin claves de traducción adicionales y el test
    compara `control.Name` con `label.Name`. Comprobado saboteando: quitar el `LabeledBy` de `cmbFuente` hace
    fallar exactamente ese caso.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T2-09] Formato de fecha según la cultura del idioma activo**
  - **Área:** i18n
  - **Ubicación:** `src/WingetUSoft/UI/HistoryWindow.xaml.cs:22` y `:188`
  - **Qué hacer:** el historial y su exportación CSV usan `"dd/MM/yyyy HH:mm"` fijo. Un usuario con la app en
    inglés lee `03/07/2026` como 7 de marzo. Formatear con la `CultureInfo` correspondiente al idioma activo.
  - **Criterio de aceptación:** con la app en inglés el historial muestra el formato mes-primero; el CSV
    exportado usa el mismo formato que la tabla.
  - **Resultado:** el formato ya no está cableado. `L.Culture` / `L.CultureFor(lang)` mapean cada idioma a su
    cultura (`es-ES`, `en-US`, `pt-BR`, `fr-FR`, `it-IT`) y `L.FormatDateTime` compone el patrón corto de esa
    cultura. La tabla y el CSV llaman al mismo método: no pueden divergir.
  - **Desviación al implementarlo:** el patrón corto de las culturas latinas es `d/M/yyyy`, no `dd/MM/yyyy`,
    así que tomarlo tal cual habría quitado el relleno con ceros que tenía el historial y roto la alineación de
    la columna (`3/7/2026` junto a `13/12/2026`). Se toma el **orden** de la cultura y se conserva el ancho
    fijo rellenando los campos de un dígito (`L.Pad`).
  - **Verificado (2026-08-22):** `CultureFormattingTests` — con la app en inglés el 3 de julio se escribe
    `07/03/2026` y en los otros cuatro idiomas `03/07/2026`; la hora sobrevive en los cinco.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T2-10] Localizar los nombres de archivo sugeridos al exportar**
  - **Área:** i18n
  - **Ubicación:** `MainWindow.xaml.cs:1083` (`actualizaciones_`), `MainWindow.xaml.cs:1199` (`winget-paquetes_`), `HistoryWindow.xaml.cs:176` (`historial_`)
  - **Qué hacer:** los tres selectores de guardado proponen un nombre en español sea cual sea el idioma.
    Pasar el prefijo por `L.T`.
  - **Criterio de aceptación:** con la app en inglés, los tres diálogos proponen nombres en inglés.
  - **Resultado:** tres claves nuevas (`export.fileUpdates`, `export.filePackages`, `export.fileHistory`) y un
    solo componedor, `L.ExportFileName(clave)`.
  - **Desviación al implementarlo:** la **fecha** del nombre no se localiza. Sigue en `yyyy-MM-dd` invariante
    a propósito: es lo que hace que los archivos se ordenen solos, y con el formato de la cultura las barras
    de `dd/MM/yyyy` serían separadores de ruta.
  - **Verificado (2026-08-22):** `CultureFormattingTests` exige que los tres nombres, en los cinco idiomas,
    encajen en `^[a-z0-9\-]+_2026-07-03$` —sin acentos, espacios ni caracteres prohibidos en Windows— y que
    los tres prefijos sean distintos entre sí dentro de cada idioma.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T2-11] Hacer que la fila de filtros envuelva**
  - **Área:** Diseño responsivo
  - **Ubicación:** `src/WingetUSoft/UI/MainWindow.xaml:258-286`
  - **Qué hacer:** es un `StackPanel Orientation="Horizontal"` con anchos fijos (ComboBox 200 + TextBox 200)
    dentro de un `ScrollViewer` con `HorizontalScrollMode="Disabled"`; requiere ≈ 850 px y el mínimo de
    ventana son 900 DIP. A 150 % de DPI o con etiquetas FR/IT (20-30 % más largas, algo que el Tier B ya
    documentó) el cuadro de búsqueda puede quedar recortado **sin scroll con el que alcanzarlo**. Sustituir
    por el `local:WrapPanel` que ya usa la barra de acciones.
  - **Criterio de aceptación:** a 900×600 con 150 % de DPI e idioma francés, los cuatro controles son
    visibles y alcanzables. *(Hallazgo pendiente de verificación: se derivó de los anchos declarados, no de
    ejecutar la app.)*
  - **Verificado (2026-08-22):** el hallazgo era correcto y ahora está comprobado contra la app real, no
    derivado de los anchos declarados: `LayoutTests.FilterRow_RemainsVisible_WhenWindowIsNarrow`. Cada
    etiqueta viaja con su control dentro de un `StackPanel` para que el par salte de fila junto.
  - **Desviación al implementarlo (importante para futuros tests de recorte):** ni `IsOffscreen` ni comparar
    el borde derecho con el de la ventana detectan esto. Se comprobó saboteando el arreglo: un control
    recortado por el `ScrollViewer` se sigue reportando **en pantalla** y con su rectángulo pegado al borde
    del recorte —el cuadro de búsqueda pasaba de 375 px a 74 sin salirse de la ventana—. La señal que sí
    sirve es el **adelgazamiento**: el test mide cada control con la ventana ancha y exige que conserve su
    ancho con la ventana en su mínimo. Además es independiente del DPI del monitor.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

### QA y DevOps

- [x] **[T2-15] Tests del protocolo del worker elevado**
  - **Área:** QA
  - **Ubicación:** `src/WingetUSoft/Services/WingetService.cs:472-598` y `:736-832`
  - **Qué hacer:** es la ruta más compleja del código (~200 líneas: named pipe, JSON por línea, autenticación
    por token, canal acotado de progreso, evento de cancelación) y **no tiene ni una prueba**. Extraer el
    serializado/parseo de mensajes a algo testeable y cubrir: `hello` con token válido e inválido, mensajes
    JSON malformados que deben ignorarse, `result` y `summary`, y cancelación a mitad de lote.
  - **Criterio de aceptación:** un token incorrecto produce `UpgradeBatchResult` sin autenticar; una línea de
    JSON corrupta no aborta la lectura del resto.
  - **Resultado:** el bucle del protocolo se separó de la tubería —ahora toma un `TextReader`— y se cubre
    con 15 tests en `ElevatedWorkerProtocolTests`, con la conversación entera en un `StringReader`: no hace
    falta elevación ni named pipe para ejercitarlo.
  - **Desviación al implementarlo:** un token incorrecto **ya no lanza**. Antes tiraba una
    `InvalidOperationException` que acababa cayendo en el `catch (Exception)` genérico del llamador y se
    reportaba como error de lectura de sesión. Ahora devuelve directamente un `UpgradeBatchResult` sin
    autenticar con el motivo (`winget.elevatedAuthFailed`), que es literalmente lo que pedía el criterio y
    no depende de un catch-all para dar un mensaje correcto.
  - **Verificado (2026-08-22):** los dos criterios, saboteando. Quitar la comparación del token hace fallar
    `Hello_WithTheWrongToken_...`; hacer que la línea corrupta relance en vez de ignorarse hace fallar los
    5 casos de `ACorruptLine_IsSkippedAndTheRestIsStillRead` —y **solo** esos 6—. También se cubre que sin
    `hello` previo no se procesa nada, que un lote cancelado a mitad conserva los resultados ya recibidos y
    marca el corte, y que un `progress` sin tamaño total no se retransmite (sería un porcentaje inventado).
  - **Esfuerzo:** alto
  - **Depende de:** ninguna

- [x] **[T2-12] Script de verificación local `verify.ps1` + hook de pre-push**
  - **Área:** DevOps (local)
  - **Ubicación:** `verify.ps1` (raíz, nuevo) y `.githooks/pre-push` (nuevo)
  - **Qué hacer:** hoy la verificación completa solo ocurre al cortar una versión con `release.ps1`, o si
    alguien se acuerda de lanzar `dotnet test` a mano; entre release y release un test roto puede vivir en
    `main` sin que nada lo señale. **La solución NO es CI** —decisión cerrada y reafirmada el 2026-08-20:
    nada de workflows, GitHub Actions ni runners hospedados—, sino bajar esa misma verificación al equipo
    de desarrollo. Crear `verify.ps1` que encadene `dotnet build` (con las advertencias tratadas como
    error), `dotnet test` de `WingetUSoft.Tests`, `dotnet format --verify-no-changes` (tras T3-09) y el
    chequeo de dependencias vulnerables (T2-13); con un flag `-Full` que añada además los UI tests de
    FlaUI, que son justamente los que un runner hospedado nunca podría correr y aquí sí. Registrarlo con
    `git config core.hooksPath .githooks` y un `pre-push` que invoque la variante rápida y aborte el push
    si falla. `release.ps1` debe pasar a llamar a `verify.ps1` en vez de duplicar sus pasos: una sola
    definición de «esto está verificado».
  - **Criterio de aceptación:** `.\verify.ps1` termina en verde sobre el `main` actual y devuelve código
    de salida distinto de 0 si se rompe un test, el build emite advertencias o hay una dependencia
    vulnerable; un `git push` con un test roto se aborta; `release.ps1` ya no repite los pasos que
    `verify.ps1` cubre.
  - **Resultado:** `verify.ps1` en la raíz, y `.githooks/pre-push` que lo invoca. Encadena compilación con
    `-warnaserror`, unitarias y el chequeo de dependencias (T2-13); con `-Full` añade los UI tests, que son
    justo lo que un runner hospedado nunca podría correr. `release.ps1` ya no repite ningún paso: llama a
    `verify.ps1` y solo decide **cuánta** verificación exige (`-Full`, o `-SkipTests` al delegar).
  - **Verificado (2026-08-22):** `.erify.ps1` termina en verde sobre el `main` actual; con un test roto a
    propósito devuelve 1 y el hook aborta el push (`sh .githooks/pre-push` → código 1). Registrado con
    `git config core.hooksPath .githooks`, que hay que ejecutar **una vez por clon** (está en la cabecera
    del hook).
  - **Desviación al implementarlo:** `dotnet format --verify-no-changes` **no** se añadió todavía —no hay
    `.editorconfig` contra el que formatear hasta T3-09—; queda anotado en la cabecera de `verify.ps1`.
    Y se añadió un `.gitattributes` con `.githooks/* text eol=lf`: con CRLF, `sh` rechaza el hook.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T2-13] Chequeo local de dependencias vulnerables y desactualizadas**
  - **Área:** Seguridad / DevOps (local)
  - **Ubicación:** `verify.ps1` (T2-12) y `release.ps1`
  - **Qué hacer:** hoy hay **0 vulnerabilidades** (verificado 2026-08-20), pero es una foto de un instante y
    nada la vuelve a tomar. Sin Dependabot ni alertas del repositorio —quedan fuera por la decisión de no
    usar servicios de CI—, el chequeo tiene que formar parte del flujo local: añadir
    `dotnet list package --vulnerable --include-transitive` como paso de `verify.ps1`, que aborte si
    encuentra algo, y `dotnet list package --outdated` como aviso informativo (sin abortar). `release.ps1`
    lo hereda al delegar en `verify.ps1`.
  - **Criterio de aceptación:** `.\verify.ps1` informa del resultado de ambos chequeos; una dependencia
    vulnerable simulada hace fallar la verificación y, por tanto, aborta el release.
  - **Resultado:** dos pasos en `verify.ps1`. El de vulnerables aborta; el de desactualizados solo informa
    (tiene un `-SkipOutdated` porque consulta NuGet y es el paso lento; el hook de pre-push lo usa).
  - **Desviación al implementarlo:** `dotnet list package --vulnerable` devuelve **0 aunque encuentre
    algo**, así que no vale con mirar el código de salida: se buscan las líneas de paquete afectado
    (`^\s*>\s`) en la salida.
  - **Verificado (2026-08-22):** con una dependencia vulnerable simulada (`System.Net.Http` 4.3.0,
    GHSA-7jgj-8wvc-jh57) la verificación falla y, con ella, el release. Saltó por partida doble: el
    `-warnaserror` del build ya la convierte en el error NU1903, y el paso dedicado la detecta también
    (1 coincidencia). Hoy sigue habiendo **0 vulnerabilidades**; 7 referencias con versión más nueva.
  - **Esfuerzo:** bajo
  - **Depende de:** T2-12

- [x] **[T2-14] `Directory.Build.props` y versiones de paquetes alineadas**
  - **Área:** DevOps
  - **Ubicación:** raíz del repositorio y los 3 `.csproj`
  - **Qué hacer:** los dos proyectos de test divergen (`Microsoft.NET.Test.Sdk` 17.12.0 frente a 17.14.1,
    `xunit.runner.visualstudio` 2.8.2 frente a 3.1.4). Centralizar `TargetFramework`, `Nullable`,
    `ImplicitUsings` y las versiones de paquetes.
  - **Criterio de aceptación:** una sola versión declarada por paquete en todo el repositorio; los 162 tests
    siguen en verde.
  - **Resultado:** `Directory.Build.props` (TargetFramework, TargetPlatformMinVersion, Nullable,
    ImplicitUsings) y `Directory.Packages.props` con gestión centralizada de versiones: los `.csproj`
    declaran **qué** paquetes usan y el `.props`, con qué versión. Los dos que divergían quedan en la más
    alta que ya usaba alguno (`Microsoft.NET.Test.Sdk` 17.14.1, `xunit.runner.visualstudio` 3.1.4).
  - **Desviación al implementarlo:** los UI tests apuntaban a `net10.0-windows10.0.19041.0` y pasan a
    22621 como el resto. Era divergencia, no decisión: la app ya exige 22621 y su
    `TargetPlatformMinVersion` sigue siendo 19041.
  - **Verificado (2026-08-22):** una sola declaración por paquete en todo el repositorio; 266 unitarios y
    36 UI tests en verde (el criterio decía 162, escrito cuando la suite era más pequeña).
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T2-17] Registrar en las notas del release qué se omitió**
  - **Área:** DevOps
  - **Ubicación:** `release.ps1:136-161` y la generación de notas en `:164-196`
  - **Qué hacer:** `-SkipTests`, `-SkipUiTests` y `-AllowDirty` avisan por consola pero no dejan rastro
    permanente: meses después no hay forma de saber qué versión salió sin verificar. Añadir una línea a las
    notas del release cuando se use alguno.
  - **Criterio de aceptación:** un release con `-SkipUiTests` incluye la advertencia en sus notas de GitHub.
  - **Resultado:** los tres flags dejan rastro en las notas publicadas, en un bloque de cita al final. Nunca
    se modifica el archivo que pase el usuario con `-NotesFile`: se compone una copia temporal.
  - **Verificado (2026-08-22):** `-DryRun` imprime ahora las notas que se publicarían (útil por sí mismo), y
    con `-SkipTests -AllowDirty` aparecen los dos avisos bajo «⚠️ Verificación incompleta en este release».
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T2-18] Limpiar `GH_TOKEN` del entorno tras el release**
  - **Área:** Seguridad / DevOps
  - **Ubicación:** `release.ps1:282-291`
  - **Qué hacer:** el PAT se toma de la credencial cacheada de git y se pone en `$env:GH_TOKEN`, donde
    persiste el resto de la sesión de PowerShell y lo hereda **todo** proceso hijo posterior. Limpiarlo en un
    `finally`.
  - **Criterio de aceptación:** al terminar `release.ps1`, `$env:GH_TOKEN` está vacío.
  - **Resultado:** el `finally` limpia `GH_TOKEN` del entorno del proceso.
  - **Desviación al implementarlo:** solo se limpia **si lo puso este script**. Un `GH_TOKEN` que ya
    estuviera en el entorno es del usuario, lo puso a propósito para `gh`, y borrárselo le rompería la
    sesión; el problema que la tarea describe es el PAT que el script saca de la credencial cacheada de git
    y deja heredable por todo proceso hijo posterior, y ese sí desaparece siempre.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

### Código

- [x] **[T2-19] Reutilizar el temporizador de rebote de la búsqueda**
  - **Área:** Código
  - **Ubicación:** `src/WingetUSoft/UI/MainWindow.xaml.cs:1374-1381`
  - **Qué hacer:** se crea un `DispatcherTimer` nuevo y se suscribe una lambda nueva **en cada pulsación de
    tecla**. Usar una única instancia y limitarse a `Stop()` / `Start()`.
  - **Criterio de aceptación:** un solo `DispatcherTimer` por instancia de ventana; el rebote de 300 ms sigue
    funcionando igual.
  - **Resultado:** el temporizador se crea una vez por ventana; cada pulsación solo hace `Stop()`/`Start()`.
    Antes quedaban vivos tantos suscriptores de `Tick` como teclas se hubieran escrito.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T2-20] Corregir la fuga de `CancellationTokenSource` en la búsqueda reentrante**
  - **Área:** Código
  - **Ubicación:** `src/WingetUSoft/UI/SearchWindow.xaml.cs:255`
  - **Qué hacer:** tras instalar con éxito se llama a `SearchAsync()`, que reemplaza `_cts` y lo pone a `null`
    en su propio `finally`; el `finally` externo ya no encuentra el CTS de la instalación para liberarlo.
    Guardar la referencia local antes de la llamada reentrante, o mover la re-búsqueda fuera del `try`.
  - **Criterio de aceptación:** todo `CancellationTokenSource` creado en la ventana se libera exactamente una
    vez.
  - **Resultado:** cada operación guarda su `CancellationTokenSource` en una variable local y libera **esa**,
    y el campo `_cts` solo se borra si sigue apuntando al mismo (`ReferenceEquals`), para no dejar sin token
    a una operación posterior. Además la re-búsqueda tras instalar sale del `try`: llamarla dentro era lo que
    hacía que `_cts` ya fuera otro cuando el `finally` iba a liberarlo.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T2-22] Leer o no redirigir el `stderr` de `where.exe`**
  - **Área:** Código
  - **Ubicación:** `src/WingetUSoft/Services/WingetService.cs:1267-1285`
  - **Qué hacer:** el proceso se lanza con `RedirectStandardError = true` pero solo se lee `StandardOutput`
    antes de `WaitForExit()`: es el patrón clásico de interbloqueo si stderr llenara el búfer de la tubería.
    Poner `RedirectStandardError = false` o leer ambos flujos.
  - **Criterio de aceptación:** no queda ningún flujo redirigido sin consumir antes de `WaitForExit`.
  - **Resultado:** `RedirectStandardError = false`. Se descartó leer ambos flujos porque `where.exe` no escribe
    en stderr nada que la app use: heredar el del proceso padre es más simple y quita el riesgo de raíz.
  - **Verificado (2026-08-22):** revisados los otros dos sitios que redirigen stderr (`CreateWingetStartInfo`);
    ambos lo consumen —uno con una lectura asíncrona del `BaseStream`, otro con `ErrorDataReceived`— antes de
    `WaitForExit`. No queda ningún flujo redirigido sin consumir.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

### Documentación

- [x] **[T2-16] Documentar la limpieza de residuos en el README**
  - **Área:** Documentación
  - **Ubicación:** `README.md:78-79` (sección «Desinstalación»)
  - **Qué hacer:** la sección no menciona que tras desinstalar se abre automáticamente una ventana que propone
    **borrar carpetas de forma recursiva e irreversible**. Es la operación más destructiva del producto y solo
    aparece en el árbol de estructura del repositorio. Explicar qué busca, que nada viene preseleccionado y
    que la eliminación no se puede deshacer.
  - **Criterio de aceptación:** la sección describe la ventana de limpieza; idealmente con la captura
    `docs/screenshots/` correspondiente si se regenera.
  - **Resultado:** la sección «Desinstalación» explica ahora **qué busca** la ventana (rutas concretas
    derivadas del nombre y el Id, en seis directorios conocidos; no rastrea el disco), que **nada viene
    marcado** y que cerrarla no borra nada, y que el borrado es **recursivo, sin papelera e irreversible**.
  - **Desviación al implementarlo:** sin captura nueva. Regenerar `docs/screenshots/` es un cambio aparte y
    el texto se sostiene solo.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

---

## 🟢 Tier T3 — Pulido y mantenimiento

> Todo esfuerzo bajo y sin dependencias. Apto para rellenar huecos entre tareas mayores.

- [x] **[T3-01] Eliminar `InverseBoolConverter` (código muerto)**
  - **Área:** Refactorización · **Ubicación:** `src/WingetUSoft/UI/Converters.cs:25-32`
  - **Qué hacer:** declarado y no referenciado en ningún `.xaml` ni `.cs` (verificado por grep). Borrarlo.
  - **Resultado:** borrado. Confirmado antes de tocarlo que no lo referencia ningún `.xaml` ni `.cs`, ni
    en `src/` ni en `tests/`.
  - **Criterio de aceptación:** compila sin errores y ningún XAML lo referencia. · **Esfuerzo:** bajo · **Depende de:** ninguna

- [x] **[T3-02] Quitar el separador de menú duplicado**
  - **Área:** UI · **Ubicación:** `src/WingetUSoft/UI/MainWindow.xaml:176-177`
  - **Qué hacer:** dos `<MenuFlyoutSeparator />` consecutivos dibujan una doble línea en el menú Herramientas.
  - **Resultado:** uno de los dos `MenuFlyoutSeparator` fuera.
  - **Criterio de aceptación:** el menú muestra un único separador en esa posición. · **Esfuerzo:** bajo · **Depende de:** ninguna

- [x] **[T3-03] Actualizar los valores de versión por defecto obsoletos**
  - **Área:** Mantenimiento · **Ubicación:** `src/WingetUSoft/installer/installer.iss:11` (`"1.2.0"`), `src/WingetUSoft/UI/MainWindow.xaml.cs:243` (`"v1.1.0"`)
  - **Qué hacer:** son reservas que nunca se actualizaron; en el `.iss` puede producir un instalador mal
    versionado si alguien invoca `ISCC` sin `/DMyAppVersion`.
  - **Resultado:** la reserva del `.iss` sube a `1.8.6` y se le añade un comentario que dice para qué
    está: `build-installer.ps1` **siempre** pasa `/DMyAppVersion`, así que la fuente real de la versión
    sigue siendo `<Version>` del `.csproj` y esto es solo la red por si alguien invoca `ISCC` a mano.
  - **Desviación al implementarlo:** la de `MainWindow.xaml.cs` **no** se actualiza a la versión de hoy,
    se sustituye por cadena vacía. Solo entraría en juego si el ensamblado no declarase versión —cosa que
    no pasa, la estampa el `.csproj`— y entonces un número cableado únicamente puede mentir: es lo que ya
    había ocurrido, se había quedado en «v1.1.0» con la app por la 1.8. El campo ya nace en `""` y el
    único consumidor lo salta con `IsNullOrEmpty`, así que la cadena vacía es el valor natural de «no se
    sabe» y actualizarla a mano no vuelve a hacer falta nunca.
  - **Criterio de aceptación:** ninguna reserva de versión anterior a la actual. · **Esfuerzo:** bajo · **Depende de:** ninguna

- [x] **[T3-04] Sustituir los escapes `í` de un comentario XML por caracteres reales**
  - **Área:** Redacción · **Ubicación:** `src/WingetUSoft/UI/MainWindow.xaml.cs:1675-1679`
  - **Qué hacer:** los escapes no se interpretan dentro de un comentario: se leen literalmente en el editor y
    en la documentación generada («línea», «índice», «podría»).
  - **Verificado (2026-08-22): ya no aplica.** El comentario con los escapes era el del `AppendLog` de
    `MainWindow`, y T2-05 lo reescribió entero al mover el registro a `UI/ActivityLog`. Comprobado que no
    queda ni una entidad XML (`&#...;`) en ningún `.cs` de `src/` ni de `tests/`.
  - **Criterio de aceptación:** el comentario se lee correctamente en el editor. · **Esfuerzo:** bajo · **Depende de:** ninguna

- [x] **[T3-05] Restaurar los acentos en los comentarios sin tildar**
  - **Área:** Redacción · **Ubicación:** `src/WingetUSoft/Services/WingetShowLabels.cs`, `src/WingetUSoft/installer/installer.iss`, partes de `MainWindow.xaml.cs`
  - **Qué hacer:** escriben «desinstalacion», «asi», «proposito», «version» mientras el resto del proyecto
    acentúa correctamente. Verificar antes que el archivo se guarda con la codificación adecuada (el propio
    `CONTEXT.md` §4 documenta el problema de BOM en PowerShell 5.1).
  - **Resultado:** acentos restaurados en `WingetShowLabels.cs` (cabecera y los 33 comentarios de idioma),
    en 13 comentarios de `installer.iss` y en cuatro sueltos de `WindowSizing`, `HistoryWindow` y
    `MainWindow`. **Solo comentarios**: ninguna cadena que llegue a la UI o al instalador.
  - **Detalle que evitó romper la alineación:** los comentarios de idioma de `WingetShowLabels` están
    alineados en columna, y todos los reemplazos conservan la misma longitud en caracteres
    (ingles→inglés, aleman→alemán, espanol→español, frances→francés, japones→japonés,
    portugues→portugués), así que la columna sobrevive intacta.
  - **Sobre la codificación, que era el riesgo señalado:** `installer.iss` ya llevaba acentos UTF-8 **sin
    BOM** (`MyAppPublisher = "Ricky Angel Jiménez Bueno"`) y sigue igual. No se cambió porque se comprobó
    contra el artefacto real: el `CompanyName` del instalador de la v1.8.6 lee «Jiménez» correctamente.
  - **Verificado (2026-08-22):** el `.iss` compila —build de prueba con `-Version 0.0.0`, instalador
    generado y su `CompanyName` con la tilde intacta; el artefacto de prueba se borró después—.
  - **Criterio de aceptación:** ortografía uniforme; el `.iss` sigue compilando y los `.ps1` ejecutándose. · **Esfuerzo:** bajo · **Depende de:** ninguna

- [x] **[T3-06] Fijar una convención de idioma para los comentarios**
  - **Área:** Redacción · **Ubicación:** `CONTEXT.md` §4 y los archivos afectados (`WingetService.cs:24,29,946-948`, `CleanupScanner.cs:3-7,48,88,96`, `MainWindow.xaml.cs:1708,1716`)
  - **Qué hacer:** la mayoría de comentarios están en español pero hay bloques en inglés, sin convención
    declarada. Decidir una, anotarla en §4 y alinear lo existente.
  - **Convención adoptada:** comentarios y documentación XML **en español**, con acentos y signos de
    apertura; en inglés solo los identificadores, las cadenas de la API de Windows y los términos
    técnicos que no se traducen (*named pipe*, *snap layout*, *hash*). Escrita en `CONTEXT.md` §4, que es
    lo que pedía el criterio.
  - **Resultado:** alineados los bloques que quedaban en inglés — la cabecera y los cuatro comentarios de
    sección de `CleanupScanner`, la nota de verificación Authenticode de `GitHubUpdateService`, las dos
    de los `Regex` de progreso, la del lector de stdout carácter a carácter y el resumen de
    `IsWingetInstalled` en `WingetService`.
  - **Criterio de aceptación:** la convención está escrita en `CONTEXT.md` §4. · **Esfuerzo:** bajo · **Depende de:** ninguna

- [x] **[T3-07] Quitar el nombre accesible en español cableado en XAML**
  - **Área:** i18n · **Ubicación:** `src/WingetUSoft/UI/SearchWindow.xaml:67`
  - **Qué hacer:** `AutomationProperties.Name="Buscar en el catálogo de winget"` se sobrescribe en tiempo de
    ejecución desde `SearchWindow.xaml.cs:106`. Funciona, pero es una cadena duplicada que se desincronizará.
  - **Resultado:** fuera el `AutomationProperties.Name` del XAML; queda la asignación de
    `ApplyLocalizedStrings`, que es la que sigue al idioma.
  - **Verificado (2026-08-22):** `SearchWindowTests.SearchBox_HasAnAccessibleName`, comprobado saboteando.
  - **Y el sabotaje corrigió el propio test, que es lo interesante:** la primera versión solo exigía «el
    nombre no está vacío» y **pasaba igual con el arreglo quitado**. Sin nombre propio, WinUI le deduce
    uno del `PlaceholderText` y el control reporta «Nombre o Id...» — que no es una etiqueta, es un
    ejemplo de qué escribir; un lector de pantalla anunciaría el ejemplo como si fuera el nombre del
    campo. El test fija ahora la cadena exacta. Vale la pena recordarlo para cualquier otro `TextBox`:
    comprobar que el nombre accesible «existe» no demuestra nada.
  - **Criterio de aceptación:** el nombre accesible existe en una sola fuente. · **Esfuerzo:** bajo · **Depende de:** ninguna

- [x] **[T3-08] Alinear `ShowUpdateNotification` con lo que promete su ajuste**
  - **Área:** UX · **Ubicación:** `src/WingetUSoft/UI/MainWindow.xaml.cs:2139-2148`
  - **Qué hacer:** el método está regido por `_settings.ShowNotifications` («Mostrar notificaciones al
    completar actualizaciones») pero solo escribe en `txtEstado`; el aviso real (sonido + parpadeo) lo hace
    `Notifier` con su propio umbral de 10 s. O se renombra el método a lo que hace, o el ajuste deja de
    regirlo. El ajuste promete más de lo que entrega.
  - **Resultado:** de las dos salidas que ofrecía el criterio se toman **las dos**. El método pasa a
    llamarse `ShowBatchResultInStatusBar`, que es lo que hace, **y** deja de regirlo el ajuste.
  - **Y al mirarlo de cerca había algo peor que un nombre desafortunado:** como el ajuste sí lo regía,
    apagar «Mostrar notificaciones» borraba también el **resumen del lote en la barra de estado**. Eso no
    es una notificación: es el resultado de lo que el usuario acaba de pedir, y quien apaga los avisos no
    está pidiendo que se le oculte qué pasó. El ajuste se queda con `Notifier` —sonido y parpadeo de la
    barra de tareas, con su umbral de 10 s—, que es exactamente lo que su texto promete.
  - **Criterio de aceptación:** el nombre del método y el texto del ajuste describen el comportamiento real. · **Esfuerzo:** bajo · **Depende de:** ninguna

- [x] **[T3-09] Añadir `.editorconfig`**
  - **Área:** Consistencia de estilo · **Ubicación:** raíz del repositorio
  - **Qué hacer:** el estilo es consistente por disciplina del autor, no por herramienta. Codificar las
    convenciones ya en uso y la severidad de los analizadores de .NET.
  - **Resultado:** `.editorconfig` en la raíz. No inventa estilo: codifica el que ya estaba en uso
    (4 espacios, CRLF, llaves en línea propia, `namespace` con ámbito de archivo, sin `this.`, campos
    privados con `_`) y añade las reglas de encoding que este proyecto ya pagó caras — BOM obligatorio en
    los `.ps1` y LF en `.githooks/`.
  - **`verify.ps1` comprueba ahora el estilo**, que era el motivo de la tarea. Dos categorías, `style` y
    `analyzers`; el paso solo comprueba, nunca reescribe.
  - **Desviación al implementarlo, y es la parte importante:** la categoría **`whitespace` queda fuera a
    propósito**, así que el criterio literal (`dotnet format --verify-no-changes` a secas) **no** se
    cumple. Con la configuración puesta, `style` y `analyzers` pasan limpias, pero `whitespace` produce
    **290 avisos en 14 archivos** y todos son lo mismo: quiere colapsar la **alineación en columnas** que
    el repositorio usa deliberadamente —las constantes de `Notifier`, los campos de los structs de
    interop, los `[GeneratedRegex]` de `ReleaseNotes` y, 211 de los 290, el diccionario de traducciones
    de `Localization.cs`—. No hay opción de `.editorconfig` que permita esa alineación: o se acepta el
    aviso o se destruye el estilo del autor en un diff de 290 líneas que no arregla nada. Se eligió
    conservarlo, y queda anotado en la cabecera de `verify.ps1` y en `CONTEXT.md` §4.
  - **Verificado (2026-08-22):** `verify.ps1` en verde, y comprobado saboteando: un `this._campo` hace
    que el paso falle con `IDE0003` y código de salida 2.
  - **Criterio de aceptación:** `dotnet format --verify-no-changes` pasa sobre el código actual. · **Esfuerzo:** bajo · **Depende de:** ninguna

- [x] **[T3-10] Uniformar el uso de `ConfigureAwait` en `Services`**
  - **Área:** Código · **Ubicación:** `GitHubUpdateService.cs` (11 usos), `WingetService.cs` (1), `CleanupScanner.cs` (0)
  - **Qué hacer:** decidir una política para la capa de servicios (`ConfigureAwait(false)` es lo correcto:
    no necesitan el contexto de UI) y aplicarla; anotarla en `CONTEXT.md` §4.
  - **Política adoptada:** `ConfigureAwait(false)` en toda la capa `Services`; en `UI`, nunca. Escrita en
    `CONTEXT.md` §4.
  - **Resultado:** aplicada a los 47 awaits de la capa (1/1 en `CleanupScanner`, 13/13 en
    `GitHubUpdateService`, 33/33 en `WingetService`). Antes la seguían 12 de 47.
  - **No la aplicó un `sed`, sino el analizador:** se activa **CA2007** como advertencia en un
    `.editorconfig` propio de `src/WingetUSoft/Services/` y `dotnet format analyzers` hace el cambio.
    Así la política deja de depender de la memoria de quien escribe: `verify.ps1` compila con
    `-warnaserror`, de modo que un await nuevo sin configurar en esa carpeta **rompe el build**. Y el
    ámbito es la carpeta, no el repositorio, porque en `UI` lo correcto es justo lo contrario.
  - **Desviación al implementarlo:** tres `await using` con tipo explícito quedan **sin** configurar, con
    `#pragma` y motivo al lado. El corrector automático de CA2007 los rompió —`.ConfigureAwait(false)`
    devuelve un `ConfiguredAsyncDisposable`, que no es un `Stream`, y no compilaba— y configurarlos de
    verdad obliga a partir cada declaración en la variable tipada más un descartable suelto: más ruido
    que beneficio, cuando la continuación que llega hasta ahí ya viene de awaits configurados.
  - **Verificado (2026-08-22):** saboteando — quitar un `ConfigureAwait` de `CleanupScanner` hace que el
    build falle con `error CA2007`.
  - **Criterio de aceptación:** política escrita y aplicada de forma uniforme en los 4 archivos de `Services`. · **Esfuerzo:** bajo · **Depende de:** ninguna

- [x] **[T3-11] No invocar un `async void` como si fuera un método**
  - **Área:** Código · **Ubicación:** `src/WingetUSoft/UI/MainWindow.xaml.cs:1979`
  - **Qué hacer:** `LnkDescargarUpdate_Click(sender, e)` se llama directamente desde
    `MenuBuscarActualizacion_Click`: no se puede esperar y sus excepciones no se pueden capturar. Extraer el
    cuerpo a un `Task DownloadAndInstallUpdateAsync()` que ambos consuman.
  - **Resultado:** el cuerpo pasa a `DownloadAndInstallUpdateAsync()`, un `Task` que consumen los dos
    caminos: el botón de la InfoBar y la confirmación de «Buscar actualización».
  - **Lo que estaba mal de verdad:** al llamar al `async void` como método, la confirmación seguía su
    camino sin esperar a la descarga y el `finally` que rehabilita el menú se ejecutaba **con la descarga
    todavía en marcha**. Y cualquier excepción de la descarga se escapaba del `try` del llamador: en un
    `async void` no hay tarea que la capture, va directa al manejador de excepciones no observadas.
  - **Criterio de aceptación:** ningún `async void` se invoca como método en el código. · **Esfuerzo:** bajo · **Depende de:** ninguna

- [x] **[T3-12] Dar salida a los `Trace` o retirarlos**
  - **Área:** Observabilidad · **Ubicación:** `src/WingetUSoft/Settings/AppSettings.cs:113,127,147`
  - **Qué hacer:** `Trace.TraceError` / `TraceWarning` se emiten sin ningún listener configurado, así que en
    Release no van a ninguna parte. Redirigirlos al log de archivo o eliminarlos.
  - **Resultado:** los cinco `Trace` pasan a `CrashLog.WriteDiagnostic(origen, mensaje)`, que anota con
    fecha en `crash.log`, el archivo que ya se recorta por tamaño y que un usuario adjuntaría a un informe.
  - **Por qué ahí y no en el log de archivo:** el `FileLog` lo crea `MainWindow`, y `AppSettings.Load()`
    corre **antes** de que exista — el mismo problema de orden que obligó a inventar `DeferredMessage`.
    `CrashLog` es estático y no depende de la UI, así que funciona desde el primer instante.
  - **Comparten archivo a propósito:** el valor de estas anotaciones está en el orden. Saber que la purga
    de logs venía fallando desde antes del error que sí se notó vale más que tener dos archivos que haya
    que cruzar a mano; van marcadas con `[diagnóstico]` para distinguirlas de un fallo no controlado.
  - **Criterio de aceptación:** todo diagnóstico emitido termina en un destino observable. · **Esfuerzo:** bajo · **Depende de:** ninguna

- [ ] **[T3-13] Aislar `CleanupScannerTests` del perfil real del usuario**
  - **Área:** QA · **Ubicación:** `tests/WingetUSoft.Tests/CleanupScannerTests.cs:32,77,107`
  - **Qué hacer:** crean directorios reales en `%LOCALAPPDATA%`. Están protegidos con `try/finally`, pero un
    proceso de test muerto deja basura en el perfil. Redirigir con `AppSettings.DataDirectoryPath` o
    parametrizar los directorios base del escáner.
  - **Criterio de aceptación:** la suite no escribe fuera de un directorio temporal. · **Esfuerzo:** bajo · **Depende de:** ninguna

- [x] **[T3-14] Pasar el `themeMode` real en `HistoryWindow`**
  - **Área:** Código · **Ubicación:** `src/WingetUSoft/UI/HistoryWindow.xaml.cs:68`
  - **Qué hacer:** `UpdateTitleBarButtonColors` pasa siempre `themeModeFallback: 0` aunque el constructor
    recibe `themeMode` y no lo conserva. Sin efecto visible hoy, pero es una reserva incorrecta.
  - **Resultado:** la ventana guarda el `themeMode` que recibe y se lo pasa a `UpdateButtonColors`. Las
    otras cinco ventanas ya pasaban el suyo; esta era la única con un `0` literal.
  - **Sobre el «sin efecto visible hoy»:** el fallback solo se usa mientras `Content` todavía no es un
    `FrameworkElement` con tema resuelto, y ahí `0` significa «seguir al sistema». Con el tema **forzado**
    a oscuro en un Windows claro, esa ventana habría pintado los botones de la barra de título en claro.
  - **Criterio de aceptación:** la ventana guarda y usa el `themeMode` recibido. · **Esfuerzo:** bajo · **Depende de:** ninguna

- [x] **[T3-15] Corregir la descripción del historial en el README**
  - **Área:** Documentación · **Ubicación:** `README.md:87`
  - **Qué hacer:** dice «registra cada actualización»; desde el Tier E también registra instalaciones
    (`SearchWindow.xaml.cs:242-250`).
  - **Resultado:** la línea menciona ahora las instalaciones además de las actualizaciones.
  - **Criterio de aceptación:** la descripción menciona ambos tipos de entrada. · **Esfuerzo:** bajo · **Depende de:** ninguna

- [x] **[T3-16] Validar el nombre del evento de cancelación del worker elevado**
  - **Área:** Seguridad · **Ubicación:** `src/WingetUSoft/Services/WingetService.cs:497`
  - **Qué hacer:** `EventWaitHandle.OpenExisting(options.CancelEventName)` acepta cualquier nombre recibido
    por argumento. No es explotable por sí solo (quien controle los argumentos ya ejecuta código como el
    usuario), pero exigir el prefijo `Local\WingetUSoft.Cancel.` cuesta una línea.
  - **Resultado:** el prefijo `Local\WingetUSoft.Cancel.` pasa a ser una constante, se usa al **crear** el
    evento y se exige al **recibirlo**.
  - **Desviación al implementarlo, y mejora el resultado:** la comprobación no se queda donde estaba el
    `OpenExisting`, sino que baja a `ParseElevatedWorkerOptions`, que es donde vive el resto de la
    validación de argumentos. Eso permitió hacer el parser `internal` y **probarlo**: es la única frontera
    entre una línea de comandos y un proceso que va a correr como administrador, y no tenía ni un test.
  - **Verificado (2026-08-22):** `ElevatedWorkerOptionsTests` — 11 casos: una invocación válida, cinco
    nombres de evento fuera del prefijo (otro espacio de nombres, otra app, otro sufijo, sin `Local\`, y
    el prefijo en minúsculas), cuatro argumentos requeridos ausentes y un argumento desconocido.
    Comprobado saboteando: neutralizar la comprobación hace fallar los cinco casos del prefijo y solo esos.
  - **Criterio de aceptación:** un nombre fuera de ese prefijo hace fallar el arranque del worker. · **Esfuerzo:** bajo · **Depende de:** ninguna

- [ ] **[T3-17] Limpiar los artefactos de compilación del árbol de trabajo**
  - **Área:** Mantenimiento · **Ubicación:** `publish/`, `src/WingetUSoft/publish/`, `src/WingetUSoft/installer/Output/`, `build.binlog`
  - **Qué hacer:** ~700 MB entre dos copias del `publish` (142 MB cada una), nueve instaladores históricos y
    un binlog. **Todo está correctamente ignorado por git** (verificado contra `git ls-files`), así que es
    solo higiene local: ralentiza búsquedas e indexado. Conservar el instalador de la última versión.
  - **Criterio de aceptación:** el árbol de trabajo baja de forma apreciable y `git status` sigue limpio. · **Esfuerzo:** bajo · **Depende de:** ninguna

---

## 🔵 Tier T4 — Futuro / Opcional

> **Explícitamente fuera del alcance inmediato.** Son propuestas de evolución: no se abordan sin una
> decisión deliberada. Dos de ellas (T4-05 y T4-06) rozan decisiones ya cerradas en la Parte I y se
> anotan aquí solo para que la auditoría no deje huecos, no para reabrirlas.

- [ ] **[T4-01] Evaluar la migración a Windows App SDK 2.x**
  - **Área:** Arquitectura / stack
  - **Ubicación:** `src/WingetUSoft/WingetUSoft.csproj:26`
  - **Qué hacer:** hoy `1.8.260317003`; `dotnet list package --outdated` reporta **2.4.0** disponible
    (2026-08-20). Es un salto mayor con riesgo real en una app *unpackaged* (activación COM, `.xbf`/`.pri`,
    el `Target CopyXamlResourcesToPublish` que existe como workaround). El incentivo principal es que
    reorganiza el empaquetado *self-contained* y podría resolver T2-03 (~40 MB de runtime de IA no usado).
  - **Criterio de aceptación:** una rama de prueba que compile, arranque y pase los UI tests, con el tamaño
    del `publish` medido antes y después; o una entrada en `CONTEXT.md` §4 explicando por qué se descarta.
  - **Esfuerzo:** alto
  - **Depende de:** T2-03

- [ ] **[T4-02] Dividir `MainWindow.xaml.cs`**
  - **Área:** Arquitectura
  - **Ubicación:** `src/WingetUSoft/UI/MainWindow.xaml.cs` (2 151 líneas, 28 campos de instancia)
  - **Qué hacer:** la clase acumula ~10 responsabilidades: menú contextual, sizing, tema, localización,
    orquestación de lotes, exportación CSV, exportación/importación winget, auto-actualización de la app,
    temporizador de auto-comprobación, registro e icono de bandeja. Además `AppSettings.Load()` corre en un
    inicializador de campo (`:109`), es decir, E/S de disco antes del constructor. Extraer por partes,
    empezando por lo más autónomo: `AppUpdateController`, `PackageImportExport`, `AutoCheckScheduler`. No
    hace falta MVVM completo para ganar mucho.
  - **Criterio de aceptación:** ningún archivo de `UI/` supera las 800 líneas; los UI tests siguen en verde.
  - **Esfuerzo:** alto
  - **Depende de:** T2-05, T2-07

- [ ] **[T4-03] Interfaces para los servicios estáticos**
  - **Área:** Arquitectura / testabilidad
  - **Ubicación:** `Services/WingetService.cs`, `Services/GitHubUpdateService.cs`, `Services/CleanupScanner.cs`
  - **Qué hacer:** las tres son clases estáticas, así que no hay nada que sustituir en un test: la lógica de
    orquestación de lotes —el corazón del producto— solo se puede probar arrancando la app con FlaUI. Hay
    además estado global mutable (`L.Current`, `AppSettings.DataDirectoryPath`,
    `WingetService._cachedWingetExecutablePath` con su `ResetCache()` que existe únicamente para los tests).
    Extraer `IWingetService` como mínimo, e inyectarlo.
  - **Criterio de aceptación:** existe un test unitario del flujo de actualización por lotes con un doble de
    `IWingetService`, sin lanzar procesos.
  - **Esfuerzo:** alto
  - **Depende de:** T4-02

- [ ] **[T4-04] Medición de cobertura de tests**
  - **Área:** QA
  - **Ubicación:** `tests/WingetUSoft.Tests/WingetUSoft.Tests.csproj`
  - **Qué hacer:** no hay `coverlet` ni ninguna configuración de cobertura. La auditoría **no midió
    cobertura**: los huecos se identificaron inspeccionando qué clases tienen archivo de test y cuáles no.
    Añadir `coverlet.collector` y generar el informe **en local** desde `verify.ps1 -Full` (T2-12),
    idealmente con `reportgenerator` para obtener un HTML navegable en una carpeta ignorada por git. Sin
    servicio externo al que subirlo: la cifra base se anota a mano en la tabla de Progreso de este archivo.
  - **Criterio de aceptación:** `.\verify.ps1 -Full` produce un informe de cobertura local; la cifra base
    queda registrada en la sección de Progreso.
  - **Esfuerzo:** bajo
  - **Depende de:** T2-12

- [ ] **[T4-05] Añadir `SECURITY.md`, `CONTRIBUTING.md` y `CHANGELOG.md`**
  - **Área:** Documentación
  - **Ubicación:** raíz del repositorio
  - **Qué hacer:** el proyecto es de autor único, lo que justifica no tener guía de contribución, pero el
    README ya invita a compilar desde el código. El más relevante de los tres es `SECURITY.md`: una app que
    descarga y **ejecuta instaladores con privilegios de administrador** debería ofrecer un canal para
    reportar vulnerabilidades. El `CHANGELOG.md` es discutible: hoy ese papel lo cumplen las notas de GitHub
    Releases y el Registro de cambios de `CONTEXT.md`, así que puede bastar con enlazarlos.
  - **Criterio de aceptación:** existe al menos `SECURITY.md` con un contacto y una política de divulgación.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [ ] **[T4-06] Documentar la reversión de una actualización fallida**
  - **Área:** DevOps / documentación
  - **Ubicación:** `README.md` (sección «Actualizaciones de la propia app»)
  - **Qué hacer:** no hay rollback: si una actualización deja la app inservible, el usuario tiene que buscar
    y descargar el instalador anterior en Releases, y el README no lo explica. Como el instalador reutiliza
    `AppId` y directorio (`UsePreviousAppDir=yes`), instalar una versión anterior encima funciona; conviene
    decirlo. Aprovechar para revisar las dependencias con versión superior disponible
    (`H.NotifyIcon.WinUI` 2.2.0 → 2.4.1, `Microsoft.NET.Test.Sdk` → 18.9.0,
    `xunit.runner.visualstudio` → 4.0.0), que quedan fuera de T2-14 por ser saltos mayores.
  - **Criterio de aceptación:** el README explica cómo volver a una versión anterior; las actualizaciones de
    paquetes están evaluadas y decididas (aplicadas o descartadas con motivo).
  - **Esfuerzo:** bajo
  - **Depende de:** T2-14

---

## 📊 Progreso

> Marcar aquí al **verificar** (build + tests + prueba real), no al escribir el código — misma regla que
> `CONTEXT.md` §7. Al cerrar una tarea: marcar su casilla arriba, añadir la fila aquí y referenciar el ID
> en el mensaje del commit (p. ej. `fix(cleanup): validar contención de rutas (T0-01)`).

### Estado por Tier

| Tier | Total | Completadas | Pendientes | % |
|---|---:|---:|---:|---:|
| T0 — Crítico | 2 | **2** | 0 | **100 %** |
| T1 — Alta | 22 | 11 | 11 | 50 % |
| T2 — Sustancial | 23 | 1 | 22 | 4 % |
| T3 — Pulido | 17 | 0 | 17 | 0 % |
| T4 — Futuro | 6 | 0 | 6 | 0 % |
| **Total** | **70** | **14** | **56** | **20 %** |

### Registro de tareas completadas

| Fecha | ID | Tarea | Verificado con | Commit | Versión |
|---|---|---|---|---|---|
| 2026-08-20 | T0-01 | Contención de rutas en `CleanupScanner` | 173/173 unitarios · 5 tests nuevos fallan al revertir la corrección | `7c7ef5c` | 1.8.3 |
| 2026-08-20 | T0-02 | Escritura atómica de `settings.json` | 173/173 unitarios · `File.Replace` + limpieza de `.tmp` | `7c7ef5c` | 1.8.3 |
| 2026-08-20 | T1-13 | Tests de contención de rutas | 8 casos nuevos en `CleanupScannerTests` | `7c7ef5c` | 1.8.3 |
| 2026-08-20 | T1-14 | Tests de atomicidad de `Save()` | 3 casos nuevos en `AppSettingsTests` | `7c7ef5c` | 1.8.3 |
| 2026-08-20 | T2-06 | `LogLineKind` unificado en una sola declaración | build limpio; las 2 copias privadas eliminadas | `191ec8a` | 1.8.3 |
| 2026-08-20 | T1-04 | `CleanupWindow` usa `LogPalette` | 174/174 · el guard test falla al revertir | `191ec8a` | 1.8.3 |
| 2026-08-20 | T1-05 | `UninstallWindow` usa `LogPalette` | 174/174 · el guard test falla al revertir | `191ec8a` | 1.8.3 |
| 2026-08-20 | T1-06 | Guard test anti-RGB cableado en registros | `EveryWindowWithAnActivityLog_TakesItsColorsFromLogPalette` | `191ec8a` | 1.8.3 |
| 2026-08-20 | T1-21 | Afirmación WCAG del README ya cierta y acotada | `README.md:102-105` nombra las 4 ventanas | `191ec8a` | 1.8.3 |
| 2026-08-21 | T1-03 | Botones de diálogo localizados (`btn.accept/yes/no`) | 196/196 · el guard test falla al recablear los literales | `146c044` | 1.8.3 |
| 2026-08-21 | T1-01 | Coincidencia por palabra completa en la clasificación de fallos | 196/196 · 5 tests nuevos fallan al volver a `Contains` | `146c044` | 1.8.3 |
| 2026-08-21 | T1-22 | Nombre real del instalador en el README | `README.md:28` coincide con `OutputBaseFilename` del `.iss` | *(en el corte)* | 1.8.3 |
| 2026-08-21 | T1-19 | `CONTEXT.md` §1, §3, §5 y §6 al día | ninguna referencia a 1.7.0 describe el estado actual | *(en el corte)* | 1.8.3 |
| 2026-08-21 | T1-20 | Changelog de 1.8.0, 1.8.1, 1.8.2 y 1.8.3 | la tabla llega hasta la 1.8.3, cada versión con fecha | *(en el corte)* | 1.8.3 |

### Línea base de la auditoría (2026-08-20)

Para poder comparar al cerrar tareas:

| Métrica | Valor | Cómo se obtuvo |
|---|---|---|
| Tests unitarios | 162 / 162 al auditar · **196 / 196 tras T0 + accesibilidad + i18n** | `dotnet test tests/WingetUSoft.Tests/…` |
| Tests de UI | 16 métodos (13 `[Fact]` + 3 `[Theory]`) | conteo estático — **no ejecutados** en la auditoría |
| Dependencias vulnerables | 0 | `dotnet list package --vulnerable --include-transitive` |
| Paquetes con versión superior | 6 | `dotnet list package --outdated` |
| Claves de localización | 363 × 5 idiomas al auditar · **366 tras T1-03** | conteo sobre `Localization.cs` |
| LOC C# (src + tests) | 10 360 | `wc -l` excluyendo `bin`/`obj`/`publish` |
| Tamaño del `publish` | 142 MB | `du -sh src/WingetUSoft/publish` |
| Tamaño del instalador | 35,8 MB | `WingetUSoft-Setup-1.8.2.exe` (línea base; la 1.8.3 se corta desde aquí) |
| Contraste del registro (tema oscuro) | ~~2,74:1 éxito · 2,71:1 error en 2 de 4 ventanas~~ → **≥ 4,5:1 en las 4** (T1-04/05) | fórmula WCAG 2.x; ahora medido por `LogPaletteTests` |
| `AutomationProperties.LiveSetting` | 0 usos | `grep -rn` sobre `src/` |
| `AutomationProperties.LabeledBy` | 0 usos | `grep -rn` sobre `src/` |
| TODO / FIXME / HACK en el código | 0 | `grep -rn` sobre `src/`, `tests/`, `tools/` |

### Zonas que la auditoría no pudo cubrir

Pendientes de verificación con acceso a un entorno adecuado; no invalidan ningún hallazgo, pero acotan
hasta dónde llega la evidencia:

- **UI tests (FlaUI):** no ejecutados — requieren sesión de escritorio interactiva y desatendida.
- **Rendimiento medido:** sin *profiler*. Arranque, memoria y tiempo hasta la primera fila sin medir.
- **Responsividad real (T2-11):** derivada de los anchos declarados en XAML, no de ejecutar la app.
- ~~**Explotabilidad de T0-01:**~~ **resuelto el 2026-08-20.** Ya no es hipótesis: el test
  `ScanAsync_NameEscapesTheBaseDirectory_ProducesNoCandidate` demuestra que, sin la corrección, un
  paquete llamado `C:\Windows` hacía que el escáner ofreciera **`C:\Windows`** como residuo
  eliminable. Lo que sigue sin comprobarse es el eslabón previo: que un instalador real escriba un
  `DisplayName` así en Agregar o quitar programas.
- **Lector de pantalla real (T1-07, T1-08, T1-09):** análisis del árbol de automatización por código, sin
  sesión de escucha con Narrador o NVDA.
- **Contraste fuera del registro:** solo se midieron `LogPalette` y los RGB cableados; el resto sale de
  `ThemeResource` de Windows, que se asume conforme.
- **Instalador end-to-end:** no ejecutado en una VM limpia sin .NET ni VC++ Redist.
- **Comportamiento en ARM64:** el instalador solo distribuye `win-x64` aunque el `.csproj` declare
  `win-arm64`; no verificado en hardware ARM.
- **Configuración del repositorio en GitHub:** *topics* y ramas protegidas no son accesibles desde el
  árbol de archivos. (Las alertas de seguridad del repositorio quedan fuera de alcance por decisión de
  proyecto: la vigilancia de dependencias es local, ver T2-13.)
