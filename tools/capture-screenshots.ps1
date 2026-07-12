<#
.SYNOPSIS
    Captura las capturas de pantalla del README conduciendo la app real por UI Automation.

.DESCRIPTION
    Lanza el WingetUSoft.exe ya compilado, fuerza tema e idioma, pulsa "Consultar actualizaciones",
    espera a que la tabla se llene y guarda un PNG por tema en docs/screenshots/.

    La app es unpackaged: su settings.json vive en %LocalAppData%\WingetUSoft, el mismo archivo que
    usa la instalación real del usuario. El script lo RESPALDA antes de tocarlo y lo RESTAURA siempre
    (incluso si falla), igual que hace SettingsBackup en los UI tests.

    Requiere sesión de escritorio interactiva y desatendida: la captura es una copia literal de lo que
    hay en pantalla (Graphics.CopyFromScreen sobre el rectángulo de la ventana), así que la ventana
    debe quedar en primer plano y sin nada encima. No requiere elevación (la app corre asInvoker).

.PARAMETER Exe
    Ruta al WingetUSoft.exe. Por defecto, el más reciente bajo src\WingetUSoft\bin\.

.PARAMETER Theme
    light, dark o both (por defecto).

.PARAMETER Language
    Idioma de la UI para las capturas: es, en, pt, fr, it. Por defecto es.

.PARAMETER SkipQuery
    No pulsa "Consultar actualizaciones" (captura la app en su estado vacío inicial).

.EXAMPLE
    .\tools\capture-screenshots.ps1
    .\tools\capture-screenshots.ps1 -Theme dark -Language en
#>
[CmdletBinding()]
param(
    [string]$Exe,
    [ValidateSet('light', 'dark', 'both')][string]$Theme = 'both',
    [ValidateSet('es', 'en', 'pt', 'fr', 'it')][string]$Language = 'es',
    [string]$OutDir,
    [int]$Width = 1280,
    [int]$Height = 860,
    [int]$QueryTimeoutSec = 180,
    [switch]$SkipQuery
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutDir) { $OutDir = Join-Path $repoRoot 'docs\screenshots' }

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class Win32Capture
{
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hWnd, int attr, out RECT value, int size);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
}
'@

# El proceso de PowerShell debe ser DPI-aware o las coordenadas de pantalla vendrían virtualizadas
# (en un monitor al 125/150 % la captura saldría desplazada y recortada).
[void][Win32Capture]::SetProcessDPIAware()

function Resolve-Exe {
    if ($Exe) {
        if (-not (Test-Path $Exe)) { throw "No existe el ejecutable: $Exe" }
        return (Resolve-Path $Exe).Path
    }
    $binRoot = Join-Path $repoRoot 'src\WingetUSoft\bin'
    if (-not (Test-Path $binRoot)) { throw "No hay build. Ejecuta primero: dotnet build WingetUSoft.slnx" }
    $candidate = Get-ChildItem -Path $binRoot -Filter 'WingetUSoft.exe' -Recurse -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $candidate) { throw "No se encontró WingetUSoft.exe bajo $binRoot. Ejecuta: dotnet build WingetUSoft.slnx" }
    return $candidate.FullName
}

function Get-SettingsPath {
    Join-Path $env:LOCALAPPDATA 'WingetUSoft\settings.json'
}

# Escribe un settings.json a medida para la captura: tema e idioma fijos, y LastVersionSeen igual a la
# versión del .exe para que NO salte el diálogo de Novedades por encima de la ventana que queremos.
function Set-CaptureSettings([string]$exePath, [int]$themeMode) {
    $settingsPath = Get-SettingsPath
    $dir = Split-Path -Parent $settingsPath
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }

    $version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exePath)
    $seen = "{0}.{1}.{2}" -f $version.FileMajorPart, $version.FileMinorPart, $version.FileBuildPart

    $settings = [ordered]@{
        SilentMode               = $true
        RunUpdatesAsAdministrator = $false
        AutoCheckIntervalMinutes = 0
        ThemeMode                = $themeMode
        LogToFile                = $false
        MinimizeToTray           = $false
        ShowNotifications        = $false
        Language                 = $Language
        LastVersionSeen          = $seen
    }
    $settings | ConvertTo-Json | Set-Content -Path $settingsPath -Encoding UTF8
}

function Find-MainWindow([int]$processId, [int]$timeoutSec = 40) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $processId)
    while ((Get-Date) -lt $deadline) {
        $w = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
        if ($w -and $w.Current.BoundingRectangle.Width -gt 0) { return $w }
        Start-Sleep -Milliseconds 300
    }
    throw "La ventana principal no apareció en $timeoutSec s."
}

function Find-ByAutomationId($parent, [string]$automationId, [int]$timeoutSec = 20) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $automationId)
    while ((Get-Date) -lt $deadline) {
        $el = $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
        if ($el) { return $el }
        Start-Sleep -Milliseconds 300
    }
    throw "No se encontró el control '$automationId'."
}

