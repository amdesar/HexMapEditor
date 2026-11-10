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
        readonly List<LibraryIcon> libraryIcons = new List<LibraryIcon>();
        readonly Dictionary<string, Bitmap> documentBitmaps = new Dictionary<string, Bitmap>(), libraryBitmaps = new Dictionary<string, Bitmap>();
        readonly Dictionary<int, LayerChange> layerBefore = new Dictionary<int, LayerChange>();
        readonly HashSet<int> strokeVisited = new HashSet<int>();
        readonly HashSet<int> bucketProcessed = new HashSet<int>();
        HashSet<int> protectedFogCells=new HashSet<int>();
        RegionState[] bucketOrigin;
        PointF? previousStrokePoint;
        bool iconMaterial, strokeIcons, strokeFog, strokeRemoveIcon, strokeFogValue, strokeStarted;
        string activeIcon, strokeIcon, strokeEntry, strokeName;
        IconAsset strokeAsset;
        BrushShape brushShape = BrushShape.Point;
        int brushSize = 1, brushHeight = 1, previewCell = -1;
        bool brushFilled = true;

        void ClearDocumentIcons() { foreach (var image in documentBitmaps.Values) image.Dispose(); documentBitmaps.Clear(); layerBefore.Clear(); strokeVisited.Clear(); previewCell = -1; iconMaterial = false; activeIcon = null; UpdateMaterialIndicator(); }
        Bitmap AssetBitmap(IconAsset asset, bool library)
        { var cache = library ? libraryBitmaps : documentBitmaps; Bitmap image; if (!cache.TryGetValue(asset.Id, out image)) { image = IconAssets.Decode(asset); cache.Add(asset.Id, image); } return image; }
        void LoadIcons()
        {
            try { string warning; var loaded = IconLibrary.Load(Path.Combine(LibraryPaths.Root, "Icons"), out warning); libraryIcons.Clear(); libraryIcons.AddRange(loaded); RefreshIconPreview(); if (!String.IsNullOrEmpty(warning)) ShowEditorMessage(warning, "Pominięte ikony", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            catch (Exception ex) { ShowEditorMessage("Nie można odczytać ikon: " + ex.Message, "Biblioteka ikon", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        void SelectIcon(string id)
        { EndCanvasGesture(true); if (!libraryIcons.Any(a => a.Id == id)) return; activeIcon = id; iconMaterial = true; objectMaterial=false;noMaterial=false; MaterialChosen(); RefreshIconSelection(); UpdateMaterialIndicator(); canvas.Invalidate(); }
        void UpdateMaterialIndicator()
        {
            UpdateTerrainSelection(); RefreshIconSelection(); RefreshObjectSelection(); UpdateBrushAvailability();
        }
        Button IconButton(LibraryIcon asset, Action choose)
        {
            var source = AssetBitmap(asset.Asset, true); var thumbnail = new Bitmap(28, 28); float factor = Math.Min(28f / source.Width, 28f / source.Height);
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
        { if (iconPreview == null) return; foreach (Button b in iconPreview.Controls) { bool chosen = iconMaterial && (string)b.Tag == activeIcon; b.FlatAppearance.BorderSize = chosen ? 3 : 1; b.FlatAppearance.BorderColor = chosen ? Color.Gold : Color.Gray; } if (deleteIconButton != null) deleteIconButton.Enabled = iconMaterial && activeIcon != null && libraryIcons.Any(a => a.Id == activeIcon); }
        void ImportIcon()
        {
            using(var choice=new GraphicSourceDialog("Dodaj ikonę","obiektów")){
                if(RunModal(()=>choice.ShowDialog(this))!=DialogResult.OK)return;
                if(choice.FromLibrary){var graphic=PickObjectGraphic(this);if(graphic!=null){string error;if(!TryChangeLibraryIcon(null,new LibraryIcon{Name=graphic.Name,Asset=graphic.Asset},false,Path.Combine(LibraryPaths.Root,"Icons"),out error))ShowEditorMessage(error,"Nie można dodać ikony",MessageBoxButtons.OK,MessageBoxIcon.Error);}return;}
            }
            using (var dialog = new OpenFileDialog { Title = "Dodaj ikonę", Filter = "Bitmapy PNG/JPG/BMP/GIF|*.png;*.jpg;*.jpeg;*.bmp;*.gif", RestoreDirectory = true })
            {
                if (RunModal(() => dialog.ShowDialog(this)) != DialogResult.OK) return;
                try
                {
                    var asset = IconAssets.Import(dialog.FileName); string error;
                    if (!TryChangeLibraryIcon(null, new LibraryIcon { Name = asset.Name, Asset = asset }, false, Path.Combine(LibraryPaths.Root,"Icons"), out error)) throw new IOException(error);
                }                catch (Exception ex) { ShowEditorMessage("Import nie powiódł się; mapa nie została zmieniona.\n" + ex.Message, "Import ikony", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
        void DeleteLibraryIcon()
        {
            var entry = iconMaterial ? libraryIcons.FirstOrDefault(a => a.Id == activeIcon) : null; if (entry == null) return;
            if (ShowEditorMessage("Usunąć ikonę „" + entry.Name + "” z biblioteki?\nMapy zachowają swoje zasoby. Undo w tej sesji przywróci pozycję i plik z kopii w pamięci.\nPlik nie trafia do Kosza. Nowa/Otwórz lub zamknięcie programu usuwa tę historię.", "Usuń ikonę", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            string error; if (!TryChangeLibraryIcon(entry, null, false, Path.GetDirectoryName(entry.FilePath), out error)) ShowEditorMessage(error,"Nie można usunąć ikony",MessageBoxButtons.OK,MessageBoxIcon.Error);
        }
        void EditLibraryIcon()
        {
            var entry = iconMaterial ? libraryIcons.FirstOrDefault(a => a.Id == activeIcon) : null;
            if (entry == null) { ShowEditorMessage("Zaznacz ikonę", "Edytuj ikonę", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using (var d = new IconEditDialog(entry, PickLibraryIcon, PickObjectGraphic))
            {
                d.CanAccept = () => !d.UpdateMap || !map.Cells.Any(c => c.IconId != null && (c.IconEntryId ?? c.IconId) == entry.Id) || GuardLayer(MapLayer.Icons, d);
                if (RunModal(() => d.ShowDialog(this)) != DialogResult.OK) return;
                var next = entry.Copy(); next.Name = d.IconName; next.Asset = d.Asset; string error;
                clearAfterIconEdit=true;
                try { if (!TryChangeLibraryIcon(entry,next,d.UpdateMap,Path.GetDirectoryName(entry.FilePath),out error)) ShowEditorMessage(error,"Nie można edytować ikony",MessageBoxButtons.OK,MessageBoxIcon.Error); else ClearIconMaterial(); }
                finally { clearAfterIconEdit=false; }
            }
        }
        LibraryIcon PickLibraryIcon(IWin32Window owner)
        {
            using (var dialog = new IconPickerDialog(libraryIcons, IconButton, BackColor)) return RunModal(() => dialog.ShowDialog(owner)) == DialogResult.OK ? dialog.Chosen : null;
        }
        void OpenIconLibrary() { var chosen = PickLibraryIcon(this); if (chosen != null) SelectIcon(chosen.Id); }
        MapData MapForSave()
        {
            var used = new HashSet<string>(map.Cells.Where(c => c.IconId != null).Select(c => c.IconId).Concat(map.Objects.Select(o=>o.AssetId)));
            return new MapData { FormatVersion = map.FormatVersion, Name = map.Name, Columns = map.Columns, Rows = map.Rows, Orientation = map.Orientation, LabelMode = map.LabelMode, Cells = map.Cells, Presets = map.Presets, ActivePresetId = map.ActivePresetId, RowParity = map.RowParity, ColumnParity = map.ColumnParity, IconAssets = map.IconAssets.Where(a => used.Contains(a.Id)).ToList(), Objects=map.Objects, Groups=map.Groups,ObjectStates=StateLibrary.ForSave(map), ExcludedStateIds=map.ExcludedStateIds };
        }
        void BeginMaterialStroke(int start, EditorTool tool)
        {
            strokeStarted = true;
            strokeBefore.Clear(); layerBefore.Clear(); strokeVisited.Clear();
            bucketProcessed.Clear(); bucketOrigin = tool == EditorTool.Bucket || tool == EditorTool.FogBucket ? map.Cells.Select(c => new RegionState(c)).ToArray() : null;
            strokeFog = tool == EditorTool.FogBrush || tool == EditorTool.FogBucket; strokeIcons = !strokeFog && iconMaterial;
            protectedFogCells=strokeFog?EffectiveFogCoverage(map):new HashSet<int>();
            var entry = libraryIcons.FirstOrDefault(a => a.Id == activeIcon); strokeAsset = entry == null ? null : entry.Asset; strokeIcon = strokeAsset == null ? null : strokeAsset.Id; strokeEntry = entry == null ? null : entry.Id; strokeName = entry == null ? null : entry.Name;
            strokeRemoveIcon = strokeIcons && strokeIcon != null && (map.Cells[start].IconEntryId ?? map.Cells[start].IconId) == strokeEntry; strokeFogValue = !map.Cells[start].Fog;
        }
        void PaintMaterial(int index)
        {
            if (!strokeVisited.Add(index)) return;
            if (!strokeFog && !strokeIcons) { PaintCell(index); return; }
            var cell = map.Cells[index]; string nextIcon = strokeRemoveIcon ? ((cell.IconEntryId ?? cell.IconId) == strokeEntry ? null : cell.IconId) : strokeIcon;
            bool targetFog=strokeFogValue&&!protectedFogCells.Contains(index);
            if (strokeFog ? cell.Fog == targetFog : strokeIcon == null || (cell.IconId == nextIcon && (strokeRemoveIcon || (cell.IconEntryId == strokeEntry && cell.IconName == strokeName)))) return;
            if (strokeIcons && !strokeRemoveIcon && !map.IconAssets.Any(a => a.Id == strokeIcon))
            {
                if (map.IconAssets.Count >= IconAssets.MaxAssets || map.IconAssets.Sum(a => (long)a.Png.Length) + strokeAsset.Png.Length > 44L * 1024 * 1024) { EndCanvasGesture(true); ShowEditorMessage("Limit zasobów ikon w sesji. Zapisz mapę i otwórz ponownie, aby zwolnić zasoby zachowane dla Undo.", "Limit ikon", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                map.IconAssets.Add(strokeAsset);
            }
            var change = new LayerChange { CellId = cell.Id, BeforeIcon = cell.IconId, BeforeFog = cell.Fog, Icons = strokeIcons, Fog = strokeFog, AfterIcon = nextIcon, AfterFog = targetFog, BeforeEntry = cell.IconEntryId, BeforeName = cell.IconName, AfterEntry = nextIcon == null ? null : strokeRemoveIcon ? cell.IconEntryId : strokeEntry, AfterName = nextIcon == null ? null : strokeRemoveIcon ? cell.IconName : strokeName };
            layerBefore.Add(index, change); change.Apply(cell, true); dirty = true;
            if (index == selected && strokeFog) { editorCellId = null; UpdateUI(); } else canvas.Invalidate();
        }
        void PaintStamp(int start)
        { foreach (int index in BrushGeometry.Stamp(map, start, brushShape, brushSize, brushHeight, brushFilled)) PaintMaterial(index); }
        void VisitBucketRegion(int start)
        {
            if (bucketOrigin == null || bucketProcessed.Contains(start)) return;
            var region = MapLogic.Flood(map, start, bucketOrigin, strokeFog);
            foreach (int index in region) bucketProcessed.Add(index);
            // Passing over opposite fog does not reverse the mode fixed at gesture start.
            if (strokeFog && bucketOrigin[start].Fog == strokeFogValue) return;
            foreach (int index in region) { if (!strokeStarted) break; PaintMaterial(index); }
        }
        void PaintAlong(Point mouse, int hit)
        {
            if (!canvas.ClientRectangle.Contains(mouse) || hit < 0) { previousStrokePoint = null; return; }
            var point = new PointF((mouse.X - pan.X) / scale, (mouse.Y - pan.Y) / scale);
            if (previousStrokePoint.HasValue)
            {
                var before = previousStrokePoint.Value; float dx = point.X - before.X, dy = point.Y - before.Y;
                int steps = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(dx * dx + dy * dy) / (BaseRadius * .4f)));
                for (int i = 1; i < steps; i++) { var p = new Point((int)Math.Round(pan.X + (before.X + dx * i / steps) * scale), (int)Math.Round(pan.Y + (before.Y + dy * i / steps) * scale)); int at = HitTest(p); if (at >= 0) PaintStamp(at); }
            }
            PaintStamp(hit); previousStrokePoint = point;
        }
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
