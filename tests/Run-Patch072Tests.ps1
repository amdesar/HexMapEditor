param([string]$Case)
$ErrorActionPreference='Stop'
if($Case -notin @('creation','editor','autosave','controls','safety')){throw 'Wymagany pojedynczy Case: creation, editor, autosave, controls lub safety. Niczego nie uruchomiono.'}
$run=Join-Path $PSScriptRoot ('_work/patch-072-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
$exe=Join-Path $run 'Patch072Tests.exe'
& (Join-Path (Split-Path $PSScriptRoot) 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch072Tests.cs') -EntryPoint HexMapEditor.Tests.Patch072Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