function Invoke-Element($element) {
    $pattern = $element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $pattern.Invoke()
}

# Un DropDownButton (Herramientas/Ayuda) expone ExpandCollapse, no Invoke — igual que en MenuActions
# de los UI tests. Sus ítems de MenuFlyout cuelgan del árbol de la propia ventana (XamlRoot), así que
# se buscan como descendientes de $window, no del escritorio.
function Expand-Element($element) {
    $pattern = $element.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
    $pattern.Expand()
}

# La consulta lanza winget y tarda; se espera a que el ListView tenga filas reales.
function Wait-ForPackages($window, [int]$timeoutSec) {
    $list = Find-ByAutomationId $window 'lvPackages'
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ListItem)
    while ((Get-Date) -lt $deadline) {
        $items = $list.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
        if ($items.Count -gt 0) {
            Write-Host "  Tabla cargada: $($items.Count) paquetes." -ForegroundColor DarkGray
            Start-Sleep -Seconds 2   # deja que terminen las animaciones/relleno de la tabla
            return $items.Count
        }
        Start-Sleep -Milliseconds 500
    }
    Write-Warning "  La tabla sigue vacía tras $timeoutSec s (¿winget sin actualizaciones pendientes?). Se captura igual."
    return 0
}

function Save-WindowPng($hwnd, [string]$path) {
    [void][Win32Capture]::SetForegroundWindow($hwnd)
    Start-Sleep -Milliseconds 900   # deja que el DWM termine de repintar la ventana ya en primer plano

    $rect = New-Object Win32Capture+RECT
    $size = [System.Runtime.InteropServices.Marshal]::SizeOf($rect)
    # EXTENDED_FRAME_BOUNDS (no GetWindowRect): excluye el margen invisible de redimensionado del DWM,
    # que si no aparece como un borde transparente/negro alrededor de la captura.
    [void][Win32Capture]::DwmGetWindowAttribute($hwnd, [Win32Capture]::DWMWA_EXTENDED_FRAME_BOUNDS, [ref]$rect, $size)

    $w = $rect.Right - $rect.Left
    $h = $rect.Bottom - $rect.Top
    if ($w -le 0 -or $h -le 0) { throw "Rectángulo de ventana inválido ($w x $h)." }

    $bmp = New-Object System.Drawing.Bitmap $w, $h
    try {
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        try {
            $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size $w, $h))
        } finally { $g.Dispose() }
        $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    } finally { $bmp.Dispose() }

    Write-Host "  Guardada: $path ($w x $h)" -ForegroundColor Green
}

