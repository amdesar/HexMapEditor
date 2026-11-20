param([Parameter(Mandatory=$true)][ValidateSet('panel','groups','notes','fixture')][string]$Case)
$ErrorActionPreference='Stop'
$run=Join-Path $PSScriptRoot ('_work/patch-0100-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
$exe=Join-Path $run 'Patch0100Tests.exe'
& (Join-Path (Split-Path $PSScriptRoot) 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch0100Tests.cs') -EntryPoint HexMapEditor.Tests.Patch0100Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
