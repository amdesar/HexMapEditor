param([Parameter(Mandatory=$true)][ValidateSet('preset','fixture')][string]$Case)
$ErrorActionPreference='Stop'
$run=Join-Path $PSScriptRoot ('_work/patch-093-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
$exe=Join-Path $run 'Patch093Tests.exe'
& (Join-Path (Split-Path $PSScriptRoot) 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch093Tests.cs') -EntryPoint HexMapEditor.Tests.Patch093Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