function Capture-Theme([string]$exePath, [string]$themeName) {
    $themeMode = if ($themeName -eq 'dark') { 2 } else { 1 }
    Write-Host "Capturando tema $themeName..." -ForegroundColor Cyan

    Set-CaptureSettings $exePath $themeMode

    $proc = Start-Process -FilePath $exePath -PassThru
    try {
        $window = Find-MainWindow $proc.Id
        $hwnd = [IntPtr]$window.Current.NativeWindowHandle

        [void][Win32Capture]::ShowWindow($hwnd, 9)      # SW_RESTORE
        [void][Win32Capture]::MoveWindow($hwnd, 60, 40, $Width, $Height, $true)
        Start-Sleep -Milliseconds 700

        if (-not $SkipQuery) {
            Write-Host "  Consultando actualizaciones (winget)..." -ForegroundColor DarkGray
            Invoke-Element (Find-ByAutomationId $window 'btnConsultar')
            [void](Wait-ForPackages $window $QueryTimeoutSec)
        }

        Save-WindowPng $hwnd (Join-Path $OutDir "main-$themeName.png")

        # Ventana "Buscar e instalar" (Tier E): se captura solo en oscuro, con una búsqueda real hecha.
        if ($themeName -eq 'dark') {
            try {
                Expand-Element (Find-ByAutomationId $window 'btnHerramientas')
                Start-Sleep -Milliseconds 600
                Invoke-Element (Find-ByAutomationId $window 'menuBuscarInstalar')
                Start-Sleep -Seconds 2

                $root = [System.Windows.Automation.AutomationElement]::RootElement
                $pidCond = New-Object System.Windows.Automation.PropertyCondition(
                    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)

                $searchWindow = $null
                $deadline = (Get-Date).AddSeconds(15)
                while ((Get-Date) -lt $deadline -and -not $searchWindow) {
                    foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $pidCond)) {
                        if ($w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
                            (New-Object System.Windows.Automation.PropertyCondition(
                                [System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'btnBuscar')))) {
                            $searchWindow = $w; break
                        }
                    }
                    if (-not $searchWindow) { Start-Sleep -Milliseconds 400 }
                }

                if ($searchWindow) {
                    $sHwnd = [IntPtr]$searchWindow.Current.NativeWindowHandle
                    [void][Win32Capture]::MoveWindow($sHwnd, 90, 30, 1100, 760, $true)
                    Start-Sleep -Milliseconds 600

                    # Una búsqueda de verdad: una ventana vacía no enseña nada de la característica.
                    $box = Find-ByAutomationId $searchWindow 'txtBuscar'
                    $box.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('7zip')
                    Invoke-Element (Find-ByAutomationId $searchWindow 'btnBuscar')

                    $list = Find-ByAutomationId $searchWindow 'lvResults'
                    $itemCond = New-Object System.Windows.Automation.PropertyCondition(
                        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                        [System.Windows.Automation.ControlType]::ListItem)
                    $deadline = (Get-Date).AddSeconds(120)
                    while ((Get-Date) -lt $deadline) {
                        if ($list.FindAll([System.Windows.Automation.TreeScope]::Descendants, $itemCond).Count -gt 0) { break }
                        Start-Sleep -Milliseconds 500
                    }
                    Start-Sleep -Seconds 2

                    Save-WindowPng $sHwnd (Join-Path $OutDir 'search-dark.png')
                    $searchWindow.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()
                    Start-Sleep -Seconds 1
                } else {
                    Write-Warning "  No apareció la ventana de búsqueda; se omite esa captura."
                }
            } catch {
                Write-Warning "  No se pudo capturar la ventana de búsqueda: $($_.Exception.Message)"
            }
        }

        # Ventana de Configuración: se captura solo en oscuro (basta una muestra de la ventana).
        # Va en try/catch aparte: si la navegación del menú falla, la captura principal (ya guardada)
        # no debe perderse por ello.
        if ($themeName -eq 'dark') {
            try {
                Expand-Element (Find-ByAutomationId $window 'btnHerramientas')
                Start-Sleep -Milliseconds 600
                Invoke-Element (Find-ByAutomationId $window 'menuConfiguracion')
                Start-Sleep -Seconds 2

                $root = [System.Windows.Automation.AutomationElement]::RootElement
                $cond = New-Object System.Windows.Automation.PropertyCondition(
                    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)

                $settingsWindow = $null
                $deadline = (Get-Date).AddSeconds(15)
                while ((Get-Date) -lt $deadline -and -not $settingsWindow) {
                    foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)) {
                        if ([IntPtr]$w.Current.NativeWindowHandle -ne $hwnd) { $settingsWindow = $w; break }
                    }
                    if (-not $settingsWindow) { Start-Sleep -Milliseconds 400 }
                }
                if ($settingsWindow) {
                    # La ventana abre a su tamaño natural y las tarjetas quedan recortadas por el pie
                    # (la página es desplazable). Se agranda antes de capturar para que se vean enteras.
                    # Se agranda hasta casi el alto del área de trabajo (sin invadir la barra de tareas:
                    # la captura es de pantalla, y la barra se dibujaría encima del pie de la ventana).
                    $sHwnd = [IntPtr]$settingsWindow.Current.NativeWindowHandle
                    $work = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
                    [void][Win32Capture]::MoveWindow($sHwnd, 120, 4, 820, ($work.Height - 8), $true)
                    Start-Sleep -Milliseconds 800
                    Save-WindowPng $sHwnd (Join-Path $OutDir 'settings-dark.png')
                } else {
                    Write-Warning "  No apareció la ventana de Configuración; se omite esa captura."
                }
            } catch {
                Write-Warning "  No se pudo capturar la ventana de Configuración: $($_.Exception.Message)"
            }
        }
    } finally {
        if (-not $proc.HasExited) {
            [void]$proc.CloseMainWindow()
            Start-Sleep -Seconds 2
            if (-not $proc.HasExited) { $proc.Kill() }
        }
    }
}

# ── Ejecución ───────────────────────────────────────────────────────────────────
$exePath = Resolve-Exe
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir -Force | Out-Null }

Write-Host "Ejecutable: $exePath" -ForegroundColor Gray
Write-Host "Salida:     $OutDir" -ForegroundColor Gray

# Respaldo del settings.json real del usuario (la app es unpackaged: es el mismo archivo).
$settingsPath = Get-SettingsPath
$backup = $null
if (Test-Path $settingsPath) {
    $backup = Join-Path $env:TEMP ("WingetUSoft.settings.capture.{0}.bak" -f (Get-Date -Format 'yyyyMMddHHmmss'))
    Copy-Item $settingsPath $backup -Force
    Write-Host "Respaldo de settings.json: $backup" -ForegroundColor Gray
}

try {
    $themes = if ($Theme -eq 'both') { @('light', 'dark') } else { @($Theme) }
    foreach ($t in $themes) { Capture-Theme $exePath $t }
    Write-Host "`nCapturas completadas en $OutDir" -ForegroundColor Green
} finally {
    if ($backup) {
        Copy-Item $backup $settingsPath -Force
        Remove-Item $backup -Force
        Write-Host "settings.json restaurado." -ForegroundColor Gray
    } elseif (Test-Path $settingsPath) {
        # No había settings previo: la app se estrenó con esta captura; no dejamos rastro.
        Remove-Item $settingsPath -Force
        Write-Host "settings.json de captura eliminado (no había uno previo)." -ForegroundColor Gray
    }
}
