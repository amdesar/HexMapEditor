$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$run = Join-Path $PSScriptRoot ('_work\icon-043-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run) | Out-Null
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class Patch043Resources
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr LoadLibraryEx(string path, IntPtr file, uint flags);
    [DllImport("kernel32.dll")]
    static extern bool FreeLibrary(IntPtr module);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr FindResource(IntPtr module, IntPtr name, IntPtr type);
    [DllImport("kernel32.dll")]
    static extern IntPtr LoadResource(IntPtr module, IntPtr resource);
    [DllImport("kernel32.dll")]
    static extern IntPtr LockResource(IntPtr resource);
    [DllImport("kernel32.dll")]
    static extern uint SizeofResource(IntPtr module, IntPtr resource);

    public static byte[] Read(string path, int type, int id)
    {
        var module = LoadLibraryEx(path, IntPtr.Zero, 2);
        if (module == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
        try
        {
            var resource = FindResource(module, new IntPtr(id), new IntPtr(type));
            if (resource == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
            var data = new byte[SizeofResource(module, resource)];
            var address = LockResource(LoadResource(module, resource));
            if (address == IntPtr.Zero) throw new InvalidOperationException("Cannot read resource.");
            Marshal.Copy(address, data, 0, data.Length);
            return data;
        }
        finally { FreeLibrary(module); }
    }
}
'@
function Assert-IconResource([string]$exe, [byte[]]$ico) {
    $group = [Patch043Resources]::Read($exe, 14, 32512)
    $count = [BitConverter]::ToUInt16($ico,4)
    if ($count -ne [BitConverter]::ToUInt16($group,4)) { throw 'Inna liczba obrazów w zasobie PE' }
    for ($i = 0; $i -lt $count; $i++) {
        $entry = 6 + 16 * $i
        $length = [BitConverter]::ToUInt32($ico,($entry+8))
        $start = [BitConverter]::ToUInt32($ico,($entry+12))
        $id = [BitConverter]::ToUInt16($group,(6 + 14 * $i + 12))
        $payload = [Patch043Resources]::Read($exe, 3, $id)
        if ($payload.Length -ne $length -or
            [Convert]::ToBase64String($payload) -ne [Convert]::ToBase64String([byte[]]$ico[$start..($start+$length-1)])) {
            throw "Bajty RT_ICON $id różnią się od ICO"
        }
    }
}
$sourceIco = [IO.File]::ReadAllBytes((Join-Path $project 'assets/app/icon.ico'))
try {
    Assert-IconResource (Join-Path $project 'HexMapEditor.exe') $sourceIco
    Write-Output 'Zastany główny EXE: RT_GROUP_ICON i RT_ICON odpowiadają bieżącemu ICO.'
} catch {
    Write-Output "Zastany główny EXE: brak zgodności lub odczytu zasobu: $($_.Exception.Message)"
}
Copy-Item -LiteralPath (Join-Path $project 'src') -Destination $run -Recurse
Copy-Item -LiteralPath (Join-Path $project 'Build.ps1'),(Join-Path $project 'Rebuild-Program.bat') -Destination $run
$assets = Join-Path $run 'assets/app'
[IO.Directory]::CreateDirectory($assets) | Out-Null
$icon = Join-Path $assets 'icon.ico'
$png = [IO.MemoryStream]::new()
$bitmap = [Drawing.Bitmap]::new(32,32)
try {
    $g = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $g.Clear([Drawing.Color]::Magenta)
        $g.FillRectangle([Drawing.Brushes]::Lime,0,0,16,32)
    } finally { $g.Dispose() }
    $bitmap.Save($png,[Drawing.Imaging.ImageFormat]::Png)
    $image = $png.ToArray()
} finally { $bitmap.Dispose(); $png.Dispose() }
$writer = [IO.BinaryWriter]::new([IO.File]::Create($icon))
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]1)
    $writer.Write([byte[]]@(32,32,0,0)); $writer.Write([uint16]1); $writer.Write([uint16]32)
    $writer.Write([uint32]$image.Length); $writer.Write([uint32]22); $writer.Write([byte[]]$image)
} finally { $writer.Dispose() }
$testIco = [IO.File]::ReadAllBytes($icon)
$exe = Join-Path $run 'HexMapEditor.exe'
Copy-Item -LiteralPath (Join-Path $project 'HexMapEditor.exe') -Destination $exe
& (Join-Path $run 'Rebuild-Program.bat') /nopause
if ($LASTEXITCODE -ne 0) { throw 'BAT nie zbudował izolowanej kopii' }
Assert-IconResource $exe $testIco
$hash = (Get-FileHash -LiteralPath $exe).Hash
[IO.File]::WriteAllText($icon,'not an ICO')
$failed = $false
try { & (Join-Path $run 'Build.ps1') } catch { $failed = $true; Write-Output $_.Exception.Message }
if (!$failed -or (Get-FileHash -LiteralPath $exe).Hash -ne $hash) { throw 'Błąd ICO naruszył poprzedni EXE' }
Write-Output "PASS: zasób testowego EXE jest identyczny bajtowo z innym ICO. Błędne ICO zachowało EXE. Katalog: $run"
