param([string]$Case)
$ErrorActionPreference='Stop'
if($Case -notin @('numbers','commands','rename')){throw 'Wymagany pojedynczy Case: numbers, commands albo rename. Niczego nie uruchomiono.'}
$run=Join-Path $PSScriptRoot ('_work/patch-080-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
$exe=Join-Path $run 'Patch080Tests.exe'
& (Join-Path (Split-Path $PSScriptRoot) 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch080Tests.cs') -EntryPoint HexMapEditor.Tests.Patch080Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
