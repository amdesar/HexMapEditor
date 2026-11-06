$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$outputDirectory = Join-Path $PSScriptRoot ('_work\maintenance-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
try {
    $testExe = Join-Path $outputDirectory 'HexMapEditor.MaintenanceTests.exe'
    & (Join-Path $projectRoot 'Build.ps1') -Output $testExe -TestSource (Join-Path $PSScriptRoot 'MaintenanceTests.cs') -EntryPoint 'HexMapEditor.Tests.MaintenanceTests'
    & $testExe
    if ($LASTEXITCODE -ne 0) { throw 'Maintenance tests failed.' }
} finally {
    $resolved = [IO.Path]::GetFullPath($outputDirectory)
    $prefix = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '_work')) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected test location.' }
    if (Test-Path -LiteralPath $resolved) { [IO.Directory]::Delete($resolved, $true) }
}
