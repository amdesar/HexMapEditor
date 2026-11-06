param([string]$Case)
$ErrorActionPreference='Stop'
if ($Case -notin @('controls','library','presets')) { throw 'Choose one case: controls, library, presets. No automatic full run.' }
$root=Split-Path -Parent $PSScriptRoot
$run=Join-Path $PSScriptRoot ('_work\patch-032-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
try {
    $exe=Join-Path $run 'HexMapEditor.Patch032Tests.exe'
    & (Join-Path $root 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch032Tests.cs') -EntryPoint 'HexMapEditor.Tests.Patch032Tests'
    & $exe $Case 2>&1 | Tee-Object -FilePath (Join-Path $run 'results.log')
    if ($LASTEXITCODE -ne 0) { throw "Targeted $Case test failed. See $run" }
} finally {
    $resolved=[IO.Path]::GetFullPath($run)
    $prefix=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '_work'))+'\'
    if (-not $resolved.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected work directory' }
    Get-ChildItem -LiteralPath $resolved -Force | Where-Object Name -ne 'results.log' | ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }
}
