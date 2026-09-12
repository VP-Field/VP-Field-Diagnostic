#define MyAppName "VP Field Diagnostic"
#define MyAppVersion "0.1.0-alpha"
#define MyAppPublisher "VP Field"
#define MyAppExeName "VP.Field.Diagnostic.exe"

[Setup]
AppId={{C2C0DC7C-91D0-4D7A-910A-1EE0170FD601}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\VP Field\VP Field Diagnostic
DefaultGroupName=VP Field
OutputDir=output
OutputBaseFilename=VP-Field-Diagnostic-Alpha-0.1-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
LicenseFile=..\EULA.txt
UninstallDisplayName={#MyAppName}
ArchitecturesAllowed=x86compatible

[Files]
Source: "..\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\EULA.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\DISCLAIMER.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\TRADEMARKS.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
