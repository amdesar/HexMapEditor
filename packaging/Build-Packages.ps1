param(
    [Parameter(Mandatory)][string]$AppExe,
    [Parameter(Mandatory)][string]$InnoCompiler,
    [Parameter(Mandatory)][string]$Output,
    [ValidatePattern('^[a-zA-Z0-9]{1,32}$')][string]$Fixture,
    [string]$FixtureInstallDir
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$work = [IO.Path]::GetFullPath((Join-Path $root 'tests/_work')).TrimEnd('\') + '\'
$destination = [IO.Path]::GetFullPath($Output)
if (-not $destination.StartsWith($work, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Paczki robocze muszą powstać w tests/_work.'
}
$exe = (Resolve-Path -LiteralPath $AppExe).Path
$compiler = (Resolve-Path -LiteralPath $InnoCompiler).Path
$numbers = (Get-Item -LiteralPath $exe).VersionInfo
if ($numbers.FileVersion -notmatch '^\d+\.\d+\.\d+\.0$' -or $numbers.ProductVersion -ne $numbers.FileVersion) {
    throw 'Niezgodne FileVersion i ProductVersion kompilacji.'
}
$version = ([version]$numbers.FileVersion).ToString(3)
[IO.Directory]::CreateDirectory($destination) | Out-Null
$argsForCompiler = @("/DVersion=$version", "/DPayload=$exe", "/O$destination")
if ($Fixture) {
    $install = [IO.Path]::GetFullPath($FixtureInstallDir)
    if (-not $install.StartsWith($work, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Wariant testowy można zainstalować wyłącznie w tests/_work.'
    }
    $argsForCompiler += "/DFixture=$Fixture", "/DFixtureInstallDir=$install"
}
& $compiler @argsForCompiler (Join-Path $PSScriptRoot 'HexMapEditor.iss')
if ($LASTEXITCODE -ne 0) { throw "ISCC zakończył się kodem $LASTEXITCODE." }
if ($Fixture) { return }

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$manifest = [ordered]@{
    'HexMapEditor.exe' = $exe
    'deployment.mode' = (Join-Path $PSScriptRoot 'portable.mode')
    'README-Portable.txt' = (Join-Path $PSScriptRoot 'README-Portable.txt')
}
$zipName = Join-Path $destination "HexMapEditor-$version-Portable.zip"
$file = [IO.File]::Open($zipName, [IO.FileMode]::CreateNew)
try {
    $zip = [IO.Compression.ZipArchive]::new($file, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($name in $manifest.Keys) {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $manifest[$name], $name) | Out-Null
        }
    } finally { $zip.Dispose() }
} finally { $file.Dispose() }
Get-FileHash -LiteralPath (Join-Path $destination "HexMapEditor-$version-Setup.exe"),$zipName -Algorithm SHA256
