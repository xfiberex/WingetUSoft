<#
.SYNOPSIS
    Corta una versión de WingetUSoft de principio a fin.

.DESCRIPTION
    Flujo completo en un paso:
      1. Valida la versión y el árbol de trabajo.
      2. Ejecuta las pruebas: unitarias + UI tests (salvo -SkipTests / -SkipUiTests).
      3. Actualiza <Version> en el .csproj si cambió.
      4. Compila el instalador (publish framework-dependent + Inno Setup).
      5. Commit del bump de versión + tag anotado vX.Y.Z.
      6. Push de la rama y el tag a origin.
      7. Crea el GitHub Release adjuntando el instalador Y su .sha256.

    Para 'gh' reutiliza la credencial de GitHub ya cacheada (la del push) si no
    estuviera autenticado; nunca se imprime el token.

    IMPORTANTE: el asset .sha256 es OBLIGATORIO mientras se publique sin firmar. La app verifica
    el instalador descargado antes de ejecutarlo (Services/GitHubUpdateService.VerifyInstallerAsync):
    firma Authenticode válida si la hay; si no, el hash SHA-256 publicado como asset. Sin ninguna de
    las dos cosas, la app borra el instalador y la auto-actualización falla.

    Firmar (-CertThumbprint/-CertFile) sigue siendo lo deseable: evita el aviso de SmartScreen
    ("editor desconocido") y es una garantía más fuerte que el hash.

.PARAMETER Version
    Versión a publicar (X.Y.Z). Si se omite, usa la del .csproj.

.PARAMETER NotesFile
    Ruta a un archivo Markdown con las notas del release. Si se omite, se genera una plantilla.

.PARAMETER SkipTests
    Omite TODAS las pruebas (unitarias y UI).

.PARAMETER SkipUiTests
    Omite solo los UI tests, y ejecuta las unitarias. Los UI tests conducen la app real por UI
    Automation, así que necesitan una sesión de escritorio interactiva y desatendida: no valen una
    sesión bloqueada, ni una consola sin escritorio (CI, tarea programada, SSH). NO necesitan
    elevación — la app corre asInvoker.

.PARAMETER AllowDirty
    Permite continuar con cambios sin commitear en el árbol de trabajo.

.PARAMETER DryRun
    Valida y muestra el plan, pero no modifica nada (ni build, ni git, ni GitHub).

.EXAMPLE
    .\release.ps1 -Version 1.3.0
    .\release.ps1 -Version 1.3.0 -DryRun
    .\release.ps1 -Version 1.3.0 -NotesFile notas.md
    .\release.ps1 -Version 1.3.0 -SkipUiTests      # sin escritorio interactivo disponible
    .\release.ps1 -Version 1.3.0 -CertThumbprint A1B2C3...
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$NotesFile,
    [switch]$SkipTests,
    [switch]$SkipUiTests,
    [switch]$AllowDirty,
    [switch]$DryRun,
    # Firma de código (recomendada: ver nota IMPORTANTE arriba): se reenvían a build-installer.ps1.
    [string]$CertThumbprint,
    [string]$CertFile,
    # SecureString: la contraseña del .pfx no puede acabar en la línea de comandos de signtool
    # ni en el historial de PowerShell (ver build-installer.ps1, que la consume).
    [SecureString]$CertPassword,
    [string]$TimestampUrl
)

$ErrorActionPreference = "Stop"

# ¿Puso este script el GH_TOKEN? Solo se limpia el que ponga él (ver el finally del final).
$ghTokenSetByThisScript = $false

function Info($m)  { Write-Host "==> $m" -ForegroundColor Cyan }
function Ok($m)    { Write-Host "[OK] $m" -ForegroundColor Green }
function Warn($m)  { Write-Host "[!] $m" -ForegroundColor Yellow }
function Die($m)   { Write-Host "[X] $m" -ForegroundColor Red; exit 1 }

# Ejecuta un comando nativo (git, gh, dotnet) con $ErrorActionPreference bajado a Continue.
#
# POR QUE HACE FALTA. En PowerShell 5.1, cuando la salida del script se canaliza o se redirige
# (`.\release.ps1 ... 2>&1 | ...`, que es como lo invoca cualquier automatizacion), un exe nativo que
# escriba en stderr se convierte en un NativeCommandError *terminante* si $ErrorActionPreference vale
# Stop -- aunque su codigo de salida sea 0. Y git escribe SIEMPRE en stderr: el informe del push, el
# progreso, la salida de los hooks. El release de la v1.8.6 murio exactamente ahi, con la rama ya
# subida y el tag sin subir, y hubo que rematarlo a mano.
#
# Este script comprueba $LASTEXITCODE explicitamente despues de cada llamada nativa, asi que el modo
# Stop no aporta nada en ellas y si rompe. Se mantiene Stop para los cmdlets, que es donde si protege.
function Invoke-Native {
    param([Parameter(Mandatory)][scriptblock]$Command)

    $previous = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try { & $Command } finally { $ErrorActionPreference = $previous }
}

