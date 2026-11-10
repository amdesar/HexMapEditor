using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace HexMapEditor
{
    internal static class TerrainTextures
    {
        internal const int Resolution = 256;
        internal const int MaxPresetBytes = 48 * 1024 * 1024;
        const int MaxEncodedBytes = 44 * 1024 * 1024;

        internal static List<IconAsset> Collect(IEnumerable<IconAsset> assets)
        {
            var result = assets.Where(a => a != null).GroupBy(a => a.Id).Select(g => g.First()).ToList();
            CheckBudget(result);
            return result;
        }

        static void CheckBudget(ICollection<IconAsset> assets)
        {
            if (assets.Count > 128 || assets.Any(a => a == null || a.Png == null) ||
                assets.Sum(a => (long)a.Png.Length) > MaxEncodedBytes)
                throw new InvalidDataException("Limit grafik terenów to 128 zasobów i 44 MiB danych Base64.");
        }

        internal static Dictionary<string, IconAsset> ReadTable(List<IconAsset> assets)
        {
            if (assets == null) throw new InvalidDataException("Brak tabeli grafik terenów.");
            CheckBudget(assets);
            var table = new Dictionary<string, IconAsset>();
            foreach (var asset in assets)
            {
                if (asset.Id == null || table.ContainsKey(asset.Id)) throw new InvalidDataException("Powtórzona lub brakująca tożsamość grafiki terenu.");
                using (var bitmap = Decode(asset)) { }
                table.Add(asset.Id, asset);
            }
            return table;
        }

        internal static Bitmap Decode(IconAsset asset)
        {
            if (asset == null || asset.Width != Resolution || asset.Height != Resolution)
                throw new InvalidDataException("Grafika terenu musi być kwadratowym PNG 256 × 256.");
            return IconAssets.Decode(asset);
        }

        internal static IconAsset Resolve(Dictionary<string, IconAsset> assets, string id)
        {
            if (id == null) return null;
            IconAsset asset;
            if (!assets.TryGetValue(id, out asset)) throw new InvalidDataException("Brak grafiki terenu: " + id);
            return asset;
        }

        internal static void Attach(TerrainPreset preset, Dictionary<string, IconAsset> assets)
        {
            if (preset == null || preset.Terrains == null) return;
            foreach (var terrain in preset.Terrains)
                if (terrain != null) terrain.Texture = Resolve(assets, terrain.TextureId);
        }

        internal static bool ValidFill(TerrainDefinition terrain)
        {
            if (terrain.FillKind == "color") return terrain.Texture == null && terrain.TextureId == null;
            return terrain.FillKind == "graphic" && terrain.Texture != null && terrain.Texture.Id == terrain.TextureId;
        }

        internal static Bitmap ReadSource(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length > 16 * 1024 * 1024) throw new InvalidDataException("Plik źródłowy może mieć najwyżej 16 MiB.");
                using (var source = Image.FromStream(stream, true, false))
                {
                    if (source is Metafile || source.Width < 1 || source.Height < 1 || source.Width > 4096 || source.Height > 4096 ||
                        !new[] { ImageFormat.Png.Guid, ImageFormat.Jpeg.Guid, ImageFormat.Bmp.Guid, ImageFormat.Gif.Guid }.Contains(source.RawFormat.Guid) ||
                        source.GetFrameCount(new FrameDimension(source.FrameDimensionsList[0])) != 1)
                        throw new InvalidDataException("Wybierz nieruchome PNG, JPEG, BMP lub GIF o wymiarach do 4096 × 4096.");
                    var bitmap = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
                    try
                    {
                        using (var g = Graphics.FromImage(bitmap)) { g.CompositingMode = CompositingMode.SourceCopy; g.DrawImageUnscaled(source, 0, 0); }
                        return bitmap;
                    }
                    catch { bitmap.Dispose(); throw; }
                }
            }
        }

        internal static GraphicsPath Mask(PointF center, float radius, bool flat)
        {
            var vertices = new PointF[6];
            for (int i = 0; i < vertices.Length; i++)
            {
                double angle = (60 * i - (flat ? 0 : 30)) * Math.PI / 180;
                vertices[i] = new PointF(center.X + radius * (float)Math.Cos(angle), center.Y + radius * (float)Math.Sin(angle));
            }
            var mask = new GraphicsPath();
            mask.AddPolygon(vertices);
            return mask;
        }

        internal static void Draw(Graphics g, GraphicsPath mask, Bitmap bitmap, RectangleF square)
        {
            var state = g.Save();
            try
            {
                g.SetClip(mask, CombineMode.Intersect);
                g.DrawImage(bitmap, square);
            }
            finally { g.Restore(state); }
        }
    }

    public sealed partial class MainWindow
    {
        readonly Dictionary<string, Bitmap> terrainBitmaps = new Dictionary<string, Bitmap>();

        void ClearTerrainBitmaps()
        {
            foreach (var bitmap in terrainBitmaps.Values) bitmap.Dispose();
            terrainBitmaps.Clear();
        }

        void DrawTerrainTexture(Graphics g, GraphicsPath mask, PointF center, float radius, IconAsset asset)
        {
            Bitmap bitmap;
            if (!terrainBitmaps.TryGetValue(asset.Id, out bitmap))
            {
                if (terrainBitmaps.Count >= 128) ClearTerrainBitmaps();
                terrainBitmaps.Add(asset.Id, bitmap = TerrainTextures.Decode(asset));
            }
            TerrainTextures.Draw(g, mask, bitmap, new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2));
        }

        void CheckTextureBudget(IEnumerable<IconAsset> added)
        {
            TerrainTextures.Collect(map.Cells.Select(c => c.Texture)
                .Concat(map.Presets.SelectMany(p => p.Terrains).Select(t => t.Texture)).Concat(added));
        }
    }
}
