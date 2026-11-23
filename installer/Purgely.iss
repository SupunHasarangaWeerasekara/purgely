; Inno Setup script for Purgely
; Build: "ISCC.exe installer\Purgely.iss"  (after publishing the app to publish\)
; Publish command (once the app project exists):
;   dotnet publish src\Purgely.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish

#define AppName "Purgely"
#define AppVersion "0.1.0"
#define AppPublisher "Purgely"
#define AppExeName "Purgely.exe"

[Setup]
; Keep this GUID fixed forever - Windows uses it to identify Purgely for upgrades/uninstall.
AppId={{6B1E5C3A-9F2D-4E7B-8A41-3C5D7E9F1A2B}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
; Show the folder page on first install only; upgrades reuse the existing folder.
DisableDirPage=auto
DisableProgramGroupPage=yes
LicenseFile=EULA.txt
SetupIconFile=..\assets\purgely.ico
UninstallDisplayIcon={app}\{#AppExeName}
OutputDir=..\dist
OutputBaseFilename=Purgely-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "EULA.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Purgely cleans up after itself too
Type: filesandordirs; Name: "{localappdata}\Purgely"
