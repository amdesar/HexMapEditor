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
        public string Id { get; set; }
        public string Name { get; set; }
        public IconAsset Asset { get; set; }
        public long LibraryOrder { get; set; }
        internal string Path;
        internal byte[] Bytes;
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
                foreach (string path in TerrainGraphicLibrary.Files(Path.GetDirectoryName(to.Path)))
                {
                    if (from != null && String.Equals(path, from.Path, StringComparison.OrdinalIgnoreCase)) continue;
                    if (TerrainGraphicLibrary.Read(path).Id == to.Id) throw new IOException("Inny plik ma już to samo ID grafiki: " + path);
                }
            }
            LibraryFiles.Transition(from == null ? null : from.Path, from == null ? null : from.Bytes,
                to == null ? null : to.Path, to == null ? null : to.Bytes);
        }
    }

    internal static class TerrainGraphicLibrary
    {
        internal static string[] Files(string directory) { return Directory.GetFiles(directory, "terrain-*.json"); }

        internal static TerrainGraphic Read(string path)
        {
            if (new FileInfo(path).Length > 1024 * 1024) throw new InvalidDataException("Za duży plik grafiki terenu.");
            var bytes = File.ReadAllBytes(path);
            var item = MapLogic.Serializer().Deserialize<TerrainGraphic>(Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF'));
            if (item == null || String.IsNullOrWhiteSpace(item.Id) || item.Id.Length > 200 ||
                String.IsNullOrWhiteSpace(item.Name) || item.Name.Length > 200 || item.LibraryOrder < 0)
                throw new InvalidDataException("Nieprawidłowe metadane grafiki terenu.");
            using (var image = TerrainTextures.Decode(item.Asset)) { }
            item.Path = Path.GetFullPath(path);
            item.Bytes = bytes;
            return item;
        }

        internal static List<TerrainGraphic> Load(string directory, out string warning)
        {
            Directory.CreateDirectory(directory);
            var errors = new List<string>();
            var items = new List<TerrainGraphic>();
            var ids = new HashSet<string>();
            foreach (var path in Files(directory).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var item = Read(path);
                    if (items.Count >= 128 || !ids.Add(item.Id)) throw new InvalidDataException("Powtórzone ID lub przekroczony limit 128 grafik.");
                    items.Add(item);
                }
                catch (Exception ex) { errors.Add(Path.GetFileName(path) + ": " + ex.Message); }
            }
            warning = String.Join(Environment.NewLine, errors);
            return LibrarySequence.Sort(items, i => i.LibraryOrder, i => i.Id).ToList();
        }

        internal static TerrainGraphic Prepare(string directory, TerrainGraphic before, string name, IconAsset asset, IEnumerable<TerrainGraphic> entries)
        {
            var path = PresetFiles.Destination(directory, "terrain-" + PresetFiles.Stem(name), before == null ? null : before.Path);
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

        IconAsset ChooseTerrainGraphic(TerrainDialog owner, bool import)
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
                    return entry == null ? null : entry.Asset;
                }
                using (var picker = new TerrainGraphicPicker(() => terrainGraphics, map.Orientation == "FlatTop"))
                {
                    picker.Import = () => ImportTerrainGraphic(picker);
                    picker.Edit = item => EditTerrainGraphic(picker, item);
                    picker.Delete = item =>
                    {
                        if (MessageBox.Show(picker, "Usunąć grafikę „" + item.Name + "” z biblioteki? Mapy i presety zachowają osadzone grafiki. Usunięcie omija Kosz i można je cofnąć w historii tej sesji.",
                            "Usuń grafikę terenu", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                            ChangeTerrainGraphic(item, null);
                    };
                    return picker.ShowDialog(owner) == DialogResult.OK ? picker.Selected.Asset : null;
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
            if (terrainGraphics.Count >= 128) throw new InvalidOperationException("Biblioteka mieści najwyżej 128 grafik terenów.");
            var asset = CropTerrainFile(owner);
            if (asset == null) return null;
            var item = TerrainGraphicLibrary.Prepare(LibraryPaths.TerrainGraphics, null, asset.Name, asset, terrainGraphics);
            ChangeTerrainGraphic(null, item);
            return item;
        }

        IconAsset CropTerrainFile(IWin32Window owner)
        {
            using (var file = new OpenFileDialog { Filter = "Grafiki (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif", RestoreDirectory = true })
            {
                if (file.ShowDialog(owner) != DialogResult.OK) return null;
                string name = Path.GetFileNameWithoutExtension(file.FileName).Trim();
                if (name.Length == 0) name = "Grafika terenu";
                using (var bitmap = TerrainTextures.ReadSource(file.FileName))
                    return ShowTerrainCrop(owner, bitmap, name.Substring(0, Math.Min(200, name.Length)));
            }
        }

        IconAsset ShowTerrainCrop(IWin32Window owner, Bitmap bitmap, string name)
        {
            using (var crop = new TerrainCropDialog(bitmap, name, map.Orientation == "FlatTop"))
                return crop.ShowDialog(owner) == DialogResult.OK ? crop.Result : null;
        }

        TerrainGraphic EditTerrainGraphic(IWin32Window owner, TerrainGraphic item)
        {
            using (var dialog = new TerrainGraphicEditDialog(item, map.Orientation == "FlatTop"))
            {
                dialog.ChooseImage = useCrop =>
                {
                    if (!useCrop) return CropTerrainFile(dialog);
                    using (var image = TerrainTextures.Decode(dialog.Asset))
                        return ShowTerrainCrop(dialog, image, dialog.GraphicName.Length == 0 ? item.Name : dialog.GraphicName);
                };
                if (dialog.ShowDialog(owner) != DialogResult.OK) return item;
                if (dialog.GraphicName == item.Name && dialog.Asset.Id == item.Asset.Id) return item;
                var next = TerrainGraphicLibrary.Prepare(LibraryPaths.TerrainGraphics, item, dialog.GraphicName, dialog.Asset, terrainGraphics);
                ChangeTerrainGraphic(item, next);
                return next;
            }
        }
    }
}
