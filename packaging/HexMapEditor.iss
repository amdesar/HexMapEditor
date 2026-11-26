#ifndef Version
  #error "Pass the compiled application version as /DVersion"
#endif
#ifndef Payload
  #error "Pass the tested HexMapEditor.exe as /DPayload"
#endif
#ifndef StorageTool
  #error "Pass the Setup storage tool as /DStorageTool"
#endif

#ifdef Fixture
  #define ProductId "HexMapEditor.Fixture." + Fixture
  #define LegacyProductId "amdesar.HexMapEditor.Fixture." + FixtureLegacyId
  #define DisplayName "HexMapEditor (test izolowany)"
  #define InstallGuard "Local\amdesar.HexMapEditor.InstallFixture"
  #define AssociationRoot "HKCU"
  #define Classes "Software\HexMapEditor.InstallFixture\" + Fixture + "\Classes"
  #define ProgramGroupPath "{app}\FixtureStart\HexMapEditor"
  #define DesktopDirectory "{app}\FixtureDesktop"
  #define InstallDirectory FixtureInstallDir
  #define FixtureProfile ExtractFileDir(FixtureInstallDir) + "\FixtureProfile"
#else
  #define ProductId "HexMapEditor.Windows"
  #define LegacyProductId "amdesar.HexMapEditor.Windows"
  #define DisplayName "HexMapEditor"
  #define InstallGuard "Local\amdesar.HexMapEditor.Installed"
  #define AssociationRoot "HKA"
  #define Classes "Software\Classes"
  #define ProgramGroupPath "{group}\HexMapEditor"
  #define DesktopDirectory "{autodesktop}"
  #define InstallDirectory "{code:DefaultProgramDirectory}"
  #define FixtureProfile ""
