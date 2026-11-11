using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace HexMapEditor
{
    public sealed class FillVariant
    {
        public string Id { get; set; }
        public string Color { get; set; }
        public string GraphicId { get; set; }
        internal GraphicDefinition Graphic;
        public FillVariant() { Id = Guid.NewGuid().ToString("N"); Color = "#777F91"; }
        internal FillVariant Copy() { return (FillVariant)MemberwiseClone(); }
    }
    internal static class FillRules
    {
        internal static void Validate(FillVariant fill)
        {
            if (fill == null || String.IsNullOrWhiteSpace(fill.Id) || fill.Id.Length > 200 || !MapLogic.ValidColor(fill.Color) ||
                (fill.GraphicId == null ? fill.Graphic != null : fill.Graphic == null || fill.Graphic.Id != fill.GraphicId))
                throw new InvalidDataException("Nieprawidłowy wariant wyglądu.");
        }
        internal static void ValidatePool(List<FillVariant> pool)
        {
            if (pool == null || pool.Count < 1 || pool.Count > 32 || pool.Select(f => f == null ? null : f.Id).Distinct().Count() != pool.Count)
                throw new InvalidDataException("Pula wymaga od 1 do 32 różnych wariantów.");
            foreach (var fill in pool) Validate(fill);
        }
        internal static void Attach(IEnumerable<FillVariant> pool, Dictionary<string, GraphicDefinition> graphics)
        {
            if (pool == null) throw new InvalidDataException("Brak puli.");
            foreach (var fill in pool) { if (fill == null) throw new InvalidDataException("Brak wariantu."); fill.Graphic = TerrainTextures.Resolve(graphics, fill.GraphicId); }
        }
        internal static List<FillVariant> Copy(IEnumerable<FillVariant> fills) { return fills.Select(f => f.Copy()).ToList(); }
        internal static FillVariant FogDefault() { return new FillVariant { Id = "fog-gray", Color = "#656B75" }; }
        internal static string Signature(IEnumerable<FillVariant> fills) { return MapLogic.Serializer().Serialize(fills); }
        internal static void SetPool(TerrainDefinition terrain, List<FillVariant> fills)
        {
            ValidatePool(fills);
            terrain.Variants = Copy(fills);
            var first = fills[0];
            terrain.Color = first.Color; terrain.TextureId = first.GraphicId; terrain.Texture = first.Graphic;
            terrain.FillKind = first.Graphic == null ? "color" : "graphic";
        }
    }
    public sealed partial class MainWindow
    {
        Random appearanceRandom = new Random();
        FillVariant NextFill(List<FillVariant> pool) { return pool[appearanceRandom.Next(pool.Count)].Copy(); }
        IEnumerable<GraphicDefinition> DocumentGraphics()
        {
            return map.Cells.Select(c => c.Texture).Concat(map.Presets.SelectMany(p => p.Terrains).SelectMany(t => t.Variants).Select(f => f.Graphic))
                .Concat(map.Cells.Select(c => c.FogFill == null ? null : c.FogFill.Graphic)).Concat(map.FogPalette.Select(f => f.Graphic));
        }
    }
}
