$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
$run=Join-Path $PSScriptRoot ('_work\ui-031-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
$generator=Join-Path $run 'FixtureGenerator.exe'
& (Join-Path $project 'Build.ps1') -Output $generator -TestSource (Join-Path $PSScriptRoot 'Patch031Tests.cs') -EntryPoint 'HexMapEditor.Tests.Patch031Tests'
& $generator --fixture $run
if ($LASTEXITCODE -ne 0) { throw 'Fixture generation failed' }
Remove-Item -LiteralPath $generator
Copy-Item -LiteralPath (Join-Path $project 'HexMapEditor.exe') -Destination (Join-Path $run 'HexMapEditor.exe')
Write-Output $run