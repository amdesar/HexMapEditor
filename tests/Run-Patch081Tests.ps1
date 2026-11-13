param([string]$Case)
$ErrorActionPreference='Stop'
if($Case -notin @('fields','stamp','controls')){throw 'Wymagany pojedynczy Case: fields, stamp albo controls. Niczego nie uruchomiono.'}
$run=Join-Path $PSScriptRoot ('_work/patch-081-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
$exe=Join-Path $run 'Patch081Tests.exe'
& (Join-Path (Split-Path $PSScriptRoot) 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch081Tests.cs') -EntryPoint HexMapEditor.Tests.Patch081Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
