param([switch]$GroupClick)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$run = Join-Path $PSScriptRoot ('_work\patch-050-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run) | Out-Null
$exe = Join-Path $run 'Patch050Tests.exe'
& (Join-Path $root 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch050Tests.cs') -EntryPoint 'HexMapEditor.Tests.Patch050Tests'
$log = Join-Path $run 'results.log'
if ($GroupClick) { & $exe 'group-click' | Tee-Object -FilePath $log }
else { & $exe | Tee-Object -FilePath $log }
if ($LASTEXITCODE -ne 0) { throw "Próba 0.5.0 nie powiodła się. Katalog: $run" }
$run
