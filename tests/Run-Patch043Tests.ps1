param([Parameter(Mandatory=$true)][ValidateSet('states','view-list','order')][string]$Case)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$run = Join-Path $PSScriptRoot ('_work\patch-043-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run) | Out-Null
$exe = Join-Path $run 'Patch043Tests.exe'
& (Join-Path $root 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch043Tests.cs') -EntryPoint 'HexMapEditor.Tests.Patch043Tests'
& $exe $Case | Tee-Object -FilePath (Join-Path $run 'results.log')
if ($LASTEXITCODE -ne 0) { throw "Próba $Case nie powiodła się. Katalog: $run" }
$run
