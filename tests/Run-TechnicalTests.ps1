$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outputDirectory = Join-Path $PSScriptRoot ('_work\technical-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$testExe = Join-Path $outputDirectory 'HexMapEditor.TechnicalTests.exe'
try {
    & (Join-Path $projectRoot 'Build.ps1') -Output $testExe -TestSource (Join-Path $PSScriptRoot 'TechnicalTests.cs') -EntryPoint 'HexMapEditor.Tests.TechnicalTests'
    if ($LASTEXITCODE -ne 0) { throw 'Technical test compilation failed.' }
    & $testExe
    if ($LASTEXITCODE -ne 0) { throw 'Technical tests failed.' }
} finally {
    if (Test-Path -LiteralPath $testExe) { Remove-Item -LiteralPath $testExe }
    [System.IO.Directory]::Delete($outputDirectory)
}
