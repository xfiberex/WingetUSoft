<#
.SYNOPSIS
    Publica WingetUSoft (win-x64) y compila el instalador con Inno Setup.

.DESCRIPTION
    1. Lee la versión del .csproj (o usa -Version).
    2. dotnet publish -c Release -r win-x64  (framework-dependent en .NET: el instalador
       descarga el .NET Desktop Runtime o el VC++ Redist SOLO si faltan de verdad, ver
       installer.iss → [Code]). El Windows App Runtime NO se descarga: viaja dentro de la app
       (WindowsAppSDKSelfContained=true en el .csproj).
    3. Compila installer.iss con ISCC; el .exe queda en installer/Output.

.PARAMETER Version
    Versión a estampar (por defecto: la del .csproj).

.PARAMETER CertThumbprint
    Huella (SHA-1) de un certificado de firma instalado en el almacén de Windows. Si se indica
    (o -CertFile), se firma el ejecutable publicado y el instalador con Authenticode.

    IMPORTANTE: WingetUSoft exige que el instalador descargado esté firmado con Authenticode
    (Services/GitHubUpdateService.VerifyAuthenticodeSignature) — sin firma, la auto-actualización
    de la app rechaza el instalador y lo borra. Publicar sin firmar rompe la actualización
    automática para quien ya tenga una versión anterior instalada (la descarga manual desde
    Releases sigue funcionando).

.PARAMETER CertFile
    Ruta a un archivo .pfx para firmar (alternativa a -CertThumbprint).

.PARAMETER CertPassword
    Contraseña del .pfx (si la tiene).

.PARAMETER TimestampUrl
    Servidor de sellado de tiempo RFC3161 (por defecto el de DigiCert).

.EXAMPLE
    .\build-installer.ps1
    .\build-installer.ps1 -Version 1.3.0
    .\build-installer.ps1 -Version 1.3.0 -CertThumbprint A1B2C3...
    .\build-installer.ps1 -Version 1.3.0 -CertFile cert.pfx -CertPassword ****
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$Configuration = "Release",
    [string]$Runtime       = "win-x64",
    [string]$CertThumbprint,
    [string]$CertFile,
    [string]$CertPassword,
    [string]$TimestampUrl  = "http://timestamp.digicert.com"
)

$ErrorActionPreference = "Stop"

# --- Firma de código (opcional pero recomendada, ver .PARAMETER CertThumbprint) -----
$signEnabled = [bool]($CertThumbprint -or $CertFile)
$signtool = $null

function Find-SignTool {
    # 1. PATH
    $cmd = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    # 2. Rutas fijas conocidas (App Certification Kit del Windows SDK, ClickOnce SDK)
    $fixed = @(
        "${env:ProgramFiles(x86)}\Windows Kits\10\App Certification Kit\signtool.exe",
        "$env:ProgramFiles\Windows Kits\10\App Certification Kit\signtool.exe",
        "${env:ProgramFiles(x86)}\Microsoft SDKs\ClickOnce\SignTool\signtool.exe"
    )
    foreach ($f in $fixed) { if (Test-Path $f) { return $f } }

    # 3. Windows Kits\bin\<version>\<arch>\signtool.exe (version mas alta, arquitectura del host)
    $arch  = if ([Environment]::Is64BitOperatingSystem) { "x64" } else { "x86" }
    $bases = @(
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin", "$env:ProgramFiles\Windows Kits\10\bin",
        "${env:ProgramFiles(x86)}\Windows Kits\8.1\bin", "$env:ProgramFiles\Windows Kits\8.1\bin"
    )
    foreach ($b in $bases) {
        if (-not (Test-Path $b)) { continue }
        $found = Get-ChildItem $b -Directory -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName "$arch\signtool.exe" } |
            Where-Object { Test-Path $_ } | Select-Object -First 1
        if ($found) { return $found }
        $direct = Join-Path $b "$arch\signtool.exe"   # layout antiguo: bin\x64\signtool.exe
        if (Test-Path $direct) { return $direct }
    }
    return $null
}

function Invoke-Sign([string[]]$files) {
    if (-not $signEnabled) { return }
    $base = @("sign", "/fd", "SHA256", "/tr", $TimestampUrl, "/td", "SHA256")
    if ($CertThumbprint)  { $base += @("/sha1", $CertThumbprint) }
    elseif ($CertFile)    { $base += @("/f", $CertFile); if ($CertPassword) { $base += @("/p", $CertPassword) } }
    foreach ($f in $files) {
        if (-not (Test-Path $f)) { continue }
        Write-Host "==> Firmando: $f" -ForegroundColor Cyan
        & $signtool @base $f
        if ($LASTEXITCODE -ne 0) { throw "signtool falló al firmar $f (código $LASTEXITCODE)" }
    }
}

