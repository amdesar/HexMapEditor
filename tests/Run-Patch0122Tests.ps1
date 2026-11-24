param([Parameter(Mandatory)][ValidateSet('controls','seed')][string]$Case,[string]$Program)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
$work=(Join-Path $PSScriptRoot '_work').TrimEnd('\')+'\'
$run=Join-Path $work ('patch-0122-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
$exe=Join-Path $run 'Probe.exe'
$arguments=@($Case)
if($Case -eq 'seed'){
    $resolved=(Resolve-Path -LiteralPath $Program).Path
    if(-not $resolved.StartsWith($work,[StringComparison]::OrdinalIgnoreCase) -or
        $resolved.Substring($work.Length) -notmatch '^patch-0122-upgrade-[a-f0-9]{32}\\Program$'){
        throw 'Tworzenie danych wymaga własnej izolowanej instalacji 0.12.2.'
    }
    $arguments+=$resolved
}
& (Join-Path $repo 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch0122Tests.cs') -EntryPoint HexMapEditor.Tests.Patch0122Tests
& $exe @arguments | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
