$ErrorActionPreference = 'Stop'
$run = Join-Path $PSScriptRoot ('_work\ui-023-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run) | Out-Null
Copy-Item -LiteralPath (Join-Path (Split-Path -Parent $PSScriptRoot) 'HexMapEditor.exe') -Destination (Join-Path $run 'HexMapEditor.exe')
$terrains = @([ordered]@{TerrainId='default-neutral'; VariantId='default-neutral-v1'; Revision=1; Name='Teren neutralny'; Color='#777F91'; FillKind='color'})
for ($i=1; $i -le 18; $i++) {
    $terrains += [ordered]@{TerrainId=('ui-023-terrain-' + $i); VariantId=('ui-023-variant-' + $i); Revision=1; Name=('Bardzo długa nazwa terenu do sprawdzenia wielokropka i przewijania ' + $i); Color='#287DB3'; FillKind='color'}
}
$preset = [ordered]@{PresetFormatVersion=1;Id='ui-portable-023';Name='Galaktyki żółte';Terrains=$terrains}
[IO.File]::WriteAllText((Join-Path $run 'Do skopiowania.terrainset.json'),($preset | ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
Write-Output $run
