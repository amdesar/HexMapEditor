param(
    [Parameter(Mandatory)][ValidateSet('upgrade','uninstall')][string]$Case,
    [string]$InnoCompiler, [string]$Candidate, [string]$Baseline, [string]$Run
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
$work=(Join-Path $PSScriptRoot '_work').TrimEnd('\')+'\'
function Invoke-Setup([string]$file,[string]$label,[string]$parameters='') {
    $log=Join-Path $runPath "$label.log"
    $p=Start-Process -FilePath $file -ArgumentList ('/VERYSILENT /SUPPRESSMSGBOXES /SP- /NORESTART /LOG="{0}" {1}' -f $log,$parameters) -WindowStyle Hidden -PassThru
    if(-not $p.WaitForExit(45000) -or $p.ExitCode -ne 0){throw "Setup fixture $label nie zakończył się poprawnie. Log $log"}
}
function Assert([bool]$value,[string]$message){if(-not $value){throw $message}}
function Snapshot([string]$program,[string]$library) {
    Get-ChildItem -LiteralPath $library,(Join-Path $program 'Data') -File -Recurse | Sort-Object FullName |
        ForEach-Object {"$($_.FullName) $((Get-FileHash -LiteralPath $_.FullName).Hash)"}
}
if($Case -eq 'uninstall'){
    $runPath=(Resolve-Path -LiteralPath $Run).Path
    Assert ($runPath.StartsWith($work,[StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path $runPath -Leaf) -match '^patch-0122-upgrade-[a-f0-9]{32}$') 'Nieprawidłowy katalog próby.'
    $program=Join-Path $runPath 'Program'
    $library=Join-Path $runPath 'Wybrana Library'
    $personal=Join-Path $runPath 'FixtureProfile/Desktop/HexMapEditor.lnk'
    $snap=Snapshot $program $library
    $hash=(Get-FileHash -LiteralPath $personal).Hash
    Invoke-Setup (Join-Path $program 'unins000.exe') '06-uninstall'
    Assert (-not (Test-Path -LiteralPath (Join-Path $program 'FixtureDesktop/HexMapEditor.lnk'))) 'Pozostał własny skrót.'
    Assert ((Get-FileHash -LiteralPath $personal).Hash -eq $hash) 'Naruszono ręczny skrót.'
    Assert (-not (Compare-Object $snap (Snapshot $program $library))) 'Naruszono dane przy deinstalacji.'
    'uninstall PASS'
    return
}
Assert ($InnoCompiler -and $Candidate -and $Baseline) 'Case upgrade wymaga -InnoCompiler, -Candidate i -Baseline.'
$compiler=(Resolve-Path -LiteralPath $InnoCompiler).Path
$next=(Resolve-Path -LiteralPath $Candidate).Path
$baseline=(Resolve-Path -LiteralPath $Baseline).Path
Assert ((Get-FileHash -LiteralPath $baseline).Hash -eq 'B0352801EB84E5E7C81CE19FA42514CB1E016B31DA76DDF306DEB219A0786FF6') 'Niezgodne bajty bazy 0.12.1.'
$id=[Guid]::NewGuid().ToString('N')
$runPath=Join-Path $work "patch-0122-upgrade-$id"
$old=Join-Path $runPath 'Baseline'
$new=Join-Path $runPath 'Candidate'
$program=Join-Path $runPath 'Program'
$library=Join-Path $runPath 'Wybrana Library'
[IO.Directory]::CreateDirectory((Join-Path $old 'packaging'))|Out-Null
[IO.Directory]::CreateDirectory((Join-Path $old 'assets/app'))|Out-Null
foreach($name in @('HexMapEditor.iss','SetupWindows.iss','SetupStorageTool.cs','installed.mode')){
    $content=(git -C $repo show "df48ca4:packaging/$name") -join "`r`n"
    if($LASTEXITCODE -ne 0){throw "Brak pliku bazy $name."}
    [IO.File]::WriteAllText((Join-Path $old "packaging/$name"),$content.TrimStart([char]0xFEFF),[Text.UTF8Encoding]::new($true))
}
[IO.File]::Copy((Join-Path $repo 'assets/app/icon.ico'),(Join-Path $old 'assets/app/icon.ico'))
$tool=Join-Path $old 'SetupStorageTool.exe'
& (Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe') /nologo /target:winexe "/out:$tool" (Join-Path $old 'packaging/SetupStorageTool.cs')
if($LASTEXITCODE -ne 0){throw 'Nie zbudowano pomocnika 0.12.1.'}
& $compiler '/DVersion=0.12.1' "/DPayload=$baseline" "/DStorageTool=$tool" "/DFixture=$id" "/DFixtureLegacyId=$($id.Substring(0,12))" "/DFixtureInstallDir=$program" "/O$old" (Join-Path $old 'packaging/HexMapEditor.iss')
if($LASTEXITCODE -ne 0){throw 'Nie zbudowano testowego Setup 0.12.1.'}
& (Join-Path $repo 'packaging/Build-Packages.ps1') -AppExe $next -InnoCompiler $compiler -Output $new -Fixture $id -FixtureInstallDir $program
$oldSetup=Join-Path $old 'HexMapEditor-0.12.1-Setup.exe'
$newSetup=Join-Path $new 'HexMapEditor-0.12.2-Setup.exe'
Invoke-Setup $oldSetup '01-old' ('/FixtureLibraryMode=custom /FixtureLibraryPath="{0}" /MERGETASKS="desktop,!hexmap"' -f $library)
$oldLink=Join-Path $program 'FixtureDesktop/HexMapEditor (instalator).lnk'
$newLink=Join-Path $program 'FixtureDesktop/HexMapEditor.lnk'
Assert ((Test-Path -LiteralPath $oldLink) -and -not (Test-Path -LiteralPath $newLink)) 'Nieoczekiwany początkowy skrót.'
$data=Join-Path $program 'Data'
$map=Join-Path $data 'Hexmaps/Mapa z 0.12.1.hexmap'
$probe=Join-Path $runPath 'Probe.exe'
& (Join-Path $repo 'Build.ps1') -Output $probe -FixtureVersion '0.12.1' -TestSource (Join-Path $PSScriptRoot 'Patch0121Tests.cs') -EntryPoint HexMapEditor.Tests.Patch0121Tests
[IO.Directory]::CreateDirectory((Split-Path $map))|Out-Null
& $probe legacy $data $map
if($LASTEXITCODE -ne 0){throw 'Nie przygotowano mapy i MRU 0.12.1.'}
[IO.Directory]::CreateDirectory((Join-Path $library 'Terrains'))|Out-Null
Move-Item -LiteralPath (Join-Path $data 'Library/Terrains/Presets') -Destination (Join-Path $library 'Terrains/Presets')
$snapshot=Snapshot $program $library
$personal=Join-Path $runPath 'FixtureProfile/Desktop/HexMapEditor.lnk'
[IO.Directory]::CreateDirectory((Split-Path $personal))|Out-Null
$shell=New-Object -ComObject WScript.Shell
$mine=$shell.CreateShortcut($personal)
$mine.TargetPath=$baseline
$mine.Description='Ręczny skrót testowy'
$mine.Save()
$personalSha=(Get-FileHash -LiteralPath $personal).Hash
Invoke-Setup $newSetup '02-collision'
Assert ((Select-String -LiteralPath (Join-Path $runPath '02-collision.log') -SimpleMatch 'Pozostawiono zewnętrzny skrót' -Quiet) -and
    (Test-Path -LiteralPath $oldLink) -and -not (Test-Path -LiteralPath $newLink)) 'Brak bezpiecznej obsługi kolizji.'
Assert ((Get-FileHash -LiteralPath $personal).Hash -eq $personalSha) 'Naruszono cudzy skrót.'
Move-Item -LiteralPath $personal -Destination (Join-Path $runPath 'Odłożony ręczny skrót.lnk')
Invoke-Setup $newSetup '03-upgrade'
Assert ((Test-Path -LiteralPath $newLink) -and -not (Test-Path -LiteralPath $oldLink)) 'Nie przeniesiono własnego skrótu.'
Assert (([IO.File]::ReadAllText((Join-Path $program 'library-location.txt'))).Trim() -eq $library) 'Utracono wybór Library.'
Assert (-not (Compare-Object $snapshot (Snapshot $program $library))) 'Zmiana bajtów danych przy aktualizacji.'
Assert ((Get-FileHash -LiteralPath (Join-Path $program 'HexMapEditor.exe')).Hash -eq (Get-FileHash -LiteralPath $next).Hash) 'Inne bajty zainstalowanego EXE.'
Assert (Test-Path -LiteralPath (Join-Path $program 'LICENSE')) 'Instalator nie dostarczył licencji.'
$key="HKCU:\Software\HexMapEditor.InstallFixture\$id\Classes\.hexmap\OpenWithProgids"
Assert (-not (Test-Path -LiteralPath $key)) 'Aktualizacja zmieniła zadanie skojarzenia.'
Invoke-Setup $newSetup '04-disable-desktop' '/MERGETASKS="!desktop"'
Assert (-not (Test-Path -LiteralPath $newLink)) 'Wyłączenie zadania zostawiło własny skrót.'
Invoke-Setup $newSetup '05-enable-desktop' '/MERGETASKS="desktop"'
Assert (Test-Path -LiteralPath $newLink) 'Nie odtworzono własnego skrótu.'
Move-Item -LiteralPath (Join-Path $runPath 'Odłożony ręczny skrót.lnk') -Destination $personal
'upgrade PASS'
$runPath
