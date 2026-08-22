<#
.SYNOPSIS
    Verificación completa del repositorio, en local.

.DESCRIPTION
    Una sola definición de «esto está verificado», para que no dependa de acordarse de lanzar
    'dotnet test' a mano ni de esperar al siguiente release:

      1. Compilación de la solución con las advertencias tratadas como error.
      2. Pruebas unitarias.
      3. Dependencias vulnerables (aborta) y desactualizadas (solo informa).
      4. Con -Full: además los UI tests de FlaUI, que conducen la app real.

    POR QUÉ NO ES CI. Decisión cerrada del proyecto (ver ROADMAP.md, T2-12): nada de GitHub
    Actions ni runners hospedados. La verificación baja al equipo de desarrollo, y con ella
    entra en el flujo justo lo que un runner hospedado NO podría correr: los UI tests, que
    necesitan una sesión de escritorio interactiva.

    Lo usan el hook .githooks/pre-push (variante rápida) y release.ps1 (que lo llama en vez de
    repetir sus pasos).

    NOTA: 'dotnet format --verify-no-changes' se añadirá aquí cuando se cierre T3-09, que es
    quien introduce el .editorconfig contra el que formatear.

.PARAMETER Full
    Añade los UI tests de FlaUI. Necesitan una sesión de escritorio interactiva y desatendida:
    no valen una sesión bloqueada ni una consola sin escritorio. NO necesitan elevación.

.PARAMETER SkipTests
    Solo compilación y chequeo de dependencias. Pensado para release.ps1 -SkipTests.

.PARAMETER SkipOutdated
    Omite el listado informativo de paquetes desactualizados (es el paso más lento: consulta NuGet).

.EXAMPLE
    .\verify.ps1
    .\verify.ps1 -Full
#>
[CmdletBinding()]
param(
    [switch]$Full,
    [switch]$SkipTests,
    [switch]$SkipOutdated
)

$ErrorActionPreference = "Stop"

function Info($m) { Write-Host "==> $m" -ForegroundColor Cyan }
function Ok($m)   { Write-Host "[OK] $m" -ForegroundColor Green }
function Warn($m) { Write-Host "[!] $m" -ForegroundColor Yellow }
function Fail($m) { Write-Host "[X] $m" -ForegroundColor Red; exit 1 }

# Ver la nota extensa en release.ps1: en PowerShell 5.1, con la salida del script canalizada o
# redirigida, un exe nativo que escriba en stderr revienta con NativeCommandError si
# $ErrorActionPreference vale Stop, aunque termine con codigo 0. Aqui se comprueba $LASTEXITCODE
# despues de cada llamada, asi que el modo Stop solo estorba en las nativas.
function Invoke-Native {
    param([Parameter(Mandatory)][scriptblock]$Command)

    $previous = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try { & $Command } finally { $ErrorActionPreference = $previous }
}

$root          = $PSScriptRoot
$solution      = Join-Path $root "WingetUSoft.slnx"
$testProject   = Join-Path $root "tests\WingetUSoft.Tests\WingetUSoft.Tests.csproj"
$uiTestProject = Join-Path $root "tests\WingetUSoft.UiTests\WingetUSoft.UiTests.csproj"

if (-not (Test-Path $solution)) { Fail "No se encontró la solución: $solution" }

# ── 1. Compilación ─────────────────────────────────────────────────────────
# -warnaserror: una advertencia nueva es una regresión. El repositorio compila hoy con cero.
Info "Compilando la solución (advertencias = error)..."
Invoke-Native { & dotnet build $solution --nologo -warnaserror }
if ($LASTEXITCODE -ne 0) { Fail "La compilación falló (o emitió advertencias)." }
Ok "Compilación limpia."

# ── 2. Pruebas unitarias ───────────────────────────────────────────────────
if ($SkipTests) {
    Warn "Pruebas omitidas (-SkipTests)."
} else {
    Info "Ejecutando pruebas unitarias..."
    Invoke-Native { & dotnet test $testProject --nologo --no-build }
    if ($LASTEXITCODE -ne 0) { Fail "Las pruebas unitarias fallaron." }
    Ok "Pruebas unitarias correctas."
}

# ── 3. Dependencias ────────────────────────────────────────────────────────
# 'dotnet list package --vulnerable' devuelve 0 aunque encuentre algo: hay que mirar la salida.
# Las líneas de paquete afectado empiezan por '>' tras la sangría.
Info "Buscando dependencias vulnerables (incluidas las transitivas)..."
$vulnerable = Invoke-Native { & dotnet list $solution package --vulnerable --include-transitive 2>&1 }
if ($LASTEXITCODE -ne 0) {
    $vulnerable | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
    Fail "No se pudo consultar las dependencias vulnerables."
}
$hits = $vulnerable | Where-Object { $_ -match '^\s*>\s' }
if ($hits) {
    $vulnerable | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
    Fail "Hay dependencias con vulnerabilidades conocidas ($($hits.Count)). Actualízalas antes de seguir."
}
Ok "Sin dependencias vulnerables conocidas."

if ($SkipOutdated) {
    Warn "Listado de paquetes desactualizados omitido (-SkipOutdated)."
} else {
    # Informativo: estar desactualizado no rompe nada, pero es la señal temprana de la próxima
    # vulnerabilidad. NO aborta.
    Info "Comprobando paquetes desactualizados (informativo)..."
    $outdated = Invoke-Native { & dotnet list $solution package --outdated 2>&1 }
    $stale = $outdated | Where-Object { $_ -match '^\s*>\s' }
    if ($stale) {
        Warn "Hay $($stale.Count) referencia(s) de paquete con versión más nueva disponible:"
        $stale | ForEach-Object { Write-Host "    $($_.Trim())" -ForegroundColor DarkGray }
    } else {
        Ok "Todos los paquetes están en su última versión."
    }
}

# ── 4. UI tests ────────────────────────────────────────────────────────────
if ($Full) {
    if (-not (Test-Path $uiTestProject)) { Fail "No se encontró el proyecto de UI tests: $uiTestProject" }
    Info "Ejecutando UI tests (abren la app real: no toques el ratón ni el teclado)..."
    Invoke-Native { & dotnet test $uiTestProject --nologo --no-build }
    if ($LASTEXITCODE -ne 0) { Fail "Los UI tests fallaron." }
    Ok "UI tests correctos."
} elseif (-not $SkipTests) {
    Warn "UI tests no ejecutados (usa -Full). No se ha verificado nada contra la app real."
}

Write-Host ""
Ok "Verificación completada."
