# WingetUSoft — Hoja de ruta de características

> Mejoras agrupadas por **tiers**, manteniéndose siempre dentro del propósito del proyecto:
> **GUI para gestionar actualizaciones y desinstalaciones de software vía winget**. Cada item
> indica dónde viviría en la arquitectura por capas (`Core` lógica pura testeable · `Settings`
> persistencia · `UI` WinUI 3 · `Localization`). El detalle del estado vive en
> [`CONTEXT.md`](CONTEXT.md).
>
> Este tier nace de comparar WingetUSoft con su proyecto hermano **FormatDiskPro** (misma
> arquitectura, mismo autor) y portar la infraestructura de app que a WingetUSoft le faltaba,
> sin salirse de su propósito (nada de discos/S.M.A.R.T./benchmark/chkdsk, eso es territorio
> de FormatDiskPro).

---

## 🔄 Tier A — Paridad con FormatDiskPro

| # | Característica | Dónde | Estado |
|---|----------------|-------|--------|
| 0 | **Migración de tests MSTest → xUnit** | `tests/WingetUSoft.Tests/*.csproj` + los 3 archivos de test existentes | ✅ Implementado |
| 1 | **Infraestructura de localización** (`L`/`AppLang`, detección de idioma del sistema) | `Localization/Localization.cs`, `Settings/AppSettings.cs` (`Language`) | ✅ Implementado (menú principal; extracción total en #7) |
| 2 | **Changelog en actualizaciones + diálogo "Novedades"** | `Services/GitHubUpdateService.cs`, `Core/ReleaseNotes.cs`, `UI/WhatsNewDialog.xaml` | ✅ Implementado |
| 3 | **Aviso al terminar + progreso en la barra de tareas** | `UI/Notifier.cs`, `UI/TaskbarProgress.cs` | ✅ Implementado |
| 4 | **Velocidad y ETA en operaciones largas** | `Core/Throughput.cs` | ✅ Implementado |
| 5 | **Historial: búsqueda, filtros y exportación** | `Settings/HistoryFilter.cs`, `UI/HistoryWindow.xaml` | ✅ Implementado |
| 6 | **Diálogo Acerca de + licencia MIT + menú Ayuda** | `LICENSE`, `UI/AboutDialog.xaml` | ✅ Implementado |
| 7 | **Extracción completa de strings (5 idiomas)** | Todas las ventanas de `UI/` | ✅ Implementado |
| 8 | **Pipeline de release (Inno Setup)** | `installer/build-installer.ps1`, `release.ps1` | ✅ Implementado |

---

## 🚫 Deliberadamente fuera de alcance

Se excluyen a propósito para no desviar el producto de su propósito (gestión de software vía
winget, no gestión de discos):

- **Presets** — no aplica: la lista de exclusiones ya cubre ese rol para paquetes.
- **Autorefresco por `WM_DEVICECHANGE`** — es específico de detección de unidades extraíbles;
  WingetUSoft ya tiene auto-comprobación periódica configurable de actualizaciones.
- **S.M.A.R.T. / Benchmark / chkdsk** — funciones de diagnóstico de disco, exclusivas de
  FormatDiskPro.
- **Tema automático/claro/oscuro** — ya implementado (`AppSettings.ThemeMode`), no requiere port.
- **Backup de settings corruptos** — ya implementado (`AppSettings.TryBackupUnreadableSettingsFile`).
- **Cualquier empaquetador distinto de Inno Setup** (MSIX, ClickOnce, etc.) — el proyecto es y
  seguirá siendo unpackaged (`WindowsPackageType=None`); Inno Setup es la única vía de distribución.

---

## Sugerencia de priorización

**#0** (migración de tests) primero, para que el resto de fases pueda portar tests de
FormatDiskPro casi literalmente. **#1** (localización) antes que las features nuevas, para que
nazcan ya localizadas. **#2–#6** son features independientes entre sí, en el orden de impacto
percibido por el usuario. **#7** (extracción total de strings) al final, una sola pasada.
**#8** (pipeline de release) al cierre, cuando ya no cambian los artefactos a empaquetar.

→ **Tier A completado y verificado (2026-07-08/09):** las 9 fases (-1 a 8) están implementadas,
con build 0/0 y 78/78 tests en verde. El pipeline de release (`installer/build-installer.ps1` +
`release.ps1`) se verificó de extremo a extremo con hardware real: publicación, compilación del
instalador con Inno Setup, firma Authenticode con un certificado autofirmado de prueba (creado y
eliminado en la misma sesión) y `release.ps1 -DryRun` con las pruebas corriendo inline.

---

## 🎨 Tier B — Mejoras visuales y revisión

Nace de un reporte del usuario: la app no se adapta a todos los tipos de pantalla (capturado con
un botón "Cancelar" recortado contra el borde de la ventana). La causa raíz está confirmada en
código — ver detalle por ítem — y varios puntos son, de nuevo, paridad con un patrón que
**FormatDiskPro ya resolvió** (`FormatDiskPro/UI/MainWindow.xaml.cs:139-168`,
`SizeAndCenterWindow()`: tamaño en DIP escalado por DPI y acotado a `DisplayArea.WorkArea`).

| # | Característica | Dónde | Estado |
|---|----------------|-------|--------|
| 1 | **Ventanas adaptadas a DPI y área de trabajo** (port de `SizeAndCenterWindow`, reemplaza los `AppWindow.Resize(...)` fijos en píxeles) | `Core/WindowSizing.cs` + `UI/WindowSizer.cs`, aplicado en las 5 ventanas | ✅ Implementado y verificado |
| 2 | **Tamaño mínimo de ventana** (`OverlappedPresenter.PreferredMinimumWidth/Height`, escalado por DPI y acotado a WorkArea) | mismos 5 archivos que #1 | ✅ Implementado y verificado |
| 3 | **Barra de "Acciones rápidas" responsiva** (WrapPanel nativo propio: los botones bajan de fila cuando no caben — arregla el recorte reportado) | `UI/WrapPanel.cs` + `UI/MainWindow.xaml:96` | ✅ Implementado y verificado |
| 4 | **Columnas del DataGrid con ancho fijo en píxeles** — cabecera + filas en un `ScrollViewer` horizontal (`MinWidth`+`ViewportWidth`): rellena si cabe, hace scroll si no | `UI/MainWindow.xaml:329-434` | ✅ Implementado y verificado |
| 5 | **Revisión de longitud de texto por idioma** (FR/IT suelen ser 20–30% más largos que ES): `TextTrimming`+tooltip defensivos en cabeceras/labels de ancho fijo | Main/History/Uninstall/Cleanup (`UI/`) | ✅ Implementado y verificado |
| 6 | **Accesibilidad**: `AutomationProperties.Name` localizado en el icono "Excluido" del DataGrid; auditadas las demás ventanas/diálogos (sin más controles solo-icono sin etiqueta) | `UI/MainWindow.xaml:417-423` + `PackageViewModel.ExcludedLabel` | ✅ Implementado y verificado |
| 7 | **Snap layouts de Windows 11** — la ventana encaja en las celdas de snap de media y cuarto de pantalla, y el contenido que no cabe es alcanzable (página desplazable) en vez de recortarse fuera de la ventana | `Core/WindowSizing.cs` (clamp del mínimo a media WorkArea) + `UI/MainWindow.xaml` (`ContentScroller`) | ✅ Implementado y verificado |
| 8 | **Infraestructura de UI tests con FlaUI** — nuevo proyecto `WingetUSoft.UiTests` (paridad con `FormatDiskPro.UiTests`); smoke + regresión de layout sobre la app real, cubre #1–#6 | `tests/WingetUSoft.UiTests/` (`FlaUI.Core` + `FlaUI.UIA3`, xUnit) | ✅ Implementado y verificado (14/14) |

**Sobre #8 (UI tests con FlaUI):** nuevo proyecto `tests/WingetUSoft.UiTests/` que replica el
patrón ya probado en `FormatDiskPro.UiTests` — un `AppFixture` (`ICollectionFixture`) que lanza
el `.exe` compilado, obtiene la ventana con `UIA3Automation`, descarta los diálogos de arranque
(Novedades / actualización disponible, que WinUI abre solo en los primeros segundos) y un
`SettingsBackup` que respalda y restaura `%AppData%\WingetUSoft\settings.json` + `history.log`
para no filtrar los cambios de las pruebas en la instalación real del usuario.

Diferencia clave con FormatDiskPro: WingetUSoft corre `asInvoker` (eleva bajo demanda vía worker
+ named pipe, ver `Services/WingetService.cs`), así que el proceso de test **no** necesita una
terminal elevada y **no** debe cubrir las operaciones reales de winget (`upgrade`/`uninstall`
disparan UAC en el escritorio seguro, inautomatizable con FlaUI). El foco son las superficies
no elevadas y de solo lectura: adaptación DPI y tamaño mínimo (#1/#2), wrap de la barra de
acciones rápidas (#3), DataGrid en ventana angosta (#4), cambio de idioma en caliente (#5),
etiquetas de accesibilidad y orden de tabulación (#6) y la navegación por diálogos (Acerca de /
Novedades / Configuración / Historial). Así da cobertura de regresión precisamente a los arreglos
de layout que motivan esta tier.

→ **Tier B — #1–#6 y #8 completados (2026-07-10):** arreglos de layout/accesibilidad (#1–#6) con
build 0/0, **89/89 tests unitarios** (78 previos + 11 de `WindowSizingTests`) y **verificación visual
del usuario OK** (captura con el WrapPanel repartiendo los 7 botones en 2 filas sin recortes); más el
proyecto **`WingetUSoft.UiTests` (#8)** con FlaUI + UIA3, **14/14 UI tests en verde** (estables en 5
corridas), que ejercen la app real y dan regresión automática a los arreglos de layout (#1 DPI/WorkArea,
#3 wrap de acciones), navegación de diálogos, cambio de idioma en caliente y apertura de ventanas.
Nuevos `Core/WindowSizing.cs` (matemática pura, testeada), `UI/WindowSizer.cs` (DPI + WorkArea + mínimo),
`UI/WrapPanel.cs` (panel de envoltura nativo, cero dependencias) y `tests/WingetUSoft.UiTests/`
(`AppFixture`/`SettingsBackup`/`DialogHelper` portados de FormatDiskPro, **sin elevación** porque la app
corre asInvoker). Decisiones con el usuario: WrapPanel nativo propio (no CommunityToolkit ni CommandBar),
las 5 ventanas siguen redimensionables con mínimo acotado a la WorkArea.

→ **Tier B COMPLETADO (2026-07-11) con el cierre de #7 (snap layouts).** El ítem no se quedó en
verificación manual: al automatizarlo aparecieron **dos bugs reales**, ambos arreglados y con test.

1. **El mínimo de la ventana bloqueaba el snap.** El mínimo de diseño (900×600 DIP) superaba la celda
   de snap en casos normales — en un 1920×1080 la celda de cuarto mide 960×520, y el mínimo de alto
   (600) ya no cabía —, así que Windows no podía encoger la ventana lo suficiente.
   `WindowSizing.ScaleMinSize` ahora acota además **cada eje a la mitad del área de trabajo**, que es
   exactamente el tamaño de una celda de snap. No hace falta ningún número fijo: en monitores grandes
   el mínimo cómodo se conserva intacto y solo se relaja en pantallas pequeñas.
2. **La tabla desaparecía de la pantalla.** Con la ventana ya encogida a un cuarto, las tres tarjetas
   superiores (cabecera, acciones, filtros) consumían los ~510 px de alto **enteros**: el DataGrid, el
   log y la barra de estado quedaban recortados fuera de la ventana con `BoundingRectangle` 0×0 y sin
   barra de scroll con la que llegar a ellos. Ahora todo el contenido de `MainWindow.xaml` vive en un
   `ContentScroller` con `MinHeight` (no `Height`) atado al `ViewportHeight`: si sobra alto el Grid se
   estira hasta el viewport y la fila `*` rellena como siempre **sin barra** (media pantalla se ve
   idéntica a una ventana normal); si falta, el Grid conserva su alto natural y la página se desplaza.

**Verificado:** build 0/0, **93/93 unitarios** (89 + 4 de `ScaleMinSize` para las celdas de snap) y
**16/16 UI tests** (14 + 2 de `SnapLayoutTests`, estables en 3 corridas), que redimensionan la app real
a las celdas de media y cuarto de pantalla y comprueban que la ventana encaja, que los 7 botones siguen
visibles y que la tabla es **alcanzable desplazando** la página. Más captura de ambas celdas.

Detalle del estado y decisiones de esta tier en [`CONTEXT.md`](CONTEXT.md).

---

## 🧭 Tier C — Auditoría de UI/UX

Nace de una auditoría de UI/UX pedida por el usuario sobre la app ya funcionando (2026-07-11). A
diferencia del Tier B, que atacaba **layout** (que la ventana quepa), este ataca **flujo y feedback**
(que el usuario entienda qué está pasando y no le mientan los datos). Los bloques 1–3 destaparon tres
bugs de fondo que no se veían desde el layout.

| # | Bloque | Dónde | Estado |
|---|--------|-------|--------|
| 1 | **Flujo y feedback**: resumen único de fallos (fin del modal por paquete dentro del bucle), barra de estado anclada + `ProgressBar` determinada, estados de la tabla (cargando / sin datos / todo al día / sin coincidencias / cancelada / error) | `UI/MainWindow.xaml`+`.cs`, `Localization/` | ✅ Implementado y verificado |
| 2 | **Modelo de selección**: `_selectedIds` como fuente de verdad (la selección sobrevive a buscar/ordenar/filtrar), casilla tri-estado de "marcar todo", contador en el botón, `Ctrl+A` estándar, casillas deshabilitadas en filas excluidas | `UI/MainWindow.xaml`+`.cs` | ✅ Implementado y verificado |
| 3 | **Datos que mentían**: orden semántico de versiones (`Core/VersionOrder.cs`), eliminación de la columna "Tam." (winget no emite ese dato) y del `winget show` por paquete que la alimentaba, y parser de `winget show` multi-idioma (`Services/WingetShowLabels.cs`) | `Core/VersionOrder.cs`, `Services/WingetShowLabels.cs`, `Services/WingetService.cs`, `UI/MainWindow.xaml`+`.cs` | ✅ Implementado y verificado |
| 4 | **Jerarquía visual y uso del color**: el rojo hace hoy cuatro trabajos a la vez (acento del sistema, botón Cancelar, errores del log, icono de excluido); "Cancelar" no es destructivo y no debería vestirse de peligro | `UI/MainWindow.xaml`, `UI/UninstallWindow.xaml` | ✅ Implementado y verificado |
| 5 | **Preferencias en dos sitios**: el menú *Opciones* mezcla preferencias (Modo/Tema/Idioma) con acciones (Exportar/Historial/Desinstalar), y el resto de preferencias vive en *Configuración* | `UI/MainWindow.xaml`, `UI/SettingsWindow.xaml` | ✅ Implementado y verificado |
| 6 | **Accesibilidad de la tabla**: las cabeceras ordenables son `StackPanel` con `Tapped` — no se pueden enfocar ni activar con el teclado, y un lector de pantalla no las anuncia como botones | `UI/MainWindow.xaml` | ✅ Implementado y verificado |

→ **Tier C — #1–#3 completados (2026-07-11, release v1.5.0):** build 0/0, **124/124 unitarios**
(95 + 17 de `VersionOrderTests` + 12 de `ParsePackageInfoTests`) y **16/16 UI tests**. Verificado
además conduciendo la app real por UI Automation. Detalle en [`CONTEXT.md`](CONTEXT.md).

→ **Tier C COMPLETADO (2026-07-12, release v1.6.0) con el cierre de #4–#6.** Build 0/0,
**131/131 unitarios** (124 + 5 de `LogPaletteTests`, +2 de descarga en `GitHubUpdateServiceTests`) y
**24/24 UI tests** (16 + 8 de `SortHeaderTests`/`SettingsTests`). Verificado conduciendo la app real.

1. **#4 — El rojo ya solo significa una cosa: peligro.** *Cancelar* interrumpe un lote, no destruye
   nada: deja de ir vestido de rojo y pasa a ser un botón normal. El icono de "excluido" era del mismo
   rojo que los errores, cuando excluir es una decisión del usuario, no un fallo: ahora es gris
   secundario, coherente con la fila atenuada. El rojo queda para lo que de verdad destruye
   (*Desinstalar*, *Eliminar*) y para los fallos del registro.
   Al mirar los colores del registro apareció un **bug de contraste**: había UN solo juego de RGB
   cableado, el mismo para claro y para oscuro, elegido para fondo claro. Sobre la tarjeta oscura, el
   verde de los aciertos y el rojo de los fallos —justo lo que se busca en un registro— eran lo peor
   de leer. Ahora `Core/LogPalette.cs` tiene un color por tema, y `LogPaletteTests` **mide el
   contraste real de cada combinación y exige el 4.5:1 de WCAG AA**, así que un color mal elegido
   rompe el build. El registro ya escrito se repinta al cambiar de tema (`RecolorLog`).
2. **#5 — Configuración es el único hogar de las preferencias.** Modo/Tema/Idioma se mudan del menú a
   la ventana de *Configuración*, repartidos en *Apariencia* y *Actualizaciones*. El menú se queda
   solo con acciones y se llama *Herramientas*. Se cerró de paso un agujero: la ventana daba por
   guardados unos cambios que podían no haber llegado al disco — ahora, si `Save()` falla, avisa y
   **no se cierra**, en vez de perderlos en silencio.
3. **#6 — Las cabeceras ordenables son botones de verdad.** Eran `StackPanel` con `Tapped`: fuera del
   orden de tabulación, sordas a Espacio/Intro y anunciadas como un panel cualquiera. Ordenar la tabla
   era **imposible sin ratón**. Ahora son `Button` enfocables, y su nombre accesible lleva la columna y
   la dirección del orden ("Nombre, orden ascendente"), que antes solo estaba en el triángulo ▲/▼ —
   invisible para un lector de pantalla. Que `SortHeaderTests` pueda activarlas por `Invoke` es la
   prueba: es el mismo patrón de UI Automation que usan el teclado y los lectores de pantalla, y sobre
   un `StackPanel` no existe.

---

## 📣 Tier D — Cara pública y publicación

Nace de comparar de nuevo con **FormatDiskPro**, esta vez no en lo técnico sino en lo que ve alguien que
llega al repo desde fuera. El fondo ya estaba hecho (Tiers A–C; en verificación del updater,
accesibilidad y pipeline WingetUSoft va **por delante** del proyecto hermano), pero la **presentación**
seguía siendo la de un repo para compilar: sin badges, sin capturas, sin instrucciones de instalación y
sin los avisos de terceros que FormatDiskPro sí publica.

| # | Ítem | Dónde | Estado |
|---|------|-------|--------|
| 1 | **README orientado al usuario final**: badges, sección *Instalación* (Releases + qué runtimes descarga el instalador + aviso de SmartScreen), sección *Actualizaciones* con el **modelo de confianza** (Authenticode → SHA-256), enlace a esta hoja de ruta | `README.md` | ✅ Implementado y verificado |
| 2 | **Capturas de pantalla reproducibles**: script que conduce la app real por UI Automation y las regenera (tema claro/oscuro + Configuración), respaldando el `settings.json` del usuario | `tools/capture-screenshots.ps1`, `docs/screenshots/` | ✅ Implementado y verificado |
| 3 | **Avisos de terceros + licencia dentro de la app**: `THIRD-PARTY-NOTICES.txt`, ambos textos **embebidos** en el `.exe` y visibles en *Ayuda → Licencia* / *Ayuda → Avisos de terceros* | `THIRD-PARTY-NOTICES.txt`, `Core/LegalText.cs`, `UI/LegalTextDialog.xaml`, `UI/MainWindow.xaml` | ✅ Implementado y verificado |
| 4 | **Refresco de `CONTEXT.md`**: la versión decía «hoy 1.2.0» (real: 1.6.0) y los pendientes aún listaban «Publicar la 1.4.1», ya publicada | `CONTEXT.md` | ✅ Implementado |

→ **Tier D COMPLETADO (2026-07-12).** Build 0/0, **137/137 unitarios** (131 + 6 de `LegalTextTests`) y
**26/26 UI tests** (24 + 2 de los diálogos legales).

1. **#3 cerró una afirmación falsa, no solo una carencia.** El README decía que la licencia se podía
   consultar en *Ayuda → Acerca de*; ese diálogo solo muestra **una línea de copyright**, no el texto de
   la MIT. Ahora sí existe el visor, y el aviso de terceros lista lo que la app **redistribuye de
   verdad** (los `PackageReference` del `.csproj`: .NET, Windows App SDK y H.NotifyIcon.WinUI — MIT,
   comprobado en el `.nuspec` del paquete, no de memoria), citando winget aparte como herramienta
   externa que **no** se redistribuye.
2. **Los textos van embebidos como recurso, y por eso hay tests.** `LegalText` es defensivo: si el
   recurso no está, devuelve `""` y el diálogo dice "Texto no disponible" — un `LogicalName` mal escrito
   **no rompería el build** y el fallo solo se vería abriendo el menú a mano. `LegalTextTests` exige que
   ambos recursos existan y digan lo que deben; los 2 UI tests nuevos abren los diálogos en la app real
   y comprueban que el cuerpo **no** es el mensaje de "no disponible".
3. **#2 no son capturas hechas a mano.** El script fuerza tema e idioma, pulsa *Consultar
   actualizaciones*, espera a que la tabla tenga filas reales y dispara; respalda y restaura el
   `settings.json` real del usuario (la app es unpackaged, es el mismo archivo) y siembra
   `LastVersionSeen` para que el diálogo de Novedades no salte encima de la foto. Captura por
   `DWMWA_EXTENDED_FRAME_BOUNDS` (no `GetWindowRect`, que arrastra el margen invisible del DWM) y con
   `SetProcessDPIAware()` (sin eso, en un monitor escalado la captura sale desplazada).

Detalle del estado y decisiones de esta tier en [`CONTEXT.md`](CONTEXT.md).
