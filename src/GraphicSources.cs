using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace HexMapEditor
{
    public sealed class GraphicSource
    {
        public string Id { get; set; }
        public string Data { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public sealed class CropTransform
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Zoom { get; set; }
        public double Angle { get; set; }
        public CropTransform() { X = Y = .5; Zoom = 1; }
        public CropTransform Copy() { return (CropTransform)MemberwiseClone(); }
    }

    public sealed class GraphicDefinition
    {
        public string Id { get; set; }
        public string SourceId { get; set; }
        public string Name { get; set; }
        public CropTransform Pointy { get; set; }
        public CropTransform Flat { get; set; }
        public bool Linked { get; set; }
        internal GraphicSource Source;
        public GraphicDefinition() { Pointy = new CropTransform(); Flat = new CropTransform(); Linked = true; }
        internal GraphicDefinition Copy()
        {
            return new GraphicDefinition { Id = Id, SourceId = SourceId, Source = Source, Name = Name,
                Pointy = Pointy.Copy(), Flat = Flat.Copy(), Linked = Linked };
        }
        internal void Seal()
        {
            SourceId = Source.Id;
            Id = "graphic-" + MapLogic.Hash(MapLogic.Serializer().Serialize(new { SourceId, Pointy, Flat, Linked }));
        }
    }

    internal static class GraphicSources
    {
        internal const int MaxFileBytes = 16 * 1024 * 1024;
        internal const long MaxTotalBytes = 64L * 1024 * 1024;
        internal const int MaxSources = 64;
        internal static string Id(byte[] bytes)
        {
            using (var hash = SHA256.Create())
                return "source-" + BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        internal static bool Finite(double value) { return !Double.IsNaN(value) && !Double.IsInfinity(value); }
        internal static bool Same(GraphicSource a, GraphicSource b)
        {
            if (a.Id != b.Id || a.Width != b.Width || a.Height != b.Height || a.Data.Length != b.Data.Length) return false;
            var x = Convert.FromBase64String(a.Data);
            var y = Convert.FromBase64String(b.Data);
            return x.Length == y.Length && x.SequenceEqual(y);
        }
        internal static GraphicSource Import(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length < 1 || stream.Length > MaxFileBytes) throw new InvalidDataException("Źródło musi mieć od 1 bajta do 16 MiB.");
                var bytes = new byte[(int)stream.Length];
                int read = 0, count;
                while (read < bytes.Length && (count = stream.Read(bytes, read, bytes.Length - read)) > 0) read += count;
                if (read != bytes.Length) throw new IOException("Nie odczytano całego obrazu.");
                var source = new GraphicSource { Id = Id(bytes), Data = Convert.ToBase64String(bytes) };
                using (var image = ReadImage(bytes))
                {
                    source.Width = image.Width;
                    source.Height = image.Height;
                }
                return source;
            }
        }
        static Bitmap ReadImage(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            using (var image = Image.FromStream(stream, true, true))
            {
                if (image is Metafile || image.Width < 1 || image.Height < 1 || image.Width > 4096 || image.Height > 4096 ||
                    !new[] { ImageFormat.Png.Guid, ImageFormat.Jpeg.Guid, ImageFormat.Bmp.Guid, ImageFormat.Gif.Guid }.Contains(image.RawFormat.Guid) ||
                    image.GetFrameCount(new FrameDimension(image.FrameDimensionsList[0])) != 1)
                    throw new InvalidDataException("Wybierz nieruchomy PNG, JPEG, BMP lub GIF do 4096 × 4096.");
                return new Bitmap(image);
            }
        }
        internal static Bitmap Decode(GraphicSource source)
        {
            if (source == null || source.Data == null || source.Data.Length > MaxFileBytes * 4 / 3 + 8 ||
                source.Width < 1 || source.Height < 1 || source.Width > 4096 || source.Height > 4096)
                throw new InvalidDataException("Nieprawidłowe źródło grafiki.");
            byte[] bytes = Convert.FromBase64String(source.Data);
            if (bytes.Length > MaxFileBytes || Id(bytes) != source.Id) throw new InvalidDataException("Uszkodzona tożsamość źródła grafiki.");
            var image = ReadImage(bytes);
            if (image.Width == source.Width && image.Height == source.Height) return image;
            image.Dispose();
            throw new InvalidDataException("Sprzeczne wymiary obrazu.");
        }
        internal static List<GraphicSource> Collect(IEnumerable<GraphicSource> sources)
        {
            var result = new Dictionary<string, GraphicSource>();
            foreach (var source in sources.Where(s => s != null))
            {
                GraphicSource previous;
                if (source.Id == null) throw new InvalidDataException("Brak ID źródła.");
                if (result.TryGetValue(source.Id, out previous))
                {
                    if (!Same(previous, source)) throw new InvalidDataException("Różne źródła o tym samym SHA-256.");
                }
                else result.Add(source.Id, source);
            }
            if (result.Count > MaxSources || result.Values.Sum(s => (long)s.Data.Length) > MaxTotalBytes * 4 / 3)
                throw new InvalidDataException("Budżet źródeł to 64 obrazy i 64 MiB pełnych danych.");
            return result.Values.ToList();
        }
        internal static Dictionary<string, GraphicSource> ReadTable(List<GraphicSource> sources)
        {
            if (sources == null) throw new InvalidDataException("Brak tabeli źródeł.");
            var unique = Collect(sources);
            if (unique.Count != sources.Count) throw new InvalidDataException("Powtórzone źródło w tabeli.");
            foreach (var source in unique) using (var image = Decode(source)) { }
            return unique.ToDictionary(s => s.Id);
        }
    }

    internal static class CropMath
    {
        internal const double MaxZoom = 24;
        internal static double Angle(double value) { return (value % 360 + 360) % 360; }
        internal static double Cover(double angle)
        {
            double radians = angle * Math.PI / 180;
            return Math.Abs(Math.Cos(radians)) + Math.Abs(Math.Sin(radians));
        }
        internal static void Clamp(CropTransform crop, GraphicSource source)
        {
            crop.Angle = Angle(crop.Angle);
            crop.Zoom = Math.Max(Cover(crop.Angle), Math.Min(MaxZoom, crop.Zoom));
            double extent = Math.Min(source.Width, source.Height) * Cover(crop.Angle) / (2 * crop.Zoom);
            double x = extent / source.Width, y = extent / source.Height;
            crop.X = Math.Max(x, Math.Min(1 - x, crop.X));
            crop.Y = Math.Max(y, Math.Min(1 - y, crop.Y));
        }
        internal static IEnumerable<CropTransform> Targets(GraphicDefinition graphic, bool flat)
        {
            return graphic.Linked ? new[] { graphic.Pointy, graphic.Flat } : new[] { flat ? graphic.Flat : graphic.Pointy };
        }
        internal static void Zoom(GraphicDefinition graphic, bool flat, double factor)
        {
            var targets = Targets(graphic, flat).ToArray();
            double minimum = targets.Max(c => Cover(c.Angle) / c.Zoom);
            double maximum = targets.Min(c => MaxZoom / c.Zoom);
            factor = Math.Max(minimum, Math.Min(maximum, factor));
            foreach (var crop in targets) { crop.Zoom *= factor; Clamp(crop, graphic.Source); }
        }
        internal static void Rotate(GraphicDefinition graphic, bool flat, double delta)
        {
            var targets = Targets(graphic, flat).ToArray();
            foreach (var crop in targets) crop.Angle = Angle(crop.Angle + delta);
            double factor = Math.Max(1, targets.Max(c => Cover(c.Angle) / c.Zoom));
            foreach (var crop in targets) { crop.Zoom *= factor; Clamp(crop, graphic.Source); }
        }
        internal static void Pan(GraphicDefinition graphic, bool flat, double x, double y)
        {
            var targets = Targets(graphic, flat).ToArray();
            // A common normalized displacement stops when either crop reaches its boundary.
            double fraction = 1;
            foreach (var crop in targets)
            {
                var next = crop.Copy(); next.X += x; next.Y += y; Clamp(next, graphic.Source);
                if (x != 0) fraction = Math.Min(fraction, Math.Max(0, (next.X - crop.X) / x));
                if (y != 0) fraction = Math.Min(fraction, Math.Max(0, (next.Y - crop.Y) / y));
            }
            foreach (var crop in targets) { crop.X += x * fraction; crop.Y += y * fraction; Clamp(crop, graphic.Source); }
        }
        internal static void Validate(CropTransform crop, GraphicSource source)
        {
            if (crop == null || !new[] { crop.X, crop.Y, crop.Zoom, crop.Angle }.All(GraphicSources.Finite) ||
                crop.Angle < 0 || crop.Angle >= 360 || crop.Zoom < 1 || crop.Zoom > MaxZoom)
                throw new InvalidDataException("Nieprawidłowe parametry kadru.");
            var copy = crop.Copy(); Clamp(copy, source);
            if (Math.Abs(copy.X - crop.X) > 1e-8 || Math.Abs(copy.Y - crop.Y) > 1e-8 || Math.Abs(copy.Zoom - crop.Zoom) > 1e-8)
                throw new InvalidDataException("Kadr wykracza poza źródło.");
        }
        internal static void Draw(Graphics graphics, Bitmap image, CropTransform crop, RectangleF target)
        {
            var state = graphics.Save();
            try
            {
                graphics.SetClip(target, CombineMode.Intersect);
                graphics.TranslateTransform(target.X + target.Width / 2, target.Y + target.Height / 2);
                graphics.RotateTransform((float)crop.Angle);
                float factor = (float)(target.Width / Math.Min(image.Width, image.Height) * crop.Zoom);
                graphics.ScaleTransform(factor, factor);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(image, new RectangleF((float)(-crop.X * image.Width), (float)(-crop.Y * image.Height), image.Width, image.Height));
            }
            finally { graphics.Restore(state); }
        }
    }
}
