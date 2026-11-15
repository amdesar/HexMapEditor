param([string]$Case)
$ErrorActionPreference='Stop'
if($Case -notin @('libraries','bucket','crop','status','fixture')){throw 'Wymagany pojedynczy jawny Case: libraries, bucket, crop, status albo fixture.'}
$run=Join-Path $PSScriptRoot ('_work/patch-092-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
$exe=Join-Path $run 'Patch092Tests.exe'
& (Join-Path (Split-Path $PSScriptRoot) 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch092Tests.cs') -EntryPoint HexMapEditor.Tests.Patch092Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run