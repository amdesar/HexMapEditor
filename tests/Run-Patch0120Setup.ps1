param(
    [Parameter(Mandatory)][ValidateSet('prepare','upgrade','running','cleanup')][string]$Case,
    [string]$InnoCompiler,
    [string]$Run
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$work = (Join-Path $PSScriptRoot '_work').TrimEnd('\') + '\'
if ($Case -eq 'prepare') {
    if (-not $InnoCompiler) { throw 'Case prepare wymaga -InnoCompiler.' }
    $fixtureId = [Guid]::NewGuid().ToString('N')
    $Run = Join-Path $work "patch-0120-setup-$fixtureId"
    $install = Join-Path $Run 'Installed'
    foreach ($version in @('0.12.0','0.12.1')) {
        $files = Join-Path $Run $version
        [IO.Directory]::CreateDirectory($files) | Out-Null
        $exe = Join-Path $files 'HexMapEditor.exe'
        & (Join-Path $repo 'Build.ps1') -Output $exe -FixtureVersion $version
        & (Join-Path $repo 'packaging/Build-Packages.ps1') -AppExe $exe -InnoCompiler $InnoCompiler -Output $files -Fixture $fixtureId -FixtureInstallDir $install
    }
    $Run
    return
}

$resolved = (Resolve-Path -LiteralPath $Run).Path
$prefix = $work + 'patch-0120-setup-'
if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Case upgrade wymaga własnego katalogu patch-0120-setup w tests/_work.'
}
$fixtureId = $resolved.Substring($prefix.Length)
if ($fixtureId -notmatch '^[a-f0-9]{32}$') { throw 'Nieprawidłowa tożsamość fixture.' }
$install = Join-Path $resolved 'Installed'
$registry = "HKCU:\Software\HexMapEditor.InstallFixture\$fixtureId\Classes"
$assoc = "$registry\.hexmap\OpenWithProgids"
$member = 'amdesar.HexMapEditor.Hexmap'

function Invoke-FixtureSetup([string]$version, [string]$phase, [string]$tasks = '') {
    $setup = Join-Path $resolved "$version\HexMapEditor-$version-Setup.exe"
    $log = Join-Path $resolved "$phase.log"
    $p = Start-Process -FilePath $setup -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /SP- /NORESTART /LOG=`"$log`" $tasks" -WindowStyle Hidden -PassThru
    if (-not $p.WaitForExit(45000)) { throw "Instalator testowy PID $($p.Id) nie zakończył pracy. Log $log" }
    if ($p.ExitCode -ne 0) { throw "Instalator testowy zwrócił $($p.ExitCode). Log $log" }
    $exe = Join-Path $install 'HexMapEditor.exe'
    if ((Get-FileHash -LiteralPath $exe).Hash -ne (Get-FileHash -LiteralPath (Join-Path $resolved "$version\HexMapEditor.exe")).Hash) {
        throw 'Instalacja ma inne bajty niż kompilacja fixture.'
    }
    if ((Get-Item -LiteralPath $exe).VersionInfo.ProductVersion -ne "$version.0") { throw 'Instalator nie ustawił oczekiwanej wersji.' }
}
function Assert-Association([bool]$expected) {
    $key = Get-Item -LiteralPath $assoc
    if (($key.GetValueNames() -contains $member) -ne $expected -or ($key.GetValueNames() -notcontains 'Fixture.OtherEditor')) {
        throw 'Nieprawidłowy stan skojarzenia w kluczu testowym.'
    }
    if ((Get-Item -LiteralPath "$registry\.hexmap").GetValue('') -ne 'Fixture.OtherEditor') {
        throw 'Zmienił się cudzy domyślny ProgID w kluczu testowym.'
    }
    $commandKey = "$registry\$member\shell\open\command"
    if ((Test-Path -LiteralPath $commandKey) -ne $expected) { throw 'Nieprawidłowy wpis polecenia.' }
    if ($expected -and (Get-Item -LiteralPath $commandKey).GetValue('') -ne ('"' + (Join-Path $install 'HexMapEditor.exe') + '" "%1"')) {
        throw 'Polecenie skojarzenia nie wskazuje instalacji.'
    }
}
function Get-DataInventory([string]$directory) {
    @(Get-ChildItem -LiteralPath $directory -File -Recurse -Force | Sort-Object FullName | ForEach-Object {
        $_.FullName.Substring($directory.Length) + ' ' + (Get-FileHash -LiteralPath $_.FullName).Hash
    })
}

$profile = Join-Path $resolved 'FixtureProfile/amdesar/HexMapEditor'
if ($Case -eq 'running') {
    $exe = Join-Path $install 'HexMapEditor.exe'
    $editor = @(Get-Process -Name HexMapEditor -ErrorAction SilentlyContinue | Where-Object Path -EQ $exe)
    if ($editor.Count -ne 1) { throw 'Uruchom jedną instancję wyłącznie z katalogu Installed fixture.' }
    $original = (Get-FileHash -LiteralPath $exe).Hash
    $setup = Join-Path $resolved '0.12.1/HexMapEditor-0.12.1-Setup.exe'
    $log = Join-Path $resolved 'running.log'
    $p = Start-Process -FilePath $setup -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /SP- /NORESTART /LOG=`"$log`"" -WindowStyle Hidden -PassThru
    if (-not $p.WaitForExit(45000)) { throw "Instalator fixture PID $($p.Id) nadal działa. Log $log" }
    if ($p.ExitCode -eq 0 -or $editor[0].HasExited -or (Get-FileHash -LiteralPath $exe).Hash -ne $original -or
        -not (Select-String -LiteralPath $log -SimpleMatch 'HexMapEditor nadal działa' -Quiet)) {
        throw 'Setup nie odrzucił działającego edytora w oczekiwany sposób.'
    }
    'Fixture running PASS. Setup odmówił, edytor nadal działa, EXE identyczny.'
    return
}
if ($Case -eq 'cleanup') {
    $exe = Join-Path $install 'HexMapEditor.exe'
    $uninstaller = Join-Path $install 'unins000.exe'
    if (-not (Test-Path -LiteralPath $uninstaller -PathType Leaf) -or
        -not (Test-Path -LiteralPath (Join-Path $profile 'settings.json') -PathType Leaf)) {
        throw 'Brak kompletnej instalacji fixture do odinstalowania.'
    }
    if (Get-Process -Name HexMapEditor -ErrorAction SilentlyContinue | Where-Object Path -EQ $exe) {
        throw 'Okno fixture nadal działa.'
    }
    $snapshot = Get-DataInventory $profile
    Invoke-FixtureSetup '0.12.1' '05-repair' '/MERGETASKS="hexmap"'
    Assert-Association $true
    $log = Join-Path $resolved '06-uninstall.log'
    $p = Start-Process -FilePath $uninstaller -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /LOG=`"$log`"" -WindowStyle Hidden -PassThru
    if (-not $p.WaitForExit(45000)) { throw "Deinstalator fixture PID $($p.Id) nie zakończył pracy. Log $log" }
    if ($p.ExitCode -ne 0 -or (Test-Path -LiteralPath $exe)) { throw "Deinstalacja fixture nie powiodła się. Log $log" }
    Assert-Association $false
    if (Compare-Object $snapshot (Get-DataInventory $profile)) { throw 'Deinstalacja zmieniła pliki danych fixture.' }
    'Fixture cleanup PASS. Ponowny Setup i deinstalacja zachowały pliki danych i obcy ProgID.'
    # These keys were populated only by this fixture, outside Software\Classes.
    $fixtureKey = "Software\HexMapEditor.InstallFixture\$fixtureId"
    foreach ($leaf in @('Classes\.hexmap\OpenWithProgids','Classes\.hexmap','Classes','')) {
        $key = if ($leaf) { "$fixtureKey\$leaf" } else { $fixtureKey }
        [Microsoft.Win32.Registry]::CurrentUser.DeleteSubKey($key, $false)
    }
    return
}

if ((Test-Path -LiteralPath $install) -or (Test-Path -LiteralPath $registry)) { throw 'Fixture wymaga czystego katalogu i klucza.' }
New-Item -Path "$registry\.hexmap" -Force | Out-Null
Set-Item -LiteralPath "$registry\.hexmap" -Value 'Fixture.OtherEditor'
New-Item -Path $assoc -Force | Out-Null
New-ItemProperty -LiteralPath $assoc -Name 'Fixture.OtherEditor' -Value '' | Out-Null
Invoke-FixtureSetup '0.12.0' '01-default'
Assert-Association $true
if (-not (Test-Path -LiteralPath (Join-Path $install 'FixtureStart/HexMapEditor.lnk'))) { throw 'Brakuje skrótu testowego.' }
if (Test-Path -LiteralPath (Join-Path $resolved 'FixtureProfile')) { throw 'Setup zapisał dane przy instalacji.' }

$probe = Join-Path $install 'Patch0120Tests.exe'
& (Join-Path $repo 'Build.ps1') -Output $probe -FixtureVersion '0.12.0' -TestSource (Join-Path $PSScriptRoot 'Patch0120Tests.cs') -EntryPoint HexMapEditor.Tests.Patch0120Tests
& $probe paths
if ($LASTEXITCODE -ne 0) { throw 'Próba danych zainstalowanego EXE nie powiodła się.' }
if ((Test-Path -LiteralPath (Join-Path $install 'Library')) -or -not (Test-Path -LiteralPath (Join-Path $profile 'recent-projects.json'))) {
    throw 'Dane nie są odłączone od plików programu.'
}
[IO.File]::WriteAllText((Join-Path $profile 'settings.json'), '{"fixture":"keep"}')
[IO.File]::WriteAllText((Join-Path $profile 'Library/Own fixture.txt'), 'retain library data')
$snapshot = Get-DataInventory $profile
Invoke-FixtureSetup '0.12.1' '02-upgrade-on'
Assert-Association $true
Invoke-FixtureSetup '0.12.1' '03-toggle-off' '/MERGETASKS="!hexmap"'
Assert-Association $false
Invoke-FixtureSetup '0.12.1' '04-inherit-off'
Assert-Association $false
if (Compare-Object $snapshot (Get-DataInventory $profile)) { throw 'Upgrade zmienił dane użytkownika fixture.' }
'Fixture upgrade PASS. Rozdzielone dane, oba stany i dziedziczenie zadania, wersja 0.12.1.0.'
$resolved
