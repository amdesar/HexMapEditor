using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        readonly List<IconAsset> libraryIcons = new List<IconAsset>();
        readonly Dictionary<string, Bitmap> documentBitmaps = new Dictionary<string, Bitmap>(), libraryBitmaps = new Dictionary<string, Bitmap>();
        readonly Dictionary<int, LayerChange> layerBefore = new Dictionary<int, LayerChange>();
        readonly HashSet<int> strokeVisited = new HashSet<int>();
        bool iconMaterial, strokeIcons, strokeFog, strokeRemoveIcon, strokeFogValue, strokeStarted;
        string activeIcon, strokeIcon;
        IconAsset strokeAsset;
        BrushShape brushShape = BrushShape.Point;
        int brushSize = 1, brushHeight = 1, previewCell = -1;
        bool brushFilled = true;

        void ClearDocumentIcons() { foreach (var image in documentBitmaps.Values) image.Dispose(); documentBitmaps.Clear(); layerBefore.Clear(); strokeVisited.Clear(); previewCell = -1; iconMaterial = false; activeIcon = null; UpdateMaterialIndicator(); }
        Bitmap AssetBitmap(IconAsset asset, bool library)
        { var cache = library ? libraryBitmaps : documentBitmaps; Bitmap image; if (!cache.TryGetValue(asset.Id, out image)) { image = IconAssets.Decode(asset); cache.Add(asset.Id, image); } return image; }
        void LoadIcons()
        {
            try { string warning; var loaded = IconAssets.LoadLibrary(Path.Combine(LibraryPaths.Root, "Icons"), out warning); libraryIcons.Clear(); libraryIcons.AddRange(loaded); RefreshIconPreview(); if (!String.IsNullOrEmpty(warning)) ShowEditorMessage(warning, "Pominięte ikony", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            catch (Exception ex) { ShowEditorMessage("Nie można odczytać ikon: " + ex.Message, "Biblioteka ikon", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        void SelectIcon(string id)
        { EndCanvasGesture(true); if (!libraryIcons.Any(a => a.Id == id)) return; activeIcon = id; iconMaterial = true; RefreshIconSelection(); UpdateMaterialIndicator(); canvas.Invalidate(); }
        void UpdateMaterialIndicator()
        {
            if (materialLabel == null) return;
            var asset = libraryIcons.FirstOrDefault(a => a.Id == activeIcon);
            materialLabel.Text = iconMaterial && asset != null ? "Ikona: " + asset.Name : "Teren: " + (map == null ? "—" : ActivePreset().Terrains.Count == 0 ? "—" : ActivePreset().Terrains[Math.Min(brushIndex, ActivePreset().Terrains.Count - 1)].Name);
            tips.SetToolTip(materialLabel, materialLabel.Text); UpdateTerrainSelection();
        }
        Button IconButton(IconAsset asset, Action choose)
        {
            var source = AssetBitmap(asset, true); var thumbnail = new Bitmap(28, 28); float factor = Math.Min(28f / source.Width, 28f / source.Height);
            using (var g = Graphics.FromImage(thumbnail)) g.DrawImage(source, (28 - source.Width * factor) / 2, (28 - source.Height * factor) / 2, source.Width * factor, source.Height * factor);
            var button = new Button { Width = 46, Height = 32, Image = thumbnail, FlatStyle = FlatStyle.Flat, Tag = asset.Id, AccessibleName = "Ikona: " + asset.Name, Margin = new Padding(1) };
            StyleButton(button); tips.SetToolTip(button, asset.Name); button.Click += (s, e) => choose(); button.Disposed += (s, e) => { if (button.Image != null) button.Image.Dispose(); }; return button;
        }
        void RefreshIconPreview()
        {
            if (iconPreview == null) return;
            foreach (Control c in iconPreview.Controls.Cast<Control>().ToArray()) c.Dispose(); iconPreview.Controls.Clear();
            foreach (var asset in libraryIcons.Take(16)) { var a = asset; iconPreview.Controls.Add(IconButton(a, () => SelectIcon(a.Id))); }
            RefreshIconSelection(); UpdateMaterialIndicator();
        }
        void RefreshIconSelection()
        { if (iconPreview == null) return; foreach (Button b in iconPreview.Controls) { bool chosen = iconMaterial && (string)b.Tag == activeIcon; b.FlatAppearance.BorderSize = chosen ? 3 : 1; b.FlatAppearance.BorderColor = chosen ? Color.Gold : Color.Gray; } if (deleteIconButton != null) deleteIconButton.Enabled = activeIcon != null && libraryIcons.Any(a => a.Id == activeIcon); }
        void ImportIcon()
        {
            using (var dialog = new OpenFileDialog { Title = "Dodaj ikonę", Filter = "Bitmapy PNG/JPG/BMP/GIF|*.png;*.jpg;*.jpeg;*.bmp;*.gif", RestoreDirectory = true })
            {
                if (RunModal(() => dialog.ShowDialog(this)) != DialogResult.OK) return;
                try
                {
                    var asset = IconAssets.Import(dialog.FileName); string directory = Path.Combine(LibraryPaths.Root, "Icons"); Directory.CreateDirectory(directory);
                    if (!libraryIcons.Any(a => a.Id == asset.Id)) { if (libraryIcons.Count >= IconAssets.MaxAssets) throw new InvalidDataException("Limit 128 ikon w bibliotece."); MapLogic.AtomicWrite(Path.Combine(directory, asset.Id + ".json"), json.Serialize(asset), false); libraryIcons.Add(asset); }
                    RefreshIconPreview(); SelectIcon(asset.Id);
                }
                catch (Exception ex) { ShowEditorMessage("Import nie powiódł się; mapa nie została zmieniona.\n" + ex.Message, "Import ikony", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
        void DeleteLibraryIcon()
        {
            var asset = libraryIcons.FirstOrDefault(a => a.Id == activeIcon); if (asset == null) return;
            if (ShowEditorMessage("Usunąć ikonę „" + asset.Name + "” z biblioteki?\nMapy zachowają własny zasób. Undo mapy nie przywróci pliku biblioteki.", "Usuń ikonę", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            try
            {
                string directory = Path.Combine(LibraryPaths.Root, "Icons"); var files = Directory.GetFiles(directory, "*.json").Where(p => json.Deserialize<IconAsset>(File.ReadAllText(p)).Id == asset.Id).ToArray();
                if (files.Length != 1) throw new InvalidDataException("Nie można jednoznacznie wskazać pliku ikony.");
                RunModal(() => { RecycleFile.Send(files[0], Handle); return true; }); libraryIcons.Remove(asset); activeIcon = null; iconMaterial = false; RefreshIconPreview();
            }
            catch (Exception ex) { ShowEditorMessage(ex.Message, "Nie można usunąć ikony", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        void OpenIconLibrary()
        {
            using (var dialog = new Form { Text = "Biblioteka ikon — wybierz", Size = new Size(520, 420), MinimumSize = new Size(350, 240), StartPosition = FormStartPosition.CenterParent, BackColor = BackColor })
            {
                var list = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(12) }; dialog.Controls.Add(list);
                string chosen = null;
                foreach (var asset in libraryIcons) { var a = asset; var button = IconButton(a, () => { chosen = a.Id; dialog.DialogResult = DialogResult.OK; }); button.Width = 88; button.Height = 76; button.Text = a.Name; button.AutoEllipsis = true; button.TextImageRelation = TextImageRelation.ImageAboveText; list.Controls.Add(button); }
                if (RunModal(() => dialog.ShowDialog(this)) == DialogResult.OK && chosen != null) SelectIcon(chosen);
            }
        }
        MapData MapForSave()
        {
            var used = new HashSet<string>(map.Cells.Where(c => c.IconId != null).Select(c => c.IconId));
            return new MapData { FormatVersion = map.FormatVersion, Name = map.Name, Columns = map.Columns, Rows = map.Rows, Orientation = map.Orientation, LabelMode = map.LabelMode, Cells = map.Cells, Presets = map.Presets, ActivePresetId = map.ActivePresetId, RowParity = map.RowParity, ColumnParity = map.ColumnParity, IconAssets = map.IconAssets.Where(a => used.Contains(a.Id)).ToList() };
        }
        void BeginMaterialStroke(int start, EditorTool tool)
        {
            strokeStarted = true;
            strokeBefore.Clear(); layerBefore.Clear(); strokeVisited.Clear();
            strokeFog = tool == EditorTool.FogBrush || tool == EditorTool.FogBucket; strokeIcons = !strokeFog && iconMaterial;
            strokeAsset = libraryIcons.FirstOrDefault(a => a.Id == activeIcon); strokeIcon = strokeAsset == null ? null : strokeAsset.Id;
            strokeRemoveIcon = strokeIcons && strokeIcon != null && map.Cells[start].IconId == strokeIcon; strokeFogValue = !map.Cells[start].Fog;
        }
        void PaintMaterial(int index)
        {
            if (!strokeVisited.Add(index)) return;
            if (!strokeFog && !strokeIcons) { PaintCell(index); return; }
            var cell = map.Cells[index]; string nextIcon = strokeRemoveIcon ? (cell.IconId == strokeIcon ? null : cell.IconId) : strokeIcon;
            if (strokeFog ? cell.Fog == strokeFogValue : strokeIcon == null || cell.IconId == nextIcon) return;
            if (strokeIcons && !strokeRemoveIcon && !map.IconAssets.Any(a => a.Id == strokeIcon))
            {
                if (map.IconAssets.Count >= IconAssets.MaxAssets || map.IconAssets.Sum(a => (long)a.Png.Length) + strokeAsset.Png.Length > 44L * 1024 * 1024) { EndCanvasGesture(true); ShowEditorMessage("Limit zasobów ikon w sesji. Zapisz mapę i otwórz ponownie, aby zwolnić zasoby zachowane dla Undo.", "Limit ikon", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                map.IconAssets.Add(strokeAsset);
            }
            var change = new LayerChange { CellId = cell.Id, BeforeIcon = cell.IconId, BeforeFog = cell.Fog, Icons = strokeIcons, Fog = strokeFog, AfterIcon = nextIcon, AfterFog = strokeFogValue };
            layerBefore.Add(index, change); change.Apply(cell, true); dirty = true;
            if (index == selected && strokeFog) { editorCellId = null; UpdateUI(); } else canvas.Invalidate();
        }
        void PaintStamp(int start)
        { foreach (int index in BrushGeometry.Stamp(map, start, brushShape, brushSize, brushHeight, brushFilled)) PaintMaterial(index); }
        void DrawCellIcon(Graphics graphics, CellData cell, PointF center)
        {
            if (cell.IconId == null) return; var asset = map.IconAssets.First(a => a.Id == cell.IconId); var image = AssetBitmap(asset, false);
            float box = BaseRadius * scale * 1.20f, ratio = Math.Min(box / image.Width, box / image.Height); float width = image.Width * ratio, height = image.Height * ratio;
            graphics.DrawImage(image, center.X - width / 2, center.Y - height / 2, width, height);
        }
        void DrawBrushPreview(Graphics graphics)
        {
            if (previewCell < 0 || (currentTool != EditorTool.Brush && currentTool != EditorTool.FogBrush)) return;
            using (var pen = new Pen(Color.FromArgb(210, 130, 220, 255), 1.6f)) foreach (int i in BrushGeometry.Stamp(map, previewCell, brushShape, brushSize, brushHeight, brushFilled)) using (var path = HexPath(ScreenCenter(i % map.Columns, i / map.Columns), BaseRadius * scale * .94f)) graphics.DrawPath(pen, path);
        }
    }
}
