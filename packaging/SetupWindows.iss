// Inno Setup 6 uses a 32-bit script process, also in 64-bit install mode.
type
  TSecurityAttributes = record
    Length, Descriptor, Inherit: LongWord;
  end;

function OpenProcess(Access: LongWord; Inherit: Boolean; ProcessId: LongWord): THandle;
  external 'OpenProcess@kernel32.dll stdcall';
function WaitForSingleObject(Handle: THandle; Milliseconds: LongWord): LongWord;
  external 'WaitForSingleObject@kernel32.dll stdcall';
function CloseHandle(Handle: THandle): Boolean;
  external 'CloseHandle@kernel32.dll stdcall';
function GetFileAttributes(Path: String): LongWord;
  external 'GetFileAttributesW@kernel32.dll stdcall';
function ConvertSecurity(Text: String; Revision: LongWord; var Descriptor: LongWord; Size: LongWord): Boolean;
  external 'ConvertStringSecurityDescriptorToSecurityDescriptorW@advapi32.dll stdcall';
function SetFileSecurity(Path: String; Information, Descriptor: LongWord): Boolean;
  external 'SetFileSecurityW@advapi32.dll stdcall';
function LocalFree(Handle: LongWord): LongWord;
  external 'LocalFree@kernel32.dll stdcall';
function CreateSecuredDirectory(Path: String; var Security: TSecurityAttributes): Boolean;
  external 'CreateDirectoryW@kernel32.dll stdcall';

function WaitForEditor: Boolean;
var
  EditorPid: Integer;
  EditorProcess: THandle;
begin
  Result := True;
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

function IsReparse(const Path: String): Boolean;
var Attr: LongWord;
begin
  Attr := GetFileAttributes(Path);
  Result := (Attr <> $FFFFFFFF) and (Attr and $400 <> 0);
end;

procedure RequireOrdinaryParents(Path: String);
var Parent: String;
begin
  Path := RemoveBackslashUnlessRoot(Path);
  repeat
    if IsReparse(Path) then RaiseException('Katalog programu lub jego podkatalog przekierowuje do innej lokalizacji: ' + Path);
    Parent := RemoveBackslashUnlessRoot(ExtractFileDir(Path));
    if (Parent = '') or (CompareText(Parent, Path) = 0) then exit;
    Path := Parent;
  until False;
end;

function IsAccountSid(const Sid: String): Boolean;
var i: Integer;
begin
  Result := (Copy(Sid, 1, 4) = 'S-1-') and (Length(Sid) >= 7) and (Length(Sid) < 128);
  for i := 5 to Length(Sid) do
    if (Sid[i] <> '-') and ((Sid[i] < '0') or (Sid[i] > '9')) then Result := False;
end;

function Arg(const Value: String): String;
var i: Integer;
begin
  if Pos('"', Value) <> 0 then RaiseException('Ścieżka zawiera niedozwolone cudzysłowy.');
  Result := '"' + Value;
  i := Length(Value);
  while (i > 0) and (Value[i] = '\') do begin
    Result := Result + '\';
    i := i-1;
  end;
  Result := Result + '"';
end;

procedure ApplyFileDacl(const Path, Access: String);
var Descriptor: LongWord;
begin
  if not ConvertSecurity('D:P(A;;FA;;;SY)(A;;FA;;;BA)(A;;' + Access + ';;;BU)',1,Descriptor,0) then
    RaiseException('Nie można przygotować uprawnień pomocnika Setup.');
  try
    if not SetFileSecurity(Path, $80000004, Descriptor) then
      RaiseException('Nie można nadać uprawnień pojedynczemu plikowi pomocnika Setup.');
  finally
    LocalFree(Descriptor);
  end;
end;

procedure InitializeStorageTool;
var Response: String;
begin
  ExtractTemporaryFile('SetupStorageTool.exe');
  Response := ExpandConstant('{tmp}\storage-response.txt');
  if not SaveStringToFile(Response, '', False) then RaiseException('Nie można przygotować odpowiedzi pomocnika Setup.');
#ifndef Fixture
  // Only these two files are opened to the original user. The response cannot
  // introduce code into the administrator-owned temporary directory.
  ApplyFileDacl(ExpandConstant('{tmp}\SetupStorageTool.exe'), 'GRGX');
  ApplyFileDacl(Response, 'GRGW');
#endif
end;

function RunStorage(const Operation, Arguments: String; var Info: TArrayOfString): String;
var
  Response: String;
  ExitCode: Integer;
begin
  SetArrayLength(Info,0);
  Response := ExpandConstant('{tmp}\storage-response.txt');
  if not SaveStringToFile(Response, '', False) then begin
    Result := 'Nie można wyczyścić odpowiedzi pomocnika Setup.';
    exit;
  end;
  Result := 'Nie można wykonać sprawdzenia z uprawnieniami bieżącego użytkownika.';
  if not ExecAsOriginalUser(ExpandConstant('{tmp}\SetupStorageTool.exe'), Arg(Response) + ' ' + Operation + ' ' + Arguments,
    ExpandConstant('{win}'), SW_HIDE, ewWaitUntilTerminated, ExitCode) then begin
    Result := Result + ' ' + SysErrorMessage(ExitCode);
    exit;
  end;
  if not LoadStringsFromFile(Response, Info) then exit;
  if (GetArrayLength(Info) >= 1) and (ExitCode = 0) and (Info[0] = 'OK') then Result := ''
  else if (GetArrayLength(Info) >= 2) and (Info[0] = 'ERROR') then Result := Info[1];
end;

procedure CreateDataLeaf(const Path, Sid: String);
#ifndef Fixture
var Security: TSecurityAttributes;
#endif
begin
  if DirExists(Path) then exit;
#ifdef Fixture
  if not CreateDir(Path) then RaiseException('Nie można utworzyć katalogu danych: ' + Path);
#else
  // Grant access only when atomically creating this new, dedicated data leaf.
  // FILE_GENERIC_READ|FILE_GENERIC_WRITE|FILE_GENERIC_EXECUTE|DELETE (Modify).
  if not ConvertSecurity('D:P(A;OICI;FA;;;SY)(A;OICI;FA;;;BA)(A;OICI;0x1301bf;;;' + Sid + ')',
    1,Security.Descriptor,0) then RaiseException('Nie można przygotować uprawnień katalogu danych.');
  try
    Security.Length := 12;
    Security.Inherit := 0;
    if not CreateSecuredDirectory(Path, Security) then RaiseException('Nie można bezpiecznie utworzyć katalogu danych: ' + Path);
  finally
    LocalFree(Security.Descriptor);
  end;
#endif
end;
