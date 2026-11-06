$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$run = Join-Path $PSScriptRoot ('_work\convenience-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run) | Out-Null
try {
    $exe = Join-Path $run 'HexMapEditor.ConvenienceTests.exe'
    & (Join-Path $projectRoot 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'ConvenienceTests.cs') -EntryPoint 'HexMapEditor.Tests.ConvenienceTests'
    & $exe
    if ($LASTEXITCODE -ne 0) { throw 'Convenience tests failed.' }
} finally {
    $resolved = [IO.Path]::GetFullPath($run)
    $prefix = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '_work')) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected test location.' }
    if (Test-Path -LiteralPath $resolved) { [IO.Directory]::Delete($resolved, $true) }
}
