using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HexMapEditor
{
    public sealed class LibraryIcon
    {
        public int IconFormatVersion { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public IconAsset Asset { get; set; }
        public long LibraryOrder { get; set; }
        internal string FilePath, SourceFile;
        internal byte[] DiskBytes;
        public LibraryIcon() { IconFormatVersion = 2; Id = Guid.NewGuid().ToString("N"); }
        internal LibraryIcon Copy() { return (LibraryIcon)MemberwiseClone(); }
    }
    internal static class IconLibrary
    {
        public static void Validate(LibraryIcon icon)
        {
            if (icon == null || (icon.IconFormatVersion != 2 && icon.IconFormatVersion != 3) || String.IsNullOrWhiteSpace(icon.Id) || icon.Id.Length > 200 || String.IsNullOrWhiteSpace(icon.Name) || icon.Name.Length > 200) throw new InvalidDataException("Nieprawidłowa pozycja biblioteki ikon.");
            using (var bitmap = IconAssets.Decode(icon.Asset)) { }
        }
        public static LibraryIcon Read(string path)
        {
            if (new FileInfo(path).Length > 3 * 1024 * 1024) throw new InvalidDataException("Za duży plik ikony.");
            byte[] bytes = File.ReadAllBytes(path); string text; using (var stream = new MemoryStream(bytes)) using (var reader = new StreamReader(stream, Encoding.UTF8, true)) text = reader.ReadToEnd();
            var json = MapLogic.Serializer(); var raw = json.DeserializeObject(text) as Dictionary<string, object>;
            if (raw == null) throw new InvalidDataException("Nieprawidłowy plik ikony.");
            LibraryIcon icon;
            if (raw.ContainsKey("IconFormatVersion")) icon = json.ConvertToType<LibraryIcon>(raw);
            else { var asset = json.ConvertToType<IconAsset>(raw); icon = new LibraryIcon { Id = asset.Id, Name = asset.Name, Asset = asset }; }
            if (icon.IconFormatVersion == 3)
            {
                icon.SourceFile = LibraryImageStore.ReadReference(raw);
                icon.Asset = SquareLibraryAsset.Read(LibraryMetadata.Catalog(path), raw, icon.SourceFile);
            }
            Validate(icon); icon.FilePath = Path.GetFullPath(path); icon.DiskBytes = bytes; return icon;
        }
        internal static byte[] Metadata(LibraryIcon entry, LibraryIcon before = null)
        {
            Validate(entry);
            if (before != null && before.IconFormatVersion == 2 && ReferenceEquals(entry.Asset, before.Asset))
                return Encoding.UTF8.GetBytes(MapLogic.Serializer().Serialize(entry));
            var source = SquareLibraryAsset.SourceFor(entry.Asset);
            entry.SourceFile = before != null && before.IconFormatVersion == 3 && before.Asset.LibrarySource.Id == source.Id
                ? before.SourceFile : LibraryImageStore.Store(LibraryMetadata.Catalog(entry.FilePath), entry.Name, source);
            entry.IconFormatVersion = 3;
            return Encoding.UTF8.GetBytes(MapLogic.Serializer().Serialize(new {
                entry.IconFormatVersion, entry.Id, entry.Name, entry.LibraryOrder,
                SourceId = source.Id, entry.SourceFile, AssetName = entry.Asset.Name, Crop = entry.Asset.LibraryCrop
            }));
        }
        public static string Destination(string directory, string name, string current)
        {
            string path = LibraryMetadata.Destination(directory, "icon-" + PresetFiles.Stem(name), current);
            if (!Path.GetFileName(path).StartsWith("icon-", StringComparison.Ordinal)) throw new IOException("Ścieżka biblioteki jest zbyt długa, aby zachować prefiks icon- i sufiks kolizji. Nie zmieniono plików.");
            return path;
        }
        public static List<LibraryIcon> Load(string directory, out string warning)
        {
            var errors = new List<string>();
            var result = ReadCatalog(directory, errors);
            string marker = Path.Combine(directory, ".starter-icons-v2");
            if (!File.Exists(marker))
            {
                try
                {
                    var ids = new HashSet<string>(LibraryMetadata.Files(directory,"*.json").Select(LibraryMetadata.ReadIdentity));
                    foreach (var entry in StarterSets.Icons())
                    {
                        if (ids.Contains(entry.Id)) continue;
                        entry.FilePath = Destination(directory, entry.Name, null);
                        LibraryFiles.Transition(null, null, entry.FilePath, Metadata(entry));
                        if (result.Count < IconAssets.MaxAssets) result.Add(Read(entry.FilePath));
                    }
                    MapLogic.AtomicWrite(marker, "1", false);
                }
                catch (IOException error) { errors.Add(error.Message); }
                catch (UnauthorizedAccessException error) { errors.Add(error.Message); }
            }
            warning = String.Join(Environment.NewLine,errors); return LibrarySequence.Sort(result, i => i.LibraryOrder, i => i.Id);
        }
        static List<LibraryIcon> ReadCatalog(string directory, List<string> errors)
        {
            return LibraryMetadata.Read(directory,"*.json",Read,i=>i.Id,(i,path)=>i.FilePath=path,IconAssets.MaxAssets,errors);
        }
    }
    internal sealed class IconLibraryChange
    {
        internal QuickLibraryState RemovedLists;
        public LibraryIcon Before, After;
        public byte[] BeforeBytes, AfterBytes;
        public string BeforeSelected, AfterSelected;
        public bool BeforeMaterial, AfterMaterial;
        public bool BeforeObjectMaterial, BeforeNoMaterial, AfterNoMaterial;
        public int Index;
        public void Write(bool after)
        {
            var from = after ? Before : After;
            var to = after ? After : Before;
            if (to != null && to.IconFormatVersion == 3)
                LibraryImageStore.VerifySquare(to.FilePath, SquareLibraryAsset.SourceFor(to.Asset), to.SourceFile);
            LibraryFiles.Transition(from == null ? null : from.FilePath, after ? BeforeBytes : AfterBytes,
                to == null ? null : to.FilePath, after ? AfterBytes : BeforeBytes);
        }
    }
}
