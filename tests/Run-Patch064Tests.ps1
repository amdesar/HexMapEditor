param([string]$Case)
$ErrorActionPreference='Stop'
if($Case -notin @('profile','comparison','image','picker','library','input','fixture')){throw 'Wymagany pojedynczy Case: profile, comparison, image, picker, library, input lub fixture. Nie uruchomiono testów.'}
$run=Join-Path $PSScriptRoot ('_work/patch-064-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
if($Case -eq 'comparison'){
    # Reproducible baseline from recorded history, not a dependency on a leftover test EXE.
    $baseline=Join-Path $run 'baseline'
    $archive=Join-Path $run 'baseline.zip'
    git -C (Split-Path $PSScriptRoot) archive --format=zip "--output=$archive" 0cf9c24 src Build.ps1 assets/app/icon.ico
    if($LASTEXITCODE -ne 0){throw 'Nie można odczytać bazy 0.6.3 (0cf9c24) z historii. Nie uruchomiono porównania.'}
    Expand-Archive -LiteralPath $archive -DestinationPath $baseline
    $entry=Join-Path $baseline 'BaselineEntry.cs'
    [IO.File]::WriteAllText($entry,'namespace HexMapEditor.Tests { internal static class BaselineEntry { [System.STAThread] static void Main() {} } }')
    $baselineExe=Join-Path $baseline 'Baseline063.exe'
    & (Join-Path $baseline 'Build.ps1') -Output $baselineExe -TestSource $entry -EntryPoint HexMapEditor.Tests.BaselineEntry
    [IO.File]::WriteAllText((Join-Path $run 'baseline-path.txt'),$baselineExe)
}
$exe=Join-Path $run 'Patch064Tests.exe'
& (Join-Path (Split-Path $PSScriptRoot) 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch064Tests.cs') -EntryPoint HexMapEditor.Tests.Patch064Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
