param([string]$Case)
$ErrorActionPreference='Stop'
if($Case -notin @('baseline','lifecycle','argument','new-file','ink','ink-wait','zoom','fixture')){throw 'Wymagany jeden jawny Case: baseline, lifecycle, argument, new-file, ink, ink-wait, zoom lub fixture. Niczego nie uruchomiono.'}
$run=Join-Path $PSScriptRoot ('_work/patch-071-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
$exe=Join-Path $run 'Patch071Tests.exe'
& (Join-Path (Split-Path $PSScriptRoot) 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch071Tests.cs') -EntryPoint HexMapEditor.Tests.Patch071Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
