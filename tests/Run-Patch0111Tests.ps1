param([Parameter(Mandatory=$true)][ValidateSet('tabs-fit','autosave','close','fixture')][string]$Case)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$run=Join-Path $PSScriptRoot ('_work/patch-0111-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
$exe=Join-Path $run 'Patch0111Tests.exe'
& (Join-Path $root 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch0111Tests.cs') -EntryPoint HexMapEditor.Tests.Patch0111Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
