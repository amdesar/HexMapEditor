param([string]$Case)
$ErrorActionPreference = 'Stop'
if ($Case -notin @('regression','lifecycle','safety')) { throw 'Użycie: Run-Patch052Tests.ps1 -Case regression|lifecycle|safety. Wymagany jeden przypadek.' }
$run = Join-Path $PSScriptRoot ('_work/patch-052-' + $Case + '-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run) | Out-Null
$exe = Join-Path $run 'Patch052Tests.exe'
& (Join-Path (Split-Path $PSScriptRoot) 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch052Tests.cs') -EntryPoint 'HexMapEditor.Tests.Patch052Tests'
& $exe $Case | Tee-Object -FilePath (Join-Path $run 'results.log')
if ($LASTEXITCODE -ne 0) { throw "Niepowodzenie $Case. $run" }
$run
