namespace HexMapEditor
{
    // Only region boundaries, frozen before the first mutation of a bucket gesture.
    internal sealed class RegionState
    {
        public readonly string Variant, Color, Terrain;
        public readonly bool Fog;
        public RegionState(CellData cell) { Variant = cell.TerrainId; Color = cell.Color; Terrain = cell.Terrain; Fog = cell.Fog; }
    }
}
