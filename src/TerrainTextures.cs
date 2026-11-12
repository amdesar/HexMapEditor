using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;

namespace HexMapEditor
{
    internal static class TerrainTextures
    {
        internal const int Resolution = GraphicLimits.TileSide;
        internal const int MaxPresetBytes = GraphicLimits.PresetBytes;
        internal static bool Same(GraphicDefinition a, GraphicDefinition b)
        {
            if (Object.ReferenceEquals(a, b)) return true;
            return a.Id == b.Id && a.SourceId == b.SourceId && GraphicSources.Same(a.Source, b.Source) &&
                MapLogic.Serializer().Serialize(new { a.Pointy, a.Flat, a.Linked }) == MapLogic.Serializer().Serialize(new { b.Pointy, b.Flat, b.Linked });
        }
        internal static List<GraphicDefinition> Collect(IEnumerable<GraphicDefinition> assets)
        {
            var result = new Dictionary<string, GraphicDefinition>();
            foreach (var asset in assets.Where(a => a != null))
            {
                if (asset.Id == null) throw new InvalidDataException("Brak ID definicji grafiki.");
                GraphicDefinition previous;
                if (result.TryGetValue(asset.Id, out previous))
                {
                    if (!Same(previous, asset)) throw new InvalidDataException("Konflikt identyfikatora grafiki.");
                }
                else result.Add(asset.Id, asset);
            }
            if (result.Count > GraphicLimits.Definitions) throw new InvalidDataException("Limit 256 definicji grafik.");
            GraphicSources.Collect(result.Values.Select(a => a.Source));
            return result.Values.ToList();
        }
        internal static Dictionary<string, GraphicDefinition> ReadTable(List<GraphicDefinition> assets, List<GraphicSource> sources)
        {
            if (assets == null) throw new InvalidDataException("Brak tabeli grafik.");
            var table = GraphicSources.ReadTable(sources);
            var result = new Dictionary<string, GraphicDefinition>();
            foreach (var asset in assets)
            {
                GraphicSource source;
                if (asset == null || asset.SourceId == null || !table.TryGetValue(asset.SourceId, out source) || asset.Id == null || result.ContainsKey(asset.Id))
                    throw new InvalidDataException("Nieprawidłowe odwołanie grafiki.");
                asset.Source = source; Validate(asset); result.Add(asset.Id, asset);
            }
            Collect(result.Values);
            return result;
        }
        internal static void Validate(GraphicDefinition asset)
        {
            if (asset == null || asset.Source == null || asset.SourceId != asset.Source.Id || String.IsNullOrWhiteSpace(asset.Name) || asset.Name.Length > 200)
                throw new InvalidDataException("Nieprawidłowa definicja grafiki.");
            CropMath.Validate(asset.Pointy, asset.Source); CropMath.Validate(asset.Flat, asset.Source);
            var copy = asset.Copy(); copy.Seal();
            if (copy.Id != asset.Id) throw new InvalidDataException("Parametry grafiki nie odpowiadają identyfikatorowi.");
        }
        internal static Bitmap Decode(GraphicDefinition asset, bool flat = false)
        {
            Validate(asset);
            using (var source = GraphicSources.Decode(asset.Source))
            {
                var result = new Bitmap(Resolution, Resolution);
                try
                {
                    using (var g = Graphics.FromImage(result)) CropMath.Draw(g, source, flat ? asset.Flat : asset.Pointy, new RectangleF(0, 0, Resolution, Resolution));
                    return result;
                }
                catch { result.Dispose(); throw; }
            }
        }
        internal static GraphicDefinition Resolve(Dictionary<string, GraphicDefinition> assets, string id)
        {
            if (id == null) return null;
            GraphicDefinition asset;
            if (!assets.TryGetValue(id, out asset)) throw new InvalidDataException("Brak grafiki: " + id);
            return asset;
        }
        internal static void Attach(TerrainPreset preset, Dictionary<string, GraphicDefinition> assets)
        {
            if (preset == null || preset.Terrains == null) return;
            foreach (var terrain in preset.Terrains) if (terrain != null)
            {
                terrain.Texture = Resolve(assets, terrain.TextureId);
                FillRules.Attach(terrain.Variants, assets);
                var first = terrain.Variants.FirstOrDefault();
                if (first == null || terrain.Color != first.Color || terrain.TextureId != first.GraphicId)
                    throw new InvalidDataException("Niespójna reprezentacja pierwszego wariantu.");
            }
        }
        internal static bool ValidFill(TerrainDefinition terrain)
        {
            if(terrain.FillKind=="gradient")return terrain.Texture==null&&terrain.TextureId==null&&terrain.Variants.Count>0&&terrain.Variants[0].Gradient!=null;
            if (terrain.FillKind == "color") return terrain.Texture == null && terrain.TextureId == null && terrain.Variants.Count>0 && terrain.Variants[0].Gradient==null;
            return terrain.FillKind == "graphic" && terrain.Texture != null && terrain.Texture.Id == terrain.TextureId;
        }
        internal static GraphicsPath Mask(PointF center, float radius, bool flat)
        {
            var vertices = new PointF[6];
            for (int i = 0; i < vertices.Length; i++)
            {
                double angle = (60 * i - (flat ? 0 : 30)) * Math.PI / 180;
                vertices[i] = new PointF(center.X + radius * (float)Math.Cos(angle), center.Y + radius * (float)Math.Sin(angle));
            }
            var mask = new GraphicsPath(); mask.AddPolygon(vertices); return mask;
        }
        internal static void Draw(Graphics g, GraphicsPath mask, Bitmap bitmap, RectangleF square)
        {
            var state = g.Save();
            try { g.SetClip(mask, CombineMode.Intersect); g.DrawImage(bitmap, square); }
            finally { g.Restore(state); }
        }
    }
    public sealed partial class MainWindow
    {
        readonly Dictionary<string, Bitmap> terrainBitmaps = new Dictionary<string, Bitmap>();
        readonly Dictionary<string, long> terrainBitmapUses = new Dictionary<string, long>();
        long terrainBitmapClock;
        void ClearTerrainBitmaps()
        {
            foreach (var bitmap in terrainBitmaps.Values) bitmap.Dispose();
            terrainBitmaps.Clear(); terrainBitmapUses.Clear();
        }
        void DrawTerrainTexture(Graphics g, GraphicsPath mask, PointF center, float radius, GraphicDefinition asset)
        {
            string key = asset.Id + ":" + map.Orientation;
            Bitmap bitmap;
            if (!terrainBitmaps.TryGetValue(key, out bitmap))
            {
                if (terrainBitmaps.Count >= GraphicLimits.TileCacheEntries)
                {
                    string oldest = terrainBitmapUses.OrderBy(item => item.Value).First().Key;
                    terrainBitmaps[oldest].Dispose(); terrainBitmaps.Remove(oldest); terrainBitmapUses.Remove(oldest);
                }
                terrainBitmaps.Add(key, bitmap = TerrainTextures.Decode(asset, map.Orientation == "FlatTop"));
            }
            terrainBitmapUses[key] = ++terrainBitmapClock;
            TerrainTextures.Draw(g, mask, bitmap, new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2));
        }
        void CheckTextureBudget(IEnumerable<GraphicDefinition> added)
        {
            var definitions = TerrainTextures.Collect(DocumentGraphics().Concat(added));
            GraphicSources.Collect(definitions.Select(d => d.Source).Concat(map.Background == null ? new GraphicSource[0] : new[] { map.Background.Source }));
        }
    }
}
