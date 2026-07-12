# WingetUSoft

Interfaz gráfica (WinUI 3) para gestionar actualizaciones y desinstalaciones de software mediante **winget** en Windows.

## Características

### Actualizaciones
- **Consulta de actualizaciones** — lista todos los paquetes con versión disponible, con soporte para versiones desconocidas (`<`).
- **Actualización selectiva** — marca los paquetes con su casilla (o todos de golpe con la casilla de la cabecera / `Ctrl+A`) y actualízalos. El botón muestra cuántos hay marcados y se deshabilita si no hay ninguno. La selección **sobrevive a buscar, ordenar y filtrar**.
- **Modo silencioso / interactivo** — compatible con las flags `--silent` e `--interactive` de winget.
- **Elevación de permisos** — ejecuta lotes elevados mediante un worker interno con comunicación por named pipe, sin scripts temporales en disco. Incluye reporte de progreso de descarga en tiempo real durante la instalación elevada.
- **Progreso siempre visible** — la barra de estado está anclada al pie de la ventana (fuera de la página desplazable) e incluye una barra de progreso que avanza también *dentro* de cada paquete, según lo descargado.
- **Resumen único de fallos** — si varios paquetes fallan, se informa en un solo diálogo al terminar el lote, en vez de interrumpirlo con un modal por cada fallo.

