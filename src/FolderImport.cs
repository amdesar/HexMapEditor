using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace HexMapEditor
{
    internal enum ImportCategory { Backgrounds, Terrains, Icons, Objects }

    internal sealed class ImportCandidate
    {
        internal readonly ImportCategory Category;
        internal readonly string Path, SourceId;
        internal string Key { get { return Category + "/" + SourceId; } }
        internal string Name { get { return System.IO.Path.GetFileName(Path); } }
        internal ImportCandidate(ImportCategory category, string path, string sourceId)
        { Category = category; Path = path; SourceId = sourceId; }
    }

    internal sealed class FolderImport
    {
        internal static readonly ImportCategory[] Categories = (ImportCategory[])Enum.GetValues(typeof(ImportCategory));
        internal readonly string Root;
        readonly HashSet<string> skipped = new HashSet<string>();
        internal FolderImport(string library) { Root = Path.GetFullPath(library); }
        internal static FolderImport Current { get { return current.Value; } }
        static readonly Lazy<FolderImport> current = new Lazy<FolderImport>(() => new FolderImport(LibraryPaths.Root));

        internal string EnsureFolders()
        {
            string import = Path.Combine(Root, "Import");
            CreateOrdinaryDirectory(import);
            foreach (var category in Categories) CreateOrdinaryDirectory(Path.Combine(import, category.ToString()));
            return import;
        }
        static void CreateOrdinaryDirectory(string path)
        {
            Directory.CreateDirectory(path);
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Folder importu nie może być przekierowaniem: " + path);
        }
        string Catalog(ImportCategory category)
        {
            return category == ImportCategory.Terrains ? Path.Combine(Root, "Terrains", "Graphics") : Path.Combine(Root, category.ToString());
        }
        static string EntryPattern(ImportCategory category)
        {
            return category == ImportCategory.Terrains ? "terrain-*.json" :
                category == ImportCategory.Backgrounds ? "background-*.json" : "*.json";
        }
        HashSet<string> Registered(ImportCategory category)
        {
            var sources = new HashSet<string>();
            string catalog = Catalog(category);
            if (!Directory.Exists(catalog)) return sources;
            foreach (var file in Directory.EnumerateFiles(catalog, EntryPattern(category)))
            {
                try
                {
                    if (new FileInfo(file).Length > 3 * 1024 * 1024) continue;
                    var raw = MapLogic.Serializer().DeserializeObject(File.ReadAllText(file)) as Dictionary<string, object>;
                    if (raw == null) continue;
                    object value;
                    if (category == ImportCategory.Terrains || category == ImportCategory.Backgrounds)
                        raw = raw.TryGetValue(category == ImportCategory.Terrains ? "Asset" : "Settings", out value)
                            ? value as Dictionary<string, object> : null;
                    if (raw != null && raw.TryGetValue("SourceId", out value) && value is string)
                        sources.Add((string)value);
                }
                catch (Exception) { /* A single unreadable entry is not a queue failure. */ }
            }
            return sources;
        }
        internal List<ImportCandidate> Scan()
        {
            var found = new List<ImportCandidate>();
            string import = Path.Combine(Root, "Import");
            try { CreateOrdinaryDirectory(import); }
            catch (IOException) { return found; }
            catch (UnauthorizedAccessException) { return found; }
            foreach (var category in Categories)
            {
                try
                {
                    string folder = Path.Combine(import, category.ToString());
                    CreateOrdinaryDirectory(folder);
                    var seen = Registered(category);
                    foreach (var file in Directory.EnumerateFiles(folder).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var source = ReadInput(file, category);
                            if (!seen.Add(source.Id)) continue;
                            var item = new ImportCandidate(category, file, source.Id);
                            lock (skipped) if (!skipped.Contains(item.Key)) found.Add(item);
                        }
                        catch (Exception) { /* Skip an unreadable image and continue with its neighbors. */ }
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            return found;
        }
        static int Limit(ImportCategory category)
        {
            return category == ImportCategory.Icons || category == ImportCategory.Objects ? SquareLibraryAsset.InputBytes : GraphicSources.MaxFileBytes;
        }
        static GraphicSource ReadInput(string file, ImportCategory category)
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) throw new IOException("Pominięto przekierowanie pliku.");
            byte[] bytes = GraphicSources.ReadFile(file, Limit(category));
            if (bytes.Length < 3 || !(bytes[0] == 137 && bytes[1] == 80 || bytes[0] == 255 && bytes[1] == 216 ||
                bytes[0] == 66 && bytes[1] == 77 || bytes[0] == 71 && bytes[1] == 73))
                throw new InvalidDataException("Nieobsługiwane źródło obrazu.");
            var source = GraphicSources.Import(bytes);
            if (Limit(category) == SquareLibraryAsset.InputBytes && (source.Width > 2048 || source.Height > 2048))
                throw new InvalidDataException("Źródło ikony lub obiektu przekracza 2048 × 2048.");
            return source;
        }
        internal GraphicSource Read(ImportCandidate item)
        {
            CheckInputFolder(item);
            var source = ReadInput(item.Path, item.Category);
            if (source.Id != item.SourceId) throw new IOException("Treść pliku zmieniła się od skanu. Odszukaj nową pozycję po ponownym skanie.");
            return source;
        }
        void CheckInputFolder(ImportCandidate item)
        {
            string import = Path.Combine(Root,"Import");
            string category = Path.Combine(import,item.Category.ToString());
            if (!String.Equals(Path.GetDirectoryName(Path.GetFullPath(item.Path)),category,StringComparison.OrdinalIgnoreCase) ||
                (File.GetAttributes(import) & FileAttributes.ReparsePoint) != 0 ||
                (File.GetAttributes(category) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Plik nie znajduje się w zwykłym folderze importu tej kategorii.");
        }
        internal void Skip(ImportCandidate item) { lock (skipped) skipped.Add(item.Key); }

        internal string Commit(ImportCandidate item, GraphicSource source, string name, GraphicDefinition terrain, IconAsset square, MapBackground background)
        {
            if (source.Id != item.SourceId) throw new IOException("Kadr nie odpowiada treści z kolejki.");
            string catalog = Catalog(item.Category);
            Directory.CreateDirectory(catalog);
            if (Registered(item.Category).Contains(item.SourceId))
            {
                Skip(item);
                return "Źródło jest już w tej bibliotece. Plik wejściowy pozostawiono.";
            }
            string warning;
            switch (item.Category)
            {
                case ImportCategory.Terrains:
                    if (terrain == null || terrain.SourceId != source.Id) throw new InvalidDataException("Brak zapisanego kadru terenu.");
                    var terrains = TerrainGraphicLibrary.Load(catalog, out warning);
                    var graphic = TerrainGraphicLibrary.Prepare(catalog, null, name, terrain, terrains);
                    new TerrainGraphicChange { After = graphic }.Write(true);
                    break;
                case ImportCategory.Backgrounds:
                    if (background == null || background.SourceId != source.Id) throw new InvalidDataException("Brak zapisanego kadru tła.");
                    var backgrounds = BackgroundLibrary.Load(catalog, out warning);
                    var entry = BackgroundLibrary.Prepare(catalog, null, name, background, backgrounds);
                    new BackgroundFileChange { After = entry }.Write(true);
                    break;
                case ImportCategory.Icons:
                    CheckSquare(square,source);
                    var icons = IconLibrary.Load(catalog, out warning);
                    if (icons.Count >= IconAssets.MaxAssets) throw new IOException("Limit ikon w bibliotece.");
                    var icon = new LibraryIcon { Name = name, Asset = square,
                        FilePath = IconLibrary.Destination(catalog, name, null), LibraryOrder = LibrarySequence.Next(icons.Select(i => i.LibraryOrder)) };
                    icon.DiskBytes = IconLibrary.Metadata(icon);
                    new IconLibraryChange { After = icon, AfterBytes = icon.DiskBytes }.Write(true);
                    break;
                case ImportCategory.Objects:
                    CheckSquare(square,source);
                    var objects = ObjectLibrary.Load(catalog, out warning);
                    if (objects.Count >= GraphicLimits.LibraryEntries) throw new IOException("Limit wzorców obiektów.");
                    var template = new ObjectTemplate { Name = name, Asset = square,
                        FilePath = ObjectLibrary.Destination(catalog, name, null), LibraryOrder = LibrarySequence.Next(objects.Select(o => o.LibraryOrder)) };
                    template.DiskBytes = ObjectLibrary.Metadata(template);
                    new ObjectLibraryChange { After = template }.Write(true);
                    break;
            }
            Skip(item);
            try { CheckInputFolder(item); ImportFileCompletion.RemoveImported(item.Path, item.SourceId, Limit(item.Category)); }
            catch (Exception ex)
            { return "Zapisano w bibliotece. Plik wejściowy pozostawiono: " + ex.Message; }
            return null;
        }
        static void CheckSquare(IconAsset square, GraphicSource source)
        {
            if (square == null || square.LibrarySource == null || square.LibrarySource.Id != source.Id)
                throw new InvalidDataException("Brak zapisanego kadru kwadratowego.");
        }
    }

    internal static class ImportFileCompletion
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetFileInformationByHandle(SafeFileHandle file, int kind, ref byte info, uint size);

        internal static void RemoveImported(string path, string sourceId, int limit)
        {
            // Keep the verified file open without sharing write or delete until the OS marks that handle for removal.
            using (var handle = CreateFile(path, 0x80010000, 1, IntPtr.Zero, 3, 0x00200000, IntPtr.Zero))
            {
                if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
                using (var stream = new FileStream(handle, FileAccess.Read))
                {
                    if (stream.Length < 1 || stream.Length > limit) throw new IOException("Rozmiar pliku uległ zmianie.");
                    using (var sha = System.Security.Cryptography.SHA256.Create())
                        if ("source-" + BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() != sourceId)
                            throw new IOException("Treść pliku uległa zmianie w trakcie importu.");
                    byte delete = 1;
                    if (!SetFileInformationByHandle(handle, 4, ref delete, sizeof(byte)))
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }
    }
}
