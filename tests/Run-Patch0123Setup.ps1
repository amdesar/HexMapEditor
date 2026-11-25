param(
    [Parameter(Mandatory)][ValidateSet('setup','remove')][string]$Case,
    [string]$InnoCompiler, [string]$Candidate, [string]$Baseline, [string]$Run
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
$work=(Join-Path $PSScriptRoot '_work').TrimEnd('\')+'\'
function Assert([bool]$ok,[string]$error){if(-not $ok){throw $error}}
function RunSetup([string]$exe,[string]$step,[string]$options='', [string]$refusal=''){
    $log=Join-Path $area "$step.log"
    $p=Start-Process -FilePath $exe -ArgumentList ('/VERYSILENT /SUPPRESSMSGBOXES /SP- /NORESTART /LOG="{0}" {1}' -f $log,$options) -WindowStyle Hidden -PassThru
    Assert ($p.WaitForExit(45000)) "Instalator próbny nadal działa ($($p.Id))."
    if($refusal){
        Assert ($p.ExitCode -ne 0 -and (Select-String -LiteralPath $log -SimpleMatch $refusal -Quiet)) "Brak oczekiwanej odmowy w $log."
    }else{Assert ($p.ExitCode -eq 0) "Nieudany krok $step. Log $log."}
}
function Snapshot($paths){
    Get-ChildItem -LiteralPath $paths -Recurse -File | Sort-Object FullName | ForEach-Object {
        "$($_.FullName) $((Get-FileHash -LiteralPath $_.FullName).Hash)"
    }
}
if($Case -eq 'remove'){
    $area=(Resolve-Path -LiteralPath $Run).Path
    Assert ($area.StartsWith($work,[StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path $area -Leaf) -match '^patch-0123-setup-[0-9a-f]{32}$') 'Wymagany własny katalog 0.12.3.'
    $fresh=Join-Path $area 'Własny program'
    $updated=Join-Path $area 'Ścieżka aktualizowana'
    $library=Join-Path $area 'Osobna Library'
    $personalLink=Join-Path $area 'FixtureProfile/Desktop/HexMapEditor.lnk'
    $preserve=@((Join-Path $fresh 'Data'),(Join-Path $updated 'Data'),(Join-Path $updated 'Library'),$library,$personalLink)
    $snap=Snapshot $preserve
    RunSetup (Join-Path $fresh 'unins000.exe') '09-remove-current-user'
    RunSetup (Join-Path $updated 'unins000.exe') '10-remove-after-upgrade'
    Assert (-not(Compare-Object $snap (Snapshot $preserve))) 'Zmiana danych lub ręcznego skrótu przy usuwaniu.'
    foreach($program in @($fresh,$updated)){
        Assert (-not(Test-Path -LiteralPath (Join-Path $program 'FixtureDesktop/HexMapEditor.lnk'))) 'Pozostał własny skrót testowy.'
    }
    'remove PASS'
    return
}
Assert ($InnoCompiler -and $Candidate -and $Baseline) 'Podaj kompilator, kandydata i bazowy EXE.'
$compiler=(Resolve-Path -LiteralPath $InnoCompiler).Path
$candidateExe=(Resolve-Path -LiteralPath $Candidate).Path
$baseExe=(Resolve-Path -LiteralPath $Baseline).Path
Assert ((Get-FileHash -LiteralPath $baseExe).Hash -eq '472A6C78F2787B3615DA735089886DEBD1FAE8CB3584F8B81986259B33785D46') 'Oczekiwany EXE 0.12.2 z d0ecf2b.'
$id=[Guid]::NewGuid().ToString('N')
$area=Join-Path $work "patch-0123-setup-$id"
$archive=Join-Path $area 'Archive'
$fresh=Join-Path $area 'Własny program'
$updated=Join-Path $area 'Ścieżka aktualizowana'
$library=Join-Path $area 'Osobna Library'
foreach($part in @('packaging','assets/app')){[IO.Directory]::CreateDirectory((Join-Path $archive $part))|Out-Null}
[IO.File]::Copy((Join-Path $repo 'assets/app/icon.ico'),(Join-Path $archive 'assets/app/icon.ico'))
[IO.File]::Copy((Join-Path $repo 'LICENSE'),(Join-Path $archive 'LICENSE'))
foreach($name in @('HexMapEditor.iss','SetupWindows.iss','SetupStorageTool.cs','installed.mode')){
    $data=(git -C $repo show "d0ecf2b:packaging/$name") -join "`r`n"
    if($LASTEXITCODE -ne 0){throw "Brak źródła 0.12.2 $name."}
    if($name -eq 'HexMapEditor.iss'){
        # Activate the original probe on demand in the isolated Setup. Real
        # 0.12.2 always calls it for per-machine installation.
        $data=$data.Replace("  Result := Result + ' ' + Arg('fixture');",
            "  Result := Result + ' ' + Arg(ExpandConstant('{param:FixtureProtection|fixture}'));")
    }
    [IO.File]::WriteAllText((Join-Path $archive "packaging/$name"),$data.TrimStart([char]0xFEFF),[Text.UTF8Encoding]::new($true))
}
$helper=Join-Path $archive 'SetupStorageTool.exe'
& (Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe') /nologo /target:winexe "/out:$helper" (Join-Path $archive 'packaging/SetupStorageTool.cs')
Assert ($LASTEXITCODE -eq 0) 'Nie zbudowano pomocnika źródłowego.'
$common=@("/DFixture=$id","/DFixtureLegacyId=$($id.Substring(0,12))","/DFixtureInstallDir=$fresh")
& $compiler '/DVersion=0.12.2' "/DPayload=$baseExe" "/DStorageTool=$helper" @common "/O$archive" (Join-Path $archive 'packaging/HexMapEditor.iss')
Assert ($LASTEXITCODE -eq 0) 'Nie zbudowano izolowanego Setup 0.12.2.'
$output=Join-Path $area 'Next'
& (Join-Path $repo 'packaging/Build-Packages.ps1') -AppExe $candidateExe -InnoCompiler $compiler -Output $output -Fixture $id -FixtureInstallDir $fresh
$freshOutput=Join-Path $area 'Fresh'
& (Join-Path $repo 'packaging/Build-Packages.ps1') -AppExe $candidateExe -InnoCompiler $compiler -Output $freshOutput -Fixture ([Guid]::NewGuid().ToString('N')) -FixtureInstallDir (Join-Path $area 'Domyślny katalog testowy')
$oldSetup=Join-Path $archive 'HexMapEditor-0.12.2-Setup.exe'
$setup=Join-Path $output 'HexMapEditor-0.12.3-Setup.exe'
$reject=Join-Path $area 'Próba odmowy'
RunSetup $oldSetup '01-original-refusal' ('/FixtureProtection=protected /DIR="{0}" /MERGETASKS="!hexmap"' -f $reject) 'Wybierz Program Files lub inny chroniony katalog'
Assert (-not(Test-Path -LiteralPath (Join-Path $reject 'HexMapEditor.exe'))) '0.12.2 nie powinna zapisać programu.'
$untrusted=Join-Path $area 'Nieufny cel'
RunSetup $setup '02-elevated-protection' ('/FixtureMachineProbe=1 /DIR="{0}"' -f $untrusted) 'Instalacja dla wszystkich wymaga chronionego katalogu'
Assert (-not(Test-Path -LiteralPath $untrusted)) 'Sprawdzenie maszynowe utworzyło katalog przed decyzją.'
$collision=Join-Path $area 'Kolizja'
[IO.Directory]::CreateDirectory($collision)|Out-Null
[IO.File]::WriteAllText((Join-Path $collision 'LICENSE'),'obcy plik')
RunSetup $setup '03-reserved-file' ('/DIR="{0}"' -f $collision) 'pliki kolidujące z instalacją'
Assert ([IO.File]::ReadAllText((Join-Path $collision 'LICENSE')) -eq 'obcy plik') 'Podmieniono obcy plik.'
RunSetup (Join-Path $freshOutput 'HexMapEditor-0.12.3-Setup.exe') '04-fresh-custom' (
    '/DIR="{0}" /FixtureLibraryMode=custom /FixtureLibraryPath="{1}" /MERGETASKS="!hexmap,desktop"' -f $fresh,$library)
Assert (([IO.File]::ReadAllText((Join-Path $fresh 'deployment.mode'))).Trim() -eq 'HexMapEditor-installed-user-v2') 'Nie zapisano trybu bieżącego użytkownika.'
Assert (([IO.File]::ReadAllText((Join-Path $fresh 'library-location.txt'))).Trim() -eq $library) 'Utracono oddzielną Library.'
$oldLibrary=Join-Path $updated 'Library'
RunSetup $oldSetup '06-old-installed' ('/DIR="{0}" /MERGETASKS="!hexmap,desktop"' -f $updated)
$oldData=Join-Path $updated 'Data'
[IO.File]::WriteAllText((Join-Path $oldData 'settings.json'),'{"Example":true}')
[IO.File]::WriteAllText((Join-Path $oldData 'recent-projects.json'),'[]')
[IO.File]::WriteAllText((Join-Path $oldLibrary 'znacznik.txt'),'zachowaj')
$probe=Join-Path $area 'MapFixture.exe'
& (Join-Path $repo 'Build.ps1') -Output $probe -FixtureVersion '0.12.2' -TestSource (Join-Path $PSScriptRoot 'Patch0121Tests.cs') -EntryPoint HexMapEditor.Tests.Patch0121Tests
& $probe legacy $oldData (Join-Path $updated 'Data/Mapa próbna 0.12.2.hexmap')
Assert ($LASTEXITCODE -eq 0) 'Nie przygotowano starej mapy i MRU.'
$personalLink=Join-Path $area 'FixtureProfile/Desktop/HexMapEditor.lnk'
[IO.Directory]::CreateDirectory((Split-Path $personalLink))|Out-Null
$shell=New-Object -ComObject WScript.Shell
$link=$shell.CreateShortcut($personalLink)
$link.TargetPath=$baseExe
$link.Save()
$ownedLink=Join-Path $updated 'FixtureDesktop/HexMapEditor.lnk'
$before=Snapshot @($oldData,$oldLibrary,$personalLink,$ownedLink)
RunSetup $setup '07-upgrade'
Assert ((Get-FileHash -LiteralPath (Join-Path $updated 'HexMapEditor.exe')).Hash -eq (Get-FileHash -LiteralPath $candidateExe).Hash) 'Inne bajty EXE po upgrade.'
Assert (-not(Compare-Object $before (Snapshot @($oldData,$oldLibrary,$personalLink,$ownedLink)))) 'Upgrade naruszył dane lub skróty.'
Assert (([IO.File]::ReadAllText((Join-Path $updated 'library-location.txt'))).Trim() -eq $oldLibrary) 'Utracono Library poprzedniej instalacji.'
'setup PASS'
$area
