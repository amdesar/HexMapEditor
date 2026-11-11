param([string]$Case)
$ErrorActionPreference='Stop'
if($Case -notin @('empty','editor','crop','presentation','background-perf')) {throw 'Wymagany pojedynczy Case: empty|editor|crop|presentation|background-perf. Nie uruchomiono testów.'}
$run=Join-Path $PSScriptRoot ('_work/patch-054-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
$exe=Join-Path $run 'Patch054Tests.exe'
& (Join-Path (Split-Path $PSScriptRoot) 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch054Tests.cs') -EntryPoint HexMapEditor.Tests.Patch054Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
