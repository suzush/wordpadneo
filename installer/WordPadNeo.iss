; WordPadNeo installer - Inno Setup 6.3+
; Values are supplied by Build-Installers.ps1 using /D switches.
#ifndef AppVersion
  #error AppVersion is required.
#endif
#ifndef SourceDirectory
  #error SourceDirectory is required.
#endif
#ifndef TargetArchitecture
  #error TargetArchitecture is required.
#endif
#ifndef InstallerOutputDirectory
  #error InstallerOutputDirectory is required.
#endif
#ifndef AppIconPath
  #error AppIconPath is required.
#endif

#if TargetArchitecture == "x64"
  #define AllowedArchitectures "x64compatible"
  #define Install64Bit "x64compatible"
#elif TargetArchitecture == "ARM64"
  #define AllowedArchitectures "arm64"
  #define Install64Bit "arm64"
#elif TargetArchitecture == "x86"
  #define AllowedArchitectures "x86compatible"
  #define Install64Bit ""
#else
  #error Unsupported target architecture.
#endif

[Setup]
; Keep this identifier stable across releases.
AppId={{8DC9E2CF-948F-49E8-92B4-BEB61C06B58D}
AppName=WordPad Neo
AppVersion={#AppVersion}
AppPublisher=suzush
DefaultDirName={autopf}\WordPad Neo
DefaultGroupName=WordPad Neo
SetupIconFile={#AppIconPath}
UninstallDisplayName=WordPad Neo
UninstallDisplayIcon={app}\WordPadNeo.ico
OutputDir={#InstallerOutputDirectory}
OutputBaseFilename=WordPadNeo_{#AppVersion}_win-{#TargetArchitecture}_setup
ArchitecturesAllowed={#AllowedArchitectures}
ArchitecturesInstallIn64BitMode={#Install64Bit}
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
WizardStyle=modern
CloseApplications=yes

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDirectory}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#AppIconPath}"; DestDir: "{app}"; DestName: "WordPadNeo.ico"; Flags: ignoreversion

[Icons]
Name: "{group}\WordPad Neo"; Filename: "{app}\wordpadneo.exe"; IconFilename: "{app}\WordPadNeo.ico"
Name: "{autodesktop}\WordPad Neo"; Filename: "{app}\wordpadneo.exe"; IconFilename: "{app}\WordPadNeo.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\wordpadneo.exe"; Description: "Launch WordPad Neo"; Flags: nowait postinstall skipifsilent
