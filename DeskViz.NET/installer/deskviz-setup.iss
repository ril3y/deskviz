#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif

#ifndef PublishDir
  #define PublishDir "..\..\publish-output"
#endif

#ifndef WidgetOutputDir
  #define WidgetOutputDir "..\WidgetOutput"
#endif

#ifndef RepoRoot
  #define RepoRoot ".."
#endif

#ifndef OutputDir
  #define OutputDir "..\..\installer-output"
#endif

#define MyAppName "DeskViz"
#define MyAppPublisher "ril3y"
#define MyAppExeName "DeskViz.App.exe"
#define MyAppURL "https://github.com/ril3y/DeskViz"

[Setup]
AppId={{E8F1A3B7-5C2D-4F6E-9A1B-3D7C8E2F4A5B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir={#OutputDir}
OutputBaseFilename=DeskViz-Setup-{#MyAppVersion}
SetupIconFile={#RepoRoot}\deskviz.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
PrivilegesRequired=admin
WizardStyle=modern
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\DeskViz.App.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#WidgetOutputDir}\*"; DestDir: "{app}\WidgetOutput"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
