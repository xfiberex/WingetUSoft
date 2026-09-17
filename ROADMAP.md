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
> 📐 **Este archivo tiene tres partes.** La **Parte I** (abajo) es el histórico de características por
> Tiers A–E: qué se construyó y dónde vive. La **[Parte II](#parte-ii--plan-de-acción-de-auditoría-t0t4)**
> es el plan de acción ejecutable salido de la auditoría técnica del **2026-08-20**, organizado en
> Tiers T0–T4 con tareas marcables. La **[Parte III](#parte-iii--tier-f-auditoría-fluent-y-uiux)** es el
> **Tier F**, en curso: la auditoría Fluent y UI/UX del **2026-09-14**, verificada en la app real, con
> tareas `F-xx`. Las tres numeraciones son independientes y no se solapan.

## Estado

| Tier | Tema | Estado | Versión |
|---|---|---|---|
| **A** | Paridad de infraestructura con FormatDiskPro | ✅ Completado | 1.3.0 |
| **B** | Layout adaptable, accesibilidad y UI tests | ✅ Completado | 1.4.0 / 1.4.1 |
| **C** | Auditoría de UI/UX (flujo, datos, color, accesibilidad) | ✅ Completado | 1.5.0 / 1.6.0 |
| **D** | Cara pública (licencia in-app, README, capturas) | ✅ Completado | 1.7.0 |
| **E** | Gestión completa de software ⚠️ *cambio de alcance* | ✅ Completado | 1.8.0 |
| **F** | Auditoría Fluent y UI/UX verificada en la app real ([Parte III](#parte-iii--tier-f-auditoría-fluent-y-uiux)) | 🚧 En curso (17 de 26) | — |

El único tier abierto es el **F**, con sus tareas en la Parte III. Las decisiones ya tomadas (y lo que se
descartó a propósito) están al final de la Parte I.

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
- **CI con GitHub Actions — reabierta y adoptada (2026-09-16), con alcance acotado.** Se descartó el
  2026-07-12 y se reafirmó el 2026-08-20 porque un runner hospedado no puede correr los UI tests y
  `release.ps1` ya cubría el corte. Con el repositorio público cambia la cuenta: los pull requests de
  terceros y de Dependabot necesitan una señal que no dependa del equipo del mantenedor. Lo que hay:
  - `.github/workflows/ci.yml`: el mismo `verify.ps1` sin `-Full` (build con `-warnaserror`, estilo,
    unitarios y dependencias vulnerables) en cada push a `main` y en cada PR. No duplica lógica: si
    cambia el script, cambia el workflow.
  - `.github/workflows/codeql.yml`: CodeQL para C# (`security-extended`), en push, en PR y cada semana. La
    primera ejecución, con `security-and-quality`, dio 374 alertas de calidad sin ninguna de seguridad (142
    en código generado); las 5 «error» de formato resultaron falsos positivos, ahora cubiertos por
    `LocalizationTests.EveryTranslation_UsesTheSamePlaceholdersAsSpanish`.
  - `.github/dependabot.yml`: NuGet y Actions, semanal; ignora las subidas mayores de WindowsAppSDK
    (T4-01 descartó la 2.x). Con NuGet apenas sirve: resuelve en Linux, da por incompatibles los paquetes
    para `net10.0-windows` y no abrió ningún PR pese a haber versiones nuevas. Por eso el CI ejecuta
    `verify.ps1` sin `-SkipOutdated` y deja el listado de desactualizados en su log.
  - Ajustes del repositorio: avisos y actualizaciones de seguridad de Dependabot, secret scanning con
    protección de push, y reporte privado de vulnerabilidades, el canal que ya indicaba `SECURITY.md`.

  **Lo que no cambia:** los UI tests siguen siendo solo locales (`verify.ps1 -Full`) y obligatorios antes
  de cada release, y `release.ps1` sigue cortando y publicando desde el equipo del mantenedor. Un CI en
  verde **no** da por verificada una tarea que toque la interfaz.
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

**Progreso (2026-08-23): 70 de 70 — plan de auditoría completo.** ✅ T0, T1, T2, T3 y T4.
T4-01 se cierra **descartando** la migración a Windows App SDK 2.x, con el spike hecho y medido
(ver `CONTEXT.md` §4): el criterio de aceptación contemplaba esa salida.

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
  - **Verificado (2026-08-22):** `.\verify.ps1` termina en verde sobre el `main` actual; con un test roto a
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
  - **Añadido al cortar la v1.8.7:** `release.ps1` actualiza también la reserva del `.iss` al subir la
    versión. Sin eso quedaba vieja en el mismo momento de publicar —exactamente el defecto que esta
    tarea venía a corregir— y solo se arreglaba si alguien se acordaba. Con el mismo cuidado de
    codificación que el `.csproj`: el `.iss` es UTF-8 sin BOM y lleva acentos.
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

- [x] **[T3-13] Aislar `CleanupScannerTests` del perfil real del usuario**
  - **Área:** QA · **Ubicación:** `tests/WingetUSoft.Tests/CleanupScannerTests.cs:32,77,107`
  - **Qué hacer:** crean directorios reales en `%LOCALAPPDATA%`. Están protegidos con `try/finally`, pero un
    proceso de test muerto deja basura en el perfil. Redirigir con `AppSettings.DataDirectoryPath` o
    parametrizar los directorios base del escáner.
  - **Resultado:** de las dos salidas del criterio se toma la segunda: `AppSettings.DataDirectoryPath` no
    servía —el escáner no lo usa—, así que se parametrizan sus directorios base. Nuevo
    `CleanupBaseDirectories`, un `record struct` con los seis; `ScanAsync` público sigue igual y delega en
    una sobrecarga `internal` que los recibe.
  - **Van en un tipo y no en una lista** porque los dos barridos usan subconjuntos distintos: el de un
    nivel mira los seis, y el de dos (`{base}\{editor}\{app}`) deja fuera `%LocalAppData%\Programs`, donde
    nadie anida por editor. Con una lista plana habría que haber cambiado ese comportamiento.
  - **Los tests se reescribieron enteros:** cada instancia crea su propio temporal con las seis
    subcarpetas y lo borra en `Dispose`, que xUnit ejecuta aunque el test falle — antes el `try/finally`
    no cubría un proceso muerto o un `Ctrl+C`.
  - **Y de paso ganaron dos casos que faltaban:** el barrido de dos niveles no estaba cubierto por
    ninguno, y ahora hay un test que exige que **ningún** resultado caiga fuera de los directorios base.
    Ese último es justo lo que hace que la suite pueda correr sin tocar el perfil.
  - **Verificado (2026-08-22):** repasada la suite entera — no queda un solo `Environment.SpecialFolder`
    en `tests/`; todo lo que se escribe cuelga de `Path.GetTempPath()`.
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

- [x] **[T3-17] Limpiar los artefactos de compilación del árbol de trabajo**
  - **Área:** Mantenimiento · **Ubicación:** `publish/`, `src/WingetUSoft/publish/`, `src/WingetUSoft/installer/Output/`, `build.binlog`
  - **Qué hacer:** ~700 MB entre dos copias del `publish` (142 MB cada una), nueve instaladores históricos y
    un binlog. **Todo está correctamente ignorado por git** (verificado contra `git ls-files`), así que es
    solo higiene local: ralentiza búsquedas e indexado. Conservar el instalador de la última versión.
  - **Resultado:** el árbol baja de **2,2 GB a 1,8 GB**. Fuera las dos copias de `publish` (142 MB y
    101 MB) y los cuatro instaladores históricos con sus checksums (1.4.0, 1.4.1, 1.8.4 y 1.8.5, unos
    130 MB). `build.binlog` ya no existía.
  - **Se conserva el instalador de la versión publicada** (1.8.6) con su `.sha256`. Los anteriores siguen
    descargables desde sus releases de GitHub.
  - **No se tocan `bin/` ni `obj/`** (202 MB + 313 MB) aunque son el grueso de lo que queda: son la salida
    de compilación viva —de ahí sale el `.exe` que conducen los UI tests— y borrarlos solo obliga a una
    recompilación completa. La tarea nombraba `publish`, los instaladores y el binlog.
  - **Verificado (2026-08-22):** comprobado antes de borrar que `git ls-files` no devuelve **nada** dentro
    de esas rutas, y después que `git status` sigue igual que antes.
  - **Criterio de aceptación:** el árbol de trabajo baja de forma apreciable y `git status` sigue limpio. · **Esfuerzo:** bajo · **Depende de:** ninguna

---

## 🔵 Tier T4 — Futuro / Opcional

> **Explícitamente fuera del alcance inmediato.** Son propuestas de evolución: no se abordan sin una
> decisión deliberada. Dos de ellas (T4-05 y T4-06) rozan decisiones ya cerradas en la Parte I y se
> anotan aquí solo para que la auditoría no deje huecos, no para reabrirlas.

- [x] **[T4-01] Evaluar la migración a Windows App SDK 2.x**
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

- [x] **[T4-02] Dividir `MainWindow.xaml.cs`**
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

- [x] **[T4-03] Interfaces para los servicios estáticos**
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

- [x] **[T4-04] Medición de cobertura de tests**
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

- [x] **[T4-05] Añadir `SECURITY.md`, `CONTRIBUTING.md` y `CHANGELOG.md`**
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

- [x] **[T4-06] Documentar la reversión de una actualización fallida**
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
| T1 — Alta | 22 | **22** | 0 | **100 %** |
| T2 — Sustancial | 23 | **23** | 0 | **100 %** |
| T3 — Pulido | 17 | **17** | 0 | **100 %** |
| T4 — Futuro | 6 | **6** | 0 | **100 %** |
| **Total** | **70** | **70** | **0** | **100 %** |

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

> Las filas de la **1.8.4 en adelante** se reconstruyeron el 2026-08-23 desde el historial de git: se
> cerraron en otro equipo y la tabla se quedó atrás. Los IDs salen de los mensajes de commit y los
> títulos, de este mismo documento. La columna *Verificado con* recoge el corte que las publicó porque
> es lo que consta: `release.ps1` ejecuta unitarios **y** UI tests y aborta si alguno falla, así que
> ninguna salió sin pasar por ahí. Las notas por tarea no se registraron en su momento y no se inventan.

| 2026-08-21 | T1-07 | Región activa (`LiveSetting`) en la barra de estado y el registro | corte v1.8.4 (unitarios + UI tests) | `37f0b9c` | 1.8.4 |
| 2026-08-21 | T1-08 | Nombre accesible para los `ToggleSwitch` de Configuración | corte v1.8.4 (unitarios + UI tests) | `37f0b9c` | 1.8.4 |
| 2026-08-21 | T1-09 | Nombre accesible en las filas y casillas de `CleanupWindow` | corte v1.8.4 (unitarios + UI tests) | `37f0b9c` | 1.8.4 |
| 2026-08-21 | T1-10 | Listar las rutas en la confirmación de borrado | corte v1.8.4 (unitarios + UI tests) | `37f0b9c` | 1.8.4 |
| 2026-08-21 | T1-15 | Extraer las cadenas en español cableadas de `MainWindow` | corte v1.8.4 (unitarios + UI tests) | `ac9ed8d` | 1.8.4 |
| 2026-08-21 | T1-16 | Extraer las cadenas en español cableadas de `AppSettings` | corte v1.8.4 (unitarios + UI tests) | `ac9ed8d` | 1.8.4 |
| 2026-08-21 | T1-17 | Extraer las cadenas en español cableadas de `WingetService` | corte v1.8.4 (unitarios + UI tests) | `ac9ed8d` | 1.8.4 |
| 2026-08-21 | T1-02 | Clasificar los fallos por código de salida, no por texto traducido | corte v1.8.4 (unitarios + UI tests) | `bc90958` | 1.8.4 |
| 2026-08-21 | T1-11 | Cerrar la ventana TOCTOU entre verificar y ejecutar el instalador | corte v1.8.4 (unitarios + UI tests) | `bc90958` | 1.8.4 |
| 2026-08-21 | T1-12 | Dejar de tragar en silencio todas las excepciones no controladas | corte v1.8.4 (unitarios + UI tests) | `bc90958` | 1.8.4 |
| 2026-08-21 | T1-18 | Sacar la contraseña del `.pfx` de la línea de comandos | corte v1.8.4 (unitarios + UI tests) | `bc90958` | 1.8.4 |
| 2026-08-21 | T2-01 | Sacar el logging a archivo del hilo de UI | corte v1.8.5 (unitarios + UI tests) | `c4a6e0c` | 1.8.5 |
| 2026-08-21 | T2-02 | Retención de los logs diarios | corte v1.8.5 (unitarios + UI tests) | `c4a6e0c` | 1.8.5 |
| 2026-08-21 | T2-03 | Investigar la exclusión del runtime de IA del paquete publicado | corte v1.8.5 (unitarios + UI tests) | `c4a6e0c` | 1.8.5 |
| 2026-08-21 | T2-21 | Cachear los ids instalados en la ventana de búsqueda | corte v1.8.5 (unitarios + UI tests) | `c4a6e0c` | 1.8.5 |
| 2026-08-21 | T2-23 | Borrar archivos en segundo plano en la ventana de limpieza | corte v1.8.5 (unitarios + UI tests) | `c4a6e0c` | 1.8.5 |
| 2026-08-22 | T2-04 | `ParseUpgradeOutput` debe consumir `WingetTable` | corte v1.8.6 (unitarios + UI tests) | `5486aff` | 1.8.6 |
| 2026-08-22 | T2-05 | Extraer un `ActivityLog` compartido | corte v1.8.6 (unitarios + UI tests) | `5486aff` | 1.8.6 |
| 2026-08-22 | T2-07 | Extraer un `WindowChrome` compartido | corte v1.8.6 (unitarios + UI tests) | `5486aff` | 1.8.6 |
| 2026-08-22 | T2-08 | Asociar las etiquetas de los filtros de la ventana principal | corte v1.8.6 (unitarios + UI tests) | `7bc5fdd` | 1.8.6 |
| 2026-08-22 | T2-09 | Formato de fecha según la cultura del idioma activo | corte v1.8.6 (unitarios + UI tests) | `7bc5fdd` | 1.8.6 |
| 2026-08-22 | T2-10 | Localizar los nombres de archivo sugeridos al exportar | corte v1.8.6 (unitarios + UI tests) | `7bc5fdd` | 1.8.6 |
| 2026-08-22 | T2-11 | Hacer que la fila de filtros envuelva | corte v1.8.6 (unitarios + UI tests) | `7bc5fdd` | 1.8.6 |
| 2026-08-22 | T2-19 | Reutilizar el temporizador de rebote de la búsqueda | corte v1.8.6 (unitarios + UI tests) | `7bc5fdd` | 1.8.6 |
| 2026-08-22 | T2-20 | Corregir la fuga de `CancellationTokenSource` en la búsqueda reentrante | corte v1.8.6 (unitarios + UI tests) | `7bc5fdd` | 1.8.6 |
| 2026-08-22 | T2-22 | Leer o no redirigir el `stderr` de `where.exe` | corte v1.8.6 (unitarios + UI tests) | `7bc5fdd` | 1.8.6 |
| 2026-08-22 | T2-12 | Script de verificación local `verify.ps1` + hook de pre-push | corte v1.8.6 (unitarios + UI tests) | `2f5338b` | 1.8.6 |
| 2026-08-22 | T2-13 | Chequeo local de dependencias vulnerables y desactualizadas | corte v1.8.6 (unitarios + UI tests) | `2f5338b` | 1.8.6 |
| 2026-08-22 | T2-14 | `Directory.Build.props` y versiones de paquetes alineadas | corte v1.8.6 (unitarios + UI tests) | `2f5338b` | 1.8.6 |
| 2026-08-22 | T2-15 | Tests del protocolo del worker elevado | corte v1.8.6 (unitarios + UI tests) | `2f5338b` | 1.8.6 |
| 2026-08-22 | T2-16 | Documentar la limpieza de residuos en el README | corte v1.8.6 (unitarios + UI tests) | `2f5338b` | 1.8.6 |
| 2026-08-22 | T2-17 | Registrar en las notas del release qué se omitió | corte v1.8.6 (unitarios + UI tests) | `2f5338b` | 1.8.6 |
| 2026-08-22 | T2-18 | Limpiar `GH_TOKEN` del entorno tras el release | corte v1.8.6 (unitarios + UI tests) | `2f5338b` | 1.8.6 |
| 2026-08-22 | T3-04 | Sustituir los escapes `í` de un comentario XML por caracteres reales | corte v1.8.7 (unitarios + UI tests) | `adb0727` | 1.8.7 |
| 2026-08-22 | T3-05 | Restaurar los acentos en los comentarios sin tildar | corte v1.8.7 (unitarios + UI tests) | `adb0727` | 1.8.7 |
| 2026-08-22 | T3-06 | Fijar una convención de idioma para los comentarios | corte v1.8.7 (unitarios + UI tests) | `adb0727` | 1.8.7 |
| 2026-08-22 | T3-09 | Añadir `.editorconfig` | corte v1.8.7 (unitarios + UI tests) | `adb0727` | 1.8.7 |
| 2026-08-22 | T3-01 | Eliminar `InverseBoolConverter` (código muerto) | corte v1.8.7 (unitarios + UI tests) | `b7d46ed` | 1.8.7 |
| 2026-08-22 | T3-02 | Quitar el separador de menú duplicado | corte v1.8.7 (unitarios + UI tests) | `b7d46ed` | 1.8.7 |
| 2026-08-22 | T3-07 | Quitar el nombre accesible en español cableado en XAML | corte v1.8.7 (unitarios + UI tests) | `b7d46ed` | 1.8.7 |
| 2026-08-22 | T3-14 | Pasar el `themeMode` real en `HistoryWindow` | corte v1.8.7 (unitarios + UI tests) | `b7d46ed` | 1.8.7 |
| 2026-08-22 | T3-15 | Corregir la descripción del historial en el README | corte v1.8.7 (unitarios + UI tests) | `b7d46ed` | 1.8.7 |
| 2026-08-22 | T3-03 | Actualizar los valores de versión por defecto obsoletos | corte v1.8.7 (unitarios + UI tests) | `b7d46ed` + `1cae138` | 1.8.7 |
| 2026-08-22 | T3-08 | Alinear `ShowUpdateNotification` con lo que promete su ajuste | corte v1.8.7 (unitarios + UI tests) | `ae3e5e6` | 1.8.7 |
| 2026-08-22 | T3-10 | Uniformar el uso de `ConfigureAwait` en `Services` | corte v1.8.7 (unitarios + UI tests) | `ae3e5e6` | 1.8.7 |
| 2026-08-22 | T3-11 | No invocar un `async void` como si fuera un método | corte v1.8.7 (unitarios + UI tests) | `ae3e5e6` | 1.8.7 |
| 2026-08-22 | T3-12 | Dar salida a los `Trace` o retirarlos | corte v1.8.7 (unitarios + UI tests) | `ae3e5e6` | 1.8.7 |
| 2026-08-22 | T3-16 | Validar el nombre del evento de cancelación del worker elevado | corte v1.8.7 (unitarios + UI tests) | `ae3e5e6` | 1.8.7 |
| 2026-08-22 | T3-13 | Aislar `CleanupScannerTests` del perfil real del usuario | corte v1.8.7 (unitarios + UI tests) | `319034f` | 1.8.7 |
| 2026-08-22 | T3-17 | Limpiar los artefactos de compilación del árbol de trabajo | corte v1.8.7 (unitarios + UI tests) | `319034f` | 1.8.7 |
| 2026-08-23 | T4-04 | Cobertura local desde `verify.ps1 -Full` | informe HTML en `coverage/report`; base **25,2 %** | `bb097c1` | *(en el corte)* |
| 2026-08-23 | T4-05 | `SECURITY.md`, `CONTRIBUTING.md` y `CHANGELOG.md` | los tres existen y el README enlaza los dos primeros | `bb097c1` | *(en el corte)* |
| 2026-08-23 | T4-06 | Vuelta atrás documentada y dependencias mayores al día | README §Actualizaciones; 5 paquetes subidos, 292/292 + 37/37 | `bb097c1` | *(en el corte)* |
| 2026-08-23 | T4-01 | Migración a Windows App SDK 2.x evaluada y **descartada** | spike real: compila y pasa los UI tests, pero el `publish` sube de 101 a 150 MB | *(en el corte)* | *(en el corte)* |
| 2026-08-23 | T4-02 | `MainWindow.xaml.cs` dividido | ningún archivo de `UI/` pasa de **745** líneas (era 2 115); 37/37 UI tests | *(en el corte)* | *(en el corte)* |
| 2026-08-23 | T4-03 | `IWingetService` + `UpgradeBatchRunner` probables | 14 tests del flujo de lotes con un doble, sin lanzar procesos | *(en el corte)* | *(en el corte)* |

### Línea base de la auditoría (2026-08-20)

Para poder comparar al cerrar tareas:

| Métrica | Valor | Cómo se obtuvo |
|---|---|---|
| Tests unitarios | 162 / 162 al auditar · **292 / 292 con el plan completo** | `dotnet test tests/WingetUSoft.Tests/…` |
| Tests de UI | 27 / 27 en la 1.8.3 · **37 / 37 hoy** (sin cambios con T4-02: el refactor no tocó comportamiento) | `dotnet test tests/WingetUSoft.UiTests/…` (el conteo estático de 16 de la auditoría eran métodos, no casos) |
| Dependencias vulnerables | 0 | `dotnet list package --vulnerable --include-transitive` |
| Paquetes con versión superior | 6 | `dotnet list package --outdated` |
| Claves de localización | 363 × 5 idiomas al auditar · **388 hoy** (T1-03, T1-15 a T1-17, T2-10) | conteo sobre `Localization.cs` |
| LOC C# (src + tests) | 10 360 al auditar · **12 959 hoy** | `wc -l` excluyendo `bin`/`obj`/`publish` |
| Tamaño del `publish` | 142 MB al auditar · **101 MB hoy** (T2-03) | `du -sh src/WingetUSoft/publish` |
| Tamaño del instalador | 35,8 MB en la 1.8.2 · **−37 % en la 1.8.5** (T2-03, fuera el runtime de IA sin usar) | `WingetUSoft-Setup-*.exe` en `installer/Output/` |
| Contraste del registro (tema oscuro) | ~~2,74:1 éxito · 2,71:1 error en 2 de 4 ventanas~~ → **≥ 4,5:1 en las 4** (T1-04/05) | fórmula WCAG 2.x; ahora medido por `LogPaletteTests` |
| `AutomationProperties.LiveSetting` | 0 usos | `grep -rn` sobre `src/` |
| `AutomationProperties.LabeledBy` | 0 usos | `grep -rn` sobre `src/` |
| TODO / FIXME / HACK en el código | 0 | `grep -rn` sobre `src/`, `tests/`, `tools/` |
| Cobertura de tests | no medida en la auditoría · **25,2 % de líneas** (T4-04) | `verify.ps1 -Full` → `coverage/report` |
| Archivo de UI más largo | 2 115 líneas (`MainWindow.xaml.cs`) · **745 hoy** (T4-02) | `wc -l src/WingetUSoft/UI/*.cs` |

### Zonas que la auditoría no pudo cubrir

Pendientes de verificación con acceso a un entorno adecuado; no invalidan ningún hallazgo, pero acotan
hasta dónde llega la evidencia:

- ~~**UI tests (FlaUI):** no ejecutados~~ **resuelto el 2026-08-21.** Ejecutados en escritorio real:
  27/27 al cortar la 1.8.3 y **37/37** hoy. `release.ps1` los exige en cada corte.
- **Rendimiento medido:** sin *profiler*. Arranque, memoria y tiempo hasta la primera fila sin medir.
- ~~**Responsividad real (T2-11):**~~ **resuelto en la 1.8.6.** La fila de filtros envuelve, y la
  comprobación ya no sale de leer el XAML.
- ~~**Explotabilidad de T0-01:**~~ **resuelto el 2026-08-20.** Ya no es hipótesis: el test
  `ScanAsync_NameEscapesTheBaseDirectory_ProducesNoCandidate` demuestra que, sin la corrección, un
  paquete llamado `C:\Windows` hacía que el escáner ofreciera **`C:\Windows`** como residuo
  eliminable. Lo que sigue sin comprobarse es el eslabón previo: que un instalador real escriba un
  `DisplayName` así en Agregar o quitar programas.
- **Lector de pantalla real (T1-07, T1-08, T1-09):** *parcialmente cubierto.* Desde la 1.8.5 hay tests
  FlaUI que leen el árbol de automatización de la app real (`0c6aae7`), que es lo que consume un lector.
  Lo que sigue sin hacerse es escuchar una sesión con Narrador o NVDA.
- **Contraste fuera del registro:** solo se midieron `LogPalette` y los RGB cableados; el resto sale de
  `ThemeResource` de Windows, que se asume conforme. *(2026-09-14: medido en el Tier F — el botón de
  peligro de Desinstalar y Limpieza y las filas atenuadas **no** llegan a 4,5:1; ver F-05 y F-06.)*
- **Instalador end-to-end:** no ejecutado en una VM limpia sin .NET ni VC++ Redist.
- **Comportamiento en ARM64:** el instalador solo distribuye `win-x64` aunque el `.csproj` declare
  `win-arm64`; no verificado en hardware ARM.
- **Configuración del repositorio en GitHub:** *topics* y ramas protegidas no son accesibles desde el
  árbol de archivos. (Las alertas de seguridad del repositorio quedan fuera de alcance por decisión de
  proyecto: la vigilancia de dependencias es local, ver T2-13.)

---
---

# Parte III — Tier F (auditoría Fluent y UI/UX)

> **Origen:** revisión de UI/UX del **2026-09-14** sobre la **v1.8.8**, hecha con la skill `winui-design`
> (Fluent Design, theming, elección de controles, accesibilidad y maquetación) y **verificada en la app
> real**: tres pasadas por UI Automation (tema claro forzado sobre Windows en oscuro, y tema oscuro;
> consulta con 26 actualizaciones, Historial con 104 entradas, Desinstalar con 113 programas), capturas con
> `PrintWindow`, contraste con la fórmula WCAG 2.x y APIs contrastadas con Microsoft Learn y la WinUI Gallery.
>
> **Qué cubre.** Solo lo que **no** cubrieron el Tier C ni el plan T0–T4. Mismo formato que la Parte II
> (tareas marcables, independientes y verificables), con numeración propia `F-xx`. Respeta las
> *Decisiones cerradas* de la Parte I: nada de toasts, MSIX ni CI.
>
> **Sin tocar los datos del usuario:** `settings.json`, su `.bak` y `logs/` se respaldaron antes de cada
> pasada y quedaron **idénticos** (comprobado por hash). Solo se lanzaron consultas de lectura
> (`winget upgrade`, `winget list`, `winget show`): no se actualizó, instaló ni desinstaló nada.

## Índice del Tier F

| Bloque | Qué entra aquí | Tareas | Esfuerzo bajo | medio | alto |
|---|---|---:|---:|---:|---:|
| **F·1** — Defectos verificados | Scroll de la tabla, accesibilidad, tema de diálogos, confirmaciones, contraste, ventanas duplicadas, bandeja | **10** | 8 | 2 | 0 |
| **F·2** — Alineación con Fluent / WinUI 3 | Mica, diálogos XAML, barra de título, estilos compartidos, etiquetas, Configuración | **6** | 4 | 2 | 0 |
| **F·3** — Experiencia de uso | Densidad, estados vacíos, feedback de lotes, textos, atajos | **7** | 3 | 4 | 0 |
| **F·4** — Verificación pendiente y QA | Contraste alto, respaldo de ajustes en los UI tests | **2** | 1 | 1 | 0 |
| **F·5** — Opcional / estructural | Ventana única con `NavigationView` | **1** | 0 | 0 | 1 |
| | **Total** | **26** | **16** | **9** | **1** |

**Orden de ejecución recomendado:** **F-25 primero** (los UI tests que validarán este tier no deben
tocar el `settings.json` real) → los *quick wins* de F·1 (F-02, F-03, F-04, F-06, F-07, F-08, F-09 y
F-10: esfuerzo bajo y sin dependencias) → F-01 y F-05 → F·2 → F·3 → F-24 → F-26 solo con decisión
explícita.

**Progreso (2026-09-17): 17 de 26.** ✅ F-01 a F-16 y F-25 (F·1 y F·2 completos).

---

## 🔴 F·1 — Defectos verificados en la app real

> Todos reproducidos conduciendo la v1.8.8. La evidencia de cada uno va en la propia tarea.

- [x] **[F-01] La tabla principal debe desplazarse por sí misma, no la página**
  - **Área:** Responsive / rendimiento / UX
  - **Ubicación:** `src/WingetUSoft/UI/MainWindow.xaml:60-74` (`ContentScroller` y el `MinHeight` atado a `ViewportHeight`)
  - **Qué hacer:** el `ScrollViewer` de página mide su contenido con alto infinito, así que la fila `*`
    se comporta como `Auto`: el `ListView` se mide entero, no desplaza por sí mismo y pierde la
    virtualización. Atar `Height` (no solo `MinHeight`) al `ViewportHeight` y calcular el mínimo en
    `SizeChanged` (alto real de las filas 0–2 + los 300 de la tabla + padding). Como `MinHeight` manda
    sobre `Height`, la página solo desplaza por debajo de ese mínimo, que es el caso de snap de ¼ para el
    que existe `ContentScroller` (Tier B #7).
  - **Criterio de aceptación:** a 1180×820 y con más filas de las que caben, `lvPackages` expone
    `VerticallyScrollable=True` y `ContentScroller` `False`, la tarjeta del registro queda dentro de la
    ventana y las cabeceras de columna siguen visibles al desplazar la lista. `SnapLayoutTests` y
    `LayoutTests` siguen en verde.
  - **Evidencia (2026-09-14):** con 26 actualizaciones, lista `VerticallyScrollable=False`
    (`VerticalViewSize` 100 %) y página `True` (44,7 %); `txtLogHeader` fuera de la ventana; al bajar la
    página desaparecen los botones y las cabeceras de columna.
  - **Al implementarlo:** el `Grid` de la página se llama `ContentGrid`, con `Height` atado al
    `ViewportHeight`. `UpdateContentMinHeight` suma el relleno, las tres tarjetas superiores (alto real +
    márgenes) y el `MinHeight` de la fila de la tabla. Se recalcula en el `SizeChanged` de cada tarjeta,
    porque la de acciones crece al pasar botones a otra línea y la cabecera al abrirse el aviso de
    actualización.
  - **Desviación:** sin winget no hay filas, y con la tabla vacía el defecto no se manifiesta, así que un
    UI test no lo detectaría. Lo fija `TableXamlTests.MainPage_FillsTheViewport_SoTheTableScrollsByItself`
    (falla al volver a `MinHeight`) y la medición en la app real con la consulta de verdad.
  - **Verificado (2026-09-16):** 325/325 unitarios y 42/42 UI tests (`SnapLayoutTests` y `LayoutTests` en
    verde). En la app real a 1180×820, con 25 actualizaciones: `lvPackages` `VerticallyScrollable=True`
    (vista al 13,8 %) y `ContentScroller` `False` (100 %); registro y barra de estado dentro de la ventana; con
    la lista al final, las cabeceras de columna y «Consultar actualizaciones» siguen visibles. A 960×520
    (cuarto de pantalla) la página vuelve a desplazar (59,5 %). `settings.json` restaurado idéntico.
    Observación para F·3: a 1180×820 la tabla muestra unas 3½ filas, porque comparte el alto 2:1 con el
    registro.
  - **Esfuerzo:** medio
  - **Depende de:** ninguna

- [x] **[F-02] Nombre accesible en las filas de Historial y Desinstalar**
  - **Área:** Accesibilidad (WCAG 2.2 AA · 4.1.2 Nombre, función, valor)
  - **Ubicación:** `src/WingetUSoft/UI/HistoryWindow.xaml:125-148` (y `HistoryEntryViewModel` en `HistoryWindow.xaml.cs:10-30`); `src/WingetUSoft/UI/UninstallWindow.xaml:167-185` (las filas son `WingetPackage` sin envolver, `UninstallWindow.xaml.cs:15`)
  - **Qué hacer:** es el mismo bug que se corrigió en la tabla principal y en Búsqueda (Tier E) y en
    Limpieza (T1-09): sin `AutomationProperties.Name`, el `ListViewItem` hereda el `ToString()` del objeto.
    Añadir un `RowLabel` localizado (fecha, nombre, versión origen → destino y estado en Historial; nombre,
    versión y origen en Desinstalar, con un ViewModel como `SearchResultViewModel`) y enlazarlo en la
    plantilla.
  - **Criterio de aceptación:** cada fila expone un `Name` legible en los 5 idiomas; un test de
    `AccessibilityTests` lo exige en ambas ventanas; `LocalizationTests` sigue en verde.
  - **Evidencia (2026-09-14):** UI Automation devuelve literalmente `WingetUSoft.HistoryEntryViewModel` y
    `WingetUSoft.WingetPackage` como nombre de las filas.
  - **Desviación al implementarlo:** el criterio pedía un test de `AccessibilityTests` en ambas ventanas, y no
    se hizo así. Desinstalar lanza `winget list` al abrirse, y los UI tests evitan a propósito todo lo que
    dependa de winget o de la red (ver `SearchWindowTests`); Historial solo tendría filas si el equipo que
    corre la suite tiene historial. En su lugar: `XamlAccessibilityTests` lee **todos** los XAML de `UI/` y
    exige `AutomationProperties.Name` en la raíz de cada plantilla de fila —cubre también Limpieza y las
    ventanas futuras—, y `RowLabelTests` fija la composición de las etiquetas (con una forma propia para
    lo instalado desde Buscar, que no tiene versión de origen, y para lo que no tiene origen).
  - **Verificado (2026-09-16):** el test estructural fallaba antes del cambio nombrando exactamente
    `HistoryWindow.xaml` y `UninstallWindow.xaml`; 298/298 unitarios. En la app real, 11 filas de Historial y
    13 de Desinstalar leídas por UI Automation, **0** con nombre de tipo (p. ej. «Battle.net, versión
    Unknown, origen winget»).
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[F-03] Los diálogos genéricos deben seguir el tema elegido**
  - **Área:** Theming
  - **Ubicación:** `src/WingetUSoft/UI/WindowDialogHelper.cs:20-49` y `src/WingetUSoft/UI/SettingsWindow.xaml.cs:171-185`
  - **Qué hacer:** el tema se fuerza por elemento y un `ContentDialog` no lo hereda; por eso `MainWindow`
    fija `RequestedTheme` en Acerca de, Licencia, Novedades y Exportar. `WindowDialogHelper` no lo hace, y
    por él pasan todas las confirmaciones y los errores. Tomar el tema de `xamlRoot.Content` en un único
    punto de preparación de diálogos (que aplique también F-12) y usarlo en todos.
  - **Criterio de aceptación:** con la app en Claro sobre Windows oscuro (y al revés), todos los diálogos
    salen en el tema de la app; un test estructural falla si aparece un `new ContentDialog` fuera de ese
    punto de preparación.
  - **Evidencia (2026-09-14):** con la app en Claro sobre Windows oscuro, «No hay programas para
    actualizar» se pinta **oscuro** mientras «Acerca de» se pinta claro.
  - **Al implementarlo:** `WindowDialogHelper.Prepare` ancla el diálogo y copia el `RequestedTheme` de la raíz
    de su ventana. Lo usan los **siete** puntos que crean diálogos, también los tres que ya copiaban el tema a
    mano (Acerca de, Licencia, Novedades); `MainWindow.CurrentTheme` desaparece.
  - **Verificado (2026-09-16):** `DialogPreparationTests` (2 tests) falla con la versión anterior de
    `SettingsWindow`, nombrando `SettingsWindow.xaml.cs:177`; 300/300 unitarios. En la app real, con Claro
    sobre Windows oscuro, el diálogo informativo pasa de `#2B2B2B` / `#202020` a `#FFFFFF` (contenido) y
    `#F3F3F3` (botones).
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[F-04] Confirmaciones con verbo y sin un Enter que ejecute lo destructivo**
  - **Área:** UX / seguridad
  - **Ubicación:** `src/WingetUSoft/UI/WindowDialogHelper.cs:32-49` (`DefaultButton = Primary` en `:45`); llamadas en `UninstallWindow.xaml.cs:132-134`, `CleanupWindow.xaml.cs:258-260`, `MainWindow.xaml.cs:598-599`, `MainWindow.Menus.cs:287`, `MainWindow.Batch.cs:43`, `MainWindow.Grid.cs:238-240` y `SearchWindow.xaml.cs:222-224`; textos en `Localization/Localization.cs:177-178` y `:518`
  - **Qué hacer:** todas las confirmaciones responden «Sí / No» («Sí, eliminar / No» en Limpieza) y el
    primario es el botón por defecto, así que **Enter** desinstala o borra de forma recursiva. La guía de
    Microsoft pide que los botones digan la respuesta concreta. Pasar el verbo como primario
    («Desinstalar», «Eliminar 3 elementos», «Actualizar 12 programas», «Importar», «Instalar», «Abrir») y
    «Cancelar» como cierre, y añadir un parámetro `destructive` que deje sin botón por defecto a
    Desinstalar y Limpieza. Con el verbo en el botón sobra el «¿Desea continuar?» de los cuerpos (F-20).
  - **Criterio de aceptación:** `btn.yes`, `btn.no` y `btn.yesDelete` dejan de usarse y se retiran; un
    test fija que las confirmaciones destructivas no tienen `DefaultButton = Primary`, **sin** conducir una
    desinstalación real.
  - **Desviación al implementarlo:** en las acciones destructivas el botón por defecto no queda vacío: pasa a
    ser **Cancelar**. Sin botón por defecto, el foco inicial del diálogo caería igualmente en el primario y
    un Intro seguiría confirmando. Efecto visible: «Cancelar» lleva el estilo de acento, que es como WinUI
    marca el botón por defecto. El verbo de «Eliminar» va sin recuento («Eliminar 3 elementos» esperaría a
    los plurales de F-20).
  - **Verificado (2026-09-16):** `ConfirmationDialogTests` (6 tests) falla si Desinstalar deja de declararse
    `destructive: true`; `LocalizationTests` fija «Cancelar» y los verbos en lugar de «Sí / No»; 305/305
    unitarios. En la app real, la confirmación de desinstalar muestra «Desinstalar» / «Cancelar» con el foco
    en **Cancelar**, leído por UI Automation sin pulsar ningún botón (el proceso se cerró con el diálogo
    abierto).
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[F-05] Un `DangerButtonStyle` real: contraste AA, estados completos y distinto del acento**
  - **Área:** Accesibilidad (WCAG 2.2 AA · 1.4.1 Uso del color y 1.4.3 Contraste mínimo) / theming
  - **Ubicación:** `src/WingetUSoft/UI/UninstallWindow.xaml:72-89` y `src/WingetUSoft/UI/CleanupWindow.xaml:75-92` (dos copias idénticas); el `DangerButtonStyle` que cita `MainWindow.xaml:152-154` no existe; acentos vecinos en `UninstallWindow.xaml:63` y `CleanupWindow.xaml:67`
  - **Qué hacer:** definir el estilo una sola vez en `App.xaml` (`BasedOn` el de botón por defecto), con
    diccionarios Light, Dark **y** HighContrast y los recursos de reposo, hover, pulsado y deshabilitado.
    Paleta ya medida, a partir de los `SystemFillColorCritical` que usa `LogPalette`: en claro, texto
    blanco sobre `#C42B1C` / `#B0271A` / `#9C2217` (5,66 / 6,66 / 7,91:1); en oscuro, texto `#1A1A1A` sobre
    `#FF99A4` / `#FFB0B8` / `#E8858F` (8,57 / 10,07 / 6,78:1). Añadir el glifo de papelera para no depender
    solo del color, y quitar el acento a «Actualizar lista» y «Volver a escanear», que no son la acción
    principal de su ventana.
  - **Criterio de aceptación:** un test mide los tres estados en ambos temas (≥ 4,5:1); ningún XAML de
    ventana declara `SolidColorBrush` con color literal; con un acento rojo en Windows, la acción
    destructiva se distingue a simple vista.
  - **Evidencia (2026-09-14):** blanco sobre `#D55C4C` (oscuro, reposo) = **3,84:1**; hover `#E06858` =
    **3,34:1**; claro hover `#D05040` = **4,28:1**; sin estado pulsado ni diccionario HighContrast. Con el
    acento rojo del sistema, «Actualizar lista» y «Desinstalar seleccionado» son dos rojos casi iguales
    (capturado).
  - **Desviación:** no es un `Style` en `App.xaml` sino un diccionario compartido,
    `UI/DangerButtonResources.xaml`, que cada botón fusiona en sus `Resources`. La plantilla de `Button` lee
    `ButtonBackgroundPointerOver`, `ButtonBackgroundPressed`... en sus estados visuales, y un `Style` solo
    alcanza el reposo: cambiar el resto obligaría a copiar la plantilla entera. Deshabilitado queda con los
    recursos por defecto (gris neutro, como cualquier botón inactivo). En alto contraste usa los
    `SystemColor*` del usuario; ahí distingue la papelera. En lugar de poner un acento rojo en Windows, se
    comprobó que el botón vecino ya no lleva acento, que era lo que hacía que los dos se confundieran.
  - **Verificado (2026-09-16):** `DangerButtonTests` (10 tests): los tres estados en los dos temas ≥ 4,5:1,
    alto contraste solo con colores del sistema, papelera y diccionario compartido en las dos ventanas,
    vecino sin acento y ninguna ventana con `SolidColorBrush` de color literal. Con el XAML anterior fallan 3
    (la lista de colores literales sale entera). 324/324 unitarios. En la app real (Desinstalar, con un
    paquete seleccionado; color medido en pantalla): claro `#C42B1C` / `#B0271A` / `#9C2217` con texto
    blanco = 5,66 / 6,66 / 7,91:1; oscuro `#FF99A4` / `#FFB0B8` / `#E8858F` con texto `#1A1A1A` = 8,57 /
    10,07 / 6,78:1; «Actualizar lista» neutro (`#FEFEFE` / `#373737`); nombre UIA «Desinstalar
    seleccionado». Limpieza comparte el mismo diccionario y solo se abre tras desinstalar algo, así que ahí
    lo cubren el compilador XAML y los tests. Incidencia de la prueba: al medir el estado pulsado, soltar el
    ratón fuera del botón contó como clic y abrió la confirmación. No se confirmó (el foco estaba en
    «Cancelar») y la app se cerró con el diálogo abierto, así que no se desinstaló nada. El script ahora
    cierra la app antes de soltar.
  - **Esfuerzo:** medio
  - **Depende de:** ninguna

- [x] **[F-06] Filas excluidas y omitidas legibles, sin `Opacity`**
  - **Área:** Accesibilidad (WCAG 2.2 AA · 1.4.3)
  - **Ubicación:** `src/WingetUSoft/UI/Converters.cs:25-32` (`0.4`), aplicado en `MainWindow.xaml:462`
  - **Qué hacer:** la opacidad atenúa por igual texto, casilla e icono y hunde el contraste. Pintar las
    celdas de texto con `TextFillColorSecondaryBrush`, conservar el icono de estado que ya distingue
    excluido de omitido y retirar `BoolToOpacityConverter` si queda sin uso.
  - **Criterio de aceptación:** el texto de las filas atenuadas llega a ≥ 4,5:1 en ambos temas, medido en
    un test con los valores de los pinceles (como hace `LogPaletteTests`).
  - **Evidencia (2026-09-14):** con `Opacity 0.4`, **2,50:1** en claro y **3,59:1** en oscuro; con
    `TextFillColorSecondaryBrush`, 6,17:1 y 9,09:1.
  - **Al implementarlo:** el pincel no puede resolverse en un convertidor (`Application.Current.Resources` no
    sigue el tema forzado por ventana, el mismo problema que T1-04), así que las celdas alternan entre dos
    **estilos** —normal y atenuado, este con `{ThemeResource TextFillColorSecondaryBrush}`— mediante un
    `BoolToObjectConverter` genérico que sustituye a `BoolToOpacityConverter`.
  - **Verificado (2026-09-16):** `TableXamlTests` mide el contraste de los dos temas y comprueba que la fila no
    usa `Opacity` (falla con el XAML anterior); 313/313 unitarios. En la app real, en tema oscuro, el texto de
    una fila atenuada mide `#CFCFCF` (antes, con opacidad 0,4, `#808080`) frente a `#FFFFFF` de una normal.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[F-07] Alinear las cabeceras con las celdas de las tablas**
  - **Área:** Consistencia visual
  - **Ubicación:** `src/WingetUSoft/UI/MainWindow.xaml:453-457`, `SearchWindow.xaml:138-141`, `UninstallWindow.xaml:162-165` y `HistoryWindow.xaml:120-123`; la referencia correcta está en `CleanupWindow.xaml:164-169`
  - **Qué hacer:** el `ListViewItem` trae `Padding="16,0,12,0"` por defecto y las cabeceras no lo
    replican. Llevar a las otras cuatro tablas el `ItemContainerStyle` con `Padding=0` que ya usa
    Limpieza, idealmente como estilo compartido (F-14).
  - **Criterio de aceptación:** un UI test compara la X de cada cabecera con la de la primera celda de su
    columna (≤ 2 px) en la ventana principal y en Historial.
  - **Evidencia (2026-09-14):** la primera columna va **+16 px** y el resto **−12 px** respecto a su
    cabecera, en Principal, Búsqueda, Desinstalar e Historial.
  - **Desviación al implementarlo:** el criterio pedía un UI test, pero las filas de la tabla principal
    dependen de una consulta a winget y las de Historial, de que el equipo tenga historial; los UI tests no
    dependen de ninguna de las dos cosas. `TableXamlTests` exige en su lugar que **toda** tabla de `UI/` anule
    el padding del `ListViewItem`, y la alineación se midió en la app real.
  - **Verificado (2026-09-16):** el test estructural falla con el XAML anterior nombrando `lvPackages` y
    `lvResults`. En la app real, desfase de **0 px** entre cabecera y celda en cuatro columnas de la tabla
    principal (con 25 actualizaciones) y en dos de Historial.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[F-08] Una sola instancia por ventana secundaria**
  - **Área:** UX / integridad de datos
  - **Ubicación:** `src/WingetUSoft/UI/MainWindow.Menus.cs:133-188`
  - **Qué hacer:** cada clic en el menú crea una ventana nueva. Con dos Configuraciones abiertas sobre el
    mismo `AppSettings` gana la última en guardar y la principal reaplica los cambios dos veces. Guardar
    la referencia de cada ventana y, si sigue abierta, restaurarla y activarla.
  - **Criterio de aceptación:** invocar dos veces Configuración, Historial, Desinstalar o Buscar deja una
    sola ventana de cada; UI test para Configuración.
  - **Evidencia (2026-09-14):** dos invocaciones seguidas dejan **2** ventanas de Configuración abiertas.
  - **Al implementarlo:** un único `OpenSingleInstanceAsync<T>` para las cuatro ventanas. Devuelve la ventana
    solo si la abrió esa llamada, así que lo que se hace al cerrarla (reaplicar ajustes, avisar de lista
    obsoleta) no se repite al reactivar la que ya estaba abierta.
  - **Verificado (2026-09-16):** `SettingsTests.SettingsWindow_OpenedTwice_StaysASingleWindow` pasa con el
    build nuevo y **falla contra el ejecutable Release 1.8.8** («Una segunda pulsación abrió otra ventana de
    Configuración»); suite de UI tests 42/42 con los datos del usuario idénticos. En la app real, Historial
    pedido dos veces deja **1** ventana.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[F-09] Menú en el icono de bandeja, con «Salir»**
  - **Área:** UX
  - **Ubicación:** `src/WingetUSoft/UI/MainWindow.Tray.cs:28-53` y `:72-92`
  - **Qué hacer:** el icono solo tiene `DoubleClickCommand`. Con «Minimizar a la bandeja al cerrar»
    activo, cerrar oculta la ventana y **no queda ninguna forma de salir** desde la interfaz. H.NotifyIcon
    ya soporta `ContextFlyout` (`ContextMenuMode`) y `LeftClickCommand`: menú localizado Abrir · Consultar
    actualizaciones · Salir, y clic simple para restaurar. «Salir» debe saltarse la intercepción de
    `OnAppWindowClosing` y seguir liberando el icono y vaciando `FileLog`. No es una notificación, así que
    no reabre la decisión cerrada sobre los toasts.
  - **Criterio de aceptación:** con la opción activa, «Salir» termina el proceso y el registro a disco
    queda completo (verificación manual: el menú nativo de la bandeja no está al alcance de los UI tests).
  - **Al implementarlo:** menú Abrir WingetUSoft · Consultar
    actualizaciones · Salir, clic simple para restaurar y reconstrucción del menú al cambiar de idioma.
    Comprobado en el código fuente de H.NotifyIcon que su menú nativo ejecuta el `Command` de cada entrada y
    nunca el `Click`, así que todas llevan `XamlUICommand`. La liberación al salir se hace una sola vez
    (`FileLog.Dispose` no admite dos llamadas). `TrayMenuTests` (3 tests) fija esas tres condiciones sobre el
    código.
  - **Defecto previo encontrado en la prueba manual:** con la opción activa, cerrar ocultaba la ventana y el
    proceso seguía vivo **sin icono en la bandeja**, también en 1.8.8. El `TaskbarIcon` se crea desde código,
    fuera del árbol XAML, y H.NotifyIcon solo lo registra en el `Loaded` o con `ForceCreate`, que nunca se
    llamaba. Ahora `MinimizeToTray` llama a `ForceCreate(enablesEfficiencyMode: false)` (sin modo eficiencia:
    la consulta automática sigue en segundo plano). Nuevo test `TrayIcon_IsForceCreated_WithoutEfficiencyMode`.
  - **Verificado (2026-09-16):** 314/314 unitarios. En la app real, conducida con UI Automation y ratón sobre
    el área de notificación: cerrar oculta la ventana con el proceso vivo y el icono «WingetUSoft» presente;
    el clic simple la restaura; el clic derecho abre el menú nativo con Abrir WingetUSoft · Consultar
    actualizaciones · Salir (captura); «Salir» termina el proceso en menos de 10 s y retira el icono.
    `settings.json` restaurado idéntico.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[F-10] Sin salto de maquetación al seleccionar una fila**
  - **Área:** UX
  - **Ubicación:** `src/WingetUSoft/UI/MainWindow.xaml:301-330` y `src/WingetUSoft/UI/MainWindow.Grid.cs:102-151`
  - **Qué hacer:** el panel de información pasa de `Collapsed` a `Visible` dentro de la tarjeta de
    filtros y empuja la tabla hacia abajo. Reservar su hueco (alto fijo con texto de marcador) o llevar el
    detalle a un panel lateral que no desplace la lista.
  - **Criterio de aceptación:** un UI test comprueba que la Y de `lvPackages` no cambia (±1 px) al
    seleccionar y deseleccionar una fila.
  - **Evidencia (2026-09-14):** la tabla baja **36 px** al seleccionar la primera fila.
  - **Al implementarlo:** el panel queda siempre visible con alto mínimo, y sin selección muestra la indicación
    que antes ocupaba una línea propia en la cabecera (`txtDetalleEstado`, retirada): así no aparece la misma
    frase dos veces y la cabecera gana una línea. También desaparece la línea «nombre | id | versiones |
    origen | estado», que repetía lo que ya dice la fila, con sus dos claves (`pkg.excluded`,
    `pkg.readyToUpdate`). El UI test del criterio se sustituye por una guarda estructural (el panel no tiene
    `Visibility` ni el código lo pliega), por el mismo motivo que F-07.
  - **Verificado (2026-09-16):** en la app real, **0 px** de salto al seleccionar la primera fila y al cambiar a
    la segunda; el panel muestra la indicación sin selección y la descripción con ella.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

---

## 🟠 F·2 — Alineación con Fluent / WinUI 3

- [x] **[F-11] Hacer visible Mica (o dejar de pedirlo)**
  - **Área:** Fluent / theming
  - **Ubicación:** `src/WingetUSoft/UI/WindowChrome.cs:57`; fondo opaco en la raíz de las seis ventanas (`MainWindow.xaml:9`, `SearchWindow.xaml:9`, `HistoryWindow.xaml:9`, `UninstallWindow.xaml:9`, `CleanupWindow.xaml:9`, `SettingsWindow.xaml:8`)
  - **Qué hacer:** `ApplicationPageBackgroundThemeBrush` es opaco y tapa por completo el `MicaBackdrop`.
    Poner la raíz en `Transparent` cuando `MicaController.IsSupported()` sea cierto y conservar el pincel
    sólido si no (Windows 10 no tiene Mica y la app admite 19041). Las tarjetas ya usan
    `CardBackgroundFillColorDefaultBrush`, pensado para ir sobre Mica. Revisar la referencia de fondo de
    `LogPalette` (`Core/LogPalette.cs:24-25`), que hoy se calcula sobre ese fondo opaco.
  - **Criterio de aceptación:** en Windows 11 los márgenes de la ventana dejan de ser `#F3F3F3` / `#202020`
    planos; con Mica no disponible el fondo sigue siendo sólido; `LogPaletteTests` en verde.
  - **Evidencia (2026-09-14):** en las capturas de `docs/screenshots` (copia literal de la pantalla),
    todos los márgenes muestreados son `#F3F3F3` (claro) y `#202020` (oscuro) exactos.
  - **Al implementarlo:** `WindowChrome.ApplyBackdrop` pide Mica solo si `MicaController.IsSupported()` y, en
    ese caso, cambia a `Transparent` la raíz de la ventana. El XAML conserva
    `ApplicationPageBackgroundThemeBrush` como reserva para Windows 10. `LogPalette` no cambia: el fondo
    medido de la tarjeta apenas se mueve, y se añadió margen al test.
  - **Verificado (2026-09-17):** en pantalla (captura del escritorio, porque Mica solo se ve en la ventana
    activa), los márgenes entre tarjetas toman el tinte del fondo de escritorio y varían dentro de la
    ventana: `#F5F2F2`–`#F9F0F0` en claro y `#212020`–`#231F1F` en oscuro. La tarjeta del registro queda
    entre `#FBFBFB`–`#FDFBFB` y `#2B2B2B`–`#2D2A2A`, y el peor color del registro, a 5,07:1 (aviso, claro)
    y 6,62:1 (acento, oscuro). Nuevo `LogPaletteTests.EveryLogColor_KeepsWcagAa_WhenMicaTintsTheCard`, que
    exige 4,5:1 contra `#F4F4F4` y `#333333`, peores que lo medido; el color con menos margen, el aviso en
    claro, deja de cumplir por debajo de `#EDEDED`. `WindowChromeTests` fija el orden: primero comprobar
    el soporte y después pedir Mica y retirar el fondo.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[F-12] Estilo moderno en los diálogos definidos en XAML**
  - **Área:** Fluent
  - **Ubicación:** `src/WingetUSoft/UI/AboutDialog.xaml:2-17`, `WhatsNewDialog.xaml:2-17` y `LegalTextDialog.xaml:2-12`
  - **Qué hacer:** el estilo implícito de `ContentDialog` no alcanza a las subclases, así que estos tres
    diálogos usan la plantilla antigua (de ahí, probablemente, los `CornerRadius` parcheados). Aplicar
    `Style="{StaticResource DefaultContentDialogStyle}"`, como hace la WinUI Gallery, y retirar los parches
    de esquinas y de estilos de botón. De paso, «Ver en GitHub» no debería llevar acento en Acerca de: no
    es la acción principal del diálogo.
  - **Criterio de aceptación:** en un mismo tema, los tres muestran la franja de contenido y la de botones
    igual que los diálogos creados por código; no queda ningún `CornerRadius` ni estilo de botón local.
  - **Evidencia (2026-09-14):** en tema oscuro, el diálogo genérico tiene el contenido en `#2B2B2B` y los
    botones en `#202020`; Acerca de es `#202020` uniforme de arriba abajo.
  - **Al implementarlo:** `Style="{StaticResource DefaultContentDialogStyle}"` en los tres, sin
    `CornerRadius` ni `PrimaryButtonStyle`/`CloseButtonStyle` locales. En Acerca de, `DefaultButton = Close`,
    así que el acento y el Intro pasan a «Cerrar».
  - **Verificado (2026-09-17):** en oscuro, Acerca de y Licencia muestran contenido `#2B2B2B` y botones
    `#202020`, igual que el diálogo genérico, y el foco inicial de Acerca de está en `CloseButton`
    (capturas). `DialogPreparationTests.XamlDialogs_UseTheModernStyle_WithoutLocalPatches` falla con el
    XAML anterior.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[F-13] Barra de título con `PreferredTheme` en vez de colores cableados**
  - **Área:** Theming
  - **Ubicación:** `src/WingetUSoft/UI/TitleBarHelper.cs:9-38` y `src/WingetUSoft/UI/WindowChrome.cs:59-66`
  - **Qué hacer:** `TitleBarHelper` fija a mano el blanco o el negro y los colores de hover, pulsado e
    inactivo. Desde Windows App SDK 1.7 (la app va en 1.8) basta con
    `AppWindow.TitleBar.PreferredTheme = TitleBarTheme.Light | Dark | UseDefaultAppMode` según el ajuste,
    reaplicado en `ActualThemeChanged`. Valorar también el control `TitleBar` (1.7+), que unifica icono,
    título y zonas de arrastre (hoy el icono se pinta a mano en `MainWindow.xaml.cs:134`).
  - **Criterio de aceptación:** `TitleBarHelper` desaparece o se reduce a `PreferredTheme`; los botones de
    la barra son correctos en claro, en oscuro y al cambiar de tema en caliente; `SnapLayoutTests` en verde.
  - **Al implementarlo:** `TitleBarHelper.cs` desaparece. `WindowChrome.ApplyTitleBarTheme` fija
    `PreferredTheme` según el tema **resuelto** del contenido, porque con «el del sistema» el ajuste no dice
    si la ventana es clara u oscura. Se aplica en `Loaded` y en `ActualThemeChanged`, y las seis ventanas
    pierden su `UpdateTitleBarButtonColors`.
  - **Desviación:** no se adopta el control `TitleBar`. Cambiaría la zona de arrastre que fijan
    `SnapLayoutTests` y el icono pintado a mano funciona; queda para F-14 si se reorganiza la cabecera.
  - **Verificado (2026-09-17):** botón cerrar medido en pantalla: glifo `#1F1F1F` sobre `#F4F2F2` en claro y
    `#E3E3E3` sobre `#212020` en oscuro. Arrancando en claro y pasando a oscuro desde Configuración, sin
    reiniciar, la ventana principal repinta el glifo a `#E3E3E3`. El fondo de los botones deja ver Mica.
    43/43 UI tests, con `SnapLayoutTests` incluido. `WindowChromeTests` impide volver a fijar colores
    `TitleBar.Button*` a mano.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[F-14] Estilos compartidos en `App.xaml`**
  - **Área:** Mantenibilidad / consistencia visual
  - **Ubicación:** `src/WingetUSoft/App.xaml:6-12` (solo fusiona `XamlControlsResources`); tarjetas repetidas en los 6 XAML de ventana
  - **Qué hacer:** el bloque de tarjeta (fondo, borde, grosor, radio y padding) se repite **30 veces** con
    cinco paddings distintos (`20,16`, `16,12`, `16,10`, `16,8` y `12`). Crear un `Styles.xaml` con
    `CardStyle`, el estilo de cabecera de tabla, `TableRowContainerStyle` (F-07), `DangerButtonStyle`
    (F-05) y `ColumnHeaderButtonStyle` (hoy local en `MainWindow.xaml:19-28`); usar `OverlayCornerRadius` y
    `ControlCornerRadius` en vez de `8` y `4` literales, y llevar los espaciados a la rampa de 4 px.
  - **Criterio de aceptación:** ningún XAML de ventana repite el bloque de tarjeta ni usa `CornerRadius`
    literales (test estructural); las capturas no cambian salvo por la normalización de espaciados.
  - **Al implementarlo:** `UI/Styles.xaml`, fusionado en `App.xaml` después de `XamlControlsResources`. Las
    30 tarjetas quedan en cuatro estilos según su relleno, todos `BasedOn` `CardStyle`: `HeaderCardStyle`
    (20,16), `CardStyle` (16,12), `TableCardStyle` (12) y `CompactCardStyle` (16,8). También van ahí
    `TableHeaderStyle` (5 cabeceras de tabla), `TableRowContainerStyle` (5 `ItemContainerStyle` en línea) y
    `ColumnHeaderButtonStyle`, que sale de `MainWindow.xaml`. Radios con `OverlayCornerRadius`,
    `ControlCornerRadius` y un `TableHeaderCornerRadius` compartido. Se normalizaron 20 espaciados a la rampa
    de 4 px (10 → 8, 6 → 4 u 8, 3 → 4, `10,6` → `12,4`), además del `16,10` de las barras.
  - **Desviación:** no hay `DangerButtonStyle`. Un `Style` no alcanza los estados de la plantilla de `Button`
    (F-05), así que el botón de peligro sigue siendo `DangerButtonResources.xaml`, fusionado por botón.
  - **Verificado (2026-09-17):** capturas antes y después, píxel a píxel, de la ventana principal,
    Configuración, Historial y Buscar e instalar, en claro y en oscuro. En las 8, la primera fila distinta
    coincide con el primer espaciado normalizado (la barra de filtros de 16,10 a 16,8, el título de
    Acciones rápidas de 10 a 8), y a partir de ahí solo hay desplazamientos de 2–4 px, sin cambios de
    color, borde ni radio (revisado a ojo). `SharedStylesTests` (4: `App.xaml` fusiona `Styles.xaml`,
    ninguna ventana repite la tarjeta, ningún `CornerRadius` literal, espaciados en la rampa) falla en 3 con
    el XAML anterior. `TableXamlTests` exige ahora el estilo compartido en las 5 tablas. 342/342 unitarios,
    43/43 UI tests.
  - **Esfuerzo:** medio
  - **Depende de:** F-05, F-07

- [x] **[F-15] Etiquetas reales en la búsqueda y los filtros de Historial, Desinstalar y Buscar**
  - **Área:** Accesibilidad (WCAG 2.2 AA · 1.3.1, 3.3.2 y 4.1.2)
  - **Ubicación:** `src/WingetUSoft/UI/HistoryWindow.xaml:53-73`, `UninstallWindow.xaml:97-104` y `SearchWindow.xaml:62-66` (con el nombre puesto por código en `SearchWindow.xaml.cs:91`)
  - **Qué hacer:** T2-08 asoció las etiquetas de la ventana principal y las otras tres se quedaron fuera.
    Poner `LabeledBy` hacia la etiqueta visible en Historial y Desinstalar, y una etiqueta visible en
    Buscar, que hoy solo tiene el placeholder. Valorar `AutoSuggestBox` con `QueryIcon` para las búsquedas
    (`QuerySubmitted` sustituye el manejo de Intro de `SearchWindow.xaml.cs:112-119`).
  - **Criterio de aceptación:** el patrón de `AccessibilityTests.MainWindowFilters_AreNamedAfterTheirVisibleLabel`
    se extiende a Historial y Desinstalar: el `Name` coincide con la etiqueta visible.
  - **Evidencia (2026-09-14):** en Historial y Desinstalar el buscador se anuncia «Nombre o Id...» (el
    placeholder) y el filtro de estado, «Todos»; ninguno tiene `LabeledBy`.
  - **Al implementarlo:** `LabeledBy` en el buscador y el filtro de Historial y en el buscador de
    Desinstalar. Buscar e instalar gana una etiqueta visible propia, «Buscar en el catálogo de winget»
    (`search.catalogLabel`, que sustituye a `search.placeholderAccessible`, antes solo un `SetName`
    invisible), en su propia línea para no ensanchar la fila de botones.
  - **Desviación:** no se adopta `AutoSuggestBox`. Intro ya busca, y cambiaría el tipo de control que
    anuncian los lectores y que fijan los tests, sin ganar nada visible.
  - **Verificado (2026-09-17):** nuevo UI test `AccessibilityTests.HistoryFilters_AreNamedAfterTheirVisibleLabel`,
    y `SearchWindowTests.SearchBox_HasAnAccessibleName` exige ahora que el nombre coincida con la etiqueta
    visible (43/43). Desinstalar lanza `winget list` al abrirse, así que su caso lo cubre
    `XamlAccessibilityTests.SearchAndFilterControls_AreLabeledByTheirVisibleLabel` (7 casos, 4 fallan con el
    XAML anterior). Captura de Buscar e instalar con la etiqueta sobre el buscador.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[F-16] Configuración con un único patrón de fila**
  - **Área:** Fluent / UX
  - **Ubicación:** `src/WingetUSoft/UI/SettingsWindow.xaml:47-177`
  - **Qué hacer:** la página mezcla cuatro patrones de etiqueta (radios con subtítulo, `ComboBox` con
    `Header`, `CheckBox`, y texto con `ToggleSwitch`). Adoptar filas tipo `SettingsCard` (título y
    descripción a la izquierda, control a la derecha) con un estilo o `UserControl` propio, sin añadir la
    dependencia del Toolkit, en la línea de `WrapPanel`. Además: tema en un `ComboBox`, como en la
    Configuración de Windows; una descripción de las consecuencias (silenciosa frente a interactiva;
    administrador = un UAC por lote); un botón «Abrir carpeta» junto a la ruta de registros; y estado vacío
    y botón por fila en la lista de excluidos.
  - **Criterio de aceptación:** todas las preferencias siguen el mismo patrón; `SettingsTests` y
    `AccessibilityTests` en verde (los nombres siguen coincidiendo con la etiqueta visible en los 5 idiomas).
  - **Evidencia (2026-09-14):** cuatro patrones distintos en una sola página; los tres radios de tema
    quedan desigualmente espaciados (`docs/screenshots/settings-light.png`).
  - **Al implementarlo:**
    - `UI/SettingsCard.cs`, un `ContentControl` con `Header` y `Description` y la plantilla en `Styles.xaml`.
      Cada opción es su propia tarjeta bajo un título de grupo (`SettingsGroupTitleStyle`), como en la
      Configuración de Windows.
    - Tema y modo pasan de radios a `ComboBox`. Administrador y registro pasan de `CheckBox` a
      interruptor.
    - Cada opción lleva una descripción de sus consecuencias, sacada del código y no supuesta: `--silent`;
      un único proceso elevado por lote, en el que Cancelar no detiene el programa en curso; aviso solo en
      operaciones de 10 s o más con la ventana en segundo plano; el temporizador sigue en la bandeja;
      30 días de registros.
    - La ruta de registros va en la descripción, con un botón «Abrir carpeta» que la crea si no existe.
    - La lista de excluidos tiene un «Quitar» por fila, que se anuncia con el paquete. La descripción de
      la fila cuenta los paquetes o, sin ninguno, explica cómo excluir uno (Supr en la ventana
      principal); la lista se oculta y «Limpiar lista» se deshabilita.
    - 17 claves nuevas en los 5 idiomas; salen `settings.logDirLabel` y `btn.removeSelected`.
  - **Defectos encontrados en la prueba en la app:**
    - La primera versión nombraba también los botones desde el título de la fila, y «Limpiar lista» se
      anunciaba «Estos paquetes no se incluirán en las actualizaciones.». `SettingsCard` ya no aplica
      `LabeledBy` a un botón, y hay un UI test que lo fija.
    - Los interruptores decían «Activado / Desactivado» con la app en inglés: WinUI toma ese texto del
      idioma de Windows. Pasaba ya en 1.8.9. Ahora sale de `toggle.on` / `toggle.off`.
  - **Verificado (2026-09-17):** capturas en oscuro (español, 13 excluidos) y en claro (inglés, lista
    vacía), arriba y abajo. «Quitar» deja 12 filas y se anuncia «Quitar EclipseAdoptium.Temurin.21.JDK de
    los excluidos». «Abrir carpeta» abre el Explorador en `logs`, y los interruptores muestran «On / Off»
    en inglés.
    - `AccessibilityTests.SettingsRows_NameTheirControlAfterTheVisibleTitle` cubre las 8 filas con control,
      antes solo 2, y `SettingsRowButtons_KeepTheirOwnText` cubre los 2 botones.
    - `XamlAccessibilityTests.SettingsWindow_UsesASingleRowPattern` prohíbe `RadioButtons`, `CheckBox` y
      `ComboBox` con `Header`, y exige que cada opción cuelgue de una `SettingsCard`; falla con el XAML
      anterior.
    - 343/343 unitarios y 51/51 UI tests.
  - **Esfuerzo:** medio
  - **Depende de:** ninguna

---

## 🟡 F·3 — Experiencia de uso

- [ ] **[F-17] Más espacio para los datos: cabecera compacta, `CommandBar` y registro plegable**
  - **Área:** UX / densidad
  - **Ubicación:** `src/WingetUSoft/UI/MainWindow.xaml:78-245` y `:549-568`; tarjetas de cabecera de Buscar, Desinstalar, Limpieza, Historial y Configuración
  - **Qué hacer:** cada ventana repite su título en una tarjeta de 28 px, cuando ya lo dice la barra de
    título, y el registro reserva sitio aunque esté vacío. Compactar o retirar las cabeceras, llevar las
    acciones a una `CommandBar` con icono y etiqueta, y meter el registro en un `Expander` que se abra solo
    al empezar una operación o ante un error. La línea fija de atajos se va con F-21.
  - **Criterio de aceptación:** a tamaño de diseño, las cabeceras de columna de la ventana principal
    empiezan por encima del 30 % del alto y Desinstalar muestra al menos 10 filas a 900×700;
    `LayoutTests` y `SnapLayoutTests` en verde; capturas del README regeneradas.
  - **Evidencia (2026-09-14):** las cabeceras de columna de la principal empiezan al **49 %** del alto
    (1180×820); Desinstalar muestra **4 de 113** programas a 900×700.
  - **Esfuerzo:** medio
  - **Depende de:** F-01, F-14

- [ ] **[F-18] Estados vacíos accionables y un único mensaje de arranque**
  - **Área:** UX / redacción
  - **Ubicación:** `src/WingetUSoft/UI/MainWindow.xaml:510-539` (`panelListState`), `MainWindow.xaml.cs:391-445` y `:618-633`; listas de `SearchWindow`, `UninstallWindow` y `CleanupWindow`
  - **Qué hacer:** al arrancar, la misma instrucción aparece en la línea de detalle, en el estado vacío y
    en la barra de estado, y el estado vacío no tiene botón. Añadir la acción al panel («Consultar
    actualizaciones» en el estado inicial; «Reintentar» en error y cancelado) y quitar la repetición.
    Extraer el panel como `UserControl` (igual que `ActivityLog`) y dar estados de carga, vacío y error a
    Buscar, Desinstalar y Limpieza, que hoy solo informan en la barra de estado. Opcional: un ajuste
    «Consultar al abrir».
  - **Criterio de aceptación:** la instrucción de arranque aparece una sola vez y su botón lanza la
    consulta (UI test); las otras tres listas muestran el panel de estado al cargar y al quedar vacías.
  - **Evidencia (2026-09-14):** «Pulsa "Consultar actualizaciones"» aparece **3 veces** en la pantalla
    inicial, sin botón en el panel.
  - **Esfuerzo:** medio
  - **Depende de:** ninguna

- [ ] **[F-19] Tabla usable durante consultas y lotes, con estado por fila**
  - **Área:** UX / feedback
  - **Ubicación:** `src/WingetUSoft/UI/MainWindow.xaml.cs:347-358` (`lvPackages.IsEnabled = !busy` en `:351`), `MainWindow.BatchObserver.cs:37-90` y `PackageViewModel.cs`
  - **Qué hacer:** mientras dura la operación, la tabla entera queda deshabilitada y en gris, y el avance
    solo se ve en la barra de estado y en el registro. Dejar la lista habilitada en modo lectura (con las
    casillas y el menú contextual bloqueados) y añadir a `PackageViewModel` un estado de operación —en
    cola, en curso, correcto, fallido con motivo— alimentado por `IUpgradeBatchObserver`, que ya recibe
    esos eventos, y por `ReportElevatedBatchStatus` en el lote elevado.
  - **Criterio de aceptación:** durante un lote la lista se puede recorrer y cada fila muestra su estado,
    con el motivo del fallo en un tooltip y en el nombre accesible; tests unitarios de las transiciones
    con el doble de `IWingetService`.
  - **Evidencia (2026-09-14):** `lvPackages` expone `IsEnabled=False` durante la consulta.
  - **Esfuerzo:** medio
  - **Depende de:** F-01

- [ ] **[F-20] Pulido de textos: plurales, tratamiento, abreviaturas y truncados**
  - **Área:** Redacción / i18n
  - **Ubicación:** `src/WingetUSoft/Localization/Localization.cs` (p. ej. `status.updatesFound`, `uninstall.countAll`, `history.summaryAll`, `confirm.updateBody` en `:414`, `confirm.openWingetRunBody` en `:416`, `uninstall.confirmBody` en `:495` y `list.colExcluded` en `:321`); plantillas de las tablas
  - **Qué hacer:** (1) formas de singular y plural reales, con un ayudante `L.Plural` según CLDR (en FR y
    PT-BR el 0 va en singular), en vez de «(s)» y «(es)»; (2) un solo tratamiento en español —el tú que ya
    usa casi toda la interfaz— frente al «¿Desea continuar?» y el «Verifique» de los diálogos; (3) «Excl.»
    → columna de icono con la cabecera descriptiva en un tooltip; (4) traducir la versión «Unknown» que
    emite winget; (5) tooltip en las celdas truncadas (Id y Nombre).
  - **Criterio de aceptación:** ninguna cadena contiene «(s)» ni «(es)» (test estructural); revisión del
    español sin formas de «usted»; `LocalizationTests` en verde.
  - **Evidencia (2026-09-14):** «Se encontraron 26 actualización(es) disponible(s).», «113 programa(s)» y
    «104 registro(s) cargados»; «Unknown» visible en Desinstalar; Ids truncados sin tooltip en tres tablas.
  - **Esfuerzo:** medio
  - **Depende de:** F-04

- [ ] **[F-21] Atajos donde se usan, y `Ctrl+F`**
  - **Área:** UX / teclado
  - **Ubicación:** `src/WingetUSoft/UI/MainWindow.xaml.cs:199-250` (`KeyboardAcceleratorPlacementMode.Hidden` en `:204`) y `MainWindow.xaml:98-101`
  - **Qué hacer:** los aceleradores cuelgan de `Content`, así que su tooltip saltaba al pasar el ratón por
    cualquier sitio; se ocultó, y los atajos pasaron a una línea fija de la cabecera. Colgar cada
    acelerador de su control (F5 en `btnConsultar`, Esc en `btnCancelar`…) para que el tooltip aparezca solo
    ahí, conservando las guardas de `IsTextInputFocused`; mostrar Supr en el menú contextual
    (`KeyboardAcceleratorTextOverride`); añadir `Ctrl+F` para el buscador; y retirar la línea fija.
  - **Criterio de aceptación:** cada atajo se descubre en el tooltip o el menú de su acción; un UI test
    comprueba que `Ctrl+F` enfoca `txtBuscar`; `header.shortcuts` deja de usarse.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [ ] **[F-22] Aviso de nueva versión breve**
  - **Área:** UX
  - **Ubicación:** `src/WingetUSoft/UI/MainWindow.AppUpdate.cs:26-50` y `MainWindow.xaml:102-119`
  - **Qué hacer:** la `InfoBar` llega a cargar 500 caracteres de changelog, cuando está pensada para un
    estado breve. Dejar el título y una línea, con un enlace «Ver novedades» que abra `WhatsNewDialog` con
    las notas, y sacar la `InfoBar` de la tarjeta de cabecera a lo alto del contenido.
  - **Criterio de aceptación:** el mensaje de la `InfoBar` no pasa de dos líneas y el changelog queda a
    un clic.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [ ] **[F-23] «Consultar» como `SplitButton` y un solo acento por ventana**
  - **Área:** UX
  - **Ubicación:** `src/WingetUSoft/UI/MainWindow.xaml:136-151`
  - **Qué hacer:** «Consultar actualizaciones» y «Consultar con desconocidas» son dos botones hermanos para
    la misma acción. Unirlos en un `SplitButton` con la variante en el desplegable (recordando la última
    elección) y mover el acento a «Actualizar seleccionados (N)» / «Actualizar todo» cuando hay resultados.
  - **Criterio de aceptación:** como mucho un botón con acento visible por ventana; `MainWindowTests`
    adaptados si cambia algún `AutomationId`.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

---

## 🔵 F·4 — Verificación pendiente y QA

- [ ] **[F-24] Probar y corregir en contraste alto**
  - **Área:** Accesibilidad (temas de contraste de Windows)
  - **Ubicación:** `src/WingetUSoft/Core/LogPalette.cs:27-41` y `src/WingetUSoft/UI/ActivityLog.xaml.cs:45-47`; diccionarios de F-05; barra de título de F-13
  - **Qué hacer:** el registro pinta RGB fijos de claro u oscuro según `ActualTheme`, sin mirar
    `AccessibilitySettings.HighContrast`. Con un tema de contraste activo, dejar que las líneas hereden
    `SystemColorWindowTextColor` sin perder el tipo de línea (que ahí no puede depender del color), y
    comprobar los botones y la barra de título en «Acuático» y «Desierto».
  - **Criterio de aceptación:** registro, botón de peligro y barra de título legibles en ambos temas de
    contraste (capturas); test unitario de que `LogPalette` no fuerza color con el contraste alto activo.
  - **Evidencia (2026-09-14):** **no verificado en ejecución**, porque activar un tema de contraste cambia
    la configuración del sistema del usuario. El riesgo sale del código: colores fijos (texto `#1B1B1B` en
    claro) sobre un fondo que en contraste alto puede ser negro.
  - **Esfuerzo:** medio
  - **Depende de:** F-05, F-13

- [x] **[F-25] Los UI tests deben respaldar el `settings.json` real**
  - **Área:** QA / datos del usuario
  - **Ubicación:** `tests/WingetUSoft.UiTests/SettingsBackup.cs:26` (usado en `AppFixture.cs:27` y `:50`) frente a `src/WingetUSoft/Settings/AppSettings.cs:10-12`
  - **Qué hacer:** `SettingsBackup` copia `%AppData%\WingetUSoft`, pero la app escribe en
    `%LocalAppData%\WingetUSoft`: el respaldo no encuentra nada, `Restore()` no repone nada y los UI tests
    corren contra el `settings.json` real (historial, exclusiones, idioma). Apuntar a
    `LocalApplicationData`, respaldar también `settings.json.bak` y `logs/`, y retirar `history.log`, que
    ya no aparece en ningún sitio de `src/` (el historial vive dentro de `settings.json`).
  - **Criterio de aceptación:** tras la suite de UI tests, el `settings.json` real queda idéntico byte a
    byte al de antes (comprobación por hash en el `Dispose` del fixture).
  - **Evidencia (2026-09-14):** `%AppData%\WingetUSoft` no existe; los datos están en `%LocalAppData%`.
  - **Al implementarlo:** además de la ruta, `Restore()` ahora **verifica** el resultado y lanza nombrando
    el archivo que no quedó idéntico (un respaldo que falla en silencio fue el problema original), y el
    fixture espera a que la app termine antes de restaurar: al cerrarse todavía vacía su registro a disco.
    La comprobación es byte a byte en `Restore()`, no por hash en el `Dispose`: es equivalente y más directa.
  - **Verificado (2026-09-16):** 4 tests nuevos en `SettingsBackupTests`, sin lanzar la app; el de la ruta
    falla al volver a `ApplicationData`, y el del archivo bloqueado destapó un caso real (la verificación
    lanzaba una `IOException` sin contexto). Suite de UI tests **41/41**, con la huella SHA-256 de los
    4 archivos de datos idéntica antes y después.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

---

## ⚪ F·5 — Opcional / estructural

> Como en el T4: no se aborda sin una decisión explícita.

- [ ] **[F-26] Evaluar una ventana única con `NavigationView`**
  - **Área:** Arquitectura de UI
  - **Ubicación:** las seis ventanas de `src/WingetUSoft/UI/`, `WindowChrome.cs` y `MainWindow.Menus.cs`
  - **Qué hacer:** hoy son seis ventanas de primer nivel sin ventana propietaria, cada una con su botón en
    la barra de tareas, y Desinstalar abre Limpieza por su cuenta. La forma natural de un gestor de
    paquetes es una sola ventana con `NavigationView` (Actualizaciones · Buscar e instalar · Instalados ·
    Historial, y Configuración al pie) y el control `TitleBar`. Resolvería de raíz F-08, la duplicación de
    cabeceras de F-17 y buena parte de F-14.
  - **Criterio de aceptación:** una rama de prueba que compile, pase los UI tests adaptados y tenga
    capturas; o una entrada en `CONTEXT.md` §4 que explique por qué se descarta (la misma salida que T4-01).
  - **Esfuerzo:** alto
  - **Depende de:** F-08, F-13, F-14

---

## 📊 Progreso del Tier F

> Misma regla que en la Parte II: marcar al **verificar** (build + tests + prueba real), añadir la fila
> al registro y referenciar el ID en el mensaje del commit (p. ej.
> `fix(a11y): nombre accesible en las filas de Historial (F-02)`).

### Estado por bloque

| Bloque | Total | Completadas | Pendientes | % |
|---|---:|---:|---:|---:|
| F·1 — Defectos verificados | 10 | 10 | 0 | 100 % |
| F·2 — Fluent / WinUI 3 | 6 | 6 | 0 | 100 % |
| F·3 — Experiencia de uso | 7 | 0 | 7 | 0 % |
| F·4 — Verificación y QA | 2 | 1 | 1 | 50 % |
| F·5 — Opcional | 1 | 0 | 1 | 0 % |
| **Total** | **26** | **17** | **9** | **65 %** |

### Registro de tareas completadas

| Fecha | ID | Tarea | Verificado con | Commit | Versión |
|---|---|---|---|---|---|
| 2026-09-16 | F-25 | Los UI tests respaldan el `settings.json` real | 4 tests nuevos · 41/41 UI tests · datos idénticos por SHA-256 | `9b77a78` | 1.8.9 |
| 2026-09-16 | F-01 | La tabla principal desplaza por sí misma, no la página | 325/325 unitarios · 42/42 UI tests · guard falla al revertir · lista 13,8 % y página 100 % en la app real | `9b77a78` | 1.8.9 |
| 2026-09-16 | F-02 | Nombre accesible en las filas de Historial y Desinstalar | 298/298 unitarios · guard XAML falla al revertir · 0 filas con nombre de tipo en la app real | `9b77a78` | 1.8.9 |
| 2026-09-16 | F-03 | Los diálogos genéricos siguen el tema elegido | 300/300 unitarios · guard falla al revertir · diálogo claro en la app real | `9b77a78` | 1.8.9 |
| 2026-09-16 | F-04 | Confirmaciones con verbo y Cancelar por defecto en lo destructivo | 305/305 unitarios · guard falla al revertir · foco en «Cancelar» en la app real | `9b77a78` | 1.8.9 |
| 2026-09-16 | F-05 | Botón de peligro con contraste AA, estados completos y papelera | 324/324 unitarios · guards fallan al revertir · 3 estados × 2 temas medidos en la app real | `9b77a78` | 1.8.9 |
| 2026-09-16 | F-06 | Filas atenuadas con el gris secundario, sin `Opacity` | 313/313 unitarios · guard falla al revertir · texto `#CFCFCF` en la app real | `9b77a78` | 1.8.9 |
| 2026-09-16 | F-07 | Cabeceras alineadas con las celdas | guard falla al revertir · 0 px de desfase en la app real | `9b77a78` | 1.8.9 |
| 2026-09-16 | F-08 | Una sola instancia por ventana secundaria | UI test falla contra 1.8.8 · 42/42 UI tests · 1 ventana de Historial en la app real | `9b77a78` | 1.8.9 |
| 2026-09-16 | F-09 | Menú en el icono de bandeja, con «Salir» | 314/314 unitarios · icono, clic simple, menú y «Salir» en la app real | `9b77a78` | 1.8.9 |
| 2026-09-16 | F-10 | Sin salto de maquetación al seleccionar una fila | guard estructural · 0 px de salto en la app real | `9b77a78` | 1.8.9 |
| 2026-09-17 | F-11 | Mica visible, con fondo sólido de reserva | márgenes con tinte medidos en pantalla · registro ≥ 5,07:1 sobre Mica · 338/338 unitarios | `50119e5` | 1.9.0 |
| 2026-09-17 | F-12 | Estilo moderno en los diálogos XAML | franjas `#2B2B2B`/`#202020` como el genérico · guard falla al revertir | `50119e5` | 1.9.0 |
| 2026-09-17 | F-13 | Barra de título con `PreferredTheme` | glifo correcto en claro, oscuro y en caliente · 43/43 UI tests | `50119e5` | 1.9.0 |
| 2026-09-17 | F-15 | Etiquetas reales en búsquedas y filtros | UI test nuevo en Historial · 4 guards fallan al revertir · 43/43 UI tests | `50119e5` | 1.9.0 |
| 2026-09-17 | F-14 | Estilos compartidos en `App.xaml` | 30 tarjetas en 4 estilos · capturas antes/después · 3 guards fallan al revertir · 43/43 UI tests | `7ddfd17` | 1.9.0 |
| 2026-09-17 | F-16 | Configuración con un único patrón de fila | capturas en 2 temas y 2 idiomas · Quitar y Abrir carpeta en la app real · 51/51 UI tests | `d3d634c` | 1.9.0 |

### Línea base del Tier F (2026-09-14, v1.8.8)

| Métrica | Valor | Cómo se obtuvo |
|---|---|---|
| Scroll de la tabla principal (26 filas, 1180×820) | lista `VerticallyScrollable=False` · página al 44,7 % de vista | UI Automation, `ScrollPattern` |
| Registro visible tras consultar | no (fuera de la ventana) | UI Automation, `BoundingRectangle` de `txtLogHeader` |
| Salto de la tabla al seleccionar una fila | 36 px | UI Automation, Y de `lvPackages` antes y después |
| Nombre accesible de las filas (Historial · Desinstalar) | `WingetUSoft.HistoryEntryViewModel` · `WingetUSoft.WingetPackage` | UI Automation, `Name` |
| Tema de los diálogos genéricos | no sigue el de la app | captura con Claro forzado sobre Windows oscuro |
| Contraste del botón de peligro | 3,84:1 (oscuro) · 3,34:1 (oscuro, hover) · 4,28:1 (claro, hover) | fórmula WCAG 2.x |
| Contraste de las filas atenuadas | 2,50:1 (claro) · 3,59:1 (oscuro) | fórmula WCAG 2.x sobre la tarjeta |
| Desalineación entre cabecera y celda | +16 px (1.ª columna) · −12 px (resto), en 4 tablas | capturas |
| Ventanas de Configuración abiertas a la vez | 2 | UI Automation |
| Inicio de las cabeceras de columna (principal) | 49 % del alto de la ventana | captura a 1180×820 |
| Filas visibles en Desinstalar | 4 de 113 (900×700) | captura |
| Fondo de página detrás de Mica | `#F3F3F3` / `#202020` planos | píxeles de `docs/screenshots` |
| Instrucción de arranque repetida | 3 veces | captura |
| Bloques de tarjeta repetidos | 30 en 6 XAML | `grep` sobre `src/WingetUSoft/UI` |

### Zonas que el Tier F no pudo cubrir

- **Contraste alto:** no se activó, porque cambia la configuración del sistema del usuario (ver F-24).
- **Lector de pantalla real:** igual que en la Parte II, se leyó el árbol de UI Automation; no se escuchó
  una sesión con Narrador ni NVDA.
- **Limpieza de residuos:** no se condujo; solo se abre tras desinstalar un programa de verdad (el mismo
  límite que T1-09). Lo que le afecta en F-04 y F-05 sale del código.
- **Buscar e instalar:** no se condujo en esta pasada; lo que le afecta en F-07 y F-15 sale del código y de
  `docs/screenshots/search-*.png`.
- **Lotes de actualización reales:** no se lanzó ninguno; F-19 se apoya en el código y en el estado de la
  tabla durante la consulta.
