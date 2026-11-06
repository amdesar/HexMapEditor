param([string]$Case = '')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$run = Join-Path $PSScriptRoot ('_work\fog-icons-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run) | Out-Null
try {
    $exe = Join-Path $run 'HexMapEditor.FogIconTests.exe'
    & (Join-Path $root 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'FogIconTests.cs') -EntryPoint 'HexMapEditor.Tests.FogIconTests'
    & $exe $Case | Tee-Object -FilePath (Join-Path $run 'results.log')
    if ($LASTEXITCODE -ne 0) { throw 'Selective fog/icon tests failed.' }
} finally {
    $resolved = [IO.Path]::GetFullPath($run)
    $prefix = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '_work')) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected test location.' }
    # Keep the small log, remove only this run's generated binaries/data/Library.
    Get-ChildItem -LiteralPath $resolved -Force | Where-Object { $_.Name -ne 'results.log' } | ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force -Recurse }
}
