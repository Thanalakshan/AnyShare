#define MyAppName "AnyShare"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "AnyShare"
#define MyAppExeName "AnyShareWindows.exe"
#define PublishDir "..\publish\win-x64"

[Setup]
AppId={{A7B3E4F1-9C2D-4E8A-B5F6-1D0C3A8E7B92}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=output
OutputBaseFilename=AnyShare-Setup-{#MyAppVersion}
SetupIconFile=..\Assets\icon\icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
LicenseFile=
InfoBeforeFile=
InfoAfterFile=

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"
Name: "startup"; Description: "Start {#MyAppName} when Windows starts"; GroupDescription: "Startup options:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "AnyShare"; ValueData: """{app}\{#MyAppExeName}"""; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
procedure WriteInitialSettings(OpenAtStartup: Boolean);
var
  SettingsDir: String;
  SettingsPath: String;
  StartupValue: String;
begin
  SettingsDir := ExpandConstant('{userappdata}\AnyShare');
  ForceDirectories(SettingsDir);
  SettingsPath := SettingsDir + '\settings.json';

  if OpenAtStartup then
    StartupValue := 'true'
  else
    StartupValue := 'false';

  SaveStringToFile(
    SettingsPath,
    '{' + #13#10 +
    '  "NetworkSpeedMonitor": false,' + #13#10 +
    '  "NetworkSharing": false,' + #13#10 +
    '  "ClipboardSharing": false,' + #13#10 +
    '  "OpenAtStartup": ' + StartupValue + ',' + #13#10 +
    '  "CurrentSpeed": "0 KB/s",' + #13#10 +
    '  "TodayUsage": "0 MB"' + #13#10 +
    '}' + #13#10,
    False
  );
end;

procedure UpdateOpenAtStartup(Enabled: Boolean);
var
  SettingsPath: String;
  AnsiContent: AnsiString;
  Content: String;
  NewValue: String;
begin
  SettingsPath := ExpandConstant('{userappdata}\AnyShare\settings.json');

  if Enabled then
    NewValue := 'true'
  else
    NewValue := 'false';

  if LoadStringFromFile(SettingsPath, AnsiContent) then
  begin
    Content := AnsiContent;
    StringChangeEx(Content, '"OpenAtStartup": true', '"OpenAtStartup": ' + NewValue, True);
    StringChangeEx(Content, '"OpenAtStartup": false', '"OpenAtStartup": ' + NewValue, True);
    SaveStringToFile(SettingsPath, Content, False);
  end
  else
  begin
    WriteInitialSettings(Enabled);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    UpdateOpenAtStartup(WizardIsTaskSelected('startup'));
  end;
end;
