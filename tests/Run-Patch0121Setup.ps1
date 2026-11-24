param(
    [Parameter(Mandatory)][ValidateSet('paths','prepare','scenario','cleanup')][string]$Case,
    [string]$InnoCompiler, [string]$BaselineExe, [string]$Run
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$work = (Join-Path $PSScriptRoot '_work').TrimEnd('\') + '\'
$testSource = Join-Path $PSScriptRoot 'Patch0121Tests.cs'
$build = Join-Path $repo 'Build.ps1'
function Build-Probe([string]$target, [string]$version) {
    & $build -Output $target -FixtureVersion $version -TestSource $testSource -EntryPoint HexMapEditor.Tests.Patch0121Tests
}
function Run-Probe([string]$binary, [string[]]$options) {
    & $binary @options
    if ($LASTEXITCODE -ne 0) { throw 'Próba modelu danych nie przeszła.' }
}
if ($Case -eq 'paths') {
    $target = Join-Path $work ('patch-0121-paths-' + [Guid]::NewGuid().ToString('N') + '/Probe.exe')
    Build-Probe $target '0.12.1'
    Run-Probe $target @('paths')
    return
}
if ($Case -eq 'prepare') {
    if (-not $InnoCompiler -or -not $BaselineExe) { throw 'Case prepare wymaga -InnoCompiler i -BaselineExe.' }
    $compiler = (Resolve-Path -LiteralPath $InnoCompiler).Path
    $baseline = (Resolve-Path -LiteralPath $BaselineExe).Path
    if ((Get-FileHash -LiteralPath $baseline).Hash -ne '905D2275289B8C1908A98EDD5071A26CD4ABB1DA84ABBD8A3EB4A296DB2473DF') {
        throw 'Przekazano inne bajty EXE niż zweryfikowana podstawa 0.12.0.'
    }
    $id = [Guid]::NewGuid().ToString('N')
    $runPath = Join-Path $work "patch-0121-setup-$id"
    $old = Join-Path $runPath 'LegacySource'
    $oldScript = Join-Path $old 'packaging/HexMapEditor.iss'
    $new = Join-Path $runPath 'New'
    $newExe = Join-Path $new 'HexMapEditor.exe'
    $legacyInstall = Join-Path $runPath 'Old program'
    $defaultInstall = Join-Path $runPath 'Default program'
    [IO.Directory]::CreateDirectory((Split-Path -Parent $oldScript)) | Out-Null
    [IO.Directory]::CreateDirectory((Join-Path $old 'assets/app')) | Out-Null
    [IO.File]::Copy((Join-Path $repo 'assets/app/icon.ico'),(Join-Path $old 'assets/app/icon.ico'))
    foreach ($file in @('HexMapEditor.iss','installed.mode')) {
        $source = (git -C $repo show "2061ea2:packaging/$file") -join [Environment]::NewLine
        if ($LASTEXITCODE -ne 0) { throw "Nie można odczytać bazowego skryptu $file." }
        [IO.File]::WriteAllText((Join-Path $old "packaging/$file"),$source.TrimStart([char]0xFEFF),[Text.UTF8Encoding]::new($true))
    }
    $legacyId = $id.Substring(0,12)
    & $compiler '/DVersion=0.12.0' "/DPayload=$baseline" "/DFixture=$legacyId" "/DFixtureInstallDir=$legacyInstall" "/O$old" $oldScript
    if ($LASTEXITCODE -ne 0) { throw 'Nie udał się build bazowego Setup 0.12.0.' }
    & $build -Output $newExe -FixtureVersion '0.12.1'
    & (Join-Path $repo 'packaging/Build-Packages.ps1') -AppExe $newExe -InnoCompiler $compiler -Output $new -Fixture $id -FixtureInstallDir $defaultInstall
    Build-Probe (Join-Path $runPath 'LegacyProbe.exe') '0.12.0'
    $runPath
    return
}

$root = (Resolve-Path -LiteralPath $Run).Path
$prefix = $work + 'patch-0121-setup-'
if (-not $root.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)) { throw 'Nie jest to katalog własnych prób 0.12.1.' }
$id = $root.Substring($prefix.Length)
if ($id -notmatch '^[a-f0-9]{32}$') { throw 'Identyfikator próby jest nieprawidłowy.' }
$setup = Join-Path $root 'New/HexMapEditor-0.12.1-Setup.exe'
$installed = Join-Path $root 'Custom program Ą'
$data = Join-Path $installed 'Data'
$library = Join-Path $root 'Moja Library Ę'
$docs = Join-Path $root 'FixtureProfile/Przekierowane Dokumenty/HexMapEditor/Library'
$legacyRoot = Join-Path $root 'FixtureProfile/amdesar/HexMapEditor'
$map = Join-Path $root 'Mapa poza instalacją.hexmap'
$desk = Join-Path $installed 'FixtureDesktop/HexMapEditor (instalator).lnk'
$classes = "HKCU:\Software\HexMapEditor.InstallFixture\$id\Classes"
$openWith = "$classes\.hexmap\OpenWithProgids"

function Run-Inno([string]$binary,[string]$step,[string]$options,[string]$failure = '') {
    $log = Join-Path $root "$step.log"
    $p = Start-Process -FilePath $binary -ArgumentList ('/VERYSILENT /SUPPRESSMSGBOXES /SP- /NORESTART /LOG="{0}" {1}' -f $log,$options) -WindowStyle Hidden -PassThru
    if (-not $p.WaitForExit(45000)) { throw "Proces fixture $($p.Id) nie zakończył pracy. Log $log" }
    if ($failure) {
        if ($p.ExitCode -eq 0 -or -not (Select-String -LiteralPath $log -SimpleMatch $failure -Quiet)) {
            throw "Brak oczekiwanej odmowy: $failure. Kod $($p.ExitCode). Log $log"
        }
    } elseif ($p.ExitCode -ne 0) { throw "Setup fixture zwrócił $($p.ExitCode). Log $log" }
}
function Assert-Association([bool]$expected) {
    $key = Get-Item -LiteralPath $openWith
    if (($key.GetValueNames() -contains 'HexMapEditor.Hexmap') -ne $expected -or
        $key.GetValueNames() -notcontains 'Fixture.OtherEditor' -or
        (Get-Item -LiteralPath "$classes\.hexmap").GetValue('') -ne 'Fixture.OtherEditor') {
        throw 'Nieprawidłowy stan naszego lub obcego skojarzenia testowego.'
    }
}
function Inventory([string]$base) {
    @(Get-ChildItem -LiteralPath $base -Recurse -File -Force | Sort-Object FullName | ForEach-Object {
        $_.FullName.Substring($base.Length) + ' ' + (Get-FileHash -LiteralPath $_.FullName).Hash
    })
}
function Assert-Library([string]$expected) {
    if ([IO.File]::ReadAllText((Join-Path $installed 'library-location.txt')).Trim() -ne $expected) { throw 'Niezgodna lokalizacja Library.' }
}
if ($Case -eq 'cleanup') {
    $uninstaller = Join-Path $installed 'unins000.exe'
    if (-not (Test-Path -LiteralPath $uninstaller -PathType Leaf) -or -not (Test-Path -LiteralPath $desk -PathType Leaf)) {
        throw 'Nie można rozpoznać końcowej instalacji testowej.'
    }
    $snapshot = @(Inventory $data) + @(Inventory $library) + @(Inventory $legacyRoot) +
        @(Inventory $docs) + @(Inventory (Join-Path $installed 'Library'))
    $foreign = (Get-FileHash -LiteralPath $desk).Hash
    $originalMap = (Get-FileHash -LiteralPath $map).Hash
    Run-Inno $uninstaller '12-uninstall' ''
    Assert-Association $false
    if ((Get-FileHash -LiteralPath $desk).Hash -ne $foreign -or
        (Get-FileHash -LiteralPath $map).Hash -ne $originalMap -or
        (Compare-Object $snapshot (@(Inventory $data) + @(Inventory $library) + @(Inventory $legacyRoot) +
            @(Inventory $docs) + @(Inventory (Join-Path $installed 'Library'))))) {
        throw 'Deinstalacja naruszyła dane, mapę lub cudzy skrót.'
    }
    'cleanup PASS'
    return
}
if ((Test-Path -LiteralPath $classes) -or (Test-Path -LiteralPath $installed)) { throw 'Próba wymaga czystego fixture.' }
Run-Inno $setup '01-runtime' '/FixtureMissingRuntime=1' 'Official page: https://dotnet.microsoft.com/download/dotnet-framework/net48'
$oldSetup = Join-Path $root 'LegacySource/HexMapEditor-0.12.0-Setup.exe'
$oldInstall = Join-Path $root 'Old program'
Run-Inno $oldSetup '02-legacy' '/MERGETASKS="!hexmap"'
[IO.Directory]::CreateDirectory($legacyRoot) | Out-Null
Run-Probe (Join-Path $root 'LegacyProbe.exe') @('legacy',$legacyRoot,$map)
$original = Inventory $legacyRoot
$unchangedMap = (Get-FileHash -LiteralPath $map).Hash
Run-Inno $setup '03-detect-legacy' '' $oldInstall
if (Test-Path -LiteralPath (Join-Path $root 'Default program/HexMapEditor.exe')) { throw 'Instalator utworzył drugą kopię.' }
Run-Inno (Join-Path $oldInstall 'unins000.exe') '04-uninstall-legacy' ''
if (Compare-Object $original (Inventory $legacyRoot)) { throw 'Odinstalowanie starszej kopii zmieniło dane.' }

New-Item -Path "$classes\.hexmap" -Force | Out-Null
Set-Item -LiteralPath "$classes\.hexmap" -Value 'Fixture.OtherEditor'
New-Item -Path $openWith -Force | Out-Null
New-ItemProperty -LiteralPath $openWith -Name 'Fixture.OtherEditor' -Value '' | Out-Null
$programArg = '/DIR="' + $installed + '"'
$switchLibrary = '/FixtureLibraryMode=custom /FixtureLibraryPath="' + $library + '"'
$occupied = [IO.Directory]::CreateDirectory((Join-Path $root 'Occupied Library')).FullName
[IO.File]::WriteAllText((Join-Path $occupied 'Own.txt'),'keep')
Run-Inno $setup '05-collision' ('/FixtureCopyLegacy=1 /FixtureLibraryMode=custom /FixtureLibraryPath="' + $occupied + '" ' + $programArg) 'Docelowa Library zawiera już dane'
if (@(Get-ChildItem -LiteralPath $occupied -Force).Count -ne 1) { throw 'Kolizja zmieniła zawartość celu.' }
Run-Inno $setup '06-copy' ("/FixtureCopyLegacy=1 $switchLibrary $programArg")
Assert-Library $library
Assert-Association $true
if (Test-Path -LiteralPath $desk) { throw 'Pulpit zaznaczony przy świeżej instalacji.' }
if ((Compare-Object $original (Inventory $legacyRoot)) -or (Get-FileHash -LiteralPath $map).Hash -ne $unchangedMap) { throw 'Zmiana oryginału po migracji.' }

$probe = Join-Path $installed 'Probe.exe'
Build-Probe $probe '0.12.1'
Run-Probe $probe @('installed',$map)
Run-Inno $setup '07-repeat-tasks' '/MERGETASKS="desktop,!hexmap"'
Assert-Library $library
Assert-Association $false
if (-not (Test-Path -LiteralPath $desk)) { throw 'Brak własnego skrótu po zaznaczeniu.' }
Run-Inno $setup '08-documents' '/FixtureLibraryMode=documents'
Assert-Library $docs
[IO.File]::WriteAllText((Join-Path $docs 'Dane z Dokumentów.txt'),'retain')
Assert-Association $false
if (-not (Test-Path -LiteralPath $desk)) { throw 'Ponowny Setup utracił zadanie desktop.' }
Run-Inno $setup '09-overlap' ('/FixtureLibraryMode=custom /FixtureLibraryPath="' + $data + '"') 'Wybierz odrębny katalog Library'
Assert-Library $docs
Run-Inno $setup '10-adjacent' '/FixtureLibraryMode=adjacent /MERGETASKS="!desktop,hexmap"'
Assert-Library (Join-Path $installed 'Library')
[IO.File]::WriteAllText((Join-Path $installed 'Library/Dane obok EXE.txt'),'retain')
Assert-Association $true
if (Test-Path -LiteralPath $desk) { throw 'Wyłączenie zadania nie usunęło własnego skrótu.' }
Run-Inno $setup '11-reopen-custom' ($switchLibrary + ' /MERGETASKS="desktop"')
Assert-Library $library
$shortcut = (New-Object -ComObject WScript.Shell).CreateShortcut($desk)
$shortcut.Description = 'Skrót ręczny fixture'
$shortcut.Save()
Run-Inno $setup '11b-foreign-shortcut' '' 'nie należy do Setup'
if ((Get-FileHash -LiteralPath (Join-Path $installed 'HexMapEditor.exe')).Hash -ne (Get-FileHash -LiteralPath (Join-Path $root 'New/HexMapEditor.exe')).Hash) {
    throw 'Zainstalowany EXE ma inne bajty.'
}
'scenario PASS'
$root