### Exploración y filtrado
- **Búsqueda en tiempo real** — filtra la lista de paquetes por nombre o ID mientras escribes.
- **Columnas ordenables** — ordena por nombre, ID, versión instalada, versión disponible o fuente haciendo clic en el encabezado. Las versiones se ordenan **numéricamente** (`1.9.0` antes que `1.10.0`), no como texto.
- **Estados de la tabla** — la tabla dice siempre en qué punto está: consultando, sin datos todavía, todo al día, sin coincidencias con los filtros, o consulta cancelada/fallida.
- **Panel de información** — al seleccionar un paquete muestra su descripción, un enlace a la página oficial y otro a las notas de versión. Funciona **en cualquier idioma de Windows** (winget traduce las etiquetas de su salida).
- **Ver en winget.run** — abre la página del paquete en [winget.run](https://winget.run) desde el menú contextual.

### Desinstalación
- **Ventana de desinstalación** — lista todos los programas instalados con búsqueda y filtrado, y permite desinstalar cualquier paquete con confirmación previa.

### Gestión y configuración
Todas las preferencias viven en un solo sitio, la ventana **Configuración**; el menú **Herramientas**
solo tiene acciones (exportar, historial, desinstalar).

- **Lista de exclusiones** — excluye paquetes permanentemente de las actualizaciones automáticas.
- **Historial** — registra cada actualización con fecha, versiones y resultado (máx. 500 entradas).
- **Exportación** — exporta la lista a CSV o TSV con neutralización de fórmulas (seguro para Excel/Calc).
- **Tema claro / oscuro** — integrado con el sistema de temas de Windows y configurable manualmente.
- **Idioma** — español, inglés, portugués, francés e italiano, aplicados en caliente.
- **Modo de actualización** — silenciosa o interactiva, y opción de ejecutar como administrador.
- **Auto-comprobación** — comprueba actualizaciones de forma periódica configurable (30 / 60 / 120 min).
- **Log de archivo** — logging opcional por día en `%LocalAppData%\WingetUSoft\logs\`.

### Accesibilidad
- **Manejable solo con teclado** — incluidas las cabeceras de la tabla, que son botones enfocables y
  anuncian por qué columna y en qué dirección está ordenada.
- **Contraste verificado** — los colores del registro de actividad cumplen WCAG AA (4.5:1) en tema
  claro y oscuro, comprobado por tests.

## Requisitos

| Componente | Versión mínima |
|---|---|
| Windows | 10 (build 19041) o superior |
| .NET | 10 |
| Windows App SDK | 1.8 |
| winget (App Installer) | Cualquier versión reciente desde Microsoft Store |

## Compilar y ejecutar

```bash
# Compilar
dotnet build WingetUSoft.slnx

# Ejecutar
dotnet run --project src/WingetUSoft/WingetUSoft.csproj

# Ejecutar tests unitarios (xUnit)
dotnet test tests/WingetUSoft.Tests/WingetUSoft.Tests.csproj

# Ejecutar tests de UI (FlaUI — lanzan la app real; requieren sesión de escritorio
# interactiva, pero NO elevación: la app corre asInvoker)
dotnet test tests/WingetUSoft.UiTests/WingetUSoft.UiTests.csproj
```

## Estructura del proyecto

```
WingetUSoft/
├── src/WingetUSoft/             # Proyecto de aplicación (WinUI 3)
│   ├── Program.cs              # Entry point
│   │
│   ├── Core/                   # Lógica de negocio pura (sin UI ni efectos externos)
│   │   ├── ReleaseNotes.cs        # Notas de versión (Markdown → texto plano)
│   │   ├── Throughput.cs          # ETA de descargas/operaciones largas
│   │   ├── VersionOrder.cs        # Orden semántico de versiones (1.9 < 1.10, "< x", "Unknown")
│   │   ├── WindowSizing.cs        # Dimensionado/centrado por DPI (puro, testeable)
│   │   ├── DelimitedTextExporter.cs # Exportación CSV/TSV segura
│   │   └── Models/
│   │       ├── WingetPackage.cs         # Paquete con versión disponible/instalada
│   │       ├── WingetPackageInfo.cs     # Metadatos enriquecidos (winget show)
│   │       ├── WingetProgressInfo.cs    # Progreso de descarga/instalación
│   │       ├── CleanupItemViewModel.cs  # ViewModel para la ventana de limpieza
│   │       ├── UpgradeResult.cs
│   │       └── UpgradeBatchResult.cs
│   │
│   ├── Services/               # Operaciones con efectos externos (procesos, red, disco)
│   │   ├── WingetService.cs       # Ejecución de winget, parsing, elevación
│   │   ├── WingetShowLabels.cs    # Etiquetas de `winget show` en los 10 idiomas que winget traduce
│   │   ├── GitHubUpdateService.cs # Auto-actualización desde GitHub Releases
│   │   └── CleanupScanner.cs      # Detección de residuos post-desinstalación
│   │
│   ├── Settings/               # Persistencia y configuración
│   │   ├── AppSettings.cs         # Carga/guardado JSON, paths, log
│   │   ├── HistoryEntry.cs        # DTO de entrada de historial
│   │   └── HistoryFilter.cs       # Filtrado del historial (lógica pura)
│   │
│   ├── Localization/           # Cadenas ES/EN/PT/FR/IT (patrón L.T("clave"))
│   │
│   ├── UI/                     # Capa de presentación (WinUI 3)
│   │   ├── MainWindow.xaml/.cs      # Ventana principal (actualizaciones)
│   │   ├── SettingsWindow.xaml/.cs  # Diálogo de configuración
│   │   ├── HistoryWindow.xaml/.cs   # Vista de historial
│   │   ├── UninstallWindow.xaml/.cs # Ventana de desinstalación
│   │   ├── CleanupWindow.xaml/.cs   # Ventana de limpieza de residuos
│   │   ├── WrapPanel.cs            # Panel de envoltura nativo (barra de acciones responsiva)
│   │   ├── WindowSizer.cs          # Wrapper DPI + WorkArea + tamaño mínimo (usa Core/WindowSizing)
│   │   ├── Converters.cs           # Convertidores de valor para XAML
│   │   ├── TitleBarHelper.cs       # Helper compartido para colores del title bar
│   │   └── WindowDialogHelper.cs   # Helper compartido para diálogos modales
│   │
│   └── installer/             # Inno Setup (installer.iss) + build-installer.ps1
│
├── tests/WingetUSoft.Tests/    # Tests unitarios (xUnit)
│   ├── AppSettingsTests.cs
│   ├── WindowSizingTests.cs    # Dimensionado por DPI/WorkArea (Tier B #1/#2)
│   └── ...
│
└── tests/WingetUSoft.UiTests/  # Tests de UI end-to-end (FlaUI + UIA3, xUnit)
    ├── AppFixture.cs           # Lanza el .exe, obtiene la ventana, respalda settings
    ├── LayoutTests.cs          # Regresión de layout: DPI/WorkArea + wrap de acciones
    └── ...
```

## Configuración

Los ajustes se almacenan en `%LocalAppData%\WingetUSoft\settings.json`. Si el archivo se corrompe, se crea una copia de seguridad automáticamente con timestamp y se restauran los valores por defecto.

## Datos de usuario

| Artefacto | Ruta |
|---|---|
| Configuración | `%LocalAppData%\WingetUSoft\settings.json` |
| Logs diarios | `%LocalAppData%\WingetUSoft\logs\YYYY-MM-DD.log` |

## Licencia

Software libre distribuido bajo la **[MIT License](LICENSE)**: puedes usarlo, modificarlo y
redistribuirlo libremente, incluso en proyectos privativos, conservando el aviso de copyright.
Se ofrece **SIN NINGUNA GARANTÍA**. La licencia también se puede consultar dentro de la app en
*Ayuda → Acerca de*.

## Privacidad

La aplicación **no recopila datos personales ni telemetría**. Se conecta a Internet únicamente
para consultar/instalar paquetes vía winget y para comprobar actualizaciones de la propia app en
GitHub Releases (HTTPS).
