# WingetUSoft

Interfaz gráfica (WinUI 3) para gestionar actualizaciones y desinstalaciones de software mediante **winget** en Windows.

## Características

### Actualizaciones
- **Consulta de actualizaciones** — lista todos los paquetes con versión disponible, con soporte para versiones desconocidas (`<`).
- **Actualización selectiva** — selecciona individualmente los paquetes a actualizar o actualiza todos de una vez.
- **Modo silencioso / interactivo** — compatible con las flags `--silent` e `--interactive` de winget.
- **Elevación de permisos** — ejecuta lotes elevados mediante un worker interno con comunicación por named pipe, sin scripts temporales en disco. Incluye reporte de progreso de descarga en tiempo real durante la instalación elevada.

### Exploración y filtrado
- **Búsqueda en tiempo real** — filtra la lista de paquetes por nombre o ID mientras escribes.
- **Columnas ordenables** — ordena por nombre, ID, versión instalada, versión disponible, tamaño o fuente haciendo clic en el encabezado.
- **Columna de tamaño** — muestra el tamaño del instalador de cada paquete, cargado en segundo plano.
- **Panel de información** — al seleccionar un paquete muestra descripción, tamaño, enlace a la página oficial y enlace a las notas de versión.
- **Ver en winget.run** — abre la página del paquete en [winget.run](https://winget.run) desde el menú contextual.

### Desinstalación
- **Ventana de desinstalación** — lista todos los programas instalados con búsqueda y filtrado, y permite desinstalar cualquier paquete con confirmación previa.

### Gestión y configuración
- **Lista de exclusiones** — excluye paquetes permanentemente de las actualizaciones automáticas.
- **Historial** — registra cada actualización con fecha, versiones y resultado (máx. 500 entradas).
- **Exportación** — exporta la lista a CSV o TSV con neutralización de fórmulas (seguro para Excel/Calc).
- **Tema claro / oscuro** — integrado con el sistema de temas de Windows y configurable manualmente.
- **Auto-comprobación** — comprueba actualizaciones de forma periódica configurable (30 / 60 / 120 min).
- **Log de archivo** — logging opcional por día en `%LocalAppData%\WingetUSoft\logs\`.

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
dotnet build WingetUSoft.csproj

# Ejecutar
dotnet run --project WingetUSoft.csproj

# Ejecutar tests
dotnet test WingetUSoft.Tests/WingetUSoft.Tests.csproj
```

## Estructura del proyecto

```
WingetUSoft/
├── Program.cs                   # Entry point
│
├── Core/                        # Lógica de negocio (sin dependencias de UI)
│   ├── WingetService.cs         # Ejecución de winget, parsing, elevación
│   ├── DelimitedTextExporter.cs # Exportación CSV/TSV segura
│   └── Models/
│       ├── WingetPackage.cs       # Paquete con versión disponible
│       ├── WingetPackageInfo.cs   # Metadatos enriquecidos (winget show)
│       ├── WingetProgressInfo.cs  # Progreso de descarga/instalación
│       ├── UpgradeResult.cs
│       └── UpgradeBatchResult.cs
│
├── Settings/                    # Persistencia y configuración
│   ├── AppSettings.cs           # Carga/guardado JSON, paths, log
│   └── HistoryEntry.cs          # DTO de entrada de historial
│
├── UI/                          # Capa de presentación (WinUI 3)
│   ├── MainWindow.xaml/.cs      # Ventana principal
│   ├── SettingsWindow.xaml/.cs  # Diálogo de configuración
│   ├── HistoryWindow.xaml/.cs   # Vista de historial
│   └── UninstallWindow.xaml/.cs # Ventana de desinstalación
│
└── WingetUSoft.Tests/           # Tests unitarios (MSTest)
    ├── AppSettingsTests.cs
    └── WingetServiceTests.cs
```

## Configuración

Los ajustes se almacenan en `%LocalAppData%\WingetUSoft\settings.json`. Si el archivo se corrompe, se crea una copia de seguridad automáticamente con timestamp y se restauran los valores por defecto.

## Datos de usuario

| Artefacto | Ruta |
|---|---|
| Configuración | `%LocalAppData%\WingetUSoft\settings.json` |
| Logs diarios | `%LocalAppData%\WingetUSoft\logs\YYYY-MM-DD.log` |
