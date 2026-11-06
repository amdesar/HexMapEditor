$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$run = Join-Path $PSScriptRoot ('_work\ui-022-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run) | Out-Null
$generator = Join-Path $run 'UiFixtures.exe'
& (Join-Path $projectRoot 'Build.ps1') -Output $generator -TestSource (Join-Path $PSScriptRoot 'UiFixtures.cs') -EntryPoint 'HexMapEditor.Tests.UiFixtures'
& $generator
if ($LASTEXITCODE -ne 0) { throw 'Fixture generation failed.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'HexMapEditor.exe') -Destination (Join-Path $run 'HexMapEditor.exe')
Write-Output ('UI directory: ' + $run)
