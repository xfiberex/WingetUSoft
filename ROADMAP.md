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
| 1 | **Ventanas adaptadas a DPI y área de trabajo** (port de `SizeAndCenterWindow`, reemplaza los `AppWindow.Resize(...)` fijos en píxeles) | `UI/MainWindow.xaml.cs:120`, `UI/SettingsWindow.xaml.cs:25`, `UI/UninstallWindow.xaml.cs:35`, `UI/CleanupWindow.xaml.cs:34`, `UI/HistoryWindow.xaml.cs:49` | ⏳ Pendiente |
| 2 | **Tamaño mínimo de ventana** (`OverlappedPresenter.PreferredMinimumWidth/Height`, hoy sin definir en ninguna ventana) | mismos 5 archivos que #1 | ⏳ Pendiente |
| 3 | **Barra de "Acciones rápidas" responsiva** (wrap o menú de desborde cuando los 7 botones no caben — causa directa del recorte reportado) | `UI/MainWindow.xaml:96` (`StackPanel Orientation="Horizontal"`, sin wrap) | ⏳ Pendiente |
| 4 | **Columnas del DataGrid con ancho fijo en píxeles** — revisar/adaptar comportamiento en ventana angosta | `UI/MainWindow.xaml:332-341` (cabecera) y `:392-401` (`lvPackages.ItemTemplate`); solo "Nombre" es `*` | ⏳ Pendiente |
| 5 | **Revisión de longitud de texto por idioma** (FR/IT suelen ser 20–30% más largos que ES) contra botones/columnas de ancho fijo, tras la extracción de 272 claves de la Fase 7 de Tier A | todas las ventanas de `UI/` | ⏳ Pendiente |
| 6 | **Accesibilidad**: `AutomationProperties.Name` en controles solo-icono (ej. el `FontIcon` de "Excluido" en la fila del DataGrid no tiene etiqueta accesible) y orden de tabulación | `UI/MainWindow.xaml:417-423` y resto de ventanas de `UI/` | ⏳ Pendiente |
| 7 | **Snap layouts de Windows 11** — verificar que las ventanas quepan en snap de media/cuarto de pantalla en portátiles de resolución baja (consecuencia directa de #1/#2, verificación manual) | — | ⏳ Pendiente |
| 8 | **Infraestructura de UI tests con FlaUI** — nuevo proyecto `WingetUSoft.UiTests` (paridad con `FormatDiskPro.UiTests`); smoke + regresión de layout sobre la app real, cubre automáticamente #1–#6 | `tests/WingetUSoft.UiTests/` (`FlaUI.Core` + `FlaUI.UIA3`, xUnit) | ⏳ Pendiente |

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

Detalle del estado y decisiones de esta tier en [`CONTEXT.md`](CONTEXT.md) cuando arranque la
implementación.
