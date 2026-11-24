using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace HexMapEditor
{
    internal static class ClipboardRaster
    {
        internal static void ValidateDimensions(int width, int height, int side, long pixels)
        {
            if (width < 1 || height < 1 || width > side || height > side || (long)width * height > pixels)
                throw new ImageBudgetException("Obraz przekracza limit boku " + side + " px lub " + pixels + " pikseli.");
        }

        internal static byte[] Read(IDataObject data, int bytesLimit, int side, long pixels)
        {
            var encoded = ReadEncoded(data, bytesLimit);
            if (encoded != null) return encoded;
            var bitmap = ReadBitmap(data);
            return bitmap == null ? null : Encode(bitmap, bytesLimit, side, pixels);
        }

        internal static byte[] ReadEncoded(IDataObject data, int bytesLimit)
        {
            if (data == null) return null;
            foreach (string format in new[] { "PNG", "JFIF" })
            {
                if (!data.GetDataPresent(format, false)) continue;
                var value = data.GetData(format, false);
                var bytes = value as byte[];
                var stream = value as MemoryStream;
                long length = bytes != null ? bytes.Length : stream != null ? stream.Length : 0;
                if (length > bytesLimit) throw new ImageBudgetException("Zakodowany obraz ze schowka przekracza limit bajtów.");
                if (length > 0) return bytes ?? stream.ToArray();
            }
            return null;
        }

        internal static BitmapSource ReadBitmap(IDataObject data)
        {
            return data != null && data.GetDataPresent(DataFormats.Bitmap, true)
                ? data.GetData(DataFormats.Bitmap, true) as BitmapSource : null;
        }

        internal static byte[] Encode(BitmapSource bitmap, int bytesLimit, int side, long pixels)
        {
            ValidateDimensions(bitmap.PixelWidth, bitmap.PixelHeight, side, pixels);
            using (var stream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
                if (stream.Length > bytesLimit) throw new ImageBudgetException("PNG ze schowka przekracza limit bajtów.");
                return stream.ToArray();
            }
        }
    }

    internal sealed class ImageBudgetException : IOException
    {
        internal ImageBudgetException(string message) : base(message) { }
    }
}
