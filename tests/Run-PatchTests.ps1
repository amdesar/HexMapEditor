$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$outputDirectory = Join-Path $PSScriptRoot ('_work\patch-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$testExe = Join-Path $outputDirectory 'HexMapEditor.PatchTests.exe'
try {
    & (Join-Path $projectRoot 'Build.ps1') -Output $testExe -TestSource (Join-Path $PSScriptRoot 'PatchTests.cs') -EntryPoint 'HexMapEditor.Tests.PatchTests'
    & $testExe
    if ($LASTEXITCODE -ne 0) { throw 'Patch tests failed.' }
} finally {
    # The control-layout test opens its own form, so startup also creates an
    # isolated Library beside the test EXE. Remove only this run's directory.
    $resolvedOutput = [IO.Path]::GetFullPath($outputDirectory)
    $workPrefix = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '_work')) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedOutput.StartsWith($workPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected test output location.' }
    if (Test-Path -LiteralPath $resolvedOutput) { [IO.Directory]::Delete($resolvedOutput, $true) }
}
