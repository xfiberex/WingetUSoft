<#
.SYNOPSIS
    Crea un certificado de firma de codigo AUTOFIRMADO (self-signed) y devuelve su huella.

.DESCRIPTION
    Pensado para validar el pipeline de firma (build-installer.ps1 -CertThumbprint ...) y para
    despliegues en entornos controlados. Es especialmente util aqui porque la auto-actualizacion
    de WingetUSoft EXIGE que el instalador descargado tenga una firma Authenticode valida
    (Core/GitHubUpdateService.VerifyAuthenticodeSignature) o lo borra; sin firmar, la
    auto-actualizacion queda rota para quien ya tenga una version anterior instalada.

    IMPORTANTE: un certificado autofirmado NO elimina los avisos de SmartScreen ni de
    "editor desconocido" en las maquinas de otros usuarios, porque su cadena no es de confianza.
    La firma solo se valida en equipos donde se haya importado el certificado a:
      - "Entidades de certificacion raiz de confianza" (Root), y
      - "Editores de confianza" (TrustedPublisher).
    Para distribucion publica real se necesita un certificado OV/EV de una CA reconocida.

.PARAMETER Subject
    CN del certificado (por defecto "WingetUSoft (Dev)").

.PARAMETER ExportPfx
    Ruta opcional para exportar el certificado (con clave privada) a un .pfx. Requiere -Password.

.PARAMETER Password
    Contrasena del .pfx exportado.

.PARAMETER Years
    Validez en anos (por defecto 3).

.PARAMETER Trust
    Importa el certificado a los almacenes de confianza del equipo ACTUAL (Root + TrustedPublisher)
    para que la firma valide aqui. Requiere ejecutar PowerShell como administrador.

.EXAMPLE
    .\new-selfsigned-cert.ps1
    .\new-selfsigned-cert.ps1 -ExportPfx wus-dev.pfx -Password "secreta"
    .\new-selfsigned-cert.ps1 -Trust    # (como admin) firma validable en este equipo
#>
[CmdletBinding()]
param(
    [string]$Subject = "WingetUSoft (Dev)",
    [string]$ExportPfx,
    [string]$Password,
    [int]$Years = 3,
    [switch]$Trust
)

$ErrorActionPreference = "Stop"

# --- Crear el certificado de firma de codigo en el almacen del usuario ------
$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject "CN=$Subject" `
    -KeyAlgorithm RSA -KeyLength 3072 `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears($Years) `
    -FriendlyName $Subject

Write-Host "[OK] Certificado autofirmado creado." -ForegroundColor Green
Write-Host ("  Subject:    {0}" -f $cert.Subject)
Write-Host ("  Thumbprint: {0}" -f $cert.Thumbprint) -ForegroundColor Cyan
Write-Host ("  Almacen:    Cert:\CurrentUser\My")
Write-Host ("  Valido hasta: {0:yyyy-MM-dd}" -f $cert.NotAfter)

# --- Exportar a .pfx (opcional) --------------------------------------------
if ($ExportPfx) {
    if (-not $Password) { throw "Para exportar a .pfx indica -Password." }
    $sec = ConvertTo-SecureString $Password -AsPlainText -Force
    Export-PfxCertificate -Cert $cert -FilePath $ExportPfx -Password $sec | Out-Null
    Write-Host ("[OK] Exportado a: {0}  (NO lo subas al repo: esta en .gitignore)" -f $ExportPfx) -ForegroundColor Green
}

# --- Confiar en el certificado en este equipo (opcional) -------------------
if ($Trust) {
    try {
        $root = Get-Item "Cert:\LocalMachine\Root" ; $root.Open("ReadWrite") ; $root.Add($cert) ; $root.Close()
        $pub  = Get-Item "Cert:\LocalMachine\TrustedPublisher" ; $pub.Open("ReadWrite") ; $pub.Add($cert) ; $pub.Close()
        Write-Host "[OK] Certificado importado a Root y TrustedPublisher (LocalMachine)." -ForegroundColor Green
    } catch {
        Write-Warning "No se pudo importar a los almacenes del equipo (ejecuta PowerShell como administrador): $($_.Exception.Message)"
    }
}

Write-Host "`n--- Como firmar con este certificado ---" -ForegroundColor Yellow
Write-Host ("  .\installer\build-installer.ps1 -Version X.Y.Z -CertThumbprint {0}" -f $cert.Thumbprint)
Write-Host ("  .\release.ps1 -Version X.Y.Z -CertThumbprint {0}" -f $cert.Thumbprint)
Write-Host "`nRecuerda: autofirmado NO quita SmartScreen para usuarios finales (cadena no confiable)." -ForegroundColor DarkYellow
