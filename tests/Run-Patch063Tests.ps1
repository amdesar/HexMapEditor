param([string]$Case)
$ErrorActionPreference='Stop'
if($Case -notin @('ink-profile','gradient','ink-gradient','fills','ui','fixture')){throw 'Wymagany pojedynczy Case: ink-profile, gradient, ink-gradient, fills, ui lub fixture. Nie uruchomiono testów.'}
$run=Join-Path $PSScriptRoot ('_work/patch-063-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
$exe=Join-Path $run 'Patch063Tests.exe'
& (Join-Path (Split-Path $PSScriptRoot) 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch063Tests.cs') -EntryPoint HexMapEditor.Tests.Patch063Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
