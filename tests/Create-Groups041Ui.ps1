$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
$run=Join-Path $PSScriptRoot ('_work\ui-041-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
& (Join-Path $project 'Build.ps1') -Output (Join-Path $run 'HexMapEditor.exe')
$generator=Join-Path $run 'FixtureGenerator.exe'
& (Join-Path $project 'Build.ps1') -Output $generator -TestSource (Join-Path $PSScriptRoot 'Groups041Tests.cs') -EntryPoint 'HexMapEditor.Tests.Groups041Tests'
& $generator fixture $run
if ($LASTEXITCODE -ne 0) { throw 'Fixture generation failed' }
Remove-Item -LiteralPath $generator
Write-Output $run
