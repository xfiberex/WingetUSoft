# WingetUSoft

Interfaz gráfica (WinForms) para gestionar actualizaciones de software mediante **winget** en Windows.

## Características

- **Consulta de actualizaciones** — lista todos los paquetes con versión disponible, con soporte para versiones desconocidas (`<`).
- **Actualización selectiva** — selecciona individualmente los paquetes a actualizar o actualiza todos de una vez.
- **Modo silencioso / interactivo** — compatible con las flags `--silent` e `--interactive` de winget.
- **Elevación de permisos** — ejecuta lotes elevados mediante un worker interno con comunicación por named pipe, sin scripts temporales en disco.
- **Lista de exclusiones** — excluye paquetes permanentemente de las actualizaciones automáticas.
- **Historial** — registra cada actualización con fecha, versiones y resultado (máx. 500 entradas).
- **Exportación** — exporta la lista a CSV o TSV con neutralización de fórmulas (seguro para Excel/Calc).
- **Tema claro / oscuro** — paleta de colores completa aplicada a todos los controles.
- **Auto-comprobación** — comprueba actualizaciones de forma periódica configurable (30 / 60 / 120 min).
- **Log de archivo** — logging opcional por día en `%LocalAppData%\WingetUSoft\logs\`.

## Requisitos

| Componente | Versión mínima |
|---|---|
| Windows | 10 (build 1809) o superior |
| .NET | 10 |
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
│       ├── WingetPackage.cs
│       ├── WingetProgressInfo.cs
│       ├── UpgradeResult.cs
│       └── UpgradeBatchResult.cs
│
├── Settings/                    # Persistencia y configuración
│   ├── AppSettings.cs           # Carga/guardado JSON, paths, log
│   └── HistoryEntry.cs          # DTO de entrada de historial
│
├── UI/                          # Capa de presentación
│   ├── Forms/
│   │   ├── FormApp.cs/.Designer.cs/.resx   # Ventana principal
│   │   ├── FormHistory.cs       # Vista de historial
│   │   └── FormSettings.cs      # Diálogo de configuración
│   └── Theme/
│       └── UiTheme.cs           # Paletas, renderers, helpers de estilo
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
