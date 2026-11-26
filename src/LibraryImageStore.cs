using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace HexMapEditor
{
    internal static class LibraryImageStore
    {
        static readonly string[] Extensions = { ".png", ".jpg", ".bmp", ".gif" };
        static readonly Regex OriginalStem = new Regex(@"-original(?:-[0-9]+)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

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
        internal static string ReadReference(Dictionary<string, object> metadata)
        {
            object value;
            if (!metadata.TryGetValue("SourceFile", out value) || value == null) return null;
            var file = value as string;
            if (!IsOriginalName(file)) throw new InvalidDataException("Nieprawidłowe odwołanie do oryginału w bibliotece.");
            return file;
        }

        static bool IsOriginalName(string file)
        {
            if (String.IsNullOrEmpty(file) || file.Length > 255 || file.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                file.Any(Char.IsControl)) return false;
            string stem = Path.GetFileNameWithoutExtension(file);
            return OriginalStem.IsMatch(stem) && PresetFiles.Stem(stem) == stem &&
                Extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase);
        }

        internal static void RequireOrdinaryPath(string path)
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Magazyn oryginałów nie może wskazywać na przekierowanie: " + path);
        }

        internal static void CheckCatalog(string directory)
        {
            RequireOrdinaryPath(directory);
            var info = new DirectoryInfo(directory);
            if (info.Name.Equals("Graphics", StringComparison.OrdinalIgnoreCase) && info.Parent != null &&
                info.Parent.Name.Equals("Terrains", StringComparison.OrdinalIgnoreCase))
                RequireOrdinaryPath(info.Parent.FullName);
        }

        internal static GraphicSource Find(string catalog, string id, string file = null)
        {
            ValidateId(id);
            catalog = Path.GetFullPath(catalog);
            CheckCatalog(catalog);
            if (file != null)
            {
                if (!IsOriginalName(file)) throw new InvalidDataException("Nieprawidłowe odwołanie do oryginału w bibliotece.");
                return Read(Path.Combine(catalog, file), id);
            }
            string root = Sources(catalog);
            if (!Directory.Exists(root)) return null;
            RequireOrdinaryPath(root);
            foreach (string suffix in Extensions)
            {
                string path = Path.Combine(root, id + suffix);
                if (File.Exists(path)) return Read(path, id);
            }
            return null;
        }

        static GraphicSource Read(string path, string id)
        {
            RequireOrdinaryPath(path);
            var bytes = GraphicSources.ReadFile(path, GraphicSources.MaxFileBytes);
            if (GraphicSources.Id(bytes) != id || !Path.GetExtension(path).Equals(Extension(bytes), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Zmieniono plik źródłowy biblioteki.");
            var source = GraphicSources.Import(bytes);
            source.MakeLibraryReference(path, true);
            return source;
        }

        internal static void VerifySquare(string entryPath, GraphicSource source, string file)
        {
            if (Find(LibraryMetadata.Catalog(entryPath), source.Id, file) == null)
                throw new InvalidDataException("Brakuje pliku źródłowego biblioteki.");
        }

        internal static string Store(string catalog, string name, GraphicSource source)
        {
            using (GraphicSources.Decode(source)) { }
            byte[] bytes = Convert.FromBase64String(source.Data);
            string extension = Extension(bytes);
            catalog = Path.GetFullPath(catalog);
            CheckCatalog(catalog);
            string mutexName = @"Local\HexMapEditor.Originals." + MapLogic.Hash(catalog.ToUpperInvariant());
            using (var mutex = new Mutex(false, mutexName))
            {
                try { mutex.WaitOne(); }
                catch (AbandonedMutexException) { }
                try { return WriteOriginal(catalog, name, extension, bytes); }
                finally { mutex.ReleaseMutex(); }
            }
        }

        static string WriteOriginal(string catalog, string name, string extension, byte[] bytes)
        {
            var occupied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in Directory.EnumerateFileSystemEntries(catalog).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                string file = Path.GetFileName(path);
                occupied.Add(Path.GetFileNameWithoutExtension(file));
                if (!IsOriginalName(file) || !extension.Equals(Path.GetExtension(file), StringComparison.OrdinalIgnoreCase)) continue;
                try
                {
                    if ((File.GetAttributes(path) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0 ||
                        new FileInfo(path).Length != bytes.Length) continue;
                    if (bytes.SequenceEqual(GraphicSources.ReadFile(path, GraphicSources.MaxFileBytes))) return file;
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            string stem = PresetFiles.Stem(String.IsNullOrWhiteSpace(name) ? "Grafika" : name);
            int limit = Math.Min(120, 210 - catalog.Length - 1 - extension.Length);
            for (int number = 1; number <= 10000; number++)
            {
                string suffix = "-original" + (number == 1 ? "" : "-" + number.ToString(CultureInfo.InvariantCulture));
                if (limit - suffix.Length < 2) throw new IOException("Ścieżka katalogu oryginałów jest zbyt długa.");
                string candidate = PresetFiles.Shorten(stem, limit - suffix.Length) + suffix;
                if (occupied.Contains(candidate)) continue;
                string file = candidate + extension;
                string target = Path.Combine(catalog, file);
                try { LibraryFiles.Transition(null, null, target, bytes); return file; }
                catch (IOException) { if (!File.Exists(target) && !Directory.Exists(target)) throw; }
            }
            throw new IOException("Zbyt wiele kolizji nazw oryginałów. Nie nadpisano żadnego pliku.");
        }
    }
}
