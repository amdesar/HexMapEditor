param([string]$Case)
$ErrorActionPreference='Stop'
if($Case -notin @('eraser','comet','panel','background-ui','fixture')){throw 'Wymagany pojedynczy Case: eraser, comet, panel, background-ui lub fixture. Nie uruchomiono testów.'}
$run=Join-Path $PSScriptRoot ('_work/patch-062-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
$exe=Join-Path $run 'Patch062Tests.exe'
& (Join-Path (Split-Path $PSScriptRoot) 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch062Tests.cs') -EntryPoint HexMapEditor.Tests.Patch062Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
