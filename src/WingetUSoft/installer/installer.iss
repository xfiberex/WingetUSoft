; Parámetros opcionales (vía /D al invocar ISCC — ver installer/build-installer.ps1):
;   /DMyAppVersion=X.Y.Z   versión a estampar (por defecto: ver #define abajo)
;   /DSourceDir=<ruta>     carpeta de publicación de .NET (por defecto: ..\publish)
#define MyAppName "WingetUSoft"
#define MyAppPublisher "Ricky Angel Jiménez Bueno"
#define MyAppExeName "WingetUSoft.exe"
#define MyAppURL "https://github.com/xfiberex/WingetUSoft"
#define MyAppUpdatesURL "https://github.com/xfiberex/WingetUSoft/releases"

#ifndef MyAppVersion
  #define MyAppVersion "1.2.0"
#endif

; SourceDir es relativo al .iss --> ../publish (raiz del proyecto) salvo que se sobrescriba con /D.
#ifndef SourceDir
  #define SourceDir "..\publish"
#endif
; Dependencias reales de la app (ver [Code] abajo):
;  - VC++ Redist: si.
;  - Windows App Runtime: NO. El proyecto compila con WindowsAppSDKSelfContained=true, asi que el
;    runtime viaja dentro de la propia carpeta de la app (Microsoft.WindowsAppRuntime.dll,
;    Microsoft.ui.xaml.dll, ...). No se descarga ni se instala: descargarlo era un ~40 MB inutil en
;    cada actualizacion.
;  - .NET: si. La app es framework-dependent (no hay hostfxr.dll junto al .exe).
#define VCRedistUrl "https://aka.ms/vs/17/release/vc_redist.x64.exe"
#define DotNet10Url "https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe"

[Setup]
; AppId fijo: NO cambiar entre versiones o se tratara como app distinta
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppUpdatesURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=Output
OutputBaseFilename=WingetUSoft-Setup-{#MyAppVersion}
; Rutas relativas al .iss (en installer/)
SetupIconFile=..\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
WizardStyle=modern
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=admin
MinVersion=10.0.19041
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Permite instalar encima sin desinstalar primero
CloseApplications=yes
CloseApplicationsFilter=*{#MyAppExeName}*
RestartApplications=no
; Reutiliza el directorio y configuracion de la version anterior
UsePreviousAppDir=yes
UsePreviousGroup=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\app.ico"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\app.ico"; Tasks: desktopicon

[Run]
; runasoriginaluser: Setup corre elevado (PrivilegesRequired=admin), asi que sin esto la app se
; relanzaria heredando el token de administrador. WingetUSoft es asInvoker a proposito (eleva bajo
; demanda con un worker por named pipe, ver Services/WingetService.cs): debe volver a arrancar como
; el usuario normal, igual que si la abriera el con su acceso directo.
Filename: "{app}\{#MyAppExeName}"; Flags: nowait shellexec runasoriginaluser; Check: IsAutoUpdate
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent shellexec runasoriginaluser

[Code]
function IsAutoUpdate: Boolean;
begin
  Result := ExpandConstant('{param:autoinstall|0}') = '1';
end;

function VCRedistInstalled(): Boolean;
var
  Installed: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM,
    'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64',
    'Installed', Installed) and (Installed = 1);
end;

// La app es framework-dependent: junto al .exe NO hay hostfxr.dll, asi que necesita el runtime
// compartido. Su WingetUSoft.runtimeconfig.json pide "Microsoft.NETCore.App" 10.0.0 -- NO
// "Microsoft.WindowsDesktop.App": WinUI 3 no es una app WindowsDesktop (eso es WPF/WinForms).
//
// La comprobacion anterior miraba subclaves de
//   HKLM\SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App
// y era doblemente incorrecta: framework equivocado, y esa clave 'sharedfx' no existe (bajo
// InstalledVersions\x64 solo hay 'sharedhost'). Por eso daba SIEMPRE "falta .NET" en cualquier
// maquina, y cada actualizacion se bajaba y reinstalaba el runtime que el usuario ya tenia.
//
// Se comprueba la carpeta del framework compartido, que es lo que hostfxr resuelve de verdad y lo
// mismo que lista 'dotnet --list-runtimes'.
function DotNetRuntimeInstalled(): Boolean;
var
  FindRec: TFindRec;
begin
  Result := False;
  if FindFirst(ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.NETCore.App\10.*'), FindRec) then
  begin
    try
      repeat
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
        begin
          Result := True;
          Exit;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  DownloadPage: TDownloadWizardPage;
  NeedVCRedist, NeedDotNet: Boolean;
  ResultCode: Integer;
begin
  if CurStep <> ssInstall then
    Exit;

  // Se evaluan aqui (y no en InitializeWizard) para que el resultado sea el mismo en instalacion
  // interactiva y en la silenciosa de la auto-actualizacion.
  NeedVCRedist := not VCRedistInstalled();
  NeedDotNet := not DotNetRuntimeInstalled();

  // Caso normal en una actualizacion: no falta nada, no se descarga ni se ejecuta nada.
  if not (NeedVCRedist or NeedDotNet) then
    Exit;

  DownloadPage := CreateDownloadPage(
    'Descargando dependencias',
    'Descargando componentes requeridos...',
    nil);
  DownloadPage.Clear;

  if NeedVCRedist then
    DownloadPage.Add('{#VCRedistUrl}', 'vc_redist.x64.exe', '');
  if NeedDotNet then
    DownloadPage.Add('{#DotNet10Url}', 'windowsdesktop-runtime-win-x64.exe', '');

  // En la auto-actualizacion Setup corre /VERYSILENT: no se muestra pagina de asistente.
  if not WizardSilent then
    DownloadPage.Show;
  try
    DownloadPage.Download;
  finally
    if not WizardSilent then
      DownloadPage.Hide;
  end;

  // SW_HIDE (antes SW_SHOW): estos instaladores corren en modo silencioso, no deben abrir ninguna
  // ventana -- y menos en una auto-actualizacion, donde Setup los espera (ewWaitUntilTerminated).
  if NeedVCRedist then
    Exec(ExpandConstant('{tmp}\vc_redist.x64.exe'),
      '/install /quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  // El Desktop Runtime es un superconjunto del .NET Runtime: trae el Microsoft.NETCore.App que la
  // app pide y ademas cubre a quien luego necesite WindowsDesktop. Por eso se comprueba NETCore.App
  // pero se instala el Desktop Runtime.
  if NeedDotNet then
    Exec(ExpandConstant('{tmp}\windowsdesktop-runtime-win-x64.exe'),
      '/install /quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;
