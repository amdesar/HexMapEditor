using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace HexMapEditor
{
    internal static class LibraryImageStore
    {
        static readonly string[] Extensions = { ".png", ".jpg", ".bmp", ".gif" };

        static string Sources(string catalog)
        {
            var path = new DirectoryInfo(Path.GetFullPath(catalog));
            if (path.Parent != null && new[] { "Icons", "Objects", "Backgrounds" }.Contains(path.Name, StringComparer.OrdinalIgnoreCase))
                path = path.Parent;
            else if (path.Parent != null && path.Parent.Parent != null &&
                path.Name.Equals("Graphics", StringComparison.OrdinalIgnoreCase) && path.Parent.Name.Equals("Terrains", StringComparison.OrdinalIgnoreCase))
                path = path.Parent.Parent;
            return Path.Combine(path.FullName, "Sources");
        }
        static string Extension(byte[] bytes)
        {
            if (bytes.Length >= 8 && bytes[0] == 137 && bytes[1] == 80 && bytes[2] == 78 && bytes[3] == 71) return ".png";
            if (bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255) return ".jpg";
            if (bytes.Length >= 6 && bytes[0] == 71 && bytes[1] == 73 && bytes[2] == 70) return ".gif";
            if (bytes.Length >= 2 && bytes[0] == 66 && bytes[1] == 77) return ".bmp";
            throw new InvalidDataException("Magazyn grafik obsługuje PNG, JPEG, GIF i BMP.");
        }
        internal static void ValidateId(string id)
        {
            if (id == null || !id.StartsWith("source-", StringComparison.Ordinal) || id.Length != 71 || !id.Substring(7).All(Uri.IsHexDigit))
                throw new InvalidDataException("Nieprawidłowy identyfikator źródła.");
        }
        internal static GraphicSource Find(string catalog, string id)
        {
            ValidateId(id);
            string root = Sources(catalog);
            foreach (string suffix in Extensions)
            {
                string path = Path.Combine(root, id + suffix);
                if (!File.Exists(path)) continue;
                var bytes = GraphicSources.ReadFile(path, GraphicSources.MaxFileBytes);
                if (GraphicSources.Id(bytes) != id || Extension(bytes) != suffix) throw new InvalidDataException("Zmieniono plik źródłowy biblioteki.");
                var source = GraphicSources.Import(bytes);
                source.MakeLibraryReference(path, true);
                return source;
            }
            return null;
        }
        internal static void Store(string catalog, GraphicSource source)
        {
            using (GraphicSources.Decode(source)) { }
            byte[] bytes = Convert.FromBase64String(source.Data);
            string root = Sources(catalog);
            string target = Path.Combine(root, source.Id + Extension(bytes));
            if (source.BinaryLibraryFile && String.Equals(source.LibraryFile, target, StringComparison.OrdinalIgnoreCase)) return;
            Directory.CreateDirectory(root);
            if (!File.Exists(target))
            {
                try { LibraryFiles.Transition(null, null, target, bytes); return; }
                catch (IOException) { if (!File.Exists(target)) throw; }
            }
            if (!bytes.SequenceEqual(GraphicSources.ReadFile(target, GraphicSources.MaxFileBytes)))
                throw new IOException("Konflikt bajtów źródła. Nie nadpisano pliku.");
        }
    }
}
