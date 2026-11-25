param(
    [Parameter(Mandatory)][ValidateSet('channel','paths','close','personal-update')][string]$Case,
    [string]$Setup
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$run=Join-Path $PSScriptRoot ('_work/patch-0120-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
$argsForTest=@($Case)
if($Case -eq 'channel'){
    if(-not $Setup){throw 'Case channel wymaga parametru -Setup.'}
    $argsForTest+=(Resolve-Path -LiteralPath $Setup).Path
}
[IO.Directory]::CreateDirectory($run)|Out-Null
$exe=Join-Path $run 'Patch0120Tests.exe'
& (Join-Path $root 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch0120Tests.cs') -EntryPoint HexMapEditor.Tests.Patch0120Tests
& $exe @argsForTest | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
