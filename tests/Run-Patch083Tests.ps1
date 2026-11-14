param([string]$Case)
$ErrorActionPreference='Stop'
if($Case -ne 'controls'){throw 'Wymagany pojedynczy Case: controls. Niczego nie uruchomiono.'}
$run=Join-Path $PSScriptRoot ('_work/patch-083-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
$exe=Join-Path $run 'Patch083Tests.exe'
& (Join-Path (Split-Path $PSScriptRoot) 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch083Tests.cs') -EntryPoint HexMapEditor.Tests.Patch083Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
