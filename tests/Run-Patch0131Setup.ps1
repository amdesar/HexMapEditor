param(
    [Parameter(Mandatory)][ValidateSet('prepare','verify')][string]$Case,
    [string]$InnoCompiler, [string]$Baseline, [string]$Map, [string]$Run
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
$work=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '_work')).Path.TrimEnd('\')+'\'
function Assert([bool]$ok,[string]$error){if(-not $ok){throw $error}}
function Snapshot($root){
    $program=Join-Path $root 'Program'
    $paths=@((Join-Path $program 'Data'),(Join-Path $root 'Separate Library'),(Join-Path $program 'FixtureDesktop'),
        (Join-Path $root 'FixtureProfile/Desktop'),(Join-Path $root 'Mapa.hexmap'))
    Get-ChildItem -LiteralPath $paths -Recurse -File | Sort-Object FullName | ForEach-Object {
        "$($_.FullName) $((Get-FileHash -LiteralPath $_.FullName).Hash)"
    }
}
if($Case -eq 'verify'){
    $area=(Resolve-Path -LiteralPath $Run).Path
    Assert ($area.StartsWith($work,[StringComparison]::OrdinalIgnoreCase) -and (Split-Path $area -Leaf) -match '^patch-0131-setup-[0-9a-f]{32}$') 'Wymagany katalog tej próby.'
    $program=Join-Path $area 'Program'
    $old=Get-Content -LiteralPath (Join-Path $area 'snapshot.txt')
    Assert (-not(Compare-Object $old (Snapshot $area))) 'Zmieniły się dane lub skróty.'
    Assert ((Get-Item -LiteralPath (Join-Path $program 'HexMapEditor.exe')).VersionInfo.FileVersion -eq '0.13.1.0') 'Brak zaktualizowanego EXE.'
    Assert ((Get-Content -LiteralPath (Join-Path $program 'deployment.mode') -Raw).Trim() -eq 'HexMapEditor-installed-user-v2') 'Zmienił się zakres.'
    Assert ((Get-Content -LiteralPath (Join-Path $program 'library-location.txt') -Raw).Trim() -eq (Join-Path $area 'Separate Library')) 'Zmieniła się Library.'
    'upgrade PASS'
    return
}

$compiler=(Resolve-Path -LiteralPath $InnoCompiler).Path
$oldExe=(Resolve-Path -LiteralPath $Baseline).Path
$fixtureMap=(Resolve-Path -LiteralPath $Map).Path
Assert ((Get-FileHash -LiteralPath $oldExe).Hash -eq 'C5EA79A658DC44EE2D2587D7EE026BEBC6C6662B67BE444EE70F94ADBEB39FCC') 'Wymagana baza EXE 0.13.0.'
Assert ($fixtureMap.StartsWith($work,[StringComparison]::OrdinalIgnoreCase)) 'Mapa musi być próbna.'
$id=[Guid]::NewGuid().ToString('N')
$area=Join-Path $work "patch-0131-setup-$id"
$archive=Join-Path $area 'Archive'
$program=Join-Path $area 'Program'
$library=Join-Path $area 'Separate Library'
$output=Join-Path $area 'Update'
$freshOutput=Join-Path $area 'Fresh'
foreach($part in @('packaging','assets/app')){[IO.Directory]::CreateDirectory((Join-Path $archive $part))|Out-Null}
Copy-Item -LiteralPath (Join-Path $repo 'assets/app/icon.ico') -Destination (Join-Path $archive 'assets/app/icon.ico')
Copy-Item -LiteralPath (Join-Path $repo 'LICENSE') -Destination $archive
foreach($name in @('HexMapEditor.iss','SetupWindows.iss','SetupStorageTool.cs','installed.mode','installed-user.mode')){
    $data=(git -C $repo show "96c1e9c:packaging/$name") -join "`r`n"
    if($LASTEXITCODE -ne 0){throw "Brak źródła 0.13.0 $name."}
    [IO.File]::WriteAllText((Join-Path $archive "packaging/$name"),$data.TrimStart([char]0xFEFF),[Text.UTF8Encoding]::new($true))
}
$helper=Join-Path $archive 'SetupStorageTool.exe'
& (Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe') /nologo /target:winexe "/out:$helper" (Join-Path $archive 'packaging/SetupStorageTool.cs')
Assert ($LASTEXITCODE -eq 0) 'Nie zbudowano pomocnika wersji bazowej.'
& $compiler '/DVersion=0.13.0' "/DPayload=$oldExe" "/DStorageTool=$helper" "/DFixture=$id" "/DFixtureLegacyId=$($id.Substring(0,12))" "/DFixtureInstallDir=$program" "/O$archive" (Join-Path $archive 'packaging/HexMapEditor.iss')
Assert ($LASTEXITCODE -eq 0) 'Nie zbudowano izolowanego Setup 0.13.0.'
$log=Join-Path $area 'baseline.log'
$baselineSetup=Join-Path $archive 'HexMapEditor-0.13.0-Setup.exe'
$p=Start-Process -FilePath $baselineSetup -ArgumentList (
    '/VERYSILENT /SUPPRESSMSGBOXES /SP- /NORESTART /LOG="{0}" /FixtureLibraryMode=custom /FixtureLibraryPath="{1}" /MERGETASKS="!hexmap,desktop"' -f $log,$library
) -WindowStyle Hidden -PassThru
Assert ($p.WaitForExit(30000) -and $p.ExitCode -eq 0) "Instalacja bazowa nie powiodła się. $log"

Copy-Item -LiteralPath $fixtureMap -Destination (Join-Path $area 'Mapa.hexmap')
$mru=ConvertTo-Json -InputObject @((Join-Path $area 'Mapa.hexmap')) -Compress
Set-Content -LiteralPath (Join-Path $program 'Data/recent-projects.json') -Value $mru -Encoding utf8NoBOM
Set-Content -LiteralPath (Join-Path $program 'Data/settings.json') -Value '{"Fixture":true}' -Encoding ascii
Set-Content -LiteralPath (Join-Path $library 'znacznik.txt') -Value 'Library własnej kopii' -Encoding utf8NoBOM
[IO.Directory]::CreateDirectory((Join-Path $area 'FixtureProfile/Desktop'))|Out-Null
Copy-Item -LiteralPath (Join-Path $program 'FixtureDesktop/HexMapEditor.lnk') -Destination (Join-Path $area 'FixtureProfile/Desktop/Ręczny skrót.lnk')
Snapshot $area | Set-Content -LiteralPath (Join-Path $area 'snapshot.txt') -Encoding utf8NoBOM

$candidate=Join-Path $area 'HexMapEditor.exe'
& (Join-Path $repo 'Build.ps1') -Output $candidate -FixtureVersion 0.13.1
& (Join-Path $repo 'packaging/Build-Packages.ps1') -AppExe $candidate -InnoCompiler $compiler -Output $output -Fixture $id -FixtureInstallDir $program
& (Join-Path $repo 'packaging/Build-Packages.ps1') -AppExe $candidate -InnoCompiler $compiler -Output $freshOutput -Fixture ([guid]::NewGuid().ToString('N')) -FixtureInstallDir (Join-Path $area 'Domyślne miejsce próby')
$oldFixture=Join-Path $program 'HexMapEditor.exe'
& (Join-Path $repo 'Build.ps1') -Output $oldFixture -FixtureVersion 0.13.0 -TestSource (Join-Path $PSScriptRoot 'Patch0131UpdateFixture.cs') -EntryPoint HexMapEditor.Tests.Patch0131UpdateFixture
"Fixture: $area"
