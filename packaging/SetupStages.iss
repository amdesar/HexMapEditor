function IsInstallWorker: Boolean;
begin
  Result := ExpandConstant('{param:HMEWorker|0}') = '1';
end;

function IsAppUpdate: Boolean;
begin
  Result := ExpandConstant('{param:HMEUpdate|0}') = '1';
end;

function IsMachineUpdate: Boolean;
begin
  Result := IsAppUpdate and (ExpandConstant('{param:HMEUpdateScope|}') = 'machine');
end;

function MachineProgram: String;
begin
  Result := '';
  if IsWin64 then RegQueryStringValue(HKLM64, UninstallKey, 'InstallLocation', Result);
  if Result = '' then RegQueryStringValue(HKLM32, UninstallKey, 'InstallLocation', Result);
  Result := RemoveBackslashUnlessRoot(Result);
end;

function UserProgram: String;
begin
  Result := '';
  RegQueryStringValue(HKCU, UninstallKey, 'InstallLocation', Result);
  Result := RemoveBackslashUnlessRoot(Result);
end;

function DefaultProgramDirectory(Param: String): String;
begin
  Result := MachineProgram;
  if Result = '' then Result := ExpandConstant('{commonpf}\HexMapEditor');
end;

function SetupScope(Param: String): String;
begin
  if IsAdminInstallMode then Result := 'machine'
  else if IsMachineUpdate then Result := 'update'
  else Result := 'user';
end;

procedure LockUpdateSource(const Digest: String);
var Source: String;
begin
  Source := ExpandConstant('{srcexe}');
  RequireOrdinaryParents(Source);
  UpdateSourceLock := OpenSetupFile(Source, $80000000, 1, 0, 3, $00200000, 0);
  if UpdateSourceLock = $FFFFFFFF then RaiseException('Nie można zablokować pliku źródłowego Setup.');
  if CompareText(GetSHA256OfFile(Source), Digest) <> 0 then
    RaiseException('Suma SHA-256 instalatora zmieniła się. Pobierz aktualizację ponownie.');
end;

function RunMachineStage(const Directory, Mode, LibraryPath, Tasks: String; ShowProgress, CopyData: Boolean): Integer;
var Source, Args, ErrorText: String; Lock: THandle; Info: TArrayOfString;
begin
  Result := 1;
  if IsUserAnAdmin then exit;
  Source := ExpandConstant('{srcexe}');
  try
    RequireOrdinaryParents(Source);
    Lock := OpenSetupFile(Source, $80000000, 1, 0, 3, $00200000, 0);
    if Lock = $FFFFFFFF then RaiseException('Nie można zablokować pliku źródłowego Setup.');
    try
      Args := Arg(Source) + ' ' + Arg(Directory) + ' ' + IntToStr(Ord(ShowProgress)) + ' ' +
        Arg(Mode) + ' ' + Arg(LibraryPath) + ' ' + Arg(Tasks) + ' ' + IntToStr(Ord(CopyData));
      ErrorText := RunStorage('machine-stage', Args, Info);
      if (ErrorText = '') and (GetArrayLength(Info) = 2) then
        Result := StrToIntDef(Info[1], 13)
      else begin
        Log('Nie udało się rozpocząć etapu zapisu Setup. ' + ErrorText);
        Result := 13;
      end;
    finally
      CloseHandle(Lock);
    end;
  except
    Log(GetExceptionMessage);
    Result := 12;
  end;
end;

function StageSucceeded(Code: Integer; const Directory: String): Boolean;
var FileVersion: String;
begin
  Result := False;
  if Code <> 0 then exit;
  if not GetVersionNumbersString(AddBackslash(Directory) + 'HexMapEditor.exe', FileVersion) then exit;
  Result := FileVersion = '{#Version}.0';
end;

function StageFailure(Code: Integer): String;
begin
  case Code of
    2, 5, 1223: Result := 'Instalacja została anulowana lub nie udzielono wymaganej zgody Windows.';
    10: Result := 'Setup wymaga ponownego uruchomienia systemu. Nie wznowiono edytora.';
    12: Result := 'Nie można bezpiecznie otworzyć instalatora do następnego etapu.';
    13: Result := 'Windows nie uruchomił etapu zapisu Setup. Sprawdź uprawnienia i komunikaty zabezpieczeń.';
  else
    Result := 'Setup nie potwierdził zakończenia instalacji. Kod: ' + IntToStr(Code) + '.';
  end;
end;

procedure ValidateAppUpdate;
var Machine, Personal, Scope, Digest: String; I: Integer;
begin
  if IsInstallWorker or IsAdminInstallMode then RaiseException('Nieprawidłowy tryb aktualizacji Setup.');
  Machine := MachineProgram;
  Personal := UserProgram;
  Scope := ExpandConstant('{param:HMEUpdateScope|}');
  Digest := ExpandConstant('{param:HMESetupHash|}');
  if IsMachineUpdate then UpdateProgram := Machine else UpdateProgram := Personal;
  if ((Scope <> 'user') and not IsMachineUpdate) or (UpdateProgram = '') or
    ((Machine <> '') and (Personal <> '')) or
    (CompareText(UpdateProgram, RemoveBackslashUnlessRoot(ExpandConstant('{param:DIR|}'))) <> 0) or
    (ExpandConstant('{param:HMEExpectedVersion|}') <> '{#Version}') or (Length(Digest) <> 64) then
    RaiseException('Nie potwierdzono ścieżki, zakresu lub wersji aktualizowanej instalacji. Setup nie rozpoczął zapisu.');
  for I := 1 to Length(Digest) do
    if Pos(Lowercase(Digest[I]), '0123456789abcdef') = 0 then
      RaiseException('Aktualizacja nie przekazała poprawnej sumy SHA-256.');
  LockUpdateSource(Digest);
end;

procedure FinishAppUpdate(Code: Integer);
var Marker, Exe: String;
begin
  if not StageSucceeded(Code, UpdateProgram) then begin
    SuppressibleMsgBox(StageFailure(Code) + #13#10 + 'Edytor możesz ponownie otworzyć z menu Start.', mbError, MB_OK, IDOK);
    exit;
  end;
  // Recheck the scope before allowing a successful update to launch the editor.
  if IsMachineUpdate then Marker := MachineProgram else Marker := UserProgram;
  if CompareText(Marker, UpdateProgram) <> 0 then begin
    SuppressibleMsgBox('Setup nie potwierdził zachowania rejestracji instalacji. Edytor nie został wznowiony.', mbError, MB_OK, IDOK);
    exit;
  end;
  Log('Zaktualizowano HexMapEditor do wersji {#Version}.');
  SuppressibleMsgBox('Zaktualizowano HexMapEditor do wersji {#Version}.', mbInformation, MB_OK, IDOK);
  Exe := AddBackslash(UpdateProgram) + 'HexMapEditor.exe';
  if not Exec(Exe, '', UpdateProgram, SW_SHOWNORMAL, ewNoWait, Code) then
    SuppressibleMsgBox('Aktualizacja jest zapisana, lecz edytor nie uruchomił się. Otwórz go z menu Start.', mbInformation, MB_OK, IDOK);
end;
