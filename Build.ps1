param([string]$Output = (Join-Path $PSScriptRoot 'HexMapEditor.exe'), [string]$TestSource, [string]$EntryPoint = 'HexMapEditor.Program')
$ErrorActionPreference = 'Stop'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
$icon = Join-Path $PSScriptRoot 'assets\app\icon.ico'
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' -ErrorAction SilentlyContinue)
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) { throw 'Brak kompilatora .NET Framework 4.x (csc.exe).' }
if ($sources.Count -eq 0) { throw 'Brak źródeł C# w katalogu src.' }
if (-not (Test-Path -LiteralPath $icon -PathType Leaf)) { throw 'Brak assets\app\icon.ico.' }
$target = [IO.Path]::GetFullPath($Output)
$tempDir = Join-Path ([IO.Path]::GetDirectoryName($target)) ('.build-' + [Guid]::NewGuid().ToString('N'))
$temp = Join-Path $tempDir ([IO.Path]::GetFileName($target))
$references = @('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Web.Extensions.dll','System.Xaml.dll')
$references += @('PresentationFramework.dll','PresentationCore.dll','WindowsBase.dll','WindowsFormsIntegration.dll') | ForEach-Object { 'WPF\' + $_ }
$arguments = @('/nologo','/optimize+',('/target:' + $(if ($TestSource) { 'exe' } else { 'winexe' })),('/main:' + $EntryPoint),('/out:' + $temp),('/win32icon:' + $icon))
foreach ($reference in $references) {
    $path = Join-Path $framework $reference
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Brak składnika kompilacji: $path" }
    $arguments += '/reference:' + $path
}
$arguments += $sources | ForEach-Object FullName
if ($TestSource) { $arguments += $TestSource }
[IO.Directory]::CreateDirectory($tempDir) | Out-Null
try {
    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Kompilacja nie powiodła się. Poprzedni EXE zachowano.' }
    if ([IO.File]::Exists($target)) {
        try { [IO.File]::Replace($temp, $target, [NullString]::Value) }
        catch { throw "Nie można podmienić EXE. Jeśli program jest uruchomiony, zamknij go i ponów próbę. $($_.Exception.Message)" }
    } else { [IO.File]::Move($temp, $target) }
} finally {
    if ([IO.File]::Exists($temp)) { [IO.File]::Delete($temp) }
    [IO.Directory]::Delete($tempDir)
}