# ── Rutas ──────────────────────────────────────────────────────────────────
$root          = $PSScriptRoot
$csproj        = Join-Path $root "src\WingetUSoft\WingetUSoft.csproj"
$verifyScript  = Join-Path $root "verify.ps1"
$issFile       = Join-Path $root "src\WingetUSoft\installer\installer.iss"
$buildScript   = Join-Path $root "src\WingetUSoft\installer\build-installer.ps1"
$outputDir     = Join-Path $root "src\WingetUSoft\installer\Output"

if (-not (Test-Path $csproj))      { Die "No se encontró el proyecto: $csproj" }
if (-not (Test-Path $buildScript)) { Die "No se encontró el script de instalador: $buildScript" }

# ── Versión ────────────────────────────────────────────────────────────────
# -Encoding UTF8 no es opcional: en PowerShell 5.1, Get-Content asume la página de códigos ANSI
# cuando el archivo no lleva BOM — y el .csproj no lo lleva. Sin esto, sus caracteres acentuados se
# leen como cp1252 y el WriteAllText de más abajo los reescribe doblemente codificados: el bump de
# versión de la v1.8.5 destrozó así los acentos de los comentarios del proyecto.
$csprojRaw = Get-Content $csproj -Raw -Encoding UTF8
$currentVersion = $null
if ($csprojRaw -match '<Version>(.*?)</Version>') { $currentVersion = $Matches[1] }

if (-not $Version) {
    if (-not $currentVersion) { Die "No hay <Version> en el .csproj y no se pasó -Version." }
    $Version = $currentVersion
}
if ($Version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') {
    Die "Versión inválida '$Version'. Usa el formato X.Y.Z (p. ej. 1.3.0)."
}
$tag = "v$Version"
Info "Versión a publicar: $Version  (tag $tag)"
if ($currentVersion -and $currentVersion -ne $Version) {
    Info "Bump de versión: $currentVersion -> $Version"
}
if (-not ($CertThumbprint -or $CertFile)) {
    Warn "Sin firma de código (-CertThumbprint/-CertFile): SmartScreen mostrará 'editor desconocido'. La auto-actualización sí funcionará: se verificará contra el asset .sha256."
}

