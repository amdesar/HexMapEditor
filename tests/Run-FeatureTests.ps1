$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$outputDirectory = Join-Path $PSScriptRoot ('_work\features-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$testExe = Join-Path $outputDirectory 'HexMapEditor.FeatureTests.exe'
try {
    & (Join-Path $projectRoot 'Build.ps1') -Output $testExe -TestSource (Join-Path $PSScriptRoot 'FeatureTests.cs') -EntryPoint 'HexMapEditor.Tests.FeatureTests'
    & $testExe
    if ($LASTEXITCODE -ne 0) { throw 'Feature tests failed.' }
} finally {
    if (Test-Path -LiteralPath $testExe) { Remove-Item -LiteralPath $testExe }
    [IO.Directory]::Delete($outputDirectory)
}
