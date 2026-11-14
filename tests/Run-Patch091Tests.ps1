param([string]$Case)
$ErrorActionPreference='Stop'
if($Case -notin @('baseline','open','quick','close','controls','crop','fixture')){throw 'Wymagany jeden jawny Case: baseline, open, quick, close, controls, crop albo fixture.'}
$run=Join-Path $PSScriptRoot ('_work/patch-091-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
$exe=Join-Path $run 'Patch091Tests.exe'
& (Join-Path (Split-Path $PSScriptRoot) 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch091Tests.cs') -EntryPoint HexMapEditor.Tests.Patch091Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
