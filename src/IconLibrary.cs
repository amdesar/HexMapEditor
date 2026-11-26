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
        internal string FilePath;
        internal byte[] DiskBytes;
        public LibraryIcon() { IconFormatVersion = 2; Id = Guid.NewGuid().ToString("N"); }
        internal LibraryIcon Copy() { return new LibraryIcon { Id = Id, Name = Name, Asset = Asset, LibraryOrder = LibraryOrder, FilePath = FilePath, DiskBytes = DiskBytes }; }
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
            if (icon.IconFormatVersion == 3) icon.Asset = SquareLibraryAsset.Read(Path.GetDirectoryName(path), raw);
            Validate(icon); icon.FilePath = Path.GetFullPath(path); icon.DiskBytes = bytes; return icon;
        }
        internal static byte[] Metadata(LibraryIcon entry)
        {
            Validate(entry);
            entry.IconFormatVersion = 3;
            return Encoding.UTF8.GetBytes(MapLogic.Serializer().Serialize(new {
                entry.IconFormatVersion, entry.Id, entry.Name, entry.LibraryOrder,
                SourceId = SquareLibraryAsset.SourceFor(entry.Asset).Id, AssetName = entry.Asset.Name, Crop = entry.Asset.LibraryCrop
            }));
        }
        public static string Destination(string directory, string name, string current)
        {
            string path = PresetFiles.Destination(directory, "icon-" + PresetFiles.Stem(name), current);
            if (!Path.GetFileName(path).StartsWith("icon-", StringComparison.Ordinal)) throw new IOException("Ścieżka biblioteki jest zbyt długa, aby zachować prefiks icon- i sufiks kolizji. Nie zmieniono plików.");
            return path;
        }
        public static List<LibraryIcon> Load(string directory, out string warning)
        {
            Directory.CreateDirectory(directory); string marker = Path.Combine(directory, ".starter-icons-v2");
            if (!File.Exists(marker))
            {
                var ids = new HashSet<string>(Directory.GetFiles(directory,"*.json").Select(SquareLibraryAsset.ReadStarterIdentity));
                foreach (var entry in StarterSets.Icons())
                {
                    if (ids.Contains(entry.Id)) continue;
                    byte[] metadata = Metadata(entry);
                    LibraryImageStore.Store(directory, SquareLibraryAsset.SourceFor(entry.Asset));
                    LibraryFiles.Transition(null, null, Destination(directory, entry.Name, null), metadata);
                }
                MapLogic.AtomicWrite(marker, "1", false);
            }
            var result = new List<LibraryIcon>(); var errors = new List<string>();
            foreach (string path in Directory.GetFiles(directory,"*.json").OrderBy(f=>f,StringComparer.OrdinalIgnoreCase)) try
            {
                var entry = Read(path); if (result.Any(i=>i.Id==entry.Id)) throw new InvalidDataException("Powtórzone logiczne ID ikony.");
                if (result.Count >= IconAssets.MaxAssets) throw new InvalidDataException("Limit biblioteki: 128 ikon."); result.Add(entry);
            }
            catch(Exception ex) { errors.Add(Path.GetFileName(path)+": "+ex.Message); }
            warning = String.Join(Environment.NewLine,errors); return LibrarySequence.Sort(result, i => i.LibraryOrder, i => i.Id);
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
                LibraryImageStore.Store(Path.GetDirectoryName(to.FilePath), SquareLibraryAsset.SourceFor(to.Asset));
            LibraryFiles.Transition(from == null ? null : from.FilePath, after ? BeforeBytes : AfterBytes,
                to == null ? null : to.FilePath, after ? AfterBytes : BeforeBytes);
        }
    }
}
