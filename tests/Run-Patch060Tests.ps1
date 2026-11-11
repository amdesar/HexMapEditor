param([string]$Case)
$ErrorActionPreference='Stop'
if($Case -notin @('acceptance','ink','pointer','darkness','visibility','gestures','fixture')) {throw 'Wymagany pojedynczy Case: acceptance|ink|pointer|darkness|visibility|gestures|fixture. Nie uruchomiono testów.'}
$run=Join-Path $PSScriptRoot ('_work/patch-060-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
$exe=Join-Path $run 'Patch060Tests.exe'
& (Join-Path (Split-Path $PSScriptRoot) 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch060Tests.cs') -EntryPoint HexMapEditor.Tests.Patch060Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
