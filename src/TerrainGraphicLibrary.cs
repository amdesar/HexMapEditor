using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HexMapEditor
{
    public sealed class TerrainGraphic
    {
        public int GraphicFormatVersion { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public GraphicDefinition Asset { get; set; }
        public long LibraryOrder { get; set; }
        internal string Path;
        internal byte[] Bytes;
        public TerrainGraphic() { GraphicFormatVersion = 2; }
    }

    internal sealed class TerrainGraphicChange
    {
        internal TerrainGraphic Before, After;

        internal void Write(bool forward)
        {
            var from = forward ? Before : After;
            var to = forward ? After : Before;
            if (to != null)
            {
                TerrainGraphicLibrary.StoreSource(Path.GetDirectoryName(to.Path), to.Asset.Source);
                foreach (string path in TerrainGraphicLibrary.Files(Path.GetDirectoryName(to.Path)))
                {
                    if (from != null && String.Equals(path, from.Path, StringComparison.OrdinalIgnoreCase)) continue;
                    if (TerrainGraphicLibrary.ReadIdentity(path) == to.Id) throw new IOException("Inny plik ma już to samo ID grafiki: " + path);
                }
            }
            LibraryFiles.Transition(from == null ? null : from.Path, from == null ? null : from.Bytes,
                to == null ? null : to.Path, to == null ? null : to.Bytes);
        }
    }

    internal static class TerrainGraphicLibrary
    {
        internal static string[] Files(string directory) { return Directory.GetFiles(directory, "terrain-*.json"); }
        internal static string ReadIdentity(string path) { return PresetCatalog.ReadIdentity(path).Id; }

        internal static TerrainGraphic Read(string path, Dictionary<string, GraphicSource> sources = null)
        {
            if (new FileInfo(path).Length > 1024 * 1024) throw new InvalidDataException("Za duży plik grafiki terenu.");
            var bytes = File.ReadAllBytes(path);
            var serializer = MapLogic.Serializer();
            var raw = serializer.DeserializeObject(Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF')) as Dictionary<string, object>;
            if (raw == null || !raw.ContainsKey("Asset")) throw new InvalidDataException("Brak definicji kadrów.");
            GraphicSchema.Definition(raw["Asset"]);
            var item = serializer.ConvertToType<TerrainGraphic>(raw);
            if (item == null || item.GraphicFormatVersion != 2 || String.IsNullOrWhiteSpace(item.Id) || item.Id.Length > 200 ||
                String.IsNullOrWhiteSpace(item.Name) || item.Name.Length > 200 || item.LibraryOrder < 0)
                throw new InvalidDataException("Nieprawidłowe metadane grafiki terenu.");
            if (item.Asset == null) throw new InvalidDataException("Brak definicji kadrów.");
            GraphicSource shared;
            if (sources != null && sources.TryGetValue(item.Asset.SourceId, out shared)) item.Asset.Source = shared;
            else { item.Asset.Source = ReadSource(Path.GetDirectoryName(path), item.Asset.SourceId); if (sources != null) sources.Add(item.Asset.SourceId, item.Asset.Source); }
            TerrainTextures.Validate(item.Asset);
            item.Path = Path.GetFullPath(path);
            item.Bytes = bytes;
            return item;
        }
        static string SourcePath(string directory, string id)
        {
            if (id == null || !id.StartsWith("source-", StringComparison.Ordinal) || id.Length != 71 || !id.Substring(7).All(Uri.IsHexDigit))
                throw new InvalidDataException("Nieprawidłowy identyfikator źródła.");
            return Path.Combine(directory, "Sources", id + ".json");
        }
        internal static GraphicSource ReadSource(string directory, string id)
        {
            string path = SourcePath(directory, id);
            if (new FileInfo(path).Length > GraphicLimits.SourceJsonBytes) throw new InvalidDataException("Za duży plik źródła.");
            var source = MapLogic.Serializer().Deserialize<GraphicSource>(File.ReadAllText(path));
            if (source == null || source.Id != id) throw new InvalidDataException("Sprzeczny identyfikator źródła.");
            using (var bitmap = GraphicSources.Decode(source)) { }
            source.MakeLibraryReference(path); // Keep metadata in the catalog, pin immutable bytes only when selected.
            return source;
        }
        internal static void StoreSource(string directory, GraphicSource source)
        {
            string path = SourcePath(directory, source.Id);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (File.Exists(path))
            {
                if (!GraphicSources.Same(ReadSource(directory, source.Id), source)) throw new IOException("Konflikt bajtów źródła.");
                return;
            }
            var stored = new DirectoryInfo(Path.GetDirectoryName(path)).GetFiles("source-*.json");
            if (stored.Length >= GraphicSources.MaxSources || stored.Sum(f => f.Length) +
                source.EncodedLength > GraphicLimits.Encoded(GraphicLimits.LibrarySources) + GraphicLimits.SourceCount * 65536L)
                throw new IOException("Magazyn źródeł biblioteki osiągnął limit 64 obrazów / 192 MiB. Zachowano dotychczasowe wpisy i historię.");
            // Immutable content store. Undo removes the logical entry, retaining reusable source bytes.
            MapLogic.AtomicWrite(path, MapLogic.Serializer().Serialize(source), false);
        }

        internal static List<TerrainGraphic> Load(string directory, out string warning)
        {
            Directory.CreateDirectory(directory);
            var errors = new List<string>();
            var items = new List<TerrainGraphic>();
            var ids = new HashSet<string>();
            var sources = new Dictionary<string, GraphicSource>();
            foreach (var path in Files(directory).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var item = Read(path, sources);
                    if (items.Count >= GraphicLimits.LibraryEntries || !ids.Add(item.Id)) throw new InvalidDataException("Powtórzone ID lub przekroczony limit 128 grafik.");
                    GraphicSources.Collect(items.Select(e => e.Asset.Source).Concat(new[] { item.Asset.Source }), true);
                    items.Add(item);
                }
                catch (Exception ex) { errors.Add(Path.GetFileName(path) + ": " + ex.Message); }
            }
            warning = String.Join(Environment.NewLine, errors);
            return LibrarySequence.Sort(items, i => i.LibraryOrder, i => i.Id).ToList();
        }

        internal static TerrainGraphic Prepare(string directory, TerrainGraphic before, string name, GraphicDefinition asset, IEnumerable<TerrainGraphic> entries, bool allowSuffix = true)
        {
            if (before == null && entries.Count() >= GraphicLimits.LibraryEntries) throw new InvalidDataException("Limit 128 aktywnych grafik biblioteki.");
            TerrainTextures.Validate(asset);
            GraphicSources.Collect(entries.Where(e => before == null || e.Id != before.Id).Select(e => e.Asset.Source).Concat(new[] { asset.Source }), true);
            var path = PresetFiles.Destination(directory, "terrain-" + PresetFiles.Stem(name), before == null ? null : before.Path, allowSuffix);
            if (!Path.GetFileName(path).StartsWith("terrain-", StringComparison.Ordinal))
                throw new IOException("Za długa ścieżka biblioteki grafik terenów.");
            var item = new TerrainGraphic
            {
                Id = before == null ? Guid.NewGuid().ToString("N") : before.Id,
                Name = name, Asset = asset, Path = path,
                LibraryOrder = before == null ? LibrarySequence.Next(entries.Select(e => e.LibraryOrder)) : before.LibraryOrder
            };
            item.Bytes = Encoding.UTF8.GetBytes(MapLogic.Serializer().Serialize(item));
            return item;
        }
    }

    public sealed partial class MainWindow
    {
        List<TerrainGraphic> terrainGraphics;

        void ApplyTerrainGraphic(TerrainGraphicChange change, bool forward)
        {
            change.Write(forward);
            var from = forward ? change.Before : change.After;
            var to = forward ? change.After : change.Before;
            if (from != null) terrainGraphics.RemoveAll(item => item.Id == from.Id);
            if (to != null) terrainGraphics.Add(to);
            terrainGraphics = LibrarySequence.Sort(terrainGraphics, i => i.LibraryOrder, i => i.Id).ToList();
        }

        void ChangeTerrainGraphic(TerrainGraphic before, TerrainGraphic after)
        {
            var change = new TerrainGraphicChange { Before = before, After = after };
            var action = new EditAction { Label = after == null ? "Usuń grafikę terenu" : "Zapisz grafikę terenu", TerrainGraphicChange = change };
            ApplyTerrainGraphic(change, true);
            PushAction(action);
            UpdateUI();
        }

        GraphicDefinition ChooseTerrainGraphic(IWin32Window owner, bool import)
        {return ChooseTerrainGraphicCore(owner,import,false);}
        void ManageTerrainGraphics(){RunModal(()=>ChooseTerrainGraphicCore(this,false,true));}
        GraphicDefinition ChooseTerrainGraphicCore(IWin32Window owner, bool import, bool manage)
        {
            try
            {
                if (terrainGraphics == null)
                {
                    string warning;
                    terrainGraphics = TerrainGraphicLibrary.Load(LibraryPaths.TerrainGraphics, out warning);
                    if (warning.Length != 0) MessageBox.Show(owner, warning, "Pominięte grafiki terenów", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                if (import)
                {
                    var entry = ImportTerrainGraphic(owner);
                    return entry == null ? null : RememberGraphic(entry);
                }
                using (var picker = new TerrainGraphicPicker(() => terrainGraphics, map.Orientation == "FlatTop", manage))
                {
                    picker.Import = () => ImportTerrainGraphic(picker);
                    picker.Edit = item => EditTerrainGraphic(picker, item);
                    picker.Delete = item =>
                    {
                        if (MessageBox.Show(picker, "Usunąć grafikę „" + item.Name + "” z biblioteki? Mapy i presety zachowają osadzone grafiki. Usunięcie omija Kosz i można je cofnąć w historii tej sesji.",
                            "Usuń grafikę terenu", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                            ChangeTerrainGraphic(item, null);
                    };
                    return picker.ShowDialog(owner) == DialogResult.OK ? RememberGraphic(picker.Selected) : null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, ex.Message, "Biblioteka grafik terenów", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
        }

        TerrainGraphic ImportTerrainGraphic(IWin32Window owner)
        {
            var asset = CropTerrainFile(owner);
            if (asset == null) return null;
            return CommitGraphicImport(asset, (existing, proposed) =>
            {
                using (var dialog = new GraphicDuplicateDialog(existing, proposed)) return dialog.ShowDialog(owner);
            }, LibraryPaths.TerrainGraphics);
        }
        TerrainGraphic CommitGraphicImport(GraphicDefinition asset, Func<TerrainGraphic, GraphicDefinition, DialogResult> decide, string directory)
        {
            var duplicate = FindGraphicDuplicate(terrainGraphics, asset);
            if (duplicate != null)
            {
                var decision = decide(duplicate, asset);
                if (decision == DialogResult.Yes) return duplicate;
                if (decision != DialogResult.No) return null;
            }
            if (terrainGraphics.Count >= GraphicLimits.LibraryEntries) throw new InvalidOperationException("Biblioteka mieści najwyżej 128 grafik terenów.");
            string name = asset.Name;
            for (int suffix = 2; terrainGraphics.Any(g => PresetCatalog.SameName(g.Name, name)); suffix++) name = asset.Name + " (" + suffix + ")";
            var item = TerrainGraphicLibrary.Prepare(directory, null, name, asset, terrainGraphics);
            ChangeTerrainGraphic(null, item);
            return item;
        }
        internal static TerrainGraphic FindGraphicDuplicate(IEnumerable<TerrainGraphic> entries, GraphicDefinition graphic)
        {
            return entries.FirstOrDefault(entry => entry.Asset.SourceId == graphic.SourceId &&
                entry.Asset.Id == graphic.Id && TerrainTextures.Same(entry.Asset, graphic));
        }

        GraphicDefinition CropTerrainFile(IWin32Window owner)
        {
            using (var file = new OpenFileDialog { Filter = "Grafiki (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif", RestoreDirectory = true })
            {
                if (file.ShowDialog(owner) != DialogResult.OK) return null;
                string name = Path.GetFileNameWithoutExtension(file.FileName).Trim();
                if (name.Length == 0) name = "Grafika terenu";
                var source = GraphicSources.Import(file.FileName);
                var graphic = new GraphicDefinition { Source = source, SourceId = source.Id, Name = name.Substring(0, Math.Min(200, name.Length)) };
                graphic.Seal();
                return OpenTerrainCrop(owner, graphic, true);
            }
        }

        GraphicDefinition ShowTerrainCrop(IWin32Window owner, GraphicDefinition graphic)
        {try{
            if(terrainGraphics==null){string warning;terrainGraphics=TerrainGraphicLibrary.Load(LibraryPaths.TerrainGraphics,out warning);if(warning.Length>0)MessageBox.Show(owner,warning,"Pominięte grafiki terenów",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
            var changed=OpenTerrainCrop(owner,graphic,false);if(changed==null||TerrainTextures.Same(changed,graphic))return changed;
            GraphicOrigin origin;var targets=graphicOrigins.TryGetValue(graphic,out origin)?terrainGraphics.Where(t=>t.Id==origin.Id).ToArray():terrainGraphics.Where(t=>TerrainTextures.Same(t.Asset,graphic)).ToArray();
            if(targets.Length==0)return changed;var saved=SaveCroppedGraphic(owner,targets,changed,targets[0].Name);return saved==null?null:RememberGraphic(saved);
        }catch(Exception ex){MessageBox.Show(owner,ex.Message,"Nie można zapisać kadru",MessageBoxButtons.OK,MessageBoxIcon.Warning);return null;}}
        GraphicDefinition OpenTerrainCrop(IWin32Window owner, GraphicDefinition graphic, bool newImport)
        {
            using (var crop = new TerrainCropDialog(graphic, map.Orientation == "FlatTop", newImport))
                return crop.ShowDialog(owner) == DialogResult.OK ? crop.Result : null;
        }

        TerrainGraphic EditTerrainGraphic(IWin32Window owner, TerrainGraphic item)
        {
            using (var dialog = new TerrainGraphicEditDialog(item, map.Orientation == "FlatTop"))
            {
                dialog.ChooseImage = useCrop =>
                {
                    if (!useCrop) return CropTerrainFile(dialog);
                    return OpenTerrainCrop(dialog, dialog.Asset, false);
                };
                if (dialog.ShowDialog(owner) != DialogResult.OK) return item;
                if (dialog.GraphicName == item.Name && dialog.Asset.Id == item.Asset.Id) return item;
                if(dialog.Asset.Id!=item.Asset.Id)return SaveCroppedGraphic(owner,new[]{item},dialog.Asset,dialog.GraphicName)??item;
                var next = TerrainGraphicLibrary.Prepare(LibraryPaths.TerrainGraphics, item, dialog.GraphicName, dialog.Asset, terrainGraphics);
                ChangeTerrainGraphic(item, next);
                return next;
            }
        }
    }
}
