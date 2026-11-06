param([string]$Case)
$ErrorActionPreference='Stop'
if ($Case -notin @('regions','controls','icons','import','names')) { throw 'Choose one case: regions, controls, icons, import, names. No automatic full run.' }
$root=Split-Path -Parent $PSScriptRoot
$run=Join-Path $PSScriptRoot ('_work\patch-031-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
try {
    $exe=Join-Path $run 'HexMapEditor.Patch031Tests.exe'
    & (Join-Path $root 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch031Tests.cs') -EntryPoint 'HexMapEditor.Tests.Patch031Tests'
    & $exe $Case 2>&1 | Tee-Object -FilePath (Join-Path $run 'results.log')
    if ($LASTEXITCODE -ne 0) { throw "Targeted $Case test failed. See $run" }
} finally {
    $resolved=[IO.Path]::GetFullPath($run)
    $prefix=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '_work'))+'\'
    if (-not $resolved.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected work directory' }
    Get-ChildItem -LiteralPath $resolved -Force | Where-Object Name -ne 'results.log' | ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }
}
