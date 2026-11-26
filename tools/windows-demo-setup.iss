#ifndef Payload
  #error "Supply an absolute portable payload directory"
#endif
#ifndef Output
  #error "Supply an absolute installer output directory"
#endif
#ifndef Revision
  #error "Supply the source revision"
#endif
#define ProductName "FleetManagement Demo"
#define Release "6.0.0-rc2"
[Setup]
AppId=FleetManagement.Demo.{#Revision}
AppName={#ProductName}
AppVersion={#Release}
AppVerName={#ProductName} {#Release} ({#Revision})
AppPublisher=FleetManagement project
DefaultDirName={localappdata}\FleetManagement\Demo\{#Revision}
DefaultGroupName=FleetManagement Demo {#Revision}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#Output}
OutputBaseFilename=FleetManagement-Setup-{#Revision}-unsigned
Compression=lzma2/fast
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\FleetManagement.Demo.exe
UninstallDisplayName={#ProductName} {#Release} ({#Revision})
VersionInfoVersion=6.0.0.2
VersionInfoProductName={#ProductName}
SetupLogging=yes
CloseApplications=yes
RestartApplications=no
[Files]
Source: "{#Payload}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\FleetManagement Demo"; Filename: "{app}\FleetManagement.Demo.exe"; WorkingDir: "{app}"
Name: "{group}\Uninstall this revision"; Filename: "{uninstallexe}"
[Run]
Filename: "{app}\FleetManagement.Demo.exe"; Description: "Open FleetManagement Demo"; Flags: nowait postinstall skipifsilent
