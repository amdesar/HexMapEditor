namespace HexMapEditor
{
    // Only region boundaries, frozen before the first mutation of a bucket gesture.
    internal sealed class RegionState
    {
        public readonly string Variant, TerrainId, Color, Terrain;
        public readonly bool Fog,Darkness;
        public RegionState(CellData cell) { TerrainId = cell.TerrainId; Variant = cell.VariantId; Color = cell.Color; Terrain = cell.Terrain; Fog = cell.Fog; Darkness=cell.Darkness; }
    }
}
