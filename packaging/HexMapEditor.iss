#ifndef Version
  #error "Pass the compiled application version as /DVersion"
#endif
#ifndef Payload
  #error "Pass the tested HexMapEditor.exe as /DPayload"
#endif

#ifdef Fixture
  #define ProductId "amdesar.HexMapEditor.Fixture." + Fixture
  #define DisplayName "HexMapEditor (test izolowany)"
  #define InstallGuard "Local\amdesar.HexMapEditor.InstallFixture"
  #define Classes "Software\HexMapEditor.InstallFixture\" + Fixture + "\Classes"
  #define ProgramGroupPath "{app}\FixtureStart\HexMapEditor"
  #define InstallDirectory FixtureInstallDir
#else
  #define ProductId "amdesar.HexMapEditor.Windows"
  #define DisplayName "HexMapEditor"
  #define InstallGuard "Local\amdesar.HexMapEditor.Installed"
  #define Classes "Software\Classes"
  #define ProgramGroupPath "{group}\HexMapEditor"
  #define InstallDirectory "{localappdata}\Programs\HexMapEditor"
#endif
#define ProgId "amdesar.HexMapEditor.Hexmap"

[Setup]
AppId={#ProductId}
AppName={#DisplayName}
AppVersion={#Version}
AppPublisher=amdesar
VersionInfoVersion={#Version}.0
VersionInfoProductVersion={#Version}.0
VersionInfoProductName=HexMapEditor Setup
VersionInfoDescription=HexMapEditor Setup
DefaultDirName={#InstallDirectory}
DefaultGroupName=HexMapEditor
DisableProgramGroupPage=yes
DisableDirPage=yes
UsePreviousAppDir=yes
UsePreviousTasks=yes
PrivilegesRequired=lowest
MinVersion=10.0
CloseApplications=no
RestartApplications=no
ChangesAssociations=yes
SetupMutex={#ProductId}.Setup
SetupIconFile=..\assets\app\icon.ico
UninstallDisplayIcon={app}\HexMapEditor.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
OutputBaseFilename=HexMapEditor-{#Version}-Setup

[Languages]
Name: polish; MessagesFile: "compiler:Languages\Polish.isl"

[Tasks]
Name: hexmap; Description: "Skojarz pliki .hexmap z HexMapEditorem"; GroupDescription: "Skojarzenia plików"

[Files]
Source: "{#Payload}"; DestDir: "{app}"; DestName: "HexMapEditor.exe"; Flags: ignoreversion
Source: "installed.mode"; DestDir: "{app}"; DestName: "deployment.mode"

[Icons]
Name: "{#ProgramGroupPath}"; Filename: "{app}\HexMapEditor.exe"; WorkingDir: "{app}"

[Registry]
Root: HKCU; Subkey: "{#Classes}\.hexmap\OpenWithProgids"; ValueType: string; ValueName: "{#ProgId}"; ValueData: ""; Tasks: hexmap; Flags: uninsdeletevalue
Root: HKCU; Subkey: "{#Classes}\{#ProgId}"; ValueType: string; ValueData: "Mapa HexMapEditor"; Tasks: hexmap; Flags: uninsdeletekey
Root: HKCU; Subkey: "{#Classes}\{#ProgId}\DefaultIcon"; ValueType: string; ValueData: """{app}\HexMapEditor.exe"",0"; Tasks: hexmap
Root: HKCU; Subkey: "{#Classes}\{#ProgId}\shell\open\command"; ValueType: string; ValueData: """{app}\HexMapEditor.exe"" ""%1"""; Tasks: hexmap

[Code]
function OpenProcess(Access: LongWord; Inherit: Boolean; ProcessId: LongWord): THandle;
  external 'OpenProcess@kernel32.dll stdcall';
function WaitForSingleObject(Handle: THandle; Milliseconds: LongWord): LongWord;
  external 'WaitForSingleObject@kernel32.dll stdcall';
function CloseHandle(Handle: THandle): Boolean;
  external 'CloseHandle@kernel32.dll stdcall';

function InitializeSetup: Boolean;
var
  EditorPid: Integer;
  EditorProcess: THandle;
begin
  Result := IsDotNetInstalled(net48, 0);
  if not Result then begin
    SuppressibleMsgBox('HexMapEditor wymaga pełnego .NET Framework 4.8 lub 4.8.1. Włącz lub zainstaluj ten składnik Windows, a potem ponów instalację.', mbError, MB_OK, IDOK);
    exit;
  end;
  { The updater starts Setup just before the editor process exits. Wait without sending it input. }
  EditorPid := StrToIntDef(ExpandConstant('{param:WaitForEditorPid|0}'), 0);
  if EditorPid <= 0 then exit;
  EditorProcess := OpenProcess($00100000, False, EditorPid);
  if EditorProcess = 0 then exit;
  try
    Result := WaitForSingleObject(EditorProcess, 30000) = 0;
    if not Result then
      SuppressibleMsgBox('Edytor nadal działa. Zamknij go w zwykły sposób i ponów instalację.', mbError, MB_OK, IDOK);
  finally
    CloseHandle(EditorProcess);
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if CheckForMutexes('{#InstallGuard}') then
    Result := 'HexMapEditor nadal działa. Zapisz pracę, zamknij edytor i ponów instalację.';
end;

function InitializeUninstall: Boolean;
begin
  Result := not CheckForMutexes('{#InstallGuard}');
  if not Result then
    SuppressibleMsgBox('Zapisz pracę i zamknij HexMapEditor przed odinstalowaniem.', mbError, MB_OK, IDOK);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssPostInstall) and not WizardIsTaskSelected('hexmap') then begin
    RegDeleteValue(HKCU, '{#Classes}\.hexmap\OpenWithProgids', '{#ProgId}');
    RegDeleteKeyIncludingSubkeys(HKCU, '{#Classes}\{#ProgId}');
  end;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if (CurPageID = wpFinished) and WizardIsTaskSelected('hexmap') then
    WizardForm.FinishedLabel.Caption := WizardForm.FinishedLabel.Caption + #13#10#13#10 +
      'Aby ustawić HexMapEditor jako domyślną aplikację dla .hexmap, użyj „Otwórz za pomocą” lub Ustawienia → Aplikacje domyślne.';
end;
