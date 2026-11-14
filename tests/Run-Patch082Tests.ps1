param([string]$Case)
$ErrorActionPreference='Stop'
if($Case -notin @('geometry','caption')){throw 'Wymagany pojedynczy Case: geometry albo caption. Niczego nie uruchomiono.'}
$run=Join-Path $PSScriptRoot ('_work/patch-082-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
$exe=Join-Path $run 'Patch082Tests.exe'
& (Join-Path (Split-Path $PSScriptRoot) 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch082Tests.cs') -EntryPoint HexMapEditor.Tests.Patch082Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
