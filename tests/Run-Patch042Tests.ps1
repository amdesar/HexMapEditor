param([Parameter(Mandatory=$true)][ValidateSet('states','groups','controls','render','fixture')][string]$Case)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$run = Join-Path $PSScriptRoot ('_work\patch-042-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run) | Out-Null
$exe = Join-Path $run 'Patch042Tests.exe'
& (Join-Path $root 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch042Tests.cs') -EntryPoint 'HexMapEditor.Tests.Patch042Tests'
& $exe $Case | Tee-Object -FilePath (Join-Path $run 'results.log')
if ($LASTEXITCODE -ne 0) { throw "Próba $Case nie powiodła się. Katalog: $run" }
$run
