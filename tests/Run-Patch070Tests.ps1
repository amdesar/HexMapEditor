param([string]$Case)
$ErrorActionPreference='Stop'
if($Case -notin @('eraser-profile','eraser-async','camera','quality','picker','start','integration','fixture')){throw 'Wymagany pojedynczy Case: eraser-profile, eraser-async, camera, quality, picker, start, integration lub fixture. Nie uruchomiono testów.'}
$run=Join-Path $PSScriptRoot ('_work/patch-070-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
$exe=Join-Path $run 'Patch070Tests.exe'
& (Join-Path (Split-Path $PSScriptRoot) 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch070Tests.cs') -EntryPoint HexMapEditor.Tests.Patch070Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
