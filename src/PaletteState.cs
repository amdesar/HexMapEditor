using System;
using System.Collections.Generic;
using System.Linq;

namespace HexMapEditor
{
    internal sealed class PaletteState
    {
        public List<TerrainPreset> Presets;
        public string ActivePresetId, SelectedTerrainId;
        public int BrushIndex;
        public static PaletteState Capture(MapData map, int brush,IEnumerable<TerrainPreset> working=null)
        {
            var presets=working??map.Presets;
            var active = presets.First(p => p.Id == map.ActivePresetId);
            return new PaletteState { Presets = presets.Select(p => p.Copy()).ToList(), ActivePresetId = map.ActivePresetId, BrushIndex = brush, SelectedTerrainId = brush >= 0 && brush < active.Terrains.Count ? active.Terrains[brush].TerrainId : null };
        }
        public string Signature { get { return MapLogic.Serializer().Serialize(new { ActivePresetId, Presets }); } }
        public void Apply(MapData map, out int brush)
        {
            map.Presets=ApplyWorking(map,out brush);
        }
        internal List<TerrainPreset> ApplyWorking(MapData map,out int brush)
        {
            var copy=Presets.Select(p=>p.Copy()).ToList();map.ActivePresetId=ActivePresetId;
            var terrains = copy.First(p => p.Id == ActivePresetId).Terrains; brush = terrains.FindIndex(t => t.TerrainId == SelectedTerrainId);
            if (brush < 0) brush = Math.Max(0, Math.Min(BrushIndex, terrains.Count - 1));
            return copy;
        }
    }
}
