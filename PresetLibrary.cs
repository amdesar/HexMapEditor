using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace HexMapEditor
{
    // Migration is separate from read-only catalog loading and from map history.
    internal static class PresetLibrary
    {
        public static string DirectoryFor(string terrains) { return Path.Combine(terrains, "Presets"); }
        static string ByteHash(byte[] bytes) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        internal static string ContentKey(TerrainPreset p)
        {
            return MapLogic.Serializer().Serialize(p.Terrains.Select(t => new { t.TerrainId, t.VariantId, t.Revision, t.Name, Color = t.Color.ToUpperInvariant(), t.FillKind }).ToArray());
        }
        static bool SamePreset(TerrainPreset a, TerrainPreset b) { return a.Id == b.Id && a.Name == b.Name && ContentKey(a) == ContentKey(b); }
        static string Backup(string source, string directory, byte[] original)
        {
            string backups = Path.Combine(directory, "_backups"); Directory.CreateDirectory(backups);
            string path = Path.Combine(backups, Path.GetFileName(source) + "." + ByteHash(original).Substring(0, 16) + ".bak");
            if (!File.Exists(path)) using (var f = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { f.Write(original, 0, original.Length); f.Flush(true); }
            if (!File.ReadAllBytes(path).SequenceEqual(original)) throw new IOException("Kopia bezpieczeństwa różni się od źródła: " + path);
            return path;
        }
        static void Retire(string source, byte[] original)
        {
            if (!File.ReadAllBytes(source).SequenceEqual(original)) throw new IOException("Plik zmienił się podczas migracji; zachowano źródło: " + source);
            // All bytes already exist in a verified backup and the active destination.
            File.Delete(source);
        }
        public static List<TerrainPreset> LoadAndMigrate(string terrains, out string warning, out List<string> actions)
        {
            string directory = DirectoryFor(terrains); Directory.CreateDirectory(directory); actions = new List<string>(); var errors = new List<string>();
            var candidates = Directory.GetFiles(terrains, "*.terrainset.json", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(directory, "*.terrainset.json", SearchOption.TopDirectoryOnly).Where(f => {
                    try { return PresetCatalog.ReadFile(f).Id == PresetCatalog.DefaultId; } catch { return false; }
                })).OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (string source in candidates)
            {
                try
                {
                    byte[] original = File.ReadAllBytes(source); var preset = PresetCatalog.ReadFile(source);
                    // Read all active files strictly: an unreadable file might conceal an ID/name collision.
                    var records = Directory.GetFiles(directory, "*.terrainset.json", SearchOption.TopDirectoryOnly).Where(f => !String.Equals(f, source, StringComparison.OrdinalIgnoreCase))
                        .Select(f => new { Path = f, Preset = PresetCatalog.ReadFile(f) }).ToArray();
                    string destination = null;
                    if (preset.Id == PresetCatalog.DefaultId)
                    {
                        var equivalent = records.Where(r => r.Preset.Id != PresetCatalog.DefaultId && ContentKey(r.Preset) == ContentKey(preset)).ToArray();
                        if (equivalent.Length > 1) throw new InvalidDataException("Kilka równoważnych kopii: wybór wymaga decyzji użytkownika; źródło pozostawiono.");
                        if (equivalent.Length == 1) destination = equivalent[0].Path;
                        else
                        {
                            var recovered = preset.Copy(); recovered.Id = "recovered-" + MapLogic.Hash(ContentKey(preset));
                            if (records.Any(r => r.Preset.Id == recovered.Id)) throw new InvalidDataException("Konflikt ID odzyskiwanego zestawu; źródło pozostawiono.");
                            string baseName = preset.Name + " — odzyskany"; recovered.Name = baseName; int suffix = 2;
                            while (PresetCatalog.SameName(recovered.Name, PresetCatalog.Default().Name) || records.Any(r => PresetCatalog.SameName(r.Preset.Name, recovered.Name))) recovered.Name = baseName + " (" + suffix++ + ")";
                            string backup = Backup(source, directory, original);
                            PresetCatalog.Write(recovered, directory, false); destination = PresetCatalog.FindFile(directory, recovered.Id);
                            if (!SamePreset(PresetCatalog.ReadFile(destination), recovered)) throw new IOException("Nie udało się potwierdzić odzyskanego pliku.");
                            Retire(source, original); actions.Add(source + " -> " + destination + "; kopia bajtowa: " + backup); continue;
                        }
                    }
                    else
                    {
                        var sameId = records.Where(r => r.Preset.Id == preset.Id).ToArray();
                        if (sameId.Length > 1 || sameId.Any(r => !SamePreset(r.Preset, preset))) throw new InvalidDataException("Konflikt ID z różną zawartością; źródło pozostawiono.");
                        if (sameId.Length == 1) destination = sameId[0].Path;
                        if (records.Any(r => r.Preset.Id != preset.Id && PresetCatalog.SameName(r.Preset.Name, preset.Name)) || PresetCatalog.SameName(preset.Name, PresetCatalog.Default().Name)) throw new InvalidDataException("Konflikt nazwy; źródło pozostawiono do decyzji użytkownika.");
                    }
                    string savedBackup = Backup(source, directory, original);
                    if (destination == null)
                    {
                        destination = Path.Combine(directory, Path.GetFileName(source));
                        if (File.Exists(destination)) throw new IOException("Docelowa nazwa pliku jest zajęta; źródło pozostawiono.");
                        // A normal move preserves even the exact source encoding/whitespace.
                        if (!File.ReadAllBytes(source).SequenceEqual(original)) throw new IOException("Źródło zmieniło się podczas migracji.");
                        File.Move(source, destination);
                    }
                    else Retire(source, original);
                    actions.Add(source + " -> " + destination + "; kopia bajtowa: " + savedBackup);
                }
                catch (Exception ex) { errors.Add(source + ": " + ex.Message); }
            }
            string loadWarning; var loaded = PresetCatalog.Load(directory, out loadWarning); if (!String.IsNullOrEmpty(loadWarning)) errors.Add(loadWarning);
            warning = String.Join(Environment.NewLine, errors); return loaded;
        }
    }
}
