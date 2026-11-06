param([ValidateSet('geometry','gestures','library','view','input')][Parameter(Mandatory=$true)][string]$Case)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
$run=Join-Path $PSScriptRoot ('_work\object-040-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
try {
    $exe=Join-Path $run 'HexMapEditor.Object040Tests.exe'
    & (Join-Path $project 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Object040Tests.cs') -EntryPoint 'HexMapEditor.Tests.Object040Tests'
    & $exe $Case 2>&1 | Tee-Object -FilePath (Join-Path $run 'results.log')
    if ($LASTEXITCODE -ne 0) { throw "Targeted $Case scenario failed: $run" }
} finally {
    $resolved=[IO.Path]::GetFullPath($run)
    $prefix=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '_work'))+'\'
    if (-not $resolved.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected work directory' }
    Get-ChildItem -LiteralPath $resolved -Force | Where-Object Name -ne 'results.log' | ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }
}