if ($signEnabled) {
    $signtool = Find-SignTool
    if (-not $signtool) { throw "Se pidió firmar pero no se encontró signtool.exe. Instala el Windows SDK o añádelo al PATH." }
    Write-Host "==> Firma de código habilitada (signtool: $signtool)" -ForegroundColor Cyan
} else {
    Write-Warning "Firma de código DESHABILITADA (sin -CertThumbprint/-CertFile). El instalador NO estará firmado, así que SmartScreen mostrará 'editor desconocido'. La auto-actualización SÍ funciona igualmente: al no haber firma, la app verifica la descarga contra el .sha256 que se genera aquí y que release.ps1 sube como asset del release (GitHubUpdateService.VerifyInstallerAsync). Firmar sigue siendo lo deseable: es una garantía más fuerte que el hash."
}

$installerDir = $PSScriptRoot
$projectDir   = Split-Path $installerDir -Parent          # carpeta del proyecto (WingetUSoft.csproj está aquí)
$csproj       = Join-Path $projectDir "WingetUSoft.csproj"

if (-not (Test-Path $csproj)) { throw "No se encontró el proyecto: $csproj" }

# --- Versión y TFM (leídos del .csproj) ------------------------------------
$csprojXml = [xml](Get-Content $csproj)
$tfm = ($csprojXml.Project.PropertyGroup.TargetFramework | Where-Object { $_ }) | Select-Object -First 1
if (-not $tfm) { $tfm = "net10.0-windows10.0.22621.0" }

if (-not $Version) {
    $Version = ($csprojXml.Project.PropertyGroup.Version | Where-Object { $_ }) | Select-Object -First 1
    if (-not $Version) { $Version = "1.0.0" }
}

# Coincide con el SourceDir por defecto de installer.iss (..\publish) para que
# `iscc installer.iss` a mano, sin /D, siga funcionando igual que antes de este script.
$publishDir = Join-Path $projectDir "publish"
Write-Host "==> Versión: $Version  (TFM: $tfm)" -ForegroundColor Cyan

# --- Localizar ISCC --------------------------------------------------------
$iscc = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    $cmd = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($cmd) { $iscc = $cmd.Source }
}
if (-not $iscc) { throw "No se encontró ISCC.exe. Instala Inno Setup 6: winget install JRSoftware.InnoSetup" }

# --- Publicar (framework-dependent: el instalador resuelve el runtime que falte) ----
Write-Host "==> Publicando ($Configuration / $Runtime)..." -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

& dotnet publish $csproj `
    -c $Configuration `
    -r $Runtime `
    --self-contained false `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish falló (código $LASTEXITCODE)" }

# --- Firmar el ejecutable publicado (antes de empaquetar) ------------------
if ($signEnabled) {
    Invoke-Sign @(
        (Join-Path $publishDir "WingetUSoft.exe"),
        (Join-Path $publishDir "WingetUSoft.dll")
    )
}

# --- Compilar instalador ---------------------------------------------------
$iss = Join-Path $installerDir "installer.iss"
Write-Host "==> Compilando instalador con Inno Setup..." -ForegroundColor Cyan
& $iscc "/DMyAppVersion=$Version" "/DSourceDir=$publishDir" $iss
if ($LASTEXITCODE -ne 0) { throw "ISCC falló (código $LASTEXITCODE)" }

$setup = Join-Path $installerDir "Output\WingetUSoft-Setup-$Version.exe"
if (Test-Path $setup) {
    # Firmar el instalador (lo que comprueba SmartScreen y VerifyAuthenticodeSignature al descargarlo).
    if ($signEnabled) { Invoke-Sign @($setup) }

    # SHA-256 del instalador YA FIRMADO (firmar cambia el binario, así que el hash va después).
    # La auto-actualización lo usa para verificar la descarga cuando el instalador no está firmado
    # (GitHubUpdateService.VerifyInstallerAsync). release.ps1 lo sube como asset del release: sin
    # este archivo, la app no puede verificar un instalador sin firma y aborta la actualización.
    $hash = (Get-FileHash $setup -Algorithm SHA256).Hash
    $sha256File = "$setup.sha256"
    "$hash *$(Split-Path $setup -Leaf)" | Out-File -FilePath $sha256File -Encoding ascii -NoNewline
    Write-Host "==> SHA-256: $hash" -ForegroundColor Cyan

    $sizeMB = [math]::Round((Get-Item $setup).Length / 1MB, 1)
    Write-Host "`n[OK] Instalador generado: $setup ($sizeMB MB)" -ForegroundColor Green
    Write-Host "[OK] Checksum generado:   $sha256File" -ForegroundColor Green
} else {
    Write-Warning "ISCC terminó pero no se encontró el instalador esperado en $setup"
}