# ── Validaciones de git ──────────────────────────────────────────────────────
Push-Location $root
try {
    Invoke-Native { & git rev-parse --is-inside-work-tree *> $null }
    if ($LASTEXITCODE -ne 0) { Die "Este directorio no es un repositorio git." }

    $branch = (Invoke-Native { & git rev-parse --abbrev-ref HEAD }).Trim()
    Info "Rama: $branch"

    # ¿Tag ya existe? (local o remoto)
    $localTag  = (Invoke-Native { & git tag --list $tag })
    if ($localTag) { Die "El tag $tag ya existe localmente. Usa otra versión o bórralo antes." }
    $remoteTag = (Invoke-Native { & git ls-remote --tags origin $tag 2>$null })
    if ($remoteTag) { Die "El tag $tag ya existe en origin. Usa otra versión." }

    # ¿Hay archivos sin rastrear? (nuevos, no añadidos con git add)
    # Estos NO se incluirán en el commit del release — el usuario debe añadirlos explícitamente.
    $untracked = (Invoke-Native { & git status --porcelain }) | Where-Object { $_ -match '^\?\?' }
    if ($untracked -and -not $AllowDirty) {
        Warn "Hay archivos nuevos sin rastrear (no se incluirán en el release):"
        $untracked | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
        Die "Añade los archivos que necesites con 'git add <archivo>' y reintenta, o usa -AllowDirty para ignorarlos."
    } elseif ($untracked) {
        Warn "Archivos sin rastrear ignorados (-AllowDirty):"
        $untracked | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
    }

    # ── Verificación ──────────────────────────────────────────────────────────────────────────
    # Este script NO define qué es "verificado": lo hace verify.ps1, que es también lo que corre el
    # hook de pre-push. Aquí solo se decide cuánto de esa verificación se exige para publicar.
    #
    # Los UI tests conducen la app REAL por UI Automation (FlaUI/UIA3): abren ventanas, pulsan botones
    # y leen el árbol de automatización. Necesitan una sesión de escritorio interactiva y desatendida
    # -- no valen una sesión bloqueada ni una consola sin escritorio (CI, tarea programada, SSH); ahí
    # hay que pasar -SkipUiTests. NO necesitan elevación: la app es asInvoker. Cubren cosas que las
    # unitarias no pueden ver: que la ventana quepa en la WorkArea y en las celdas de snap, que el
    # cambio de idioma repinte los controles ya visibles, o que un filtro no quede recortado contra el
    # borde. Justo el tipo de regresión que se cuela en un release.
    if (-not (Test-Path $verifyScript)) { Die "No se encontró el script de verificación: $verifyScript" }

    $verifyArgs = @{}
    if ($SkipTests) {
        Warn "Pruebas omitidas (-SkipTests): solo se verifican compilación y dependencias."
        $verifyArgs.SkipTests = $true
    } elseif ($SkipUiTests) {
        Warn "UI tests omitidos (-SkipUiTests): este release NO se ha verificado contra la app real."
    } else {
        $verifyArgs.Full = $true
        Info "Los UI tests abren la app real: no toques el ratón ni el teclado."
    }

    Info "Verificando el repositorio (verify.ps1)..."
    Invoke-Native { & $verifyScript @verifyArgs }
    if ($LASTEXITCODE -ne 0) { Die "La verificación falló. Release abortado." }
    Ok "Repositorio verificado."

    # ── Notas del release ──────────────────────────────────────────────────────
    $notesPath = $NotesFile
    $tempNotes = $null
    if (-not $notesPath) {
        $tempNotes = Join-Path $env:TEMP "wus_release_$Version.md"
        @(
            "## WingetUSoft v$Version",
            "",
            "Instalador para Windows x64 (descarga .NET 10 o el VC++ Redist solo si faltan en el equipo).",
            "",
            "Descarga ``WingetUSoft-Setup-$Version.exe`` y ejecútalo.",
            "",
            "La app comprueba actualizaciones automáticamente desde el menú *Ayuda → Buscar actualización de WingetUSoft…*.",
            "",
            "El asset ``WingetUSoft-Setup-$Version.exe.sha256`` es el hash con el que la app verifica la descarga antes de ejecutarla."
        ) | Out-File -FilePath $tempNotes -Encoding utf8
        $notesPath = $tempNotes
    }
    if (-not (Test-Path $notesPath)) { Die "No se encontró el archivo de notas: $notesPath" }

    # Los flags que rebajan la verificación avisan por consola, y ese aviso se lo lleva el viento.
    # Meses después, mirando un release, no había forma de saber si salió verificado. Se anota en las
    # notas publicadas, que es lo único que sobrevive.
    $omissions = @()
    if ($SkipTests)   { $omissions += "**Pruebas omitidas** (``-SkipTests``): esta versión se publicó sin ejecutar las pruebas unitarias ni los UI tests." }
    if ($SkipUiTests -and -not $SkipTests) { $omissions += "**UI tests omitidos** (``-SkipUiTests``): esta versión no se verificó contra la app real." }
    if ($AllowDirty)  { $omissions += "**Árbol de trabajo sucio** (``-AllowDirty``): había archivos sin rastrear que no entraron en el commit del release." }

    if ($omissions) {
        # Nunca se modifica el archivo que pasó el usuario (-NotesFile): se compone una copia temporal.
        $notesWithWarning = Join-Path $env:TEMP "wus_release_notes_$Version.md"
        $body = @(Get-Content $notesPath -Encoding UTF8)
        $body += @("", "---", "", "> ⚠️ **Verificación incompleta en este release.**")
        foreach ($o in $omissions) { $body += "> - $o" }
        $body | Out-File -FilePath $notesWithWarning -Encoding utf8
        if ($tempNotes) { Remove-Item $tempNotes -Force -ErrorAction SilentlyContinue }
        $tempNotes = $notesWithWarning
        $notesPath = $notesWithWarning
        Warn "Las notas del release incluirán la advertencia de verificación incompleta ($($omissions.Count) punto/s)."
    }

    # ── DRY RUN: mostrar plan y salir ────────────────────────────────────────
    if ($DryRun) {
        Write-Host ""
        Warn "DRY RUN — no se modificará nada. Plan:"
        $signNote = if ($CertThumbprint -or $CertFile) { " (firmando con Authenticode)" } else { " (SIN firmar — la app verificará por el .sha256)" }
        Write-Host "    1. Actualizar <Version> a $Version en el .csproj" -ForegroundColor DarkGray
        Write-Host "    2. build-installer.ps1 -Version $Version$signNote" -ForegroundColor DarkGray
        Write-Host "    3. git add -u  (todos los archivos rastreados modificados)" -ForegroundColor DarkGray
        Write-Host "       git commit -m 'release: v$Version'" -ForegroundColor DarkGray
        Write-Host "       git tag -a $tag" -ForegroundColor DarkGray
        Write-Host "    4. git push origin $branch" -ForegroundColor DarkGray
        Write-Host "       git push origin $tag" -ForegroundColor DarkGray
        Write-Host "    5. gh release create $tag (assets: WingetUSoft-Setup-$Version.exe + .sha256)" -ForegroundColor DarkGray
        Write-Host ""
        Write-Host "    Notas que se publicarían:" -ForegroundColor DarkGray
        Get-Content $notesPath -Encoding UTF8 | ForEach-Object { Write-Host "      $_" -ForegroundColor DarkGray }
        if ($tempNotes) { Remove-Item $tempNotes -Force -ErrorAction SilentlyContinue }
        Ok "Dry run completado."
        return
    }

    # ── 1. Bump de versión ───────────────────────────────────────────────────
    if ($currentVersion -ne $Version) {
        Info "Actualizando <Version>/<AssemblyVersion>/<FileVersion> en el .csproj..."
        # AssemblyVersion/FileVersion son de 4 partes: reutiliza el 4º segmento si ya lo tenía, si no ".0".
        $asmVersion = if ($Version -match '^\d+\.\d+\.\d+$') { "$Version.0" } else { $Version }
        $newRaw = $csprojRaw -replace '<Version>.*?</Version>', "<Version>$Version</Version>"
        $newRaw = $newRaw -replace '<AssemblyVersion>.*?</AssemblyVersion>', "<AssemblyVersion>$asmVersion</AssemblyVersion>"
        $newRaw = $newRaw -replace '<FileVersion>.*?</FileVersion>', "<FileVersion>$asmVersion</FileVersion>"
        [System.IO.File]::WriteAllText($csproj, $newRaw, (New-Object System.Text.UTF8Encoding($false)))

        # La reserva del installer.iss también, o se queda vieja en cuanto sale una versión. Solo la
        # usa un ISCC invocado a mano —build-installer.ps1 siempre pasa /DMyAppVersion— pero una
        # reserva que solo puede decaer es justo lo que T3-03 vino a arreglar.
        #
        # Mismo cuidado con la codificación que con el .csproj: el .iss es UTF-8 SIN BOM y lleva
        # acentos (incluido MyAppPublisher, que acaba en el CompanyName del ejecutable).
        if (Test-Path $issFile) {
            $issRaw = Get-Content $issFile -Raw -Encoding UTF8
            $issNew = $issRaw -replace '(#define\s+MyAppVersion\s+")[^"]*(")', "`${1}$Version`${2}"
            if ($issNew -ne $issRaw) {
                Info "Actualizando la reserva de versión de installer.iss..."
                [System.IO.File]::WriteAllText($issFile, $issNew, (New-Object System.Text.UTF8Encoding($false)))
            }
        }
    }

    # ── 2. Compilar instalador ─────────────────────────────────────────────────
    Info "Compilando el instalador..."
    $buildArgs = @{ Version = $Version }
    if ($CertThumbprint) { $buildArgs.CertThumbprint = $CertThumbprint }
    if ($CertFile)       { $buildArgs.CertFile       = $CertFile }
    if ($CertPassword)   { $buildArgs.CertPassword   = $CertPassword }
    if ($TimestampUrl)   { $buildArgs.TimestampUrl   = $TimestampUrl }
    Invoke-Native { & $buildScript @buildArgs }
    if ($LASTEXITCODE -ne 0) { Die "La compilación del instalador falló." }
    $setup = Join-Path $outputDir "WingetUSoft-Setup-$Version.exe"
    if (-not (Test-Path $setup)) { Die "No se encontró el instalador esperado: $setup" }
    $sizeMB = [math]::Round((Get-Item $setup).Length / 1MB, 1)
    Ok "Instalador: $setup ($sizeMB MB)"

    # Lo genera build-installer.ps1. Es lo que la app usa para verificar la descarga mientras los
    # instaladores se publiquen sin firmar (GitHubUpdateService.VerifyInstallerAsync): si no se sube
    # como asset, la auto-actualización no puede verificar nada y se niega a instalar.
    $setupHash = "$setup.sha256"
    if (-not (Test-Path $setupHash)) { Die "No se encontró el checksum esperado: $setupHash" }
    Ok "Checksum: $setupHash"

    # ── 3. Commit + tag ──────────────────────────────────────────────────────
    # Añade todos los archivos rastreados modificados/eliminados (tracked changes).
    # Los archivos nuevos sin rastrear requieren 'git add' manual previo.
    Info "Preparando commit de release..."
    Invoke-Native { & git add -u }
    if ($LASTEXITCODE -ne 0) { Die "git add -u falló." }
    $staged = (Invoke-Native { & git diff --cached --name-only })
    if ($staged) {
        Info "Archivos incluidos en el commit:"
        $staged | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
        Invoke-Native { & git commit -m "release: v$Version" }
        if ($LASTEXITCODE -ne 0) { Die "git commit falló." }
        Ok "Commit de release creado."
    } else {
        Info "Sin cambios que commitear; se etiqueta el HEAD actual."
    }
    Info "Creando tag $tag..."
    Invoke-Native { & git tag -a $tag -m "WingetUSoft $tag" }
    if ($LASTEXITCODE -ne 0) { Die "git tag falló." }

    # ── 4. Push ──────────────────────────────────────────────────────────────
    Info "Push de la rama y el tag a origin..."
    Invoke-Native { & git push origin $branch }
    if ($LASTEXITCODE -ne 0) { Die "git push de la rama falló." }
    Invoke-Native { & git push origin $tag }
    if ($LASTEXITCODE -ne 0) { Die "git push del tag falló." }
    Ok "Rama y tag publicados."

    # ── 5. GitHub Release ────────────────────────────────────────────────────
    $gh = @(
        "C:\Program Files\GitHub CLI\gh.exe",
        "C:\Program Files (x86)\GitHub CLI\gh.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $gh) {
        $cmd = Get-Command gh -ErrorAction SilentlyContinue
        if ($cmd) { $gh = $cmd.Source }
    }
    if (-not $gh) { Die "gh (GitHub CLI) no está instalado. Instálalo: winget install GitHub.cli  — el tag YA está publicado; crea el release manualmente o reintenta." }

    # Asegurar autenticación: si gh no está logueado, reutilizar la credencial cacheada de git.
    Invoke-Native { & $gh auth status 2>$null }
    $authOk = $LASTEXITCODE -eq 0

    if (-not $authOk) {
        Warn "gh no autenticado; reutilizando la credencial de git cacheada (local, no se muestra)."
        $cred = Invoke-Native { "protocol=https`nhost=github.com`n`n" | & git credential fill 2>$null }
        $pwdLine = $cred | Where-Object { $_ -like 'password=*' } | Select-Object -First 1
        if ($pwdLine) {
            # A partir de aquí el PAT vive en el entorno del proceso y lo hereda TODO hijo posterior,
            # durante el resto de la sesión de PowerShell. El finally de abajo lo deshace.
            $ghTokenSetByThisScript = $true
            $env:GH_TOKEN = $pwdLine.Substring(9)
        }
        if (-not $env:GH_TOKEN) { Die "No se pudo obtener credencial para gh. Ejecuta 'gh auth login' y reintenta (el tag ya está publicado)." }
    }

    Info "Creando el GitHub Release..."
    Invoke-Native { & $gh release create $tag --title "WingetUSoft $tag" --notes-file $notesPath $setup $setupHash }
    if ($LASTEXITCODE -ne 0) { Die "gh release create falló (el tag ya está publicado; puedes reintentar el release)." }

    if ($tempNotes) { Remove-Item $tempNotes -Force -ErrorAction SilentlyContinue }
    Write-Host ""
    Ok "Release $tag publicado: https://github.com/xfiberex/WingetUSoft/releases/tag/$tag"
}
finally {
    # El PAT sale de la credencial cacheada de git y se pone en $env:GH_TOKEN para que 'gh' lo use.
    # Sin esto seguía ahí el resto de la sesión de PowerShell, heredándolo cualquier proceso que se
    # lanzara después desde la misma consola.
    #
    # Solo se limpia si lo puso este script: un GH_TOKEN que ya estuviera en el entorno es del usuario,
    # se puso a propósito y no es de este script borrarlo (ver ROADMAP.md, T2-18).
    if ($ghTokenSetByThisScript) {
        Remove-Item Env:GH_TOKEN -ErrorAction SilentlyContinue
        Write-Host "[OK] GH_TOKEN limpiado del entorno." -ForegroundColor Green
    }
    Pop-Location
}
