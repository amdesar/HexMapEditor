param([string]$Case)
$ErrorActionPreference='Stop'
if($Case -notin @('groups','fixture','outside','fixture-outside','backgrounds','size-pointer','presentation','panel-profile','acceptance','background-controls','panel-layout','pointer-focus','background-overwrite')){throw 'Wymagany pojedynczy Case: groups, outside, backgrounds, size-pointer, presentation, panel-profile, acceptance, background-controls, panel-layout, pointer-focus, background-overwrite lub jawne fixture/fixture-outside. Nie uruchomiono testów.'}
$run=Join-Path $PSScriptRoot ('_work/patch-061-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
$exe=Join-Path $run 'Patch061Tests.exe'
& (Join-Path (Split-Path $PSScriptRoot) 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch061Tests.cs') -EntryPoint HexMapEditor.Tests.Patch061Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
