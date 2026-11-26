using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HexMapEditor
{
    internal static class SquareLibraryAsset
    {
        internal const int InputBytes = 8 * 1024 * 1024;
        internal static GraphicSource TakeBitmap(System.Drawing.Bitmap bitmap)
        {
            using (bitmap)
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return GraphicSources.Import(stream.ToArray());
            }
        }
        internal static GraphicSource SourceFor(IconAsset asset)
        { return asset.LibrarySource ?? GraphicSources.Import(Convert.FromBase64String(asset.Png)); }

        internal static IconAsset Read(string directory, Dictionary<string, object> raw, string file)
        {
            object id, name, crop;
            if (!raw.TryGetValue("SourceId", out id) || !raw.TryGetValue("AssetName", out name) || !raw.TryGetValue("Crop", out crop))
                throw new InvalidDataException("Brak metadanych źródła grafiki.");
            var source = LibraryImageStore.Find(directory, id as string, file);
            if (source == null) throw new InvalidDataException("Brakuje pliku źródłowego biblioteki.");
            if (crop == null) return IconAssets.FromPng(source, name as string);
            var values = crop as Dictionary<string, object>;
            if (values == null || !new[] { "X", "Y", "Zoom", "Angle" }.All(values.ContainsKey))
                throw new InvalidDataException("Niepełny kadr kwadratowy.");
            return Render(source, name as string, MapLogic.Serializer().ConvertToType<CropTransform>(values));
        }
        internal static IconAsset Render(GraphicSource source, string name, CropTransform crop)
        {
            if (crop == null || !new[] { crop.X, crop.Y, crop.Zoom, crop.Angle }.All(GraphicSources.Finite) ||
                crop.X < -4 || crop.X > 5 || crop.Y < -4 || crop.Y > 5 || crop.Zoom <= 0 || crop.Zoom > CropMath.MaxZoom ||
                crop.Angle < 0 || crop.Angle >= 360 || source.EncodedLength > GraphicLimits.Encoded(InputBytes) ||
                source.Width > 2048 || source.Height > 2048)
                throw new InvalidDataException("Nieprawidłowy kadr kwadratowy lub rozmiar źródła.");
            using (var bitmap = GraphicSources.Decode(source))
            using (var rendered = new SquareCropState(bitmap.Size, crop).Render(bitmap))
                return Prepare(rendered, source, name, crop);
        }
        internal static IconAsset Prepare(System.Drawing.Bitmap bitmap, GraphicSource source, string name, CropTransform crop)
        {
            var asset = IconAssets.FromBitmap(bitmap, name);
            using (IconAssets.Decode(asset)) { }
            asset.LibrarySource = source;
            asset.LibraryCrop = crop.Copy();
            return asset;
        }
    }
}
