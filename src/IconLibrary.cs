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
        internal string FilePath;
        internal byte[] DiskBytes;
        public LibraryIcon() { IconFormatVersion = 2; Id = Guid.NewGuid().ToString("N"); }
        internal LibraryIcon Copy() { return new LibraryIcon { Id = Id, Name = Name, Asset = Asset, FilePath = FilePath, DiskBytes = DiskBytes }; }
    }
    internal static class IconLibrary
    {
        public static void Validate(LibraryIcon icon)
        {
            if (icon == null || icon.IconFormatVersion != 2 || String.IsNullOrWhiteSpace(icon.Id) || icon.Id.Length > 200 || String.IsNullOrWhiteSpace(icon.Name) || icon.Name.Length > 200) throw new InvalidDataException("Nieprawidłowa pozycja biblioteki ikon.");
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
            Validate(icon); icon.FilePath = Path.GetFullPath(path); icon.DiskBytes = bytes; return icon;
        }
        public static string Destination(string directory, string name, string current)
        {
            string path = PresetFiles.Destination(directory, "icon-" + PresetFiles.Stem(name), current);
            if (!Path.GetFileName(path).StartsWith("icon-", StringComparison.Ordinal)) throw new IOException("Ścieżka biblioteki jest zbyt długa, aby zachować prefiks icon- i sufiks kolizji. Nie zmieniono plików.");
            return path;
        }
        public static List<LibraryIcon> Load(string directory, out string warning)
        {
            Directory.CreateDirectory(directory); string marker = Path.Combine(directory, ".starter-icons-v1");
            if (!File.Exists(marker))
            {
                int i = 0;
                foreach (var asset in IconAssets.Starters())
                {
                    var entry = new LibraryIcon { Id = "starter-" + i++, Name = asset.Name, Asset = asset };
                    bool present = Directory.GetFiles(directory,"*.json").Any(f => { try { return Read(f).Id == entry.Id; } catch { return false; } });
                    if (!present) MapLogic.AtomicWrite(Destination(directory, entry.Name, null), MapLogic.Serializer().Serialize(entry), false);
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
            warning = String.Join(Environment.NewLine,errors); return result;
        }
    }
    internal sealed class IconLibraryChange
    {
        public LibraryIcon Before, After;
        public byte[] BeforeBytes, AfterBytes;
        public string BeforeSelected, AfterSelected;
        public bool BeforeMaterial, AfterMaterial;
        public int Index;
        public void Write(bool after)
        { var from = after ? Before : After; var to = after ? After : Before; LibraryFiles.Transition(from == null ? null : from.FilePath, after ? BeforeBytes : AfterBytes, to == null ? null : to.FilePath, after ? AfterBytes : BeforeBytes); }
    }
}