#endif
#define ProgId "HexMapEditor.Hexmap"

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
DisableDirPage=no
DisableStartupPrompt=yes
UsePreviousAppDir=yes
UsePreviousTasks=yes
AllowUNCPath=no
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline
UsePreviousPrivileges=no
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
CloseApplications=no
RestartApplications=no
ChangesAssociations=yes
SetupMutex={#ProductId}.Setup.{code:SetupScope}
SetupIconFile=..\assets\app\icon.ico
UninstallDisplayIcon={app}\HexMapEditor.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
OutputBaseFilename=HexMapEditor-{#Version}-Setup
#ifdef Fixture
SetupLogging=yes
#endif

[Languages]
Name: polish; MessagesFile: "compiler:Languages\Polish.isl"

[Tasks]
Name: hexmap; Description: "Skojarz pliki .hexmap z HexMapEditorem"; GroupDescription: "Opcje dodatkowe"
Name: desktop; Description: "Utwórz skrót na pulpicie"; GroupDescription: "Opcje dodatkowe"; Flags: unchecked

[Files]
Source: "{#StorageTool}"; DestName: "SetupStorageTool.exe"; Flags: dontcopy noencryption
Source: "{#Payload}"; DestDir: "{app}"; DestName: "HexMapEditor.exe"; Flags: ignoreversion
Source: "installed.mode"; DestDir: "{app}"; DestName: "deployment.mode"; Flags: ignoreversion; Check: IsAdminInstallMode
Source: "installed-user.mode"; DestDir: "{app}"; DestName: "deployment.mode"; Flags: ignoreversion; Check: not IsAdminInstallMode
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "{tmp}\library-location.txt"; DestDir: "{app}"; Flags: external ignoreversion

[Icons]
Name: "{#ProgramGroupPath}"; Filename: "{app}\HexMapEditor.exe"; WorkingDir: "{app}"

[Registry]
Root: {#AssociationRoot}; Subkey: "{#Classes}\.hexmap\OpenWithProgids"; ValueType: string; ValueName: "{#ProgId}"; ValueData: ""; Tasks: hexmap; Flags: uninsdeletevalue
Root: {#AssociationRoot}; Subkey: "{#Classes}\{#ProgId}"; ValueType: string; ValueData: "Mapa HexMapEditor"; Tasks: hexmap; Flags: uninsdeletekey
Root: {#AssociationRoot}; Subkey: "{#Classes}\{#ProgId}\DefaultIcon"; ValueType: string; ValueData: """{app}\HexMapEditor.exe"",0"; Tasks: hexmap
Root: {#AssociationRoot}; Subkey: "{#Classes}\{#ProgId}\shell\open\command"; ValueType: string; ValueData: """{app}\HexMapEditor.exe"" ""%1"""; Tasks: hexmap

[Code]
#include "SetupWindows.iss"

const
  DesktopTag = 'Skrót utworzony przez HexMapEditor Setup';
  RuntimePageUrl = 'https://dotnet.microsoft.com/download/dotnet-framework/net48';
  UninstallKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#ProductId}_is1';
var
  LibraryPage: TWizardPage;
  AdjacentRadio, DocumentsRadio, CustomRadio: TNewRadioButton;
  LibraryEdit: TNewEdit;
  LibraryBrowse: TNewButton;
  CopyLegacy: TNewCheckBox;
  DocumentsLibrary, CustomLibrary, LegacyProgram, LegacyRoot, OriginalSid, PreviousProgram, OriginalDesktop: String;
  LegacyData, MigrationOffered, DelegationDone, DelegationBusy: Boolean;
  OppositeProgram, MachineTarget, UpdateProgram: String;
  UpdateSourceLock: THandle;

#include "SetupStages.iss"

function InitializeSetup: Boolean;
var
  ErrorCode: Integer;
begin
  if not IsAdminInstallMode and IsUserAnAdmin then begin
    SuppressibleMsgBox('Uruchom Setup zwykłym dwuklikiem. Jeżeli wybrana lokalizacja wymaga uprawnień administratora, kreator poprosi o zgodę Windows.', mbError, MB_OK, IDOK);
    Result := False;
    exit;
  end;
  if IsAdminInstallMode and not IsInstallWorker then begin
    Result := False;
    exit;
  end;
  Result := IsDotNetInstalled(net48, 0);
#ifdef Fixture
  if ExpandConstant('{param:FixtureMissingRuntime|0}') = '1' then Result := False;
#endif
  if not Result then begin
    Log('Missing runtime. Official page: ' + RuntimePageUrl);
    if SuppressibleTaskDialogMsgBox('Brakuje .NET Framework 4.8 lub 4.8.1',
      'Pobierz pełne środowisko .NET Framework Runtime z oficjalnej strony Microsoftu. Po zainstalowaniu środowiska uruchom Setup ponownie.',
      mbError, MB_OKCANCEL, ['Strona pobrania .NET Framework Runtime'], 0, IDCANCEL) = IDOK then
      if not ShellExecAsOriginalUser('open', RuntimePageUrl, '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode) then
        MsgBox('Otwórz stronę w przeglądarce: ' + RuntimePageUrl, mbInformation, MB_OK);
    exit;
  end;
  if IsAppUpdate then begin
    try
      ValidateAppUpdate;
    except
      MsgBox(GetExceptionMessage, mbError, MB_OK);
      Result := False;
      exit;
    end;
  end;
  Result := WaitForEditor;
end;

procedure DeinitializeSetup;
begin
  if (UpdateSourceLock <> 0) and (UpdateSourceLock <> $FFFFFFFF) then CloseHandle(UpdateSourceLock);
  if IsAppUpdate and not IsMachineUpdate and DelegationDone then FinishAppUpdate(0);
end;

function HasProgramEntry(const ProgramDir, Name: String): Boolean;
begin
  Result := GetFileAttributes(AddBackslash(ProgramDir) + Name) <> $FFFFFFFF;
end;

function ChosenLibrary: String;
begin
  if AdjacentRadio.Checked then Result := AddBackslash(WizardDirValue) + 'Library'
  else if DocumentsRadio.Checked then Result := DocumentsLibrary
  else Result := Trim(LibraryEdit.Text);
end;

procedure LibraryPickChanged(Sender: TObject);
begin
  LibraryEdit.ReadOnly := not CustomRadio.Checked;
  LibraryBrowse.Enabled := CustomRadio.Checked;
  if CustomRadio.Checked then LibraryEdit.Text := CustomLibrary
  else LibraryEdit.Text := ChosenLibrary;
end;

procedure BrowseLibrary(Sender: TObject);
var Folder: String;
begin
  Folder := LibraryEdit.Text;
  if BrowseForFolder('Wybierz katalog Library', Folder, False) then LibraryEdit.Text := Folder;
end;

procedure SaveCustomLibrary(Sender: TObject);
begin
  if CustomRadio.Checked and not LibraryEdit.ReadOnly then CustomLibrary := LibraryEdit.Text;
end;

procedure InitializeWizard;
var
  Info: TArrayOfString;
  Mode, SavedPath, ErrorText: String;
  Width, RadioLeft: Integer;
  RecordedPath: TArrayOfString;
begin
  InitializeStorageTool;
  ErrorText := RunStorage('inspect', Arg('{#FixtureProfile}') + ' ' + Arg('{#LegacyProductId}') + ' ' + Arg('{#ProductId}'), Info);
  if (ErrorText <> '') or (GetArrayLength(Info) <> 8) then
    RaiseException('Nie można ustalić konta użytkownika i jego Dokumentów. ' + ErrorText);
  DocumentsLibrary := Info[1];
  LegacyProgram := Info[2];
  LegacyRoot := Info[3];
  LegacyData := Info[4] = '1';
  OriginalSid := Info[5];
  OriginalDesktop := Info[6];
  if not IsAccountSid(OriginalSid) then RaiseException('Nieprawidłowa tożsamość użytkownika Setup.');
  MachineTarget := MachineProgram;
  if IsAdminInstallMode then begin
    PreviousProgram := MachineTarget;
    OppositeProgram := Info[7];
  end else begin
    PreviousProgram := Info[7];
    if (PreviousProgram = '') and not IsInstallWorker then PreviousProgram := MachineTarget
    else OppositeProgram := MachineTarget;
  end;
  if PreviousProgram <> '' then WizardForm.DirEdit.Text := PreviousProgram;

  LibraryPage := CreateCustomPage(wpSelectDir, 'Lokalizacja biblioteki',
    'Wybierz katalog terenów, ikon, obiektów i presetów. Położenie biblioteki nie zmienia położenia map.');
  Width := LibraryPage.SurfaceWidth;
  RadioLeft := ScaleX(2);
  AdjacentRadio := TNewRadioButton.Create(WizardForm);
  AdjacentRadio.Parent := LibraryPage.Surface;
  AdjacentRadio.SetBounds(RadioLeft, ScaleY(6), Width-RadioLeft, ScaleY(22));
  AdjacentRadio.Caption := 'Obok programu';
  DocumentsRadio := TNewRadioButton.Create(WizardForm);
  DocumentsRadio.Parent := LibraryPage.Surface;
  DocumentsRadio.SetBounds(RadioLeft, ScaleY(34), Width-RadioLeft, ScaleY(22));
  DocumentsRadio.Caption := 'W Dokumentach';
  DocumentsRadio.Enabled := DocumentsLibrary <> '';
  CustomRadio := TNewRadioButton.Create(WizardForm);
  CustomRadio.Parent := LibraryPage.Surface;
  CustomRadio.SetBounds(RadioLeft, ScaleY(62), Width-RadioLeft, ScaleY(22));
  CustomRadio.Caption := 'Własna lokalizacja';
  LibraryEdit := TNewEdit.Create(WizardForm);
  LibraryEdit.Parent := LibraryPage.Surface;
  LibraryEdit.SetBounds(0, ScaleY(94), Width-ScaleX(109), ScaleY(24));
  LibraryEdit.OnChange := @SaveCustomLibrary;
  LibraryBrowse := TNewButton.Create(WizardForm);
  LibraryBrowse.Parent := LibraryPage.Surface;
  LibraryBrowse.SetBounds(Width-ScaleX(101), ScaleY(93), ScaleX(101), ScaleY(26));
  LibraryBrowse.Caption := 'Przeglądaj…';
  LibraryBrowse.OnClick := @BrowseLibrary;
  CopyLegacy := TNewCheckBox.Create(WizardForm);
  CopyLegacy.Parent := LibraryPage.Surface;
  CopyLegacy.SetBounds(0, ScaleY(136), Width, ScaleY(28));
  CopyLegacy.Caption := 'Skopiuj Library, MRU i ustawienia z 0.12.0';
  MigrationOffered := LegacyData and (GetPreviousData('LegacyOffered', '0') <> '1');
  CopyLegacy.Visible := MigrationOffered;

  Mode := GetPreviousData('LibraryMode', 'adjacent');
  SavedPath := GetPreviousData('LibraryPath', '');
  if (PreviousProgram <> '') and (SavedPath = '') then
    if LoadStringsFromFile(AddBackslash(PreviousProgram) + 'library-location.txt', RecordedPath) and (GetArrayLength(RecordedPath) = 1) then begin
      SavedPath := RecordedPath[0];
      if CompareText(SavedPath, AddBackslash(PreviousProgram) + 'Library') <> 0 then Mode := 'custom';
    end;
  if IsInstallWorker and (PreviousProgram = '') then begin
    Mode := ExpandConstant('{param:HMELibraryMode|adjacent}');
    SavedPath := ExpandConstant('{param:HMELibrary|}');
    CopyLegacy.Checked := MigrationOffered and (ExpandConstant('{param:HMECopyLegacy|0}') = '1');
  end;
  if (Mode = 'documents') and (SavedPath <> '') then begin
    DocumentsLibrary := SavedPath;
    DocumentsRadio.Enabled := True;
  end;
  CustomLibrary := SavedPath;
  if (Mode <> 'custom') or (CustomLibrary = '') then CustomLibrary := DocumentsLibrary;
#ifdef Fixture
  Mode := ExpandConstant('{param:FixtureLibraryMode|' + Mode + '}');
  if ExpandConstant('{param:FixtureLibraryPath|}') <> '' then CustomLibrary := ExpandConstant('{param:FixtureLibraryPath}');
  CopyLegacy.Checked := MigrationOffered and (ExpandConstant('{param:FixtureCopyLegacy|0}') = '1');
#endif
  if (Mode = 'documents') and DocumentsRadio.Enabled then DocumentsRadio.Checked := True
  else if Mode = 'custom' then CustomRadio.Checked := True
  else AdjacentRadio.Checked := True;
  AdjacentRadio.OnClick := @LibraryPickChanged;
  DocumentsRadio.OnClick := @LibraryPickChanged;
  CustomRadio.OnClick := @LibraryPickChanged;
  LibraryPickChanged(nil);
end;

function ValidPathField(const Path: String): Boolean;
begin
  Result := (Length(Path) > 3) and (Length(Path) < 248) and (Pos('"',Path) = 0) and (Pos(#13,Path) = 0) and (Pos(#10,Path) = 0) and
    (((Path[2] = ':') and (Path[3] = '\')) or ((Copy(Path,1,2) = '\\') and (Copy(Path,1,4) <> '\\?\') and (Copy(Path,1,4) <> '\\.\')));
end;

function ProgramChoiceError: String;
var ProgramDir: String; Candidate: TFindRec;
begin
  ProgramDir := RemoveBackslashUnlessRoot(WizardDirValue);
  Result := '';
  if LegacyProgram <> '' then
    Result := 'Wykryto instalację HexMapEditor 0.12.0 dla bieżącego użytkownika:' + #13#10 + LegacyProgram + #13#10#13#10 +
      'Zapisz pracę, zamknij edytor i odinstaluj starą wersję w ustawieniach Windows. Następnie uruchom ten Setup ponownie. ' +
      'Starsza deinstalacja pozostawia Library, mapy i dane użytkownika.'
  else if OppositeProgram <> '' then
    Result := 'Wykryto kolidującą instalację w „' + OppositeProgram + '”. Zachowano obie lokalizacje.'
  else if not ValidPathField(ProgramDir) then
    Result := 'Podaj pełną ścieżkę katalogu programu na dostępnym dysku.'
  else if (PreviousProgram <> '') and (CompareText(PreviousProgram, ProgramDir) <> 0) then
    Result := 'Istniejąca instalacja jest w „' + PreviousProgram + '”. Aby wybrać inny katalog programu, zamknij edytor i odinstaluj program. ' +
      'Katalogi danych pozostaną na miejscu.'
  else if (PreviousProgram = '') and
    (HasProgramEntry(ProgramDir, 'HexMapEditor.exe') or HasProgramEntry(ProgramDir, 'deployment.mode') or
     HasProgramEntry(ProgramDir, 'library-location.txt') or HasProgramEntry(ProgramDir, 'LICENSE')) then
    Result := 'Ten katalog zawiera pliki kolidujące z instalacją. Wybierz inny katalog instalacji.'
  else if (PreviousProgram = '') and FindFirst(AddBackslash(ProgramDir) + 'unins*', Candidate) then begin
    FindClose(Candidate);
    Result := 'Ten katalog zawiera pliki dezinstalatora. Wybierz inny katalog instalacji.';
  end;
end;

function SelectedLibraryMode: String;
begin
  if AdjacentRadio.Checked then Result := 'adjacent'
  else if DocumentsRadio.Checked then Result := 'documents'
  else Result := 'custom';
end;

procedure CancelButtonClick(CurPageID: Integer; var Cancel, Confirm: Boolean);
begin
  if DelegationBusy then Cancel := False
  else if DelegationDone then begin
    Cancel := True;
    Confirm := False;
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var ErrorText, Scope, ReadyText, Mode, LibraryPath, Tasks: String;
  Info: TArrayOfString; Code: Integer; CopyData: Boolean;
begin
  ErrorText := '';
  if CurPageID = wpSelectDir then ErrorText := ProgramChoiceError;
  if CurPageID = LibraryPage.ID then begin
    if not ValidPathField(ChosenLibrary) then ErrorText := 'Podaj pełną, dostępną ścieżkę katalogu Library.';
    if CustomRadio.Checked then CustomLibrary := Trim(LibraryEdit.Text);
  end;
  if (CurPageID = wpReady) and not IsInstallWorker and not IsAdminInstallMode then begin
    ErrorText := ProgramChoiceError;
    Scope := 'user';
    if (ErrorText = '') and (MachineTarget <> '') then Scope := 'machine'
    else if (ErrorText = '') and (PreviousProgram = '') then begin
      ErrorText := RunStorage('program-scope', Arg(WizardDirValue), Info);
      if (ErrorText = '') and (GetArrayLength(Info) = 2) then Scope := Info[1]
      else if ErrorText = '' then ErrorText := 'Nie można rozpoznać uprawnień katalogu.';
    end;
    if (ErrorText = '') and (Scope = 'machine') then begin
      Result := False;
      Mode := '';
      LibraryPath := '';
      Tasks := '';
      CopyData := False;
      if MachineTarget = '' then begin
        Mode := SelectedLibraryMode;
        LibraryPath := ChosenLibrary;
        Tasks := WizardSelectedTasks(False);
        CopyData := CopyLegacy.Visible and CopyLegacy.Checked;
      end;
      DelegationBusy := True;
      ReadyText := WizardForm.ReadyMemo.Text;
      WizardForm.ReadyMemo.Text := ReadyText + #13#10#13#10 + 'Trwa instalacja. Windows może poprosić o zgodę administratora.';
      WizardForm.NextButton.Enabled := False;
      WizardForm.BackButton.Enabled := False;
      WizardForm.CancelButton.Enabled := False;
      try
        Code := RunMachineStage(WizardDirValue, Mode, LibraryPath, Tasks, IsMachineUpdate, CopyData);
      finally
        WizardForm.ReadyMemo.Text := ReadyText;
        DelegationBusy := False;
        WizardForm.NextButton.Enabled := True;
        WizardForm.BackButton.Enabled := True;
        WizardForm.CancelButton.Enabled := True;
      end;
      if IsMachineUpdate then begin
        FinishAppUpdate(Code);
        DelegationDone := True;
        WizardForm.Close;
      end else if StageSucceeded(Code, WizardDirValue) then begin
        DelegationDone := True;
        SuppressibleMsgBox('HexMapEditor został zainstalowany. Uruchom go z menu Start.', mbInformation, MB_OK, IDOK);
        WizardForm.Close;
      end else if not WizardSilent then MsgBox(StageFailure(Code), mbError, MB_OK);
      exit;
    end;
  end;
  Result := ErrorText = '';
  if not Result then begin
    Log(ErrorText);
    SuppressibleMsgBox(ErrorText, mbError, MB_OK, IDOK);
  end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := (PreviousProgram <> '') and
    ((PageID = wpSelectDir) or (PageID = LibraryPage.ID) or (PageID = wpSelectTasks));
end;

function UpdateReadyMemo(Space, NewLine, MemoUserUserInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
begin
  Result := MemoDirInfo + NewLine + NewLine + 'Lokalizacja biblioteki' + NewLine + Space + ChosenLibrary +
    NewLine + NewLine + 'Dane edytora' + NewLine + Space + AddBackslash(WizardDirValue) + 'Data' +
    NewLine + NewLine + MemoTasksInfo;
  if CopyLegacy.Visible and CopyLegacy.Checked then
    Result := Result + NewLine + Space + 'Skopiuj dane 0.12.0 z „' + LegacyRoot + '”. Mapy i oryginały pozostaną na miejscu.';
end;

procedure RegisterPreviousData(PreviousDataKey: Integer);
begin
  SetPreviousData(PreviousDataKey, 'LibraryMode', SelectedLibraryMode);
  SetPreviousData(PreviousDataKey, 'LibraryPath', ChosenLibrary);
  if MigrationOffered or (GetPreviousData('LegacyOffered', '0') = '1') then
    SetPreviousData(PreviousDataKey, 'LegacyOffered', '1');
end;

function DesktopLink(const Name: String): String;
begin
  Result := ExpandConstant('{#DesktopDirectory}') + '\' + Name + '.lnk';
end;

function OwnDesktopLink(const Path: String): Boolean;
var Shell, Link: Variant;
begin
  Result := False;
  if (not FileExists(Path)) or IsReparse(Path) then exit;
  try
    Shell := CreateOleObject('WScript.Shell');
    Link := Shell.CreateShortcut(Path);
    Result := (Link.Description = DesktopTag) and (Link.Arguments = '') and
      (CompareText(Link.TargetPath, ExpandConstant('{app}\HexMapEditor.exe')) = 0);
  except
    Log('Nie można rozpoznać właściciela skrótu na pulpicie.');
  end;
end;

function StorageArguments: String;
begin
  Result := Arg(WizardDirValue) + ' ' + Arg(AddBackslash(WizardDirValue) + 'Data') + ' ' + Arg(ChosenLibrary);
end;

function IsProtectedScope: Boolean;
begin
  Result := IsAdminInstallMode;
#ifdef Fixture
  if ExpandConstant('{param:FixtureMachineProbe|0}') = '1' then Result := True;
#endif
end;

function CopyArguments: String;
begin
  Result := Arg(LegacyRoot) + ' ' + Arg(AddBackslash(WizardDirValue) + 'Data') + ' ' + Arg(ChosenLibrary);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ProgramDir, DataDir, AdjacentDir: String;
  Info, Location: TArrayOfString;
begin
  Result := ProgramChoiceError;
  if Result <> '' then exit;
  if not ValidPathField(ChosenLibrary) then begin
    Result := 'Podaj pełną ścieżkę katalogu Library.';
    exit;
  end;
  ProgramDir := RemoveBackslashUnlessRoot(WizardDirValue);
  DataDir := AddBackslash(ProgramDir) + 'Data';
  AdjacentDir := AddBackslash(ProgramDir) + 'Library';
  try
    Result := CheckTargetEditor(ProgramDir);
    if Result <> '' then exit;
    RequireOrdinaryParents(ProgramDir);
    if IsProtectedScope then Result := CheckMachineProgram(ProgramDir)
    else Result := RunStorage('user-program', Arg(ProgramDir), Info);
    if Result <> '' then begin
      Log(Result);
      exit;
    end;
    if not DirExists(ProgramDir) then begin
      if not ForceDirectories(ProgramDir) then RaiseException('Nie można utworzyć katalogu programu.');
    end;
    RequireOrdinaryParents(ProgramDir);
    RequireOrdinaryParents(AddBackslash(ProgramDir) + 'HexMapEditor.exe');
    RequireOrdinaryParents(AddBackslash(ProgramDir) + 'deployment.mode');
    RequireOrdinaryParents(AddBackslash(ProgramDir) + 'library-location.txt');
    RequireOrdinaryParents(AddBackslash(ProgramDir) + 'LICENSE');
    if IsProtectedScope then begin
      Result := CheckMachineProgram(ProgramDir);
      if Result <> '' then begin
        Log(Result);
        exit;
      end;
    end;
    RequireOrdinaryParents(DataDir);
    CreateDataLeaf(DataDir, OriginalSid);
    if CompareText(RemoveBackslashUnlessRoot(ChosenLibrary), AdjacentDir) = 0 then begin
      RequireOrdinaryParents(AdjacentDir);
      CreateDataLeaf(AdjacentDir, OriginalSid);
    end;
    Result := RunStorage('probe', StorageArguments, Info);
    if Result <> '' then begin
      Log(Result);
      exit;
    end;
    SetArrayLength(Location, 1);
    Location[0] := ChosenLibrary;
    if not SaveStringsToUTF8File(ExpandConstant('{tmp}\library-location.txt'), Location, False) then
      RaiseException('Nie można zapisać konfiguracji biblioteki.');
    if CopyLegacy.Visible and CopyLegacy.Checked then begin
      Result := RunStorage('copy', CopyArguments, Info);
      if Result = '' then Log('Skopiowano dane 0.12.0. Oryginały i pliki map pozostawiono na miejscu.');
    end;
  except
    Result := GetExceptionMessage;
  end;
  if Result <> '' then Log(Result);
end;

procedure RemoveOwnAssociation;
begin
  RegDeleteValue({#AssociationRoot}, '{#Classes}\.hexmap\OpenWithProgids', '{#ProgId}');
  RegDeleteKeyIncludingSubkeys({#AssociationRoot}, '{#Classes}\{#ProgId}');
end;

procedure RemoveOwnDesktopLinks;
var Current, Previous: String;
begin
  Current := DesktopLink('HexMapEditor');
  Previous := DesktopLink('HexMapEditor (instalator)');
  if OwnDesktopLink(Current) then DeleteFile(Current);
  if OwnDesktopLink(Previous) then DeleteFile(Previous);
end;

procedure ApplyDesktop;
var TemporaryLink, Exe, Current, Previous, PersonalLink: String;
begin
  if not WizardIsTaskSelected('desktop') then begin
    RemoveOwnDesktopLinks;
    exit;
  end;
  Current := DesktopLink('HexMapEditor');
  Previous := DesktopLink('HexMapEditor (instalator)');
  PersonalLink := AddBackslash(OriginalDesktop) + 'HexMapEditor.lnk';
  if ((FileExists(Current) or IsReparse(Current)) and not OwnDesktopLink(Current)) or
    ((OriginalDesktop <> '') and (CompareText(Current, PersonalLink) <> 0) and FileExists(PersonalLink)) then begin
    Log('Pozostawiono zewnętrzny skrót HexMapEditor. Skrót programu jest w menu Start.');
    SuppressibleMsgBox('Na pulpicie istnieje już skrót „HexMapEditor”. Pozostawiono go bez zmian. Skrót programu znajdziesz w menu Start.',
      mbInformation, MB_OK, IDOK);
    exit;
  end;
  if not OwnDesktopLink(Current) then begin
#ifdef Fixture
    ForceDirectories(ExpandConstant('{#DesktopDirectory}'));
#endif
    Exe := ExpandConstant('{app}\HexMapEditor.exe');
    TemporaryLink := CreateShellLink(ExpandConstant('{tmp}\desktop-owned.lnk'), DesktopTag, Exe, '',
      ExpandConstant('{app}'), Exe, 0, SW_SHOWNORMAL);
    if not CopyFile(TemporaryLink, Current, True) then
      RaiseException('Nie można utworzyć skrótu na pulpicie. Skrót programu jest w menu Start.');
  end;
  if OwnDesktopLink(Previous) then DeleteFile(Previous);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssDone then DelegationDone := IsAppUpdate and not IsMachineUpdate;
  if CurStep <> ssPostInstall then exit;
  if not WizardIsTaskSelected('hexmap') then RemoveOwnAssociation;
  try
    ApplyDesktop;
  except
    Log(GetExceptionMessage);
    SuppressibleMsgBox(GetExceptionMessage, mbError, MB_OK, IDOK);
  end;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if (LibraryPage <> nil) and (CurPageID = LibraryPage.ID) then LibraryPickChanged(nil);
  if (CurPageID = wpFinished) and WizardIsTaskSelected('hexmap') then
    WizardForm.FinishedLabel.Caption := WizardForm.FinishedLabel.Caption + #13#10#13#10 +
      'Aby ustawić HexMapEditor jako domyślną aplikację dla .hexmap, użyj „Otwórz za pomocą” lub Ustawienia → Aplikacje domyślne.';
end;

function InitializeUninstall: Boolean;
begin
  if not IsAdminInstallMode and IsUserAnAdmin then begin
    SuppressibleMsgBox('Uruchom deinstalator bieżącego użytkownika bez podniesionych uprawnień.', mbError, MB_OK, IDOK);
    Result := False;
    exit;
  end;
  Result := not CheckForMutexes('{#InstallGuard}');
  if not Result then
    SuppressibleMsgBox('Zapisz pracę i zamknij HexMapEditor przed odinstalowaniem.', mbError, MB_OK, IDOK);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then RemoveOwnDesktopLinks;
end;
