param([string]$Case)
$ErrorActionPreference = 'Stop'
if ($Case -notin @('crop','pool','library','fog','background','portable','controls')) {
    throw 'Użycie: Run-Patch051Tests.ps1 -Case crop|pool|library|fog|background|portable|controls. Wymagany jeden przypadek.'
}
$root = Split-Path -Parent $PSScriptRoot
$run = Join-Path $PSScriptRoot ('_work\patch-051-' + $Case + '-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run) | Out-Null
$exe = Join-Path $run 'Patch051Tests.exe'
& (Join-Path $root 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch051Tests.cs') -EntryPoint 'HexMapEditor.Tests.Patch051Tests'
& $exe $Case | Tee-Object -FilePath (Join-Path $run 'results.log')
if ($LASTEXITCODE -ne 0) { throw "Próba $Case nie powiodła się. Katalog $run" }
$run
