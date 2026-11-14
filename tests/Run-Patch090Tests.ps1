param([string]$Case)
$ErrorActionPreference='Stop'
if($Case -notin @('geometry','quick','presets','controls')){throw 'Wymagany pojedynczy Case: geometry, quick, presets albo controls. Niczego nie uruchomiono.'}
$run=Join-Path $PSScriptRoot ('_work/patch-090-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
$exe=Join-Path $run 'Patch090Tests.exe'
& (Join-Path (Split-Path $PSScriptRoot) 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch090Tests.cs') -EntryPoint HexMapEditor.Tests.Patch090Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
