#define MyAppName "WingetUSoft"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "xfiberex"
#define MyAppExeName "WingetUSoft.exe"
#define MyAppURL "https://github.com/xfiberex/WingetUSoft"
#define MyAppUpdatesURL "https://github.com/xfiberex/WingetUSoft/releases"
; SourceDir es relativo al .iss --> ../publish (raiz del proyecto)
#define SourceDir "..\publish"
#define VCRedistUrl "https://aka.ms/vs/17/release/vc_redist.x64.exe"
#define WinAppRuntimeUrl "https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe"

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
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
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
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  VCRedistNeedsInstall: Boolean;
  WinAppRuntimeNeedsInstall: Boolean;
  ResultCode: Integer;

function VCRedistInstalled(): Boolean;
var
  Installed: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM,
    'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64',
    'Installed', Installed) and (Installed = 1);
end;

function WinAppRuntimeInstalled(): Boolean;
begin
  // Windows App Runtime 1.x registra su presencia aquí
  Result := RegKeyExists(HKLM, 'SOFTWARE\Microsoft\WindowsAppRuntime\1.8') or
            RegKeyExists(HKLM, 'SOFTWARE\Microsoft\WindowsAppSDK\1.8');
end;

procedure InitializeWizard();
begin
  VCRedistNeedsInstall := not VCRedistInstalled();
  WinAppRuntimeNeedsInstall := not WinAppRuntimeInstalled();
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  DownloadPage: TDownloadWizardPage;
begin
  if CurStep = ssInstall then
  begin
    if VCRedistNeedsInstall or WinAppRuntimeNeedsInstall then
    begin
      DownloadPage := CreateDownloadPage(
        'Descargando dependencias',
        'Descargando componentes requeridos...',
        nil);
      DownloadPage.Clear;

      if VCRedistNeedsInstall then
        DownloadPage.Add('{#VCRedistUrl}', 'vc_redist.x64.exe', '');
      if WinAppRuntimeNeedsInstall then
        DownloadPage.Add('{#WinAppRuntimeUrl}', 'windowsappruntimeinstall-x64.exe', '');

      DownloadPage.Show;
      try
        DownloadPage.Download;
      finally
        DownloadPage.Hide;
      end;

      if VCRedistNeedsInstall then
        Exec(ExpandConstant('{tmp}\vc_redist.x64.exe'),
          '/install /quiet /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);

      if WinAppRuntimeNeedsInstall then
        Exec(ExpandConstant('{tmp}\windowsappruntimeinstall-x64.exe'),
          '--quiet', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
    end;
  end;
end;
