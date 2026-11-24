param(
    [Parameter(Mandatory=$true)][ValidateSet('metadata','panel','notes','fixture')][string]$Case,
    [string]$WordSamples
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$work=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '_work'))
$run=Join-Path $work ('patch-0101-'+$Case+'-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run)|Out-Null
if ($WordSamples) {
    $source=(Resolve-Path -LiteralPath $WordSamples).Path
    if (!$source.StartsWith($work+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Próbki muszą pochodzić z tests/_work.' }
    foreach ($name in @('Rich Text Format.sample','HTML Format.sample','Picture-only.rtf.sample')) {
        Copy-Item -LiteralPath (Join-Path $source $name) -Destination $run
    }
}
$exe=Join-Path $run 'Patch0101Tests.exe'
& (Join-Path $root 'Build.ps1') -Output $exe -TestSource (Join-Path $PSScriptRoot 'Patch0101Tests.cs') -EntryPoint HexMapEditor.Tests.Patch0101Tests
& $exe $Case | Tee-Object (Join-Path $run 'results.log')
if($LASTEXITCODE -ne 0){throw "FAIL $Case $run"}
$run
