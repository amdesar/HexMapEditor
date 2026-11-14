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
    public sealed class IconAsset
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Png { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
    internal static class IconAssets
    {
        public const int MaxAssets = 128, MaxBytes = 2 * 1024 * 1024;
        static string Id(byte[] data) { using (var hash = SHA256.Create()) return "icon-" + BitConverter.ToString(hash.ComputeHash(data)).Replace("-", "").ToLowerInvariant(); }
        public static IconAsset FromBitmap(Bitmap image, string name)
        {
            using (var stream = new MemoryStream()) { image.Save(stream, ImageFormat.Png); byte[] bytes = stream.ToArray(); return new IconAsset { Id = Id(bytes), Name = name, Png = Convert.ToBase64String(bytes), Width = image.Width, Height = image.Height }; }
        }
        public static Bitmap Decode(IconAsset asset)
        {
            if (asset == null || String.IsNullOrWhiteSpace(asset.Name) || asset.Name.Length > 200 || asset.Png == null || asset.Png.Length > MaxBytes * 4 / 3 + 8 || asset.Width < 1 || asset.Height < 1 || asset.Width > 512 || asset.Height > 512) throw new InvalidDataException("Nieprawidłowy zasób ikony lub przekroczony limit 512×512 / 2 MiB.");
            byte[] bytes = Convert.FromBase64String(asset.Png);
            if (bytes.Length > MaxBytes || bytes.Length < 24 || bytes[0] != 137 || bytes[1] != 80 || bytes[2] != 78 || bytes[3] != 71 || Id(bytes) != asset.Id) throw new InvalidDataException("Uszkodzony PNG lub identyfikator zasobu ikony.");
            // Check PNG dimensions before the decoder allocates image memory.
            Func<int, long> integer = p => ((long)bytes[p] << 24) | ((long)bytes[p + 1] << 16) | ((long)bytes[p + 2] << 8) | bytes[p + 3];
            if (integer(16) != asset.Width || integer(20) != asset.Height) throw new InvalidDataException("Sprzeczne wymiary PNG.");
            using (var stream = new MemoryStream(bytes)) using (var source = Image.FromStream(stream, true, true))
            { if (source.Width != asset.Width || source.Height != asset.Height) throw new InvalidDataException("Nieprawidłowe wymiary ikony."); return new Bitmap(source); }
        }
        internal static Bitmap ReadImport(string path)
        {
            if(new FileInfo(path).Length>8*1024*1024)throw new InvalidDataException("Import: plik może mieć najwyżej 8 MiB.");
            using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))using(var image=Image.FromStream(stream,true,false)){
                if(image is Metafile||image.Width>2048||image.Height>2048||image.Width<1||image.Height<1)throw new InvalidDataException("Import: bitmapa do2048×2048; bez metaplików i SVG.");
                if(!new[]{ImageFormat.Png.Guid,ImageFormat.Jpeg.Guid,ImageFormat.Bmp.Guid,ImageFormat.Gif.Guid}.Contains(image.RawFormat.Guid)||image.GetFrameCount(new FrameDimension(image.FrameDimensionsList[0]))!=1)throw new InvalidDataException("Obsługiwane są nieruchome PNG, JPG, BMP i GIF.");
                return new Bitmap(image);
            }
        }
        public static IconAsset Import(string path)
        {
            using(var image=ReadImport(path)){float factor=Math.Min(1f,512f/Math.Max(image.Width,image.Height));using(var bitmap=new Bitmap(Math.Max(1,(int)(image.Width*factor)),Math.Max(1,(int)(image.Height*factor)),PixelFormat.Format32bppArgb)){
                using(var g=Graphics.FromImage(bitmap)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(image,new Rectangle(0,0,bitmap.Width,bitmap.Height));}string name=Path.GetFileNameWithoutExtension(path);return FromBitmap(bitmap,name.Substring(0,Math.Min(200,name.Length)));
            }}
        }
        public static void Validate(IEnumerable<IconAsset> assets)
        {
            if (assets == null) throw new InvalidDataException("Brak kolekcji zasobów ikon.");
            var ids = new HashSet<string>(); long size = 0;
            foreach (var asset in assets) { if (ids.Count >= MaxAssets || asset == null || !ids.Add(asset.Id)) throw new InvalidDataException("Powtórzone ikony lub limit 128 zasobów."); using (var bitmap = Decode(asset)) { } size += asset.Png.Length; if (size > 44L * 1024 * 1024) throw new InvalidDataException("Ikony przekraczają wspólny limit około 32 MiB PNG."); }
        }
        public static List<IconAsset> Starters()
        {
            var list = new List<IconAsset>();
            foreach (string name in new[] { "Krzyż", "Namiot", "Wieża", "Czaszka", "Gwiazda" }) using (var b = new Bitmap(128, 128))
            {
                using (var g = Graphics.FromImage(b)) using (var fill = new SolidBrush(Color.WhiteSmoke)) using (var edge = new Pen(Color.FromArgb(25, 28, 36), 7))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    if (name == "Krzyż") { g.FillRectangle(fill, 48, 12, 32, 104); g.FillRectangle(fill, 14, 45, 100, 30); g.DrawRectangle(edge, 48, 12, 32, 104); g.DrawRectangle(edge, 14, 45, 100, 30); }
                    if (name == "Namiot") { var p = new[] { new Point(64, 14), new Point(115, 108), new Point(13, 108) }; g.FillPolygon(fill, p); g.DrawPolygon(edge, p); g.FillPolygon(Brushes.Black, new[] { new Point(64, 55), new Point(86, 108), new Point(42, 108) }); }
                    if (name == "Wieża") { g.FillRectangle(fill, 30, 35, 68, 80); for (int x = 24; x <= 88; x += 32) g.FillRectangle(fill, x, 14, 20, 40); g.DrawRectangle(edge, 30, 35, 68, 80); g.FillRectangle(Brushes.Black, 53, 76, 22, 39); }
                    if (name == "Czaszka") { g.FillEllipse(fill, 18, 12, 92, 82); g.FillRectangle(fill, 37, 65, 54, 48); g.DrawEllipse(edge, 18, 12, 92, 82); g.FillEllipse(Brushes.Black, 34, 40, 22, 26); g.FillEllipse(Brushes.Black, 73, 40, 22, 26); for (int x = 46; x <= 82; x += 18) g.DrawLine(edge, x, 91, x, 113); }
                    if (name == "Gwiazda") { var p = Enumerable.Range(0, 10).Select(i => { double a = (i * 36 - 90) * Math.PI / 180; float r = i % 2 == 0 ? 55 : 24; return new PointF(64 + r * (float)Math.Cos(a), 64 + r * (float)Math.Sin(a)); }).ToArray(); g.FillPolygon(fill, p); g.DrawPolygon(edge, p); }
                }
                list.Add(FromBitmap(b, name));
            }
            return list;
        }
    }
    internal sealed class LayerChange
    {
        public string CellId, BeforeIcon, AfterIcon;
        public string BeforeEntry, AfterEntry, BeforeName, AfterName;
        public bool BeforeFog, AfterFog, Icons, Fog;
        public FillVariant BeforeFill, AfterFill;
        public bool FogAppearance;
        public bool Dark, BeforeDark, AfterDark, DarkAppearance;
        public FillVariant BeforeDarkFill, AfterDarkFill;
        public void Apply(CellData cell, bool after) { if (Icons) { cell.IconId = after ? AfterIcon : BeforeIcon; cell.IconEntryId = after ? AfterEntry : BeforeEntry; cell.IconName = after ? AfterName : BeforeName; } if(Dark)cell.Darkness=after?AfterDark:BeforeDark;if(DarkAppearance)cell.DarkFill=(after?AfterDarkFill:BeforeDarkFill).Copy(); if (Fog) cell.Fog = after ? AfterFog : BeforeFog; if (FogAppearance) cell.FogFill = (after ? AfterFill : BeforeFill).Copy(); }
    }
}
