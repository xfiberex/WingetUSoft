# Contexto del proyecto — WingetUSoft

> **Qué es este archivo.** El contexto **vivo** del proyecto: qué es, cómo está montado, qué se decidió
> y por qué, y qué pasó en cada versión. Sirve para retomar el trabajo sin releer el código (y sin
> repetir errores ya pagados). **Mantenerlo con cada cambio relevante:** actualizar §3 _Estado actual_ y
> añadir una entrada al _Registro de cambios_ (fecha absoluta). Commitearlo junto al cambio.
>
> Reparto con [`ROADMAP.md`](ROADMAP.md): allí, **qué se va a hacer** (características por tiers); aquí,
> **qué hay hecho, cómo y por qué**.

| | |
|---|---|
| **Repositorio** | https://github.com/xfiberex/WingetUSoft |
| **Versión publicada** | **1.8.7** ([release](https://github.com/xfiberex/WingetUSoft/releases/tag/v1.8.7), sin firmar) |
| **En `main`, sin publicar** | nada — `main` y el último tag coinciden |
| **Stack** | C# / .NET 10 · **WinUI 3** (Windows App SDK 1.8, unpackaged, `net10.0-windows10.0.22621.0`, min. 10.0.19041.0) · **xUnit** + **FlaUI** · Inno Setup 6 |
| **Última actualización** | 2026-08-23 |

---

## 1. Qué es

Interfaz gráfica (WinUI 3) para **gestionar software** con **winget** en Windows: **busca e instala
programas nuevos**, consulta y actualiza paquetes (individual o en lote, silencioso o interactivo),
desinstala programas, **exporta e importa la lista de paquetes** (JSON nativo de winget), exporta a
CSV/TSV, mantiene un historial y se auto-actualiza vía GitHub Releases. No gestiona discos ni
almacenamiento (eso es **FormatDiskPro**, el proyecto hermano: misma arquitectura, mismo autor).

> ⚠️ **El propósito cambió en el Tier E (2026-07-12).** Hasta la v1.7.0 era, literalmente, «gestionar
> **actualizaciones y desinstalaciones**»: la app solo tocaba lo ya instalado, y buscar/instalar software
> nuevo estaba **explícitamente fuera de alcance**. El usuario decidió ampliarlo. Si un comentario o
> documento viejo dice lo contrario, **manda esta sección**.

---

## 2. Arquitectura

**Regla de oro:** la lógica de negocio pura y testeable vive en `Core` (sin dependencias de WinUI /
`Process` / `HttpClient`); las operaciones con efectos externos (winget, red, disco) viven en `Services`.
La UI, `Services` y `Settings` consumen `Core`. Namespace único `WingetUSoft`.

```
src/WingetUSoft/
├─ Core/                     Lógica pura (sin UI ni efectos externos) — aquí van los tests
│  ├─ WingetTable.cs         Tablas de winget por posición de columna, NO por cabecera (Tier E)
│  ├─ WingetSearchParser.cs  Resultados de `winget search` (la col. "Coincidencia" es opcional)
│  ├─ SkippedVersions.cs     "Omitir esta versión": caduca sola al salir una nueva (Tier E)
│  ├─ VersionOrder.cs        Orden semántico de versiones: 1.9 < 1.10, "< x", "Unknown" (Tier C)
│  ├─ LogPalette.cs          Colores del registro por tema, con contraste WCAG AA testeado (Tier C)
│  ├─ WindowSizing.cs        Dimensionado/centrado por DPI, acotado a WorkArea (Tier B)
│  ├─ LegalText.cs           Licencia MIT y avisos de terceros embebidos en el .exe (Tier D)
│  ├─ ReleaseNotes.cs        Markdown de GitHub → texto plano (diálogo de novedades)
│  ├─ Throughput.cs          ETA de descargas y operaciones largas
│  ├─ DelimitedTextExporter.cs  Exportación CSV/TSV con neutralización de fórmulas
│  └─ Models/                WingetPackage, WingetSearchResult, WingetPackageInfo, WingetProgressInfo…
│
├─ Services/                 Efectos externos (procesos, red, disco)
│  ├─ WingetService.cs       winget: upgrade/search/install/export/import, parsing, elevación
│  ├─ WingetShowLabels.cs    Etiquetas de `winget show` en los 10 idiomas que winget traduce
│  ├─ GitHubUpdateService.cs Auto-actualización (verifica Authenticode → SHA-256)
│  └─ CleanupScanner.cs      Residuos post-desinstalación
│
├─ Settings/                 AppSettings (JSON), HistoryEntry, HistoryFilter
├─ Localization/             Cadenas ES/EN/PT/FR/IT — patrón L.T("clave")
├─ UI/                       WinUI 3
│  ├─ MainWindow             Actualizaciones (tabla, lotes, tray icon)
│  ├─ SearchWindow           Buscar e instalar del catálogo (Tier E)
│  ├─ SettingsWindow         Configuración: único hogar de las preferencias (Tier C)
│  ├─ UninstallWindow · CleanupWindow · HistoryWindow
│  ├─ AboutDialog · LegalTextDialog · WhatsNewDialog
│  └─ WindowSizer · WrapPanel · Notifier · TaskbarProgress · Converters · helpers
│
└─ installer/                Inno Setup (installer.iss) + build-installer.ps1 → Output/ (gitignored)

tests/WingetUSoft.Tests/     Unitarios (xUnit) sobre Core/Services/Settings
tests/WingetUSoft.UiTests/   E2E sobre la app real (FlaUI + UIA3); los ejecuta release.ps1
tools/                       capture-screenshots.ps1 (regenera las capturas del README)
docs/screenshots/            Capturas usadas en el README
release.ps1                  Corte de versión en un paso (tests + instalador + tag + GitHub Release)
```

---

## 3. Estado actual

| | |
|---|---|
| **Build** | 0 advertencias / 0 errores (`dotnet build WingetUSoft.slnx`) |
| **Tests unitarios** | **292/292** |
| **UI tests (FlaUI)** | **37/37** — los corre `verify.ps1 -Full`, y `release.ps1` a través de él: un release no sale si la app real no pasa |
| **Tiers** | A, B, C, D y E **completados**. En curso: el plan de auditoría de [`ROADMAP.md`](ROADMAP.md) (Parte II, T0-T4): **70 de 70 — completo** |
| **Publicado** | hasta la **v1.8.7**. `main` y el último tag coinciden |

**Tiers, de un vistazo** (detalle en [`ROADMAP.md`](ROADMAP.md); el porqué, en el Registro de cambios):

| Tier | Qué trajo | Versión |
|---|---|---|
| **A** | Paridad de infraestructura con FormatDiskPro: localización (5 idiomas), novedades/changelog, avisos y progreso en taskbar, ETA, historial con filtros, *Acerca de* + licencia MIT, pipeline de release | 1.3.0 |
| **B** | Layout: DPI/WorkArea, tamaño mínimo, barra responsiva, snap layouts + proyecto de UI tests (FlaUI) | 1.4.0/1.4.1 |
| **C** | Auditoría UI/UX: flujo y feedback, modelo de selección, datos que mentían, color, preferencias unificadas, accesibilidad de la tabla | 1.5.0/1.6.0 |
| **D** | Cara pública: licencia y avisos de terceros **in-app**, README de usuario con capturas, script de capturas | 1.7.0 |
| **E** | ⚠️ **Cambio de alcance**: buscar e instalar software, exportar/importar paquetes, omitir versión | 1.8.0 |

**Los UI tests** necesitan **sesión de escritorio interactiva y desatendida** (no vale sesión bloqueada ni
consola sin escritorio: ahí, `-SkipUiTests`), pero **no** elevación — la app corre `asInvoker`.

---

## 4. Decisiones y convenciones clave

- **Namespace único** `WingetUSoft` para app y tests.
- **Comentarios y documentación XML en español**, con acentos y signos de apertura (`¿`, `¡`). En
  inglés van solo los identificadores, las cadenas de la API de Windows y los términos técnicos que no
  se traducen (*named pipe*, *snap layout*, *hash*). La mayoría del repositorio ya era así; T3-06 alineó
  los bloques sueltos que quedaban en inglés (`CleanupScanner`, la verificación Authenticode, los
  lectores de stdout) y T3-05 devolvió las tildes a `WingetShowLabels` y a `installer.iss`.
  El **estilo del código** lo fija `.editorconfig` (T3-09), no la disciplina: `verify.ps1` comprueba las
  categorías `style` y `analyzers` de `dotnet format`. La categoría `whitespace` queda fuera a
  propósito: el repositorio alinea en columnas (constantes, campos de structs interop, el diccionario de
  traducciones) y `dotnet format` querría colapsar esa alineación — 290 avisos que no arreglan nada.
- **Tests: xUnit** (migrado desde MSTest en Tier A #0). No reintroducir MSTest/NUnit/TUnit.
- **Localización:** patrón `L.T("clave", args…)`, diccionario clave → `string[5]` (ES/EN/PT/FR/IT). Todo
  string de UI pasa por ahí. El idioma del sistema solo se detecta en el **primer arranque** (sin
  `settings.json` previo); después manda la elección del usuario.
  ⚠️ **`L.T` devuelve la propia clave si no la conoce** — un error de tipeo no rompe el build ni los
  tests, se ve como texto raro en la UI. `LocalizationUsageTests` escanea el código y lo caza (Tier E).
- **Nunca parsear la salida de winget por el nombre de la cabecera ni por el texto de una etiqueta:**
  winget **traduce su salida** al idioma de Windows. Las tablas se parsean por **posición de columna**
  (`Core/WingetTable`) y las etiquetas de `winget show` tienen su tabla de 10 idiomas
  (`Services/WingetShowLabels`, generada con un script desde el `.msixbundle` oficial). Aprendido a la
  mala en el Tier C #3.
- **`ConfigureAwait(false)` en toda la capa `Services`.** No toca la UI —habla con winget, la red y el
  disco—, así que no necesita volver al hilo de la interfaz después de un await, y hacerlo encola una
  continuación que compite con el repintado. En `UI` es al revés: ahí **no** se configura, porque el
  código de después del await sí toca controles. Lo impone el analizador **CA2007**, activado como
  advertencia en `src/WingetUSoft/Services/.editorconfig`, y `verify.ps1` compila con `-warnaserror`:
  un await nuevo sin configurar en esa carpeta rompe el build. Única excepción, con `#pragma` y motivo
  al lado: las declaraciones `await using` con tipo explícito, donde `.ConfigureAwait(false)` devuelve
  un `ConfiguredAsyncDisposable` y no compila.
- **`winget pin` NO sirve para "omitir esta versión":** sus anclajes congelan el paquete también para
  las versiones futuras. Se resuelve en la app (`Core/SkippedVersions`). Ver Tier E.
- **Elevación:** la app corre **`asInvoker`**; los lotes elevados usan un **worker interno con named
  pipe** (sin scripts temporales en disco). Ver `Services/WingetService.cs`.
- **Auto-actualización:** `GitHubUpdateService` **verifica el instalador antes de ejecutarlo**: firma
  Authenticode válida si la hay; si no, **SHA-256** contra el asset `...exe.sha256` del release. Sin
  ninguna de las dos: lo borra y aborta. **Todo release debe subir el `.sha256`.**
- **Exportación CSV/TSV:** `Core/DelimitedTextExporter` neutraliza fórmulas (prefijo `'` ante `=`/`+`/
  `-`/`@`) — seguro de abrir en Excel/Calc.
- **Publicación:** `dotnet publish -r win-x64 --self-contained false` (**framework-dependent**, pese a
  `WindowsAppSDKSelfContained=true`: esa propiedad empaqueta el Windows App SDK, no el runtime de .NET).
  El instalador detecta y descarga **VC++ Redist** y **.NET 10 Desktop Runtime** solo si faltan.
- **Windows App SDK: se queda en 1.8 — la 2.x está descartada (T4-01, probado el 2026-08-23).** No es
  desconfianza a ojo: se hizo el spike completo en una rama y **funciona** (compila con cero
  advertencias, arranca y pasa los 37 UI tests). Se descarta por lo que cuesta, no por lo que rompe.

  El único incentivo que la tarea le veía era que 2.x reorganiza el empaquetado *self-contained* y
  podría ahorrar los ~41 MB del runtime de IA sin usar. Ese incentivo **ya no existe**: eso lo resolvió
  T2-03 sobre la 1.8. Y en 2.4.0 el arreglo de T2-03 **deja de aplicarse**, así que el peso vuelve:

  | `publish` (Release, win-x64, framework-dependent) | Tamaño |
  |---|---:|
  | Windows App SDK 1.8.260317003 (hoy) | **101 MB** |
  | Windows App SDK 2.4.0 | **150 MB** |

  En 2.4.0 reaparecen en el publish `onnxruntime.dll`, `DirectML.dll` y toda la familia
  `Microsoft.Windows.AI.*`; en 1.8 solo quedan ~0,5 MB de DLLs de proyección pequeñas. Son **+49 MB que
  cada usuario se descargaría en cada actualización** a cambio de ninguna función nueva.

  La causa está localizada: el `Target RemoveUnusedAIRuntime` del `.csproj` engancha en
  `AddMicrosoftWindowsAppSDKPayloadFilesFromMsix;AddMicrosoftWindowsAppSDKPayloadFilesFromComponents`, y
  en los targets de la 2.x **el primero ya no existe** (solo sobrevive `…FromComponents`). MSBuild ignora
  en silencio un `AfterTargets` que nombra un target inexistente, así que el filtro deja de correr sin
  avisar de nada.

  **Qué haría falta para reabrirlo:** rehacer T2-03 contra la composición de carga útil de la 2.x y
  volver a medir el `publish`. Mientras 1.8 siga recibiendo servicing y la app no use Windows AI
  Foundation, la migración es coste sin retorno.
- **Instalador (Inno Setup):** `AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}` — **no cambiar nunca**
  (permite actualización in-place). `PrivilegesRequired=admin`, `CloseApplications=yes`. **Único
  empaquetador**: nada de MSIX/ClickOnce.
- **Versionado:** fuente única en `WingetUSoft.csproj` `<Version>` (hoy `1.8.7`); `release.ps1` sube
  `<Version>`, `<AssemblyVersion>` y `<FileVersion>` a la vez — la app y el updater leen `AssemblyVersion`.
- **Scripts PowerShell con acentos o `—`: guardar siempre con BOM UTF-8.** Windows PowerShell 5.1 asume
  el codepage ANSI para `.ps1` sin BOM y el tokenizer se rompe ("Falta el paréntesis de cierre"). Mismo
  hallazgo que documenta FormatDiskPro.
- **`release.ps1` solo hace `git add -u`** (archivos ya rastreados): los archivos **nuevos** hay que
  `git add`earlos **antes**, o el release saldría sin ellos.
- **PowerShell 5.1 convierte el stderr de un exe nativo en error terminante** cuando la salida del
  script se redirige o se canaliza y `$ErrorActionPreference` vale `Stop` — aunque el exe termine con
  código 0. Y git escribe *siempre* en stderr. Fue lo que mató el release de la v1.8.6 entre el push de
  la rama y el del tag. **Ya está resuelto de raíz:** `release.ps1` y `verify.ps1` envuelven cada
  llamada nativa en `Invoke-Native`, así que ya se pueden invocar redirigiendo la salida. Si se añade
  una llamada a `git`, `gh` o `dotnet` a cualquiera de los dos, tiene que ir dentro del envoltorio.

---

## 5. Tareas comunes

| Tarea | Comando |
|-------|---------|
| Compilar | `dotnet build WingetUSoft.slnx` |
| Ejecutar | `dotnet run --project src/WingetUSoft/WingetUSoft.csproj` |
| Tests unitarios | `dotnet test tests/WingetUSoft.Tests/WingetUSoft.Tests.csproj` |
| Tests de UI (app real) | `dotnet test tests/WingetUSoft.UiTests/WingetUSoft.UiTests.csproj` |
| Instalador | `src\WingetUSoft\installer\build-installer.ps1` (`-CertThumbprint <huella>` para firmar) |
| Capturas del README | `.\tools\capture-screenshots.ps1` |
| **Verificar el repositorio** | `.\verify.ps1` (rápida) · `.\verify.ps1 -Full` (añade los UI tests) |
| Activar el hook de pre-push | `git config core.hooksPath .githooks` (una vez por clon) |
| **Publicar versión** | `.\release.ps1 -Version X.Y.Z` (`-DryRun` para simular) |

`release.ps1`: validar → **`verify.ps1 -Full`** → bump `<Version>` → instalador (+ `.sha256`) → commit
+ tag `vX.Y.Z` → push → `gh release create` con **los dos assets**. Flags: `-DryRun`, `-SkipTests`,
`-SkipUiTests`, `-AllowDirty`, `-NotesFile`, y los de firma. Usar cualquiera de los tres primeros deja
constancia en las notas publicadas del release.

**Qué significa «verificado» lo define `verify.ps1`, en un solo sitio**: build con `-warnaserror`,
unitarias, dependencias vulnerables (aborta) y desactualizadas (informa), y con `-Full` los UI tests.
Lo comparten el hook de pre-push y `release.ps1`.

---

## 6. Pendientes / ideas

- **Plan de auditoría de [`ROADMAP.md`](ROADMAP.md) Parte II: 70 de 70, cerrado.** No queda nada
  pendiente del plan. Lo próximo que se aborde vendrá de uso real o de reportes, no de la auditoría.
  Recordar en cada corte: el release sube **dos assets** (`.exe` + `.sha256`); sin el `.sha256`, la app
  no puede verificar un instalador sin firmar y **rechaza la actualización**. Recordar en cada corte:
  el release sube **dos assets** (`.exe` + `.sha256`); sin el `.sha256`, la app no puede verificar un
  instalador sin firmar y **rechaza la actualización**.
- **Certificado de firma de código (OV/EV) — descartado por ahora.** Consecuencia asumida: SmartScreen
  dice "editor desconocido" en cada instalación, y la verificación se apoya en el SHA-256 (detecta
  manipulación en tránsito, no un compromiso de la cuenta de GitHub). El pipeline ya lo soporta si algún
  día hay certificado (`build-installer.ps1 -CertThumbprint …`; el updater prefiere la firma sobre el hash).
- **CI (GitHub Actions) — adoptado el 2026-09-16 con el repositorio ya público**, tras descartarse dos veces
  (2026-07-12 y al cerrar T2-12). El workflow ejecuta el mismo `verify.ps1` sin `-Full`, así que la
  definición de «verificado» sigue viviendo en un solo sitio. Lo acompañan CodeQL, Dependabot, secret
  scanning y el reporte privado de vulnerabilidades. Los UI tests siguen siendo locales (`-Full`, hook de
  pre-push y `release.ps1`): un runner hospedado no tiene el escritorio interactivo que necesitan.
- **Notificación toast — descartada.** El aviso ya existe (sonido + parpadeo de la barra de tareas +
  progreso en el icono), y en una app unpackaged el toast exige registrar un servidor COM del
  `AppNotificationManager`: mucha fontanería para un beneficio marginal. **No quedan ideas abiertas.**

---

## 7. Cómo mantener este documento

1. Tras un cambio relevante: entrada nueva en el **Registro de cambios** (fecha absoluta) + actualizar §3.
2. Si cambia una convención o decisión, reflejarlo en §4 (es la sección que evita repetir errores).
3. Marcar el ítem como ✅ en [`ROADMAP.md`](ROADMAP.md) cuando esté **verificado** (build + tests + prueba
   real), no cuando esté escrito.
4. Commitear este archivo **junto** con el cambio, para que el contexto viaje entre equipos.

---

# Registro de cambios

| Fecha | Versión | Qué |
|---|---|---|
| 2026-09-17 | **1.9.0** | **Tier F · bloque F·2 completo** — Mica visible, diálogos y barra de título modernos, estilos compartidos, etiquetas reales en búsquedas y filtros, y Configuración rehecha con un único patrón de fila |
| 2026-09-17 | **1.8.9** | **Tier F · bloque F·1 completo** — la bandeja por fin funciona (icono y menú con «Salir»), la tabla desplaza por sí misma, confirmaciones con verbo, botón de peligro AA y diálogos con el tema elegido; CI, CodeQL y Dependabot en GitHub |
| 2026-08-23 | **1.8.8** | **Tier T4 y cierre del plan de auditoría** — MainWindow dividido, el flujo de lotes por fin probable, cobertura en local, y SECURITY/CONTRIBUTING/CHANGELOG |
| 2026-08-22 | **1.8.7** | **Tier T3 completo** — el ajuste de notificaciones ya no oculta el resumen del lote, arreglo del flujo de auto-actualización, `.editorconfig` con comprobación de estilo y diagnósticos que por fin van a algún sitio |
| 2026-08-22 | **1.8.6** | **Tier T2 completo** — filtros accesibles y que envuelven, fechas y nombres de archivo por idioma, `verify.ps1` + hook de pre-push, y pruebas del protocolo del worker elevado |
| 2026-08-21 | **1.8.5** | **Rendimiento (T2)** — instalador −37 % (fuera el runtime de IA sin usar), registro fuera del hilo de UI, purga de logs, caché en la búsqueda |
| 2026-08-21 | **1.8.4** | **Auditoría T1 completo** — TOCTOU del instalador, registro de fallos que no se pisa, fallos clasificados por código, accesibilidad e i18n |
| 2026-08-21 | **1.8.3** | **Auditoría T0 + T1** — recorrido de rutas en la limpieza, guardado atómico de la configuración, contraste WCAG AA en las 4 ventanas, botones de diálogo localizados |
| 2026-07-21 | **1.8.2** | Icono en la barra de título y título simplificado; titular de copyright con nombre legal (app, LICENSE e instalador) |
| 2026-07-20 | **1.8.1** | Estado vacío real en Historial, pulido de layout en 4 ventanas y capturas de pantalla para el README |
| 2026-07-12 | **1.8.0** | **Tier E** — buscar/instalar, exportar/importar, omitir versión (⚠️ cambio de alcance) |
| 2026-07-12 | **1.7.0** | **Tier D** — licencia y avisos de terceros in-app, README de usuario, capturas |
| 2026-07-12 | **1.6.0** | **Tier C #4–#6** + fix de la auto-actualización rota desde 1.4.1 |
| 2026-07-11 | **1.5.0** | **Tier C #1–#3** — flujo/feedback, selección, datos que mentían |
| 2026-07-11 | **1.4.1** | Snap layouts (Tier B #7) + 3 bugs del flujo instalar/actualizar |
| 2026-07-10 | **1.4.0** | **Tier B** — layout adaptable, accesibilidad y UI tests con FlaUI |
| 2026-07-09 | **1.3.0** | **Tier A** completado — paridad con FormatDiskPro + pipeline de release |

---

### 2026-09-17 — Tier F, bloque F·2: alineación con Fluent y WinUI 3 (F-11 a F-16, release v1.9.0)

**17 de 26** del Tier F, con F·1 y F·2 completos. Build 0/0, **343/343 unitarios**, **51/51 UI tests**. Salto de
versión menor y no de parche: cambia cómo se ve toda la app y Configuración se rehace entera.

**1. Mica de verdad (F-11).** Se pedía `MicaBackdrop` y la raíz de cada ventana lo tapaba con
`ApplicationPageBackgroundThemeBrush`, que es opaco. Ahora la raíz pasa a transparente solo si
`MicaController.IsSupported()`: el pincel sólido del XAML queda como reserva para Windows 10. El riesgo estaba en
el registro, cuyo contraste se calculaba contra un fondo fijo. Medido en pantalla, la tarjeta apenas se mueve
(`#FBFBFB`–`#FDFBFB`, `#2B2B2B`–`#2D2A2A`), y el test exige ya 4,5:1 contra fondos más desfavorables que lo medido.
El aviso en tema claro es el color con menos margen: deja de cumplir por debajo de `#EDEDED`.

**2. Diálogos y barra de título (F-12, F-13).** El estilo implícito de `ContentDialog` no alcanza a las subclases
definidas en XAML, así que Acerca de, Novedades y Licencia usaban la plantilla antigua con parches de esquinas.
`TitleBarHelper` pintaba a mano blanco o negro en cada estado y cada ventana tenía que acordarse de llamarlo. Lo
sustituye `AppWindow.TitleBar.PreferredTheme`, fijado desde el tema **resuelto** del contenido, porque con «el del
sistema» el ajuste no dice si la ventana es clara u oscura.

**3. Estilos compartidos (F-14).** El bloque de tarjeta estaba 30 veces con cinco rellenos. `UI/Styles.xaml` lo
reduce a cuatro estilos, con radios del sistema y espaciados en la rampa de 4 px. Se comprobó con capturas antes
y después, píxel a píxel: las diferencias empiezan exactamente en el primer espaciado normalizado. El botón de
peligro sigue siendo un diccionario y no un `Style`, por lo mismo que en F-05.

**4. Etiquetas y Configuración (F-15, F-16).** Búsquedas y filtros de Historial, Desinstalar y Buscar e instalar
toman el nombre de su etiqueta visible. Configuración mezclaba cuatro patrones de rotulado y pasa a uno:
`SettingsCard`, una fila con título y descripción a la izquierda y control a la derecha, sin la dependencia del
Community Toolkit. Las descripciones salen del código y no de lo que se suponía: `--silent`, un proceso elevado
por lote en el que Cancelar no detiene el programa en curso, avisos solo en operaciones de 10 s o más, y 30 días
de registros. La prueba en la app destapó dos defectos:
- La fila también nombraba los botones desde su título, así que «Limpiar lista» se anunciaba con el título de la
  fila.
- Los interruptores decían «Activado» con la app en inglés, porque WinUI toma ese texto del idioma de Windows.
  Pasaba ya en 1.8.9.

**Capturas del README** regeneradas con `tools/capture-screenshots.ps1`: las anteriores mostraban la Configuración
vieja, los diálogos sin franja de botones y el fondo sin Mica.

---

### 2026-09-17 — Tier F, bloque F·1: defectos verificados en la app real (F-01 a F-10 y F-25, release v1.8.9)

**11 de 26** del Tier F ([`ROADMAP.md`](ROADMAP.md) Parte III), con el bloque F·1 completo. Build 0/0,
**326/326 unitarios**, **42/42 UI tests**. Cada defecto se reprodujo primero conduciendo la v1.8.8, y cada
guard nuevo se hizo fallar contra el código anterior antes de darlo por bueno.

**1. La bandeja nunca funcionó (F-09).** Con «Minimizar a la bandeja al cerrar» activo, cerrar ocultaba la
ventana y el proceso seguía vivo **sin icono**: no quedaba otra salida que el Administrador de tareas. Lo
destapó la prueba manual del menú nuevo, no un test. El `TaskbarIcon` se creaba desde código, fuera del
árbol XAML, y H.NotifyIcon solo lo registra en el `Loaded` o con `ForceCreate`, que nunca se llamaba. Ahora
se crea sin modo eficiencia (la consulta automática sigue en segundo plano) y tiene menú Abrir · Consultar
actualizaciones · Salir. Sus entradas llevan `Command` y no `Click`, porque el menú nativo de H.NotifyIcon
solo ejecuta el comando.

**2. La tabla desplaza por sí misma (F-01).** El `ScrollViewer` de página medía con alto infinito: la fila
`*` se comportaba como `Auto`, la lista se medía entera, sin scroll propio ni virtualización, y empujaba el
registro fuera de la ventana. `Height` atado al viewport y un `MinHeight` calculado desde las tarjetas
superiores conservan el scroll de página solo donde hace falta (snap de cuarto de pantalla).

**3. Diálogos y confirmaciones (F-03, F-04).** Los diálogos genéricos no seguían el tema forzado de la
app: `ContentDialog` no hereda el `RequestedTheme` de la ventana, y ahora todos pasan por
`WindowDialogHelper.Prepare`. Las confirmaciones dicen la acción («Desinstalar», «Eliminar») en lugar de
«Sí / No», y en lo destructivo el botón por defecto es Cancelar, así que Intro ya no borra.

**4. Contraste y accesibilidad (F-02, F-05, F-06).** El botón de peligro bajaba a 3,34:1; ahora es un
diccionario compartido con reposo, hover y pulsado ≥ 4,5:1 en los dos temas, alto contraste con los
colores del sistema y papelera para no depender del color. No es un `Style`: la plantilla de `Button` lee
los recursos de cada estado directamente, y un `Style` solo alcanza el reposo. Las filas excluidas pasan de
`Opacity 0.4` (2,50:1) al gris secundario del tema, y las filas de Historial y Desinstalar dejan de
anunciarse como un nombre de tipo .NET.

**5. Pulido de tablas y ventanas (F-07, F-08, F-10).** Cabeceras alineadas con sus celdas (el
`ListViewItem` traía 16/12 px de relleno), una sola instancia por ventana secundaria y ningún salto de
36 px al seleccionar una fila.

**6. Los UI tests respaldaban una carpeta que no existe (F-25).** Corrían sobre el `settings.json` real del
usuario. Ahora respaldan `%LocalAppData%\WingetUSoft`, restauran después de que la app termine de vaciar su
registro y fallan si algo no queda idéntico byte a byte.

**Fuera de la app:** con el repositorio ya público se reabrió la decisión sobre CI (§4): `verify.ps1` sin
`-Full` en GitHub Actions, CodeQL `security-extended`, Dependabot y secret scanning. Dependabot resultó casi
ciego para NuGet con un proyecto `net10.0-windows`, así que los parches de esta versión (Windows App SDK
1.8.260804001, SDK.BuildTools 10.0.28000.2705, Test.Sdk 18.10.1) se subieron a mano. CodeQL destapó que
ningún test comprobaba que las cinco traducciones usen los mismos marcadores de formato; ya lo hay.

---

### 2026-08-23 — Tier T4 y cierre del plan de auditoría (release v1.8.8)

**70 de 70.** El plan de auditoría de [`ROADMAP.md`](ROADMAP.md) Parte II queda completo. Build 0/0,
**292/292 unitarios**, **37/37 UI tests**, cobertura **25,2 %** de líneas.

Ninguna de estas tareas cambia lo que el usuario ve, salvo la documentación: es un corte de estructura
y de herramienta. Los 37 UI tests pasan igual antes y después, que es exactamente lo que se le pide a un
refactor.

**1. `MainWindow.xaml.cs` deja de ser un archivo de 2 115 líneas (T4-02).** Acumulaba una decena de
responsabilidades. Se reparte en parciales por tema —lote, menús, tabla, registro, auto-actualización,
bandeja— y `PackageViewModel` sale a su propio archivo. Ningún archivo de `UI/` pasa ya de **745**
líneas. No se movió una sola línea de lógica: el reparto lo hizo un script que recorta bloques enteros,
para que cualquier diferencia de comportamiento fuera un fallo del recorte y no una decisión escondida.
De paso, `AppSettings.Load()` sale del inicializador de campo: ahí la E/S ocurría **antes** del cuerpo
del constructor y de `InitializeComponent`, así que un fallo de disco reventaba con la ventana a medio
construir y sin dónde mostrar el aviso.

**2. El corazón del producto por fin se puede probar (T4-03).** El bucle que actualiza un lote —cuenta
aciertos, clasifica fallos y decide cuándo parar— vivía dentro de `MainWindow`, atado a la barra de
progreso y llamando a la clase estática `WingetService`. La única forma de ejercitarlo era arrancar la
app con FlaUI y dejar que lanzara procesos de verdad. Ahora es `Core/UpgradeBatchRunner`, que habla con
`IWingetService` y cuenta lo que pasa por `IUpgradeBatchObserver`; la ventana implementa el observador.
**14 tests nuevos** cubren el camino feliz, los fallos que no detienen el lote, y las dos formas de
cancelar: la excepción y el token ya cancelado al volver de winget.

Un detalle que los tests destaparon: `Progress<T>` **no** invoca al observador en el acto, sino que lo
publica en el `SynchronizationContext` capturado. En la app ese contexto es la cola de la UI, que es
justo lo que hace falta porque winget reporta desde un hilo de fondo. En un test no hay contexto y los
callbacks llegaban tarde. Se resolvió instalando un contexto síncrono **en el test**, no relajando el
código: el despacho asíncrono es correcto y necesario.

**3. Cobertura medida, en local y sin puerta (T4-04).** `verify.ps1 -Full` recolecta con
`coverlet.collector` en la misma pasada de tests y genera un HTML navegable en `coverage/report`, con
`reportgenerator` declarado como herramienta local. Base: **25,2 %** global, que es un número honesto y
poco informativo por sí solo — `UI/` no lo puede cubrir un test unitario. Lo que importa es el desglose:
`Core` y `Settings` van del **83 % al 100 %** (`HistoryFilter`, `LogPalette`, `ReleaseNotes`,
`Throughput`, `WindowSizing` y `UpgradeBatchRunner` al 100 %). **No hay umbral mínimo a propósito**: un
mínimo invita a escribir tests que tocan líneas sin comprobar nada, que es peor que no tenerlos.

**4. Windows App SDK 2.x: probado y descartado (T4-01).** El spike completo está hecho y **funciona**
—compila sin advertencias y pasa los 37 UI tests—, pero el `publish` sube de **101 a 150 MB** porque el
arreglo de T2-03 deja de aplicarse: en los targets de la 2.x ya no existe
`AddMicrosoftWindowsAppSDKPayloadFilesFromMsix`, y MSBuild ignora en silencio un `AfterTargets` que
nombra un target inexistente. El razonamiento y lo que haría falta para reabrirlo, en §4.

**5. Cara pública del repositorio (T4-05) y vuelta atrás (T4-06).** `SECURITY.md` con canal privado de
reporte y alcance explícito —qué cuenta como fallo y qué es una limitación ya declarada—, más
`CONTRIBUTING.md` y un `CHANGELOG.md` que apunta a los dos registros que ya existen en vez de crear un
tercero. El README explica cómo volver a una versión anterior y por qué funciona (`AppId` fijo y
`UsePreviousAppDir`), que los datos de usuario sobreviven, y que el aviso de actualización reaparecerá
porque la propia app no tiene «omitir versión». Con ello suben cinco dependencias con salto mayor
pendiente, incluida `H.NotifyIcon.WinUI` 2.2.0 → 2.4.1.

---

### 2026-08-22 — Tier T3 cerrado: el estilo deja de depender de la disciplina (T3-01 a T3-17, release v1.8.7)

**63 de 70.** T0, T1, T2 y T3 completos; solo queda T4, que no se abre sin decisión explícita.

**1. `.editorconfig`, y `verify.ps1` que lo hace cumplir (T3-09).** El estilo era consistente por
disciplina del autor, no por herramienta. Ahora está codificado —el que ya estaba en uso, más las dos
reglas de encoding que este proyecto pagó caras: BOM en los `.ps1` y LF en `.githooks/`— y `verify.ps1`
comprueba las categorías `style` y `analyzers` de `dotnet format`.

> **La categoría `whitespace` queda fuera, y es una desviación del criterio literal.** Produce 290
> avisos en 14 archivos, todos lo mismo: quiere colapsar la **alineación en columnas** que el
> repositorio usa a propósito —las constantes de `Notifier`, los structs de interop, y 211 de los 290 en
> el diccionario de traducciones—. No hay opción que la permita: o se acepta el aviso o se destruye el
> estilo del autor en un diff que no arregla nada.

**2. La convención de idioma de los comentarios no estaba escrita (T3-05, T3-06).** Ahora sí, en §4:
español con acentos; en inglés solo identificadores, cadenas de la API de Windows y términos que no se
traducen. Alineados los bloques sueltos que quedaban en inglés y devueltas las tildes a
`WingetShowLabels` y a `installer.iss`.

> Detalle que evitó romper algo: los comentarios de idioma de `WingetShowLabels` están alineados en
> columna y todos los reemplazos conservan la longitud (ingles→inglés, aleman→alemán…). Y el `.iss` se
> deja en UTF-8 **sin BOM** porque se comprobó contra el artefacto real: el `CompanyName` del instalador
> de la v1.8.6 lee «Jiménez» bien.

**3. Dos cosas que no eran cosmética (T3-08, T3-11).** El ajuste «Mostrar notificaciones» regía un método
que solo escribe en la barra de estado, así que apagarlo **borraba también el resumen del lote** — que no
es una notificación, es el resultado de lo que el usuario acaba de pedir. Y
`LnkDescargarUpdate_Click`, un `async void`, se llamaba como método desde la confirmación de «Buscar
actualización»: el `finally` que rehabilita el menú corría con la descarga aún en marcha y las
excepciones se escapaban del `try` del llamador.

**4. La política de `ConfigureAwait` la impone ahora un analizador (T3-10).** La seguían 12 de 47 awaits
de `Services`. En vez de aplicarla a mano, se activa **CA2007** como advertencia en un `.editorconfig`
propio de esa carpeta y `dotnet format analyzers` hace el cambio; como `verify.ps1` compila con
`-warnaserror`, un await nuevo sin configurar rompe el build. El ámbito es la carpeta porque en `UI` lo
correcto es lo contrario.

> El corrector automático rompió tres `await using` con tipo explícito —`.ConfigureAwait(false)` devuelve
> un `ConfiguredAsyncDisposable`, que no es un `Stream`—. Quedan sin configurar, con `#pragma` y motivo.

**5. Lo que se diagnosticaba no iba a ninguna parte (T3-12).** Los cinco `Trace` de `AppSettings` se
emitían sin listener: en Release, al vacío. Ahora van a `crash.log` con fecha y marca `[diagnóstico]`,
en el mismo archivo que los fallos no controlados, porque el valor está en el orden — saber que la purga
de logs venía fallando desde antes del error que sí se notó.

**6. Y dos de higiene (T3-13, T3-17).** Los tests del escáner creaban carpetas reales en
`%LOCALAPPDATA%`; ahora el escáner recibe sus seis directorios base como parámetro y la suite entera
escribe solo bajo `Path.GetTempPath()`. El árbol de trabajo baja de **2,2 GB a 1,8 GB** quitando las dos
copias de `publish` y los cuatro instaladores históricos; se conserva el de la versión publicada.

**Verificado:** `.\verify.ps1 -Full` en verde — build 0/0, **279/279** unitarias, **37/37** UI tests.
T3-07, T3-09, T3-10 y T3-16 comprobados saboteando su arreglo.

> **Y un sabotaje corrigió el propio test, que es lo más útil que salió de aquí.** El primer test de
> T3-07 exigía solo que el nombre accesible «no estuviera vacío» y **pasaba con el arreglo quitado**: sin
> nombre propio, WinUI le deduce uno del `PlaceholderText` y el control reporta «Nombre o Id...», que es
> un ejemplo de qué escribir, no una etiqueta — un lector de pantalla anunciaría el ejemplo como si fuera
> el nombre del campo. Para cualquier otro `TextBox`: comprobar que el nombre accesible *existe* no
> demuestra nada.

---

### 2026-08-22 — El release de la v1.8.6 murió a mitad: `NativeCommandError` en PowerShell 5.1 (release v1.8.7)

La v1.8.6 se publicó, pero `release.ps1` no llegó al final por su cuenta: reventó justo después de
`git push origin main`, con la rama ya subida y el tag sin subir. Hubo que rematar a mano el push del tag
y el `gh release create`.

**Qué pasó.** En PowerShell 5.1, cuando la salida del script se canaliza o se redirige
(`.\release.ps1 ... 2>&1 | ...`, que es como lo invoca cualquier automatización), un ejecutable nativo
que escriba en stderr se convierte en un **`NativeCommandError` terminante** si `$ErrorActionPreference`
vale `Stop` — **aunque su código de salida sea 0**. Y git escribe *siempre* en stderr: el informe del
push (`To https://github.com/...`), el progreso, y la salida de los hooks. No es que fallara el push:
falló el hecho de que informara de su éxito.

> El script ya conocía la trampa —tenía un baile manual de `$ErrorActionPreference` alrededor de
> `gh auth status` y de `git credential fill`, con su comentario— pero solo en los dos sitios donde
> alguien la había sufrido antes. Lo que faltaba era generalizarla.

**El arreglo.** Un `Invoke-Native { ... }` en ambos scripts que baja `$ErrorActionPreference` a
`Continue` solo mientras dura la llamada nativa. Los dos comprueban `$LASTEXITCODE` explícitamente
después de cada una, así que el modo `Stop` no aporta nada ahí y sí rompe; se mantiene para los cmdlets,
que es donde sí protege. Envueltas todas las llamadas a `git`, `gh`, `dotnet` y a los scripts hijos.

**Verificado (2026-08-22):** reproducido y corregido en el mismo banco de pruebas. Con
`$ErrorActionPreference = "Stop"` y la salida canalizada, `git push --dry-run` **lanza**
`NativeCommandError`; con el envoltorio, la misma llamada sigue adelante y devuelve 0. Y `verify.ps1`
completo, invocado con `2>&1 |` —la forma exacta que falló— termina en verde.

---

### 2026-08-22 — Tier T2 cerrado: verificación local, protocolo del worker y documentación (T2-12 a T2-18, release v1.8.6)

Cuarto corte del Tier T2 y último: **46 de 70** tareas. **T0, T1 y T2 completos.**

**1. «Verificado» pasa a estar definido en un solo sitio (T2-12, T2-13).** Hasta hoy la verificación
completa solo ocurría al cortar una versión, o si alguien se acordaba de lanzar `dotnet test`: entre
release y release, un test roto podía vivir en `main` sin que nada lo señalara. La solución **no es CI**
—decisión cerrada, y reafirmada aquí— sino `verify.ps1`: build con `-warnaserror`, unitarias, dependencias
vulnerables (aborta) y desactualizadas (informa), y con `-Full` los UI tests. Lo invocan el hook
`.githooks/pre-push` y `release.ps1`, que ya no repite ninguno de esos pasos.

> Lo interesante del reparto: lo que un runner hospedado **nunca** podría correr —los UI tests, que
> necesitan escritorio interactivo— es justo lo que aquí sí entra en el flujo.
>
> Tres detalles que costó descubrir. `dotnet list package --vulnerable` devuelve **0 aunque encuentre
> algo**, así que hay que mirar la salida y no el código de salida. El hook necesita un `.gitattributes`
> con `eol=lf`, o `sh` lo rechaza en Windows. Y `verify.ps1` necesita **BOM**: sin él, PowerShell 5.1 lo
> lee como ANSI y escupe los acentos rotos —la misma trampa que corrompió el `.csproj` en la v1.8.5—.
>
> Comprobado con fallos reales: con un test roto a propósito el hook aborta el push, y con
> `System.Net.Http` 4.3.0 (GHSA-7jgj-8wvc-jh57) la verificación falla por partida doble —el `-warnaserror`
> la convierte en el error NU1903 y el paso dedicado la detecta igual—.

**2. La ruta más compleja del código ya tiene pruebas (T2-15).** El protocolo del worker elevado —named
pipe, JSON por línea, autenticación por token, canal acotado de progreso, evento de cancelación— no tenía
ni una, porque probarlo de verdad exigía levantar un proceso elevado. Separado el bucle de la tubería
(ahora toma un `TextReader`), la conversación entera cabe en un `StringReader`: 15 tests.

> **Un cambio de comportamiento a propósito:** un token incorrecto ya no lanza. Antes tiraba una excepción
> que acababa en el `catch (Exception)` genérico del llamador y se reportaba como «error de lectura de
> sesión»; ahora devuelve un lote sin autenticar con su motivo. La regla que sí se mantiene intacta: sin
> `hello` válido no se procesa **nada**, ni un solo resultado; pero una línea que no sea JSON se ignora y
> se sigue leyendo, porque el ruido de un proceso elevado no debe costar el lote entero.
>
> Ambas comprobadas saboteando: quitar la comparación del token, o hacer que la línea corrupta relance,
> hace fallar exactamente esos 6 tests y ninguno más.

**3. Un solo sitio donde declarar versiones (T2-14).** `Directory.Build.props` y `Directory.Packages.props`
con gestión centralizada: los `.csproj` dicen **qué** paquetes usan, el `.props` con qué versión. Los dos
proyectos de test habían divergido en `Microsoft.NET.Test.Sdk` y `xunit.runner.visualstudio`, con lo que un
mismo test podía comportarse distinto según dónde viviera; y los UI tests apuntaban a un TFM más viejo que
el resto, que era divergencia y no decisión.

**4. Lo que se omite queda escrito, y el PAT ya no se queda puesto (T2-17, T2-18).** `-SkipTests`,
`-SkipUiTests` y `-AllowDirty` avisaban solo por consola: meses después no había forma de saber si una
versión salió verificada. Ahora dejan un bloque de advertencia en las notas publicadas (y `-DryRun` las
imprime, que resulta útil por sí mismo). Y el token que `release.ps1` saca de la credencial cacheada de git
para `gh` se limpia en el `finally` —**solo si lo puso él**: un `GH_TOKEN` que ya estuviera en el entorno es
del usuario, y borrárselo le rompería la sesión—.

**5. La operación más destructiva ya está documentada (T2-16).** El README no mencionaba que tras
desinstalar se abre sola una ventana que **propone borrar carpetas de forma recursiva e irreversible**.
Ahora explica qué busca (rutas concretas derivadas del nombre y el Id, en seis directorios conocidos; no
rastrea el disco), que nada viene marcado, y que el borrado no pasa por la papelera.

**Verificado:** `.\verify.ps1 -Full` en verde —build 0 advertencias / 0 errores, **266/266** unitarias,
**36/36** UI tests—.

---

### 2026-08-22 — Auditoría T2: responsive, i18n y los sueltos de código (T2-08 a T2-11, T2-19, T2-20, T2-22, release v1.8.6)

Tercer corte del Tier T2: **39 de 70** tareas, y **16 de 23** del tier.

**1. Los filtros de la ventana principal no se anunciaban (T2-08).** «Fuente:», «Excluidos:» y
«Buscar:» eran `TextBlock` sueltos: un lector de pantalla decía «cuadro combinado», «botón» y «cuadro
de edición» sin decir de qué. Asociados con `LabeledBy`, igual que los interruptores de Configuración en
T1-08: el nombre sale de la etiqueta visible, así que sigue al idioma en los cinco sin claves nuevas.

**2. La fila de filtros no cabía en la ventana mínima (T2-11).** Era un `StackPanel` horizontal con dos
anchos fijos de 200 px dentro de un `ScrollViewer` con `HorizontalScrollMode="Disabled"`: pedía unos
850 px contra un mínimo de ventana de 900 DIP, y a 150 % de DPI o con etiquetas FR/IT el cuadro de
búsqueda quedaba recortado **sin scroll con el que alcanzarlo**. Ahora es el mismo `WrapPanel` que la
barra de acciones, con cada etiqueta emparejada con su control para que el par salte de fila junto.

> **Cómo se detecta un recorte, que no es como parecía.** El primer test comparó `IsOffscreen` y el borde
> derecho del control con el de la ventana, y **pasaba con el bug puesto**. Sabotear el arreglo lo
> explicó: un control recortado por el `ScrollViewer` se sigue reportando en pantalla y con su
> rectángulo pegado al borde del recorte —el cuadro de búsqueda pasaba de 375 px a 74 sin salirse de la
> ventana—. La señal que sí sirve es el **adelgazamiento**: medir cada control con la ventana ancha y
> exigir que conserve su ancho con la ventana en su mínimo. Además es independiente del DPI del monitor.
> Anotado en el ROADMAP para los próximos tests de recorte.

**3. Las fechas y los nombres de archivo seguían en español (T2-09, T2-10).** El historial y su CSV
usaban `dd/MM/yyyy HH:mm` cableado —con la app en inglés, `03/07/2026` se lee como 7 de marzo— y los
tres diálogos de guardado proponían `actualizaciones_`, `winget-paquetes_` e `historial_` fuera cual
fuera el idioma. Ahora `L` expone `Culture`, `FormatDateTime` y `ExportFileName`, y tabla y CSV llaman
al mismo método: no pueden divergir.

> Dos decisiones que no son obvias. El patrón corto de las culturas latinas es `d/M/yyyy`, no
> `dd/MM/yyyy`: tomarlo tal cual habría quitado el relleno con ceros y roto la alineación de la columna
> de fechas, así que se toma el **orden** de la cultura y se conserva el ancho fijo. Y la fecha de los
> **nombres de archivo** no se localiza: sigue en `yyyy-MM-dd` invariante porque es lo que los ordena
> solos, y porque con el formato de la cultura las barras de `dd/MM/yyyy` serían separadores de ruta.

**4. Tres sueltos de código (T2-19, T2-20, T2-22).** El rebote de la búsqueda creaba un `DispatcherTimer`
y suscribía una lambda nueva **en cada pulsación**; ahora es uno por ventana. En la ventana de búsqueda,
instalar con éxito llamaba a `SearchAsync()` desde dentro del `try`, y esa reemplazaba `_cts` y lo ponía
a `null`, con lo que el `finally` externo ya no encontraba el CTS de la instalación para liberarlo: cada
operación guarda ahora el suyo en una local y la re-búsqueda salió del `try`. Y `where.exe` se lanzaba
con `RedirectStandardError = true` leyendo solo stdout antes de `WaitForExit`, el patrón clásico de
interbloqueo: ya no se redirige.

**Verificado:** build 0 advertencias / 0 errores, **251/251** unitarios (6 nuevos en
`CultureFormattingTests`) y **36/36** UI tests. Los dos tests nuevos de UI se comprobaron saboteando el
arreglo correspondiente: cada uno falla, y solo él.

---

### 2026-08-21 — Auditoría T2: refactorización estructural (T2-04, T2-05, T2-07, release v1.8.6)

Segundo corte del Tier T2: **32 de 70** tareas. Tres bloques que estaban copiados entre 2 y 6 veces.

**1. `ParseUpgradeOutput` troceaba la tabla por su cuenta (T2-04).** Encontrar la línea de guiones,
deducir dónde empieza cada columna y recortar por posición estaba escrito **dos veces, idéntico**: en
`WingetTable` y en `WingetService`. Un cambio de formato de winget —que ya pasó una vez— obligaba a
acordarse de tocar los dos sitios. Ahora `WingetService` consume `WingetTable.ParseRows` y se queda
solo con lo suyo: qué columna es cada cosa y cuándo una fila es un paquete. Los 8 tests de
`ParseUpgradeOutput` siguen en verde **sin tocarlos**, que era el criterio.

> `ParseRows` es nuevo y devuelve también la línea original de cada fila. Hace falta porque el plan B
> de `WingetService` —separar por dos o más espacios cuando el recorte por posición no cuadra— necesita
> la línea cruda; sin ella habría que parsear la salida dos veces.

**2. Cuatro registros de actividad, cuatro comportamientos (T2-05).** Había cuatro `AppendLog`, con
tres estrategias de color y tres límites de recorte. Las diferencias no eran decisiones: la ventana de
búsqueda no coloreaba las líneas normales, dos ventanas conservaban RGB que el Tier C ya había retirado
de la principal por ilegibles, y solo la principal repintaba lo escrito al cambiar de tema. Nuevo
`UI/ActivityLog.xaml`, que las cuatro consumen.

> Lo que **no** se unificó, a propósito: el límite de líneas (es de cada ventana) y, en la principal,
> deducir el tipo de línea por su prefijo y volcar a disco — no son cosa del widget, son de la única
> ventana que retransmite la salida cruda de winget.
>
> **La regresión que salió, y quién la cazó.** Al mover el registro al control, el nombre accesible se
> quedó en el `UserControl` y el `RichTextBlock` interior —donde aterriza un lector de pantalla— se
> quedó sin ninguno. Lo detectó `AccessibilityTests.ActivityLog_HasAnAccessibleName`, escrito el mismo
> día para verificar T1-07. Se arregló **el código, no el test**: el control expone
> `SetAccessibleName` y cada ventana se lo pasa junto a su encabezado ya traducido, así que además
> ahora sigue al idioma.

**3. El arranque de las seis ventanas (T2-07).** Icono, barra de título extendida, fondo Mica, tamaño
por DPI y tema estaban copiados en las seis, con las diferencias justas para no poder fiarse de
ninguna: unas aplicaban el tema antes de extender la barra y otras después, unas cualificaban
`MicaBackdrop` y otras no. Nuevo `UI/WindowChrome.cs`; cada ventana queda en una llamada. El tamaño de
diseño y los mínimos siguen siendo de cada una — meterlos ahí solo trasladaría la divergencia.

**`LogPaletteTests` se reescribió, no se relajó.** Guardaba «las cuatro ventanas usan `LogPalette`»,
premisa que T2-05 elimina. Ahora exige que el registro se pinte en **un solo sitio** y que ningún
archivo de UI vuelva a colorear líneas por su cuenta. Comprobado saboteando: reintroducir un RGB
cableado en una ventana lo hace fallar.

---

### 2026-08-21 — `release.ps1` corrompía los acentos del `.csproj` (release v1.8.6)

Salió solo al cortar la v1.8.5, y llevaba ahí desde siempre: **el `.csproj` nunca había tenido un
carácter no ASCII** hasta que T2-03 le añadió un comentario largo en español.

`release.ps1` lee el proyecto con `Get-Content -Raw` para estampar la versión. En **PowerShell 5.1**,
`Get-Content` asume la página de códigos ANSI cuando el archivo **no lleva BOM** — y el `.csproj` no lo
lleva. Así que leyó los bytes UTF-8 como cp1252 y el `WriteAllText` de después los reescribió doblemente
codificados: `carga útil` → `carga Ãºtil`. El commit `release: v1.8.5` debería haber tocado 3 líneas y
tocó 11 — ahí se vio.

Arreglado con `-Encoding UTF8` al leer, y comprobado con un ciclo leer/modificar/escribir real, no de
palabra. El texto del `.csproj` se restauró revirtiendo la doble codificación.

> **No afecta a lo publicado:** el daño era en comentarios XML, así que el instalador de la v1.8.5 y su
> `.sha256` son correctos. Pero cualquier release futuro habría vuelto a corromper el archivo, y cada vez
> un poco más.

---

### 2026-08-21 — Auditoría T2: bloque de rendimiento (T2-01, T2-02, T2-03, T2-21, T2-23, release v1.8.5)

Primer corte del Tier T2: **29 de 70** tareas. Cinco puntos donde la app hacía trabajo caro sin
necesidad — tres en el hilo de UI, uno en disco y uno en el peso de la descarga.

**1. El registro a disco paraba el hilo de UI en cada línea (T2-01).** `AppendLogFile` hacía
`Directory.CreateDirectory` + `File.AppendAllText` —abrir, escribir y cerrar el archivo— por **cada
línea**, síncronamente y dentro de un `lock`. Y no son líneas sueltas: durante una actualización la app
retransmite toda la salida de winget, así que un lote de diez paquetes abría y cerraba el archivo
cientos de veces, cada una parando justo al hilo que tiene que repintar la barra de progreso. Nuevo
`Services/FileLog.cs`: el hilo de UI solo encola, y una tarea consumidora mantiene el `StreamWriter`
abierto mientras haya actividad. Una ráfaga entera = **una apertura**, y hay un test que lo fija.

> Dos decisiones que no son obvias: la **marca de tiempo se pone al encolar**, no al escribir —si no, un
> pico de escritura desplazaría las horas del registro respecto a cuándo pasaron las cosas—, y el
> archivo **se suelta tras cinco segundos de silencio** en vez de quedarse abierto para siempre, que
> retendría un handle sobre el archivo que la purga (T2-02) tiene que poder borrar.

**2. Los registros diarios crecían para siempre (T2-02).** Un `.log` por día, `LogToFile = true` de
fábrica, sin límite de tamaño ni de antigüedad. Ahora se purgan al arrancar los de más de 30 días, en
segundo plano. Se filtra por **nombre** (`aaaa-mm-dd.log`) y no por fecha del sistema de archivos:
copiar o restaurar la carpeta reescribe las fechas de los archivos, y entonces se borraría lo que no
toca. Los archivos que no escribimos nosotros no se tocan.

> **El test destapó un off-by-one.** «Los últimos 30 días» incluye hoy, así que el corte está hace 29
> días: con `AddDays(-30)` sobrevivían 31 archivos. Salió de escribir el criterio de aceptación tal
> cual estaba redactado, no de revisar el código.

**3. ~41 MB de runtime de IA que nadie usa (T2-03).** Con `WindowsAppSDKSelfContained`, la SDK copia al
publish toda su carga útil, incluida la pila de IA en el equipo: `onnxruntime.dll` (20,6 MB),
`DirectML.dll` (17,7 MB) y los `Microsoft.Windows.AI.*` (~2,6 MB). Esta app gestiona paquetes de
winget y **no referencia ni una** de esas APIs. **Se puede excluir: `publish` 142 MB → 101 MB, y el instalador que descarga el usuario
34,2 MB → 21,6 MB (−37 %)** — el ahorro real supera al del publish porque esos DLL comprimen mal.

> **Cómo, y por qué no por la vía oficial.** La SDK compone la carga útil en dos targets
> (`AddMicrosoftWindowsAppSDKPayloadFilesFromMsix` y `...FromComponents`) y ambos consultan un item
> `MicrosoftWindowsAppSDKFilesExcluded`. Ese sería el gancho oficial, pero hay que rellenarlo con rutas
> completas derivadas de `WindowsAppSdkComponentPackages`, que la SDK define **dentro** de sus propios
> targets y no existe al evaluar el proyecto. Se filtra por nombre en un target propio detrás de los
> suyos, que no depende de en qué paquete de componentes viva cada archivo.
>
> **Verificado, no supuesto:** el riesgo real era romper la activación COM de WinUI 3 *unpackaged*, que
> no se ve compilando. Los 32 UI tests se ejecutaron **contra el ejecutable publicado y recortado**
> (`WINGETUSOFT_EXE`), no contra el build de desarrollo: la app arranca, abre ventanas, cambia de
> idioma y navega los menús sin la pila de IA.

**4. Un `winget list` completo por cada búsqueda (T2-21).** La ventana de búsqueda cruzaba sus
resultados con la lista de instalados para pintar la columna «Instalado», lanzando un proceso externo
en **cada** consulta. La lista no cambia entre búsquedas: solo puede cambiarla una instalación, y esa
la hace esa misma ventana, que es donde se invalida el caché. Solo se cachea el resultado bueno: si
winget falla se devuelve vacío **sin guardarlo**, porque cachear el vacío dejaría la columna mintiendo
el resto de la sesión.

**5. `File.Delete` en el hilo de UI (T2-23).** En la ventana de limpieza los directorios iban a
`Task.Run` y los archivos no. Borrar un archivo parece barato, pero sobre una unidad de red o un disco
dormido bloquea la ventana justo mientras informa del progreso. Las dos ramas van al hilo de fondo.

---

### 2026-08-21 — La verificación de accesibilidad que la auditoría daba por manual (release v1.8.4)

La v1.8.4 salió con los criterios de aceptación de **T1-07, T1-08 y T1-09 sin verificar**: la auditoría
los marcaba como «verificación manual, no automatizable con FlaUI». Lo eran solo en parte.

**La distinción que faltaba.** Lo que no se puede automatizar es **oír** al Narrador. Pero lo que falla
en la práctica no es cómo suena el anuncio: es que las **propiedades de UI Automation no estén ahí** —
que la barra no sea región activa, que no emita el evento, que un control no tenga nombre—. Y eso un
cliente UIA como FlaUI lo ve exactamente igual que lo vería un lector de pantalla. Cinco tests nuevos
en `tests/WingetUSoft.UiTests/AccessibilityTests.cs`:

- **`LiveSetting = Polite`** en la barra de estado (T1-07).
- **El evento `LiveRegionChanged` se emite de verdad** al cambiar el texto (T1-07). Es la mitad que de
  verdad importa: marcar el elemento no anuncia nada por sí solo. El test se suscribe al evento y
  provoca el cambio por la vía más barata que no invoca a winget — cambiar el idioma, que hace que
  `ApplyLocalizedStrings` repinte la barra («Listo.» → «Ready.»).
- **El registro tiene nombre accesible** (T1-07).
- **Los interruptores de Configuración se llaman como su etiqueta visible** (T1-08), comparando ambos
  `Name`: es lo que garantiza que el nombre siga al idioma sin claves nuevas.

**`CleanupWindow` (T1-09) se queda fuera de los UI tests, y no por comodidad:** no es alcanzable
conduciendo la app. Solo se abre desde `UninstallWindow` **después de desinstalar un programa de
verdad**, y ningún test desinstala nada del equipo. Se cubre la composición de las etiquetas en un
unitario (`CleanupItemLabelsTests`), que es donde estaba el fallo.

> **Los tests se probaron contra el bug.** Este proyecto ya se llevó dos sustos con suites en verde
> mientras el fallo estaba vivo (los tests de contraste y los de claves, ambos en el corte del 1.8.3),
> así que no basta con que pasen. Se revirtieron a mano las tres piezas —`TrackStatusText` comentado y
> el `LabeledBy` de un interruptor retirado— y se comprobó que fallan **exactamente los tres tests
> correspondientes**, mientras los dos que tocan piezas intactas siguen en verde. Después se restauró
> el código y se confirmó el verde completo.

**Lo que sigue siendo manual**, y así queda escrito en el ROADMAP: cómo suena el anuncio, si llega en
buen momento y si el texto se entiende de oído. Eso no lo cubre ningún test.

---

### 2026-08-21 — Auditoría T1 completado: los cuatro de fondo (T1-02, T1-11, T1-12, T1-18, release v1.8.4)

Cuarto corte de la Parte II de [`ROADMAP.md`](ROADMAP.md): **24 de 70** tareas. **T0 y T1 cerrados
enteros.** Build 0/0, **223/223 unitarios**, **27/27 UI tests**.

**1. Clasificar fallos por código, no por texto (T1-02) — y dos códigos que estaban cambiados.**
La cadena de `if` de `GetFailureReason` clasificaba por texto y solo reconocía español e inglés: en un
Windows en francés, italiano, alemán o portugués **no casaba ninguna rama** y todo caía al «última línea
con sentido», es decir, el usuario veía la línea cruda de winget. Es la misma trampa que el proyecto ya
documentó en `WingetTable` y resolvió en `WingetShowLabels` — winget traduce su salida — aplicada aquí
por tercera vez. Ahora hay una tabla de 12 códigos que se comprueba **primero**, contra el `ExitCode`
del proceso y contra el texto, y el reconocimiento por palabras queda de último recurso.

> **Lo que apareció al verificar contra la fuente:** dos asignaciones estaban mal, y la auditoría las
> daba por buenas (su criterio de aceptación pedía literalmente la incorrecta). Según la tabla oficial
> de `winget-cli`, **`0x8A150011` es «el hash del instalador no coincide con el manifiesto»**, no «no
> hay actualización aplicable» — la app presentaba **un fallo de integridad como un tranquilizador «ya
> estás al día»**, que es justo la confusión más cara de las posibles. Y `0x8A150014` es «no se
> encontró el paquete», no «ningún instalador aplicable». Los códigos correctos son `0x8A15002B` y
> `0x8A150010`. Hay un test que fija las dos.

**2. La ventana TOCTOU del instalador (T1-11).** El instalador se descargaba a una ruta fija y
predecible (`%TEMP%\WingetUSoft_Update.exe`), se cerraba el `FileStream` —obligatorio desde el arreglo
de la v1.4.1—, se verificaba, y `Process.Start` ocurría después **sin nada que impidiera sustituirlo
entre medias**. Como el instalador es `PrivilegesRequired=admin`, quien colara ahí su binario obtendría
administrador a través de un UAC que el usuario reconoce como legítimo. Dos defensas, y hacen falta las
dos: cada descarga estrena un subdirectorio de nombre aleatorio, y el archivo se retiene con
`FileShare.Read` desde antes de verificar hasta después de lanzar (nuevo `VerifiedInstaller`, que el
consumidor toma con `using`).

> **El riesgo de repetir la v1.4.1** era evidente: allí un `FileShare` demasiado restrictivo dejó la
> auto-actualización muerta. Que `FileShare.Read` permita **ejecutar** el archivo retenido es una
> promesa de la plataforma, no del código, así que hay un test que la comprueba de verdad: retiene un
> ejecutable real y lo lanza.
>
> Y el primer test escrito **encontró un bug de la propia implementación**: la limpieza de directorios
> viejos corría después de crear el nuevo y, como su nombre casa con el mismo patrón, se lo llevaba por
> delante.

**3. Excepciones tragadas en silencio (T1-12).** El manejador hacía tres cosas mal a la vez: escribía
`crash.log` con `File.WriteAllText` —así que **la segunda excepción borraba la evidencia de la
primera**, cuando en una cadena de fallos la primera suele ser la causa y el resto el eco—, no ponía
fecha, y marcaba `Handled = true` incondicionalmente, dejando la app viva en un estado desconocido sin
avisar a nadie. Nuevo `Core/CrashLog.cs`: añade con fecha, recorta por tamaño, y solo se traga los
tipos de los que se sabe volver; el resto se anota, se avisa con un `MessageBox` de Win32 (síncrono, no
necesita `XamlRoot` ni un despachador vivo) y se deja caer. En `Program.Main` el aviso se omite si la
invocación es la del worker elevado: ahí no hay nadie mirando y un diálogo colgaría el proceso.

**4. La contraseña del `.pfx` en la línea de comandos (T1-18).** `build-installer.ps1` pasaba
`/p $CertPassword` a `signtool`: mientras firma, **cualquier usuario de la máquina puede leer sus
argumentos** (`Get-CimInstance Win32_Process`), y ahí va la clave privada del certificado de firma.
Además el parámetro era `[string]`, así que quedaba en el historial de PowerShell. Ahora es
`[SecureString]`, el `.pfx` se importa al almacén del usuario **dentro de este proceso** y se firma
siempre por huella; el certificado se retira al terminar, también si algo falla a mitad (`trap`).

**Un patrón nuevo, distinto al de los cortes anteriores.** Aquí no fue «la corrección que se aplicó a
una ventana y no a las demás»: fueron **tres premisas que nadie había ido a verificar contra la
fuente** — qué significan de verdad los códigos de winget, si `FileShare.Read` deja ejecutar, y quién
puede leer los argumentos de un proceso ajeno. Las tres se resolvieron mirando la fuente o escribiendo
el test que las prueba, no razonando sobre ellas.

---

### 2026-08-21 — Auditoría T1: cadenas en español cableadas (T1-15 a T1-17, release v1.8.4)

Tercer corte de la Parte II de [`ROADMAP.md`](ROADMAP.md): **18 de 70** tareas, **T1 al 73 %**.
Build 0/0, **196/196 unitarios**.

Tres focos de texto que salían en español **en los cinco idiomas**. Los tres eran invisibles para los
tests que ya existían: `LocalizationTests` comprueba que cada clave dada de alta tenga sus 5
traducciones y `LocalizationUsageTests` que toda clave usada exista — ninguno de los dos puede ver un
texto que **nunca pasó por `L.T`**.

**1. `MainWindow` (T1-15).** El prefijo «Iniciando:» de cada paquete del lote, el «¡Nuevas
actualizaciones disponibles!» de la auto-comprobación y el aviso de log ilegible, con su etiqueta
«[aviso]» incluida. Tres claves nuevas.

**2. `AppSettings` (T1-16) — el interesante.** Los mensajes de configuración corrupta o no guardada se
le muestran al usuario en un diálogo, pero **no se pueden traducir donde se producen**: `Load()` corre
antes de que se fije el idioma, porque el idioma es justamente uno de los ajustes que está leyendo.
Traducirlos ahí los dejaría siempre en el idioma por defecto.

> La solución es un tipo nuevo, `DeferredMessage(Key, Args)`: se guarda la **clave y sus argumentos**,
> no el texto formateado, y `LastLoadError` / `LastSaveError` pasan de `string?` a `DeferredMessage?`.
> El texto se resuelve con `.Text` en el punto de presentación — que además lo deja correcto si el
> idioma cambia entre que el error se produce y que se muestra.

**3. `WingetService` (T1-17).** `BuildWingetCommandErrorMessage` recibía el verbo **ya escrito en
español** («consultar actualizaciones», «listar programas instalados», «buscar paquetes») y lo
incrustaba en una plantilla también española. Ahora recibe la **clave** del verbo y compone el mensaje
entero en el idioma activo; el test adaptado comprueba además que no se cuele la clave sin resolver.
Más cuatro excepciones que acaban en diálogo (sesión elevada, winget no encontrado, ruta propia).

> **Lo que se deja sin traducir a propósito:** las tres `ArgumentException` de
> `ParseElevatedWorkerOptions`. Corren en el proceso *worker*, que arranca directo desde `Program.Main`
> sin leer los ajustes —no hay idioma que aplicar— y saltan antes de que exista la tubería, así que su
> texto nunca vuelve al proceso principal. Solo se disparan si la app compone mal su propia invocación:
> son diagnóstico de protocolo, no mensajes de producto. Queda escrito en el método.

---

### 2026-08-21 — Auditoría T1: bloque de accesibilidad (T1-07 a T1-10, release v1.8.4)

Segundo corte de la Parte II de [`ROADMAP.md`](ROADMAP.md): **15 de 70** tareas, **T1 al 55 %**.
Build 0/0, **196/196 unitarios**.

**1. La barra de estado no existía para un lector de pantalla (T1-07).** `grep -rn "LiveSetting" src`
daba **cero resultados**. La barra de estado es lo único que el usuario mira durante una operación larga
—«Actualizando 3 de 8», «Completado», «Error»— pero cambiar `Text` no notifica nada al árbol de
automatización, y el foco nunca está encima: está en el botón que lanzó la operación. Nuevo
`UI/LiveRegion.cs`, enganchado en las cuatro ventanas con registro.

> **Por qué un callback y no un ayudante `SetStatus(...)`:** hay **62** asignaciones a `txtEstado`
> repartidas por cuatro ventanas. `RegisterPropertyChangedCallback(TextBlock.TextProperty, …)` cubre
> todas de una vez — y también las que se escriban en el futuro, que es donde una reescritura de 62
> puntos se habría vuelto a desincronizar.
>
> **El registro NO se convierte en región activa** (desviación consciente sobre lo que pedía la tarea):
> una región activa se anuncia leyendo *todo* su contenido, y el registro llega a 400 líneas — cada
> línea nueva releería el bloque entero. Recibe en su lugar un nombre accesible (`LabeledBy` →
> `txtLogHeader`) y el progreso se anuncia donde procede: en la barra de estado.

**2. Interruptores que no decían de qué eran (T1-08).** `tsShowNotifications` y `tsMinimizeToTray`
llevaban su etiqueta en un `TextBlock` aparte, sin `Header` ni nombre accesible: el lector anunciaba
«interruptor, desactivado» y nada más. `AutomationProperties.LabeledBy` los ata a su etiqueta visible,
que ya se localiza en tiempo de ejecución — así el nombre sale correcto en los cinco idiomas sin claves
nuevas. Es el primer uso de `LabeledBy` en el proyecto (antes: 0 apariciones).

**3. Las filas de Limpieza se anunciaban por su tipo .NET (T1-09).** Mismo bug que ya se corrigió en la
tabla principal y en la búsqueda al conducir la app real, vivo todavía en la ventana **que borra
carpetas de forma recursiva**: el `ListViewItem` heredó el `ToString()` del ViewModel
(«WingetUSoft.CleanupItemViewModel») y la casilla se anunciaba solo como «casilla de verificación», sin
ruta. `CleanupItemViewModel` gana `RowLabel` y `SelectLabel`, con dos claves nuevas × 5 idiomas.

**4. La última pantalla antes de borrar no nombraba ni una ruta (T1-10).** `cleanup.confirmDeleteBody`
decía solo «¿Eliminar 7 elemento(s) seleccionado(s)?» para una operación irreversible sobre el sistema
de archivos. Ahora lista hasta 10 rutas con el sufijo «…y N más», reutilizando el formato y la clave
`list.andMore` de la confirmación de «Actualizar todo».

**El patrón, otra vez.** Igual que en el corte anterior, tres de los cuatro hallazgos son *una corrección
que se aplicó a una ventana y no a las demás* — y la que quedó fuera fue, las dos veces, la de limpieza:
la única que borra archivos.

---

### 2026-08-21 — Auditoría T0 + T1: seguridad, integridad y accesibilidad (release v1.8.3)

Primer corte del plan de auditoría de [`ROADMAP.md`](ROADMAP.md) Parte II: **11 de 70** tareas.
Build 0/0, **196/196 unitarios**, **27/27 UI tests**.

**1. Recorrido de rutas en el escaneo de residuos (T0-01).** `CleanupScanner` construía rutas candidatas
con `Path.Combine(baseDir, nombreDelPaquete)`. `Path.Combine` **no normaliza `..` y descarta el primer
argumento si el segundo está enraizado**: un paquete llamado `C:\Windows` hacía que el escáner ofreciera
`C:\Windows` como residuo borrable, con borrado recursivo detrás del botón. Ahora todo candidato pasa por
`TryCombineInside`, que normaliza con `GetFullPath` y exige que el resultado quede **dentro** del
directorio base. El nombre del paquete lo elige el manifiesto de winget, no el usuario, así que la entrada
no es de confianza.

**2. Guardado no atómico de `settings.json` (T0-02).** `Save()` escribía directamente sobre el archivo
final: una interrupción a mitad dejaba un JSON truncado y **toda la configuración perdida**. Ahora escribe
a `.tmp` y publica con `File.Replace` (atómico en NTFS), dejando `.bak` como copia previa.

**3. Contraste ilegible en dos de las cuatro ventanas (T1-04/05).** El Tier C #4 retiró de `MainWindow`
unos RGB cableados que no llegaban al 4,5:1 de WCAG AA sobre la tarjeta oscura (medían 2,74:1 el verde y
2,71:1 el rojo)… y dejó las mismas constantes en `CleanupWindow` y `UninstallWindow`, justo las que
informan de qué archivos se borraron y cuáles fallaron. Ambas usan ya `LogPalette`. El README afirmaba que
el contraste estaba «comprobado por tests» cuando era cierto en la mitad de las ventanas.

**4. Botones de diálogo en español en los cinco idiomas (T1-03).** `WindowDialogHelper` cableaba
«Aceptar», «Sí» y «No», y lo usan las cuatro ventanas: en francés el usuario confirmaba un borrado
pulsando «Sí», y «No» ni siquiera es palabra francesa. La causa de raíz está en la firma: un valor por
defecto de parámetro debe ser constante en tiempo de compilación, así que `L.T(...)` **no compila** ahí.
Los defaults pasan a `null` y se resuelven en el cuerpo.

**5. «red» dentro de «required» (T1-01).** La clasificación de fallos buscaba términos con `Contains`, así
que cualquier error que mencionara `requi‑red`, `sha‑red`, `expi‑red`, `configu‑red` o `registe‑red` se le
presentaba al usuario como **«Error de red»**, mandándolo a revisar su conexión por un archivo que falta.
Ahora exige límite de palabra.

**La lección que se repite.** Cuatro de estos cinco hallazgos son *la misma corrección aplicada a una
ventana y no a las demás*. Por eso el corte incluye dos tests que leen el código fuente en vez de medir
comportamiento: uno exige que toda ventana con registro tome sus colores de `LogPalette`, y otro que
ningún texto de botón de diálogo sea un literal. Los tests de contraste y de claves ya existían y pasaban
en verde mientras los bugs estaban vivos, porque solo cubrían a quien usara la pieza correcta.

---

### 2026-07-12 — Tier E: buscar/instalar, exportar/importar y omitir versión ⚠️ *cambio de alcance*

El usuario eligió tres de las cuatro ideas del plan; la tercera **amplía el propósito del producto**
(ver §1). Build 0/0, **162/162 unitarios**, **27/27 UI tests**, verificado conduciendo la app real.

**Lo construido.** `Core/WingetTable` (parseo genérico de tablas) + `Core/WingetSearchParser` +
`WingetSearchResult`; `WingetService` gana `SearchPackagesAsync`, `InstallPackageAsync`,
`ExportPackagesAsync` e `ImportPackagesAsync` (el runner interactivo, cableado a `upgrade`, se generalizó
a `RunWingetStreamingAsync(arguments, …)`: install e import necesitan el mismo progreso). Nueva
`UI/SearchWindow`, que **cruza los resultados con lo ya instalado** y los marca — sin eso el usuario
intentaría instalar algo que ya tiene y winget fallaría con un error críptico. `Core/SkippedVersions` +
`AppSettings.SkippedVersions`. Tres ítems nuevos en *Herramientas* y uno en el menú contextual.

**1. "Omitir esta versión" no se puede delegar en `winget pin`.** Winget no tiene esa operación: `pin add`
excluye el paquete de `upgrade --all` hasta que se quite, `--blocking` lo bloquea del todo y `--version`
ancla a un **rango**. Anclar a la versión de hoy para "saltársela" congelaría el paquete **también para
las futuras** — lo contrario de lo pedido. Se resuelve como en Chrome o Sparkle: se recuerda la versión
descartada y la omisión **caduca sola** cuando winget ofrece otra. `Prune` retira las omisiones muertas en
cada consulta (el paquete se actualizó por fuera o ya no aparece), para que `settings.json` no las acumule.
Omitir ≠ excluir: excluir es permanente y para todo el paquete.

**2. Bug real: el menú contextual de la tabla era inalcanzable con teclado.** Estaba enganchado a
`RightTapped`, que **solo dispara el ratón**: ni Shift+F10 ni la tecla Menú lo abrían, así que *todas* sus
acciones (actualizar la fila, copiar, excluir, y ahora omitir) quedaban fuera del alcance de quien no usa
ratón. Ahora usa `ContextRequested`, que WinUI dispara en ambos casos (con teclado no hay posición de
puntero: el menú se ancla al contenedor de la fila). **Lo destapó conducir la app real**, no revisar código.

**3. Bug real: las filas se anunciaban con el nombre de la clase.** El `ListViewItem` heredaba el
`ToString()` del ViewModel → un lector de pantalla decía "WingetUSoft.PackageViewModel". Ahora tienen
nombre accesible propio ("*programa*, versión instalada X, disponible Y"). Igual en la ventana de búsqueda.

**4. Importar no es solo "instalar lo que falta": también actualiza.** Verificado contra winget 1.29.280
con un archivo de un paquete ya instalado: winget responde *"Se encontró un paquete existente ya
instalado. Intentando actualizar…"* y lo actualiza. El diálogo de confirmación lo dice; la redacción
inicial habría mentido sobre lo que la operación hace en el equipo.

**5. El parser de `search` no puede mapear columnas por índice.** `winget search` intercala la columna
**"Coincidencia"** (Tag/Moniker) **solo cuando el paquete casó por ahí**: la misma consulta devuelve filas
de 4 y de 5 columnas, y un parser por índice fijo tomaría "Moniker: 7zip" como *origen*. Se mapea por
posición relativa (nombre/Id/versión primeros, **origen siempre el último**) y las columnas se localizan
por la línea de guiones, nunca por el nombre de la cabecera (winget la traduce). Tests con salida real.

**6. Test nuevo que caza una clase entera de bug silencioso.** Se usó `L.T("ctx.include")` sin dar de alta
la clave: no rompió el build ni ningún test, porque **`L.T` devuelve la propia clave** cuando no la conoce
— el usuario habría visto el literal "ctx.include" en el menú. `LocalizationUsageTests` escanea el código
fuente, extrae todas las llamadas `L.T("…")` y exige que cada clave exista en `L.Map`. Se comprobó que
**falla** al retirar la clave: un test que nunca ha fallado no prueba nada.

**Verificado en la app real** (UI Automation): consulta con 24 paquetes; *Omitir esta versión* desde el
menú abierto **con teclado**, con la barra de estado confirmándolo; búsqueda de "7zip" en el catálogo real
con **9 resultados**, botón *Instalar* deshabilitado sin selección y habilitado al elegir uno. Export e
import se verificaron ejecutando los comandos exactos que construye el servicio (sus diálogos de archivo
son nativos y no se automatizaron): `export` escribió el JSON (exit 0) e `import` actualizó Git a 2.55.0.2.

### 2026-07-12 — Tier D: cara pública (release v1.7.0)

Nace de comparar con FormatDiskPro: el fondo ya estaba hecho —y en verificación del updater,
accesibilidad y pipeline **por delante** del hermano—, pero la **presentación** seguía siendo la de un
repo para compilar. Build 0/0, **137/137 unitarios**, **26/26 UI tests**.

- **Licencia y avisos de terceros dentro de la app.** No había `THIRD-PARTY-NOTICES.txt`, y el README
  **afirmaba en falso** que la licencia se veía en *Ayuda → Acerca de* (ese diálogo solo muestra una línea
  de copyright). Nuevos `THIRD-PARTY-NOTICES.txt`, `Core/LegalText` y `UI/LegalTextDialog`, con ambos
  textos **embebidos como recurso** en el `.exe` (no archivos sueltos que el usuario pueda borrar). El
  aviso lista lo que la app **redistribuye de verdad** (.NET, Windows App SDK, H.NotifyIcon.WinUI — MIT,
  comprobado en el `.nuspec`, no de memoria) y cita winget aparte como herramienta externa **no**
  redistribuida.
- **Por qué hay tests para eso:** `LegalText` es defensivo (devuelve `""` si el recurso falta), así que un
  `LogicalName` mal escrito **no rompería el build** — se vería como "Texto no disponible" al abrir el menú
  a mano. `LegalTextTests` (6) exige que ambos recursos existan y digan lo que deben; 2 UI tests abren los
  diálogos en la app real y comprueban que el cuerpo **no** es el mensaje de "no disponible".
- **README para quien instala:** badges, *Instalación* desde Releases, *Actualizaciones* con el modelo de
  confianza (Authenticode → SHA-256, con su alcance honesto). Cada afirmación se verificó contra el código
  antes de escribirla (flags de `release.ps1`, generación del `.sha256`, qué runtimes descarga el instalador).
- **Capturas reproducibles:** `tools/capture-screenshots.ps1` conduce la app real por UI Automation
  (fuerza tema e idioma, consulta, abre ventanas) y **respalda/restaura el `settings.json` real** del
  usuario. Captura con `DWMWA_EXTENDED_FRAME_BOUNDS` (no `GetWindowRect`, que arrastra el margen invisible
  del DWM) y `SetProcessDPIAware()` (sin eso, en un monitor escalado la captura sale desplazada).

### 2026-07-12 — Tier C #4–#6 (release v1.6.0) — Tier C completado

Build 0/0, **131/131 unitarios**, **24/24 UI tests**. Desde esta versión **`release.ps1` ejecuta también
los UI tests**: un release no sale si la app real no pasa.

- **#4 — El rojo ya solo significa peligro.** *Cancelar* interrumpe un lote, no destruye nada: deja de ir
  de rojo. El icono de "excluido" era del **mismo rojo que los errores**, cuando excluir es una decisión
  del usuario: pasa a gris. El rojo queda para lo que destruye (*Desinstalar*, *Eliminar*) y los fallos.
  **Bug encontrado al mirar esos colores:** había UN solo juego de RGB para claro y oscuro, elegido para
  fondo claro; sobre la tarjeta oscura, el verde de los aciertos y el rojo de los fallos —justo lo que se
  busca en un registro— eran lo peor de leer. `Core/LogPalette` da un color por tema y `LogPaletteTests`
  **mide el contraste real y exige 4.5:1 (WCAG AA)**: un color mal elegido rompe el build. El color se
  resuelve con `rtbLog.ActualTheme`, **no** con `Application.Current.RequestedTheme` (el tema se fuerza por
  elemento: con "Claro" sobre un Windows oscuro, la aplicación seguiría diciendo "oscuro").
- **#5 — *Configuración* es el único hogar de las preferencias.** Modo/Tema/Idioma se mudan del menú a la
  ventana; el menú se queda con acciones y pasa a llamarse *Herramientas*. **Agujero cerrado de paso:** la
  ventana llamaba a `Save()` e **ignoraba el resultado**, cerrándose igual — unos cambios que no llegaban
  al disco se perdían en silencio. Ahora, si falla, avisa y **no se cierra**.
- **#6 — Las cabeceras ordenables son botones de verdad.** Eran `StackPanel` con `Tapped`: fuera del orden
  de tabulación y sordas a Espacio/Intro — **ordenar la tabla era imposible sin ratón**. Ahora son `Button`
  enfocables cuyo nombre accesible lleva la columna **y la dirección** del orden, que antes solo vivía en
  el triángulo ▲/▼ (invisible para un lector de pantalla).
- **Trampa evitada en los tests:** `LocalizationTests` comprobaba una clave con `L.T("menu.options")`, pero
  `L.T` devuelve *la propia clave* si no la conoce — al borrarla, el test habría seguido pasando en falso.
  Ahora se comprueba contra `L.Map`.

### 2026-07-12 — fix: la auto-actualización se bloqueaba el archivo a sí misma (rota desde 1.4.1)

Reportado al intentar pasar de 1.4.1 a 1.5.0: *"The process cannot access the file
'…\WingetUSoft_Update.exe' because it is being used by another process"*. **No era otro proceso: era la app
bloqueándose su propio archivo** (el mensaje de Windows es genérico y despista).

`DownloadInstallerAsync` declaraba el `FileStream` con `await using` **a nivel de método** y `FileShare.None`
(exclusivo). En C# ese handle sigue abierto hasta que el método **retorna** — pero antes de retornar, el
propio método llamaba a `VerifyInstallerAsync`, que abre ese archivo para calcular el SHA-256. Y el `catch`
empeoraba el diagnóstico: su `File.Delete` fallaba por lo mismo, lanzando una `IOException` que
**sustituía** al error original. `git log -S` lo sitúa en **v1.4.1**: no fallaba "a veces", **fallaba siempre**
(y `VerifyAuthenticodeSignature` sufría lo mismo, devolviendo `false` en silencio).

- **Arreglo:** la descarga se mueve a `DownloadToFileAsync`, cuyo `FileStream` se cierra al salir del método,
  **antes** de verificar. El borrado del instalador rechazado pasa a best-effort.
- **Tests:** `GitHubUpdateServiceTests` levanta un servidor HTTP sobre `TcpListener` (no `HttpListener`: en
  Windows exige reservar la URL como administrador) y ejercita la descarga completa. Se comprobó que
  **fallan contra el código de 1.4.1**. Los tests que había solo cubrían el hash, nunca la descarga — por eso
  el bug pasó.
- **Consecuencia operativa:** quien esté en 1.4.1 o 1.5.0 **no puede auto-actualizarse**; tiene que instalar
  a mano una vez. A partir de ahí, vuelve a funcionar.

### 2026-07-11 — Tier C #1–#3 (release v1.5.0): flujo, selección y datos que mentían

A diferencia del Tier B (layout: que la ventana quepa), este ataca **flujo y feedback**. Tres bugs de fondo:

- **Bloque 1 — Se acabaron los modales encadenados.** `HandleFailedUpgrade` abría un `ContentDialog`
  *dentro del bucle*: con 6 paquetes fallidos, 6 modales y **el lote parado esperando clics**. Ahora se
  acumulan y se muestra **un único diálogo** al terminar. La barra de estado sale del `ScrollViewer` y se
  ancla al pie (antes, con la lista cargada, el único indicador de progreso quedaba **bajo el pliegue**), y
  gana una `ProgressBar` que avanza también *dentro* de cada paquete. La tabla explica **en qué estado
  está** (consultando / sin datos / al día / sin coincidencias / cancelada / error), dibujado **encima** del
  `ListView` y no en su lugar: colapsarlo lo sacaría del árbol de automatización. Arreglada de paso una
  **captura de variable** en el reporter de progreso (el lambda capturaba la `i` del `for`).
- **Bloque 2 — Había dos conceptos de "seleccionado".** La fila resaltada y la casilla competían, y como
  `LoadPackagesToGrid` reconstruye los ViewModels, **ordenar o buscar borraba las casillas**. Ahora la
  fuente de verdad es `_selectedIds` (por Id) y la selección **sobrevive a buscar, ordenar y filtrar**. Más:
  casilla tri-estado de "marcar todo", contador en el botón (adiós al modal "no hay nada seleccionado"),
  `Ctrl+A` estándar y casillas deshabilitadas en filas excluidas. **Bug real:** los aceleradores se
  disparaban **con el foco dentro del buscador** — `Ctrl+A` marcaba todos los paquetes en vez de
  seleccionar el texto, y **`Supr` excluía un paquete en vez de borrar un carácter**.
- **Bloque 3 — Tres datos que mentían.** (1) **Orden de versiones como texto**: `1.10.0` quedaba antes que
  `1.9.0` → nuevo `Core/VersionOrder` (compara tramos numéricos como números; cubre `< 13.5.0.359`,
  `Unknown` y preliberaciones). (2) **La columna "Tam." era imposible de rellenar**: `winget show` **no
  emite** ningún tamaño de instalador, y para leer ese campo inexistente se lanzaba **un proceso por
  paquete en cada reconstrucción de la tabla** (cada tecla del buscador, cada clic de orden). Eliminada
  columna, cargador y caché. (3) **El panel de detalle estaba roto en todo Windows que no fuera inglés**:
  `winget` **traduce las etiquetas de su salida** (`Descripción:`, `Página principal:`…), y el parser
  buscaba las inglesas. **Esto invalidó la premisa que este mismo documento daba por buena en Tier A #7.**
  Se resolvió sin inventar traducciones: se descargó el `.msixbundle` oficial de winget v1.29.280, se
  extrajeron sus **79 paquetes de idioma** y se volcaron sus `resources.pri` con `makepri`. Dos hallazgos
  que no se habrían acertado a mano: winget **solo traduce estas etiquetas a 10 idiomas** (en el resto cae
  al inglés, así que la tabla cubre el **100 %** de las salidas posibles), y las etiquetas tienen **trampas
  invisibles** — el francés lleva **espacio duro** (U+00A0) antes de los dos puntos, el chino tradicional usa
  dos puntos de **ancho completo**, y **el coreano no lleva dos puntos**. `WingetShowLabels` se **generó con
  un script**, y se verificó que ninguna de las 30 etiquetas colisiona con las otras 296 `ShowLabel*`.

### 2026-07-11 — v1.4.1: snap layouts (Tier B #7) + 3 bugs del flujo instalar/actualizar

**#7 no era una consecuencia gratuita de #1/#2:** al automatizarlo con FlaUI aparecieron **dos bugs
reales**, invisibles en una ventana de tamaño normal. (1) **El mínimo de la ventana impedía el snap**: el
mínimo de diseño (900×600 DIP) supera la celda de snap de cuarto en un 1920×1080 (960×520), así que Windows
no podía encoger la ventana lo suficiente. `ScaleMinSize` acota ahora cada eje a **la mitad de la WorkArea**
— que es exactamente el tamaño de una celda —, sin números fijos: en monitores grandes el mínimo se conserva
intacto. (2) **La tabla desaparecía de la pantalla**: encogida a un cuarto, las tres tarjetas superiores
consumían el alto entero y el DataGrid quedaba recortado **fuera** de la ventana (`BoundingRectangle` 0×0,
`IsOffscreen`), sin scroll con el que llegar a él. Ahora todo vive en un `ContentScroller` cuyo Grid usa
**`MinHeight` (no `Height`) atado al `ViewportHeight`**: si sobra alto, rellena sin barra; si falta, la
página se desplaza.

**Los 3 bugs del flujo con GitHub** (reporte: la app pedía descargar una librería ya instalada y luego no
volvía a ejecutarse):

1. **El Windows App Runtime se descargaba sin necesitarlo.** `installer.iss` comprobaba una clave del
   registro que **no existe** → siempre concluía "falta" y bajaba ~40 MB. Pero la app **no lo necesita**:
   con `WindowsAppSDKSelfContained=true` el runtime viaja dentro de la carpeta. Comprobación eliminada.
2. **El .NET siempre se daba por ausente.** La comprobación miraba una rama del registro que no existe **y**
   el framework equivocado (`Microsoft.WindowsDesktop.App`, cuando WinUI 3 pide `Microsoft.NETCore.App`).
   Ahora se comprueba la carpeta del framework compartido, que es lo que resuelve hostfxr.
3. **La auto-actualización estaba muerta.** Exigía firma Authenticode y **borraba** el instalador si no la
   tenía… pero los releases se publican **sin firmar**. Fallaba **siempre**, antes de ejecutar nada.
   **Decidido con el usuario:** verificar por **SHA-256** publicado como asset, manteniendo la firma como vía
   preferente. `build-installer.ps1` genera el `.sha256` (después de firmar, porque firmar cambia el binario)
   y `release.ps1` lo sube como segundo asset.

**Verificado de punta a punta** con el instalador real (`/VERYSILENT /NORESTART /autoinstall=1` con `/LOG`):
actualiza 1.3.0 → 1.4.1 con **0 descargas de runtime**, **ninguna ventana**, la app **se relanza sola** y
como **usuario normal** (`runasoriginaluser`; antes heredaba el token de administrador de Setup).

### 2026-07-10 — v1.4.0: Tier B — layout adaptable, accesibilidad y UI tests (FlaUI)

Origen: reporte del usuario con el botón "Cancelar" **recortado** contra el borde de la ventana.

- **Nuevos:** `Core/WindowSizing` (matemática pura: tamaño en DIP × escala DPI, acotado a `WorkArea`, y
  mínimo escalado **y acotado**), `UI/WindowSizer` (wrapper DPI + WorkArea, usado por las 5 ventanas, que
  reemplaza los `AppWindow.Resize(...)` fijos en píxeles) y `UI/WrapPanel` (panel de envoltura **nativo
  propio**, cero dependencias — decisión del usuario frente a CommunityToolkit/CommandBar). Las 5 ventanas
  siguen **redimensionables**, con mínimo acotado (no se copia el modelo de ventana fija de FormatDiskPro).
- **Refinamiento no obvio:** la columna "Nombre" (`*`) no rellenaba en ventanas anchas, porque un
  `ScrollViewer` horizontal mide el contenido con ancho **infinito** y el `*` colapsa a `Auto`. Se enlazó el
  `Width` del Grid al `ViewportWidth` del scroller, con `MinWidth` de piso.
- **#8 — Nuevo proyecto `tests/WingetUSoft.UiTests`** (FlaUI + UIA3, xUnit), que porta el patrón de
  `FormatDiskPro.UiTests` con una diferencia clave: **sin `EnsureElevated()`**, porque la app corre
  `asInvoker`. `SettingsBackup` respalda y restaura el `settings.json` real (la app es unpackaged: es el
  mismo archivo que usa la instalación del usuario). No se cubren `upgrade`/`uninstall` reales: disparan
  UAC en el escritorio seguro, inautomatizable con FlaUI.
- **Dos hallazgos reales al endurecer los tests:** el proceso de test no era DPI-aware (`GetMonitorInfo`
  devolvía coordenadas virtualizadas mientras UIA reporta píxeles físicos) → `[ModuleInitializer]` con
  `SetProcessDpiAwarenessContext(PER_MONITOR_AWARE_V2)`; y el `WrapPanel` necesita su propio pase de
  Measure/Arrange tras el resize → settle + reintento por botón. **No hubo que añadir ningún `x:Name`** a la
  app: todos los AutomationId ya existían.

### 2026-07-09 — v1.3.0: Tier A completado (paridad con FormatDiskPro) + pipeline de release

Se comparó WingetUSoft con FormatDiskPro para portar la **infraestructura de app** que faltaba, sin salirse
del propósito de entonces. Nueve fases (#0–#8): migración de tests **MSTest → xUnit**; **localización** a 5
idiomas (272 claves al cerrar #7); **changelog + diálogo de Novedades**; **aviso al terminar + progreso en
la barra de tareas**; **ETA/velocidad**; **historial con búsqueda, filtros y exportación**; **Acerca de +
licencia MIT + menú Ayuda**; y el **pipeline de release**. Decisiones tomadas con el usuario: **MIT** (no
GPLv3 como FormatDiskPro), **xUnit** (mejor encaje con FlaUI) e **Inno Setup como único empaquetador**.

- **Decisión de despliegue (desviación deliberada de FormatDiskPro):** publish **framework-dependent**, no
  self-contained. `WindowsAppSDKSelfContained=true` solo empaqueta el Windows App SDK, y el `installer.iss`
  ya traía código que **detecta y descarga** los runtimes que falten; forzar self-contained habría duplicado
  ~150 MB en el instalador sin necesidad.
- **Límite deliberado de la localización (#7):** no se tradujeron las claves de parseo de la salida de
  `winget`, "porque deben coincidir literalmente con el CLI". ⚠️ **Esa premisa resultó FALSA y era un bug**
  — winget traduce su salida. Corregido en el Tier C #3.
- **Hallazgo de plataforma:** los `.ps1` se guardaron sin BOM UTF-8; PowerShell 5.1 los leyó con el codepage
  ANSI y los acentos/`—` **rompieron el tokenizer** ("Falta el paréntesis de cierre"). Reescritos con BOM
  (ver §4).
- **Bug de `release.ps1` corregido antes del primer corte:** el bump solo tocaba `<Version>`, dejando
  `<AssemblyVersion>`/`<FileVersion>` obsoletos — y tanto el título de la ventana como la comparación del
  updater leen **`AssemblyVersion`**. Ahora actualiza las tres etiquetas.
- **Lección operativa (pagada dos veces):** `release.ps1` solo hace `git add -u`, así que los ~20 archivos
  nuevos del Tier A hubo que `git add`earlos a mano; sin eso el tag habría apuntado a un build incompleto.
- **Verificado de punta a punta:** build del instalador con y sin firma (certificado autofirmado de prueba,
  creado y **eliminado** en la misma sesión), y `release.ps1 -DryRun` con las 78 pruebas corriendo inline.
  Con el cert autofirmado, `WinVerifyTrust` **rechaza** el instalador — lo que confirma que el gate de firma
  del updater funcionaba como se esperaba (un cert real de una CA pasaría; uno autofirmado no).
