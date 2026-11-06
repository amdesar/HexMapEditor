$ErrorActionPreference = 'Stop'
$logDirectory = Join-Path $PSScriptRoot ('_work\all-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($logDirectory) | Out-Null
$log = Join-Path $logDirectory 'results.log'
foreach ($suite in @('History', 'Technical', 'Gesture', 'Feature', 'Patch', 'Maintenance')) {
    & (Join-Path $PSScriptRoot ('Run-' + $suite + 'Tests.ps1')) | Tee-Object -FilePath $log -Append
}
Write-Output ('Test log: ' + $log)
