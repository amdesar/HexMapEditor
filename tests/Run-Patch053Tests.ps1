param([string]$Case)
$ErrorActionPreference='Stop'
if($Case -notin @('regions','boundaries','updates','local','safety','perf')) {throw 'Wymagany Case: regions|boundaries|updates|local|safety|perf. Brak uruchomienia domyślnego.'}
$run=Join-Path $PSScriptRoot ('_work/patch-053-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
$exe=Join-Path $run 'Patch053Tests.exe'
& (Join-Path (Split-Path $PSScriptRoot) 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch053Tests.cs') -EntryPoint HexMapEditor.Tests.Patch053Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
