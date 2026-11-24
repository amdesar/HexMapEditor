param([string]$Output = (Join-Path $PSScriptRoot 'HexMapEditor.exe'), [string]$TestSource, [string]$EntryPoint = 'HexMapEditor.Program',
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+$')][string]$FixtureVersion)
$ErrorActionPreference = 'Stop'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
$icon = Join-Path $PSScriptRoot 'assets\app\icon.ico'
$target = [IO.Path]::GetFullPath($Output)
try {
if ($FixtureVersion) {
    $fixtureRoot = (Join-Path $PSScriptRoot 'tests\_work').TrimEnd('\') + '\'
    if (-not $target.StartsWith($fixtureRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Kompilacja pomocnicza jest dozwolona wyłącznie w tests/_work.'
    }
}
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' -ErrorAction SilentlyContinue)
if ($FixtureVersion) { $sources = @($sources | Where-Object Name -NotIn @('AssemblyInfo.cs', 'ReleaseMetadata.cs')) }
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) { throw 'Brak kompilatora .NET Framework 4.x (csc.exe).' }
if ($sources.Count -eq 0) { throw 'Brak źródeł C# w katalogu src.' }
if (-not (Test-Path -LiteralPath $icon -PathType Leaf)) {
    throw "Brak ikony: $icon. Oczekiwana nazwa to icon.ico. Sprawdź rozszerzenie, także gdy Windows je ukrywa."
}
try {
    $bytes = [IO.File]::ReadAllBytes($icon)
    if ($bytes.Length -lt 22 -or [BitConverter]::ToUInt16($bytes,0) -ne 0 -or [BitConverter]::ToUInt16($bytes,2) -ne 1) {
        throw 'Plik nie ma poprawnego nagłówka ICO.'
    }
    $count = [BitConverter]::ToUInt16($bytes,4)
    $tableEnd = 6 + 16 * $count
    if ($count -eq 0 -or $tableEnd -gt $bytes.Length) { throw 'Niepoprawna tablica obrazów ICO.' }
    for ($i = 0; $i -lt $count; $i++) {
        $entry = 6 + 16 * $i
        $length = [long][BitConverter]::ToUInt32($bytes,($entry + 8))
        $offset = [long][BitConverter]::ToUInt32($bytes,($entry + 12))
        if ($length -eq 0 -or $offset -lt $tableEnd -or $offset + $length -gt $bytes.Length) {
            throw 'Obraz ICO wykracza poza plik.'
        }
    }
    Add-Type -AssemblyName System.Drawing
    $validated = [Drawing.Icon]::new($icon)
    $validated.Dispose()
} catch {
    throw "Nie można odczytać icon.ico ($icon). Sprawdź format ICO, nie tylko nazwę pliku. $($_.Exception.Message)"
}
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
    if ($FixtureVersion) {
        foreach ($metadata in @('AssemblyInfo.cs', 'ReleaseMetadata.cs')) {
            $file = Join-Path $tempDir $metadata
            $content = [IO.File]::ReadAllText((Join-Path (Join-Path $PSScriptRoot 'src') $metadata))
            $content = [regex]::Replace($content, '(?<=")\d+\.\d+\.\d+(?=(?:\.0)?")', $FixtureVersion)
            [IO.File]::WriteAllText($file, $content)
            $arguments += $file
        }
        $arguments += '/define:PACKAGE_FIXTURE'
    }
    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Kompilacja nie powiodła się.' }
    if ([IO.File]::Exists($target)) {
        try { [IO.File]::Replace($temp, $target, [NullString]::Value) }
        catch { throw "Nie można podmienić EXE. Jeśli program jest uruchomiony, zamknij go i ponów próbę. $($_.Exception.Message)" }
    } else { [IO.File]::Move($temp, $target) }
} finally {
    if ([IO.File]::Exists($temp)) { [IO.File]::Delete($temp) }
    if ($FixtureVersion) {
        foreach ($metadata in @('AssemblyInfo.cs', 'ReleaseMetadata.cs')) {
            [IO.File]::Delete((Join-Path $tempDir $metadata))
        }
    }
    try { [IO.Directory]::Delete($tempDir) } catch { Write-Warning "Pozostał katalog tymczasowy: $tempDir" }
}
} catch {
    $status = if ([IO.File]::Exists($target)) { 'Poprzedni EXE zachowano.' } else { 'Brak poprzedniego EXE.' }
    throw "$($_.Exception.Message) $status"
}
Write-Host "Zbudowano: $target"
Write-Host "Osadzono ICO: $icon. Eksplorator może nadal wyświetlać ikonę z pamięci podręcznej."
