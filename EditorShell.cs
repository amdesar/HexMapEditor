using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    // Icons are drawn locally. No font pictograms, downloads or external assets.
    internal static class EditorIcons
    {
        static readonly Dictionary<string, Bitmap> cache = new Dictionary<string, Bitmap>();
        public static Bitmap Draw(string key)
        {
            Bitmap existing; if (cache.TryGetValue(key, out existing)) return existing;
            var image = new Bitmap(22, 22);
            using (var g = Graphics.FromImage(image)) using (var p = new Pen(Color.WhiteSmoke, 1.8f))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                if (key == "undo" || key == "redo")
                { if (key == "redo") { g.TranslateTransform(22, 0); g.ScaleTransform(-1, 1); } g.DrawArc(p, 4, 6, 14, 12, 205, 240); g.DrawLines(p, new[] { new Point(3, 3), new Point(3, 11), new Point(10, 9) }); }
                else if (key == "pointy" || key == "flat")
                { var points = Enumerable.Range(0, 6).Select(i => { double a = (i * 60 + (key == "flat" ? 0 : -90)) * Math.PI / 180; return new PointF(11 + 9 * (float)Math.Cos(a), 11 + 9 * (float)Math.Sin(a)); }).ToArray(); g.DrawPolygon(p, points); }
                else if (key == "new") { g.DrawRectangle(p, 5, 2, 12, 18); g.DrawLine(p, 8, 11, 14, 11); g.DrawLine(p, 11, 8, 11, 14); }
                else if (key == "open" || key == "library") { g.DrawLines(p, new[] { new Point(2, 18), new Point(2, 5), new Point(9, 5), new Point(11, 8), new Point(20, 8), new Point(17, 18), new Point(2, 18) }); }
                else if (key == "save" || key == "saveas") { g.DrawRectangle(p, 3, 3, 16, 16); g.DrawRectangle(p, 7, 3, 8, 6); g.DrawRectangle(p, 7, 13, 8, 6); if (key == "saveas") g.DrawLine(p, 1, 20, 20, 1); }
                else if (key == "select") g.DrawPolygon(p, new[] { new Point(4, 2), new Point(17, 12), new Point(11, 13), new Point(8, 19) });
                else if (key == "hand") { g.DrawEllipse(p, 5, 8, 12, 12); for (int x = 5; x <= 17; x += 4) g.DrawLine(p, x, 10, x, 2 + Math.Abs(x - 11) / 2); }
                else if (key == "brush") { g.DrawLines(p, new[] { new Point(6, 13), new Point(16, 2), new Point(20, 5), new Point(10, 16) }); g.DrawBezier(p, 8, 13, 2, 12, 7, 20, 2, 20); g.DrawLine(p, 2, 20, 10, 16); }
                else if (key == "bucket") { g.DrawPolygon(p, new[] { new Point(2, 11), new Point(10, 3), new Point(18, 11), new Point(10, 19) }); g.DrawLine(p, 3, 11, 17, 11); g.DrawEllipse(p, 18, 13, 3, 5); }
                else if (key == "fit") { foreach (int x in new[] { 2, 20 }) foreach (int y in new[] { 2, 20 }) { g.DrawLine(p, x, y, x == 2 ? 8 : 14, y); g.DrawLine(p, x, y, x, y == 2 ? 8 : 14); } }
                else { g.DrawRectangle(p, 3, 3, 16, 16); g.DrawLine(p, 7, 8, 15, 8); g.DrawLine(p, 7, 13, 15, 13); }
            }
            cache.Add(key, image); return image;
        }
    }

    public sealed partial class MainWindow
    {
        Panel shellToolbar;
        readonly Label detailCoordinate = new Label(), detailTerrain = new Label();
        readonly FlowLayoutPanel paletteTiles = new FlowLayoutPanel();
        readonly Dictionary<EditorTool, Button> toolButtons = new Dictionary<EditorTool, Button>();
        void ConfigureButton(Button button, string text, string icon, string help, Action action, int width)
        {
            button.Text = text; button.Image = EditorIcons.Draw(icon); button.TextImageRelation = TextImageRelation.ImageBeforeText;
            button.Width = width; button.Height = 34; button.Margin = new Padding(2); button.FlatStyle = FlatStyle.Flat;
            button.AccessibleName = help; tips.SetToolTip(button, help); StyleButton(button); button.Click += (s, e) => action();
        }
        Button ShellButton(Control parent, string text, string icon, string help, Action action, int width)
        { var button = new Button(); ConfigureButton(button, text, icon, help, action, width); parent.Controls.Add(button); return button; }
        void InitializeShell()
        {
            shellToolbar = new Panel { Dock = DockStyle.Top, Height = 83, BackColor = Color.FromArgb(34, 38, 48) }; Controls.Add(shellToolbar);
            var file = new FlowLayoutPanel { Location = new Point(4, 4), Size = new Size(638, 38), WrapContents = false };
            var view = new FlowLayoutPanel { Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(ClientSize.Width - 490, 4), Size = new Size(486, 38), WrapContents = false };
            shellToolbar.Controls.Add(file); shellToolbar.Controls.Add(view);
            ShellButton(file, "Nowa", "new", "Nowa mapa", NewProject, 70); ShellButton(file, "Otwórz", "open", "Otwórz mapę", OpenProject, 80);
            ConfigureButton(saveButton, "Zapisz", "save", "Zapisz (Ctrl+S)", () => SaveProject(), 86); file.Controls.Add(saveButton);
            ShellButton(file, "Zapisz jako", "saveas", "Zapisz jako (Ctrl+Shift+S)", () => SaveAsProject(), 100);
            ConfigureButton(undoButton, "", "undo", "Cofnij operację mapy", Undo, 32); file.Controls.Add(undoButton);
            ConfigureButton(redoButton, "", "redo", "Ponów operację mapy", Redo, 32); file.Controls.Add(redoButton);
            ShellButton(file, "Biblioteka", "library", "Pokaż lokalizację biblioteki", () => ShowEditorMessage("Lokalna biblioteka:\n" + LibraryPaths.Root + "\n\nZestawy terenów można zapisywać w panelu po lewej. Pozostałe katalogi są przygotowane na przyszłe funkcje.", "Biblioteka", MessageBoxButtons.OK, MessageBoxIcon.Information), 98);
            ConfigureButton(orientationToggle, "Pointy Top", "pointy", "Zmień orientację", () => ChangeOrientation(map.Orientation == "PointyTop" ? "FlatTop" : "PointyTop"), 128); view.Controls.Add(orientationToggle);
            ConfigureButton(labelToggle, "Osie", "labels", "Zmień tryb etykiet", CycleLabelMode, 88); view.Controls.Add(labelToggle);
            ConfigureButton(zoomButton, "100%", "fit", "Ustaw skalę 100%", SetZoom100, 62); zoomButton.Image = null; view.Controls.Add(zoomButton);
            ConfigureButton(fitButton, "Dopasuj", "fit", "Dopasuj mapę (prawy panel jest nakładką)", FitMap, 94); view.Controls.Add(fitButton);
            ShellButton(view, "Rozmiar", "labels", "Dodaj lub usuń wiersze i kolumny", ResizeProject, 90);
            mapName.SetBounds(12, 45, ClientSize.Width - 24, 30); mapName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; mapName.TextAlign = ContentAlignment.MiddleCenter; mapName.Font = new Font(Font, FontStyle.Bold); mapName.AutoEllipsis = true; mapName.AccessibleName = "Nazwa projektu"; shellToolbar.Controls.Add(mapName);
            var status = new Panel { Dock = DockStyle.Bottom, Height = 27, BackColor = shellToolbar.BackColor }; statusLabel.Dock = DockStyle.Fill; statusLabel.Padding = new Padding(10, 4, 0, 0); status.Controls.Add(statusLabel); Controls.Add(status);
            canvas.BackColor = Color.FromArgb(25, 28, 36); canvas.TabStop = true; Controls.Add(canvas); canvas.SendToBack();
            canvas.Paint += PaintMap; canvas.MouseDown += CanvasMouseDown; canvas.MouseMove += CanvasMouseMove; canvas.MouseUp += CanvasMouseUp; canvas.MouseWheel += CanvasMouseWheel;
            canvas.MouseCaptureChanged += (s, e) => { if (!canvas.Capture && gestureButton != MouseButtons.None) EndCanvasGesture(true); };
            left.BackColor = shellToolbar.BackColor; left.AutoScroll = true; Controls.Add(left);
            leftToggle.FlatStyle = FlatStyle.Flat; StyleButton(leftToggle); leftToggle.AccessibleName = "Zwiń lub rozwiń paletę"; tips.SetToolTip(leftToggle, leftToggle.AccessibleName); leftToggle.Click += (s, e) => { leftCollapsed = !leftCollapsed; PositionOverlays(); canvas.Invalidate(); }; Controls.Add(leftToggle);
            BuildPalettePanel(); BuildDetailsPanel(); Controls.Add(right);
        }
        void BuildPalettePanel()
        {
            var body = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(8), Width = 216 };
            left.Controls.Add(body);
            var tools = new FlowLayoutPanel { Width = 198, Height = 44, WrapContents = false };
            var names = new[] { "Zaznaczanie", "Przesuwanie", "Pędzel", "Wiadro" }; var icons = new[] { "select", "hand", "brush", "bucket" };
            for (int i = 0; i < 4; i++) { var tool = (EditorTool)i; var b = ShellButton(tools, "", icons[i], names[i], () => SelectTool(tool), 44); toolButtons.Add(tool, b); } body.Controls.Add(tools); SelectTool(EditorTool.Select);
            body.Controls.Add(new Label { Text = "Zestaw terenów", AutoSize = true, Margin = new Padding(3, 10, 3, 4) });
            terrainPicker.Width = 194; terrainPicker.DropDownStyle = ComboBoxStyle.DropDownList; terrainPicker.DisplayMember = "Name";
            terrainPicker.SelectedIndexChanged += (s, e) => { if (updating || terrainPicker.SelectedItem == null) return; EndCanvasGesture(true); var chosen = (TerrainPreset)terrainPicker.SelectedItem; if (!map.Presets.Any(p => p.Id == chosen.Id)) map.Presets.Add(chosen.Copy()); map.ActivePresetId = chosen.Id; brushIndex = 0; RefreshPresetPicker(); MarkDirty(); }; body.Controls.Add(terrainPicker);
            var actions = new FlowLayoutPanel { Width = 198, Height = 70, WrapContents = true };
            SmallButton(actions, "Nowy zestaw", CreatePreset, 94); SmallButton(actions, "Nazwa", RenamePreset, 92); SmallButton(actions, "Zapisz zestaw", SavePreset, 190); body.Controls.Add(actions);
            paletteTiles.Width = 198; paletteTiles.AutoSize = true; paletteTiles.WrapContents = true; body.Controls.Add(paletteTiles);
            var edit = new FlowLayoutPanel { Width = 198, Height = 66 }; SmallButton(edit, "Dodaj", () => EditTerrain(true), 60); SmallButton(edit, "Edytuj", () => EditTerrain(false), 60); SmallButton(edit, "Usuń", RemoveTerrain, 60); body.Controls.Add(edit);
            RefreshPresetPicker();
        }
        Button SmallButton(Control parent, string text, Action action, int width)
        { var b = new Button { Text = text, Width = width, Height = 28, Margin = new Padding(2), FlatStyle = FlatStyle.Flat, AccessibleName = text }; StyleButton(b); b.Click += (s, e) => action(); parent.Controls.Add(b); return b; }
        void SelectTool(EditorTool tool)
        {
            EndCanvasGesture(true); currentTool = tool;
            foreach (var pair in toolButtons) { pair.Value.BackColor = pair.Key == tool ? Color.FromArgb(93, 111, 156) : Color.FromArgb(49, 54, 67); pair.Value.FlatAppearance.BorderSize = pair.Key == tool ? 3 : 1; pair.Value.AccessibleDescription = pair.Key == tool ? "Aktywne" : "Nieaktywne"; }
        }
        void BuildDetailsPanel()
        {
            right.BackColor = Color.FromArgb(34, 38, 48); right.Visible = false;
            var header = new Panel { Dock = DockStyle.Top, Height = 38 }; var title = new Label { Text = "Szczegóły heksu", Dock = DockStyle.Fill, Padding = new Padding(14, 10, 0, 0) }; header.Controls.Add(title);
            rightClose.Text = "Zamknij"; rightClose.Dock = DockStyle.Right; rightClose.Width = 85; StyleButton(rightClose); rightClose.Click += (s, e) => CloseDetailsPanel(); header.Controls.Add(rightClose);
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(14) }; var content = new Panel { Location = new Point(14, 14), Height = 770, Width = 375 };
            scroll.Controls.Add(content); right.Controls.Add(scroll); right.Controls.Add(header);
            scroll.Resize += (s, e) => { content.Width = Math.Max(200, scroll.ClientSize.Width - 48); };
            detailCoordinate.SetBounds(0, 0, 360, 24); detailTerrain.SetBounds(0, 26, 360, 28); detailTerrain.AutoEllipsis = true; content.Controls.Add(detailCoordinate); content.Controls.Add(detailTerrain);
            content.Controls.Add(new Label { Text = "Własna nazwa heksu", Location = new Point(0, 60), AutoSize = true });
            cellName.SetBounds(0, 84, 360, 26); cellName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; cellName.AccessibleName = "Własna nazwa heksu"; content.Controls.Add(cellName);
            cellName.TextChanged += (s, e) => NameEdited(); cellName.LostFocus += (s, e) => textHistory.BreakGroups();
            cellName.HistoryUndo = () => TextUndo("name", false);
            cellName.MouseDown += (s, e) => textHistory.BreakGroups();
            cellName.KeyDown += (s, e) => { if (e.Control || new[] { Keys.Left, Keys.Right, Keys.Up, Keys.Down, Keys.Home, Keys.End }.Contains(e.KeyCode)) textHistory.BreakGroups(); };
            var history = new FlowLayoutPanel { Location = new Point(0, 112), Width = 360, Height = 34 }; SmallButton(history, "Cofnij nazwę", () => TextUndo("name", false), 116); SmallButton(history, "Ponów nazwę", () => TextUndo("name", true), 116); content.Controls.Add(history);
            content.Controls.Add(new Label { Text = "Opis (tekst, listy i tabele)", Location = new Point(0, 152), AutoSize = true });
            cellDescription.SetBounds(0, 178, 360, 570); cellDescription.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; content.Controls.Add(cellDescription);
            cellDescription.Changed = DescriptionEdited; cellDescription.HistoryRequested = repeat => TextUndo("description", repeat);
            cellDescription.SaveRequested = asNew => { if (asNew) SaveAsProject(); else SaveProject(); };
            cellDescription.BreakHistoryGroup = () => textHistory.BreakGroups();
            cellDescription.Error = message => ShowEditorMessage(message, "Opis heksu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            cellDescription.TableSizeRequested = () => { using (var d = new TableSizeDialog()) return RunModal(() => d.ShowDialog(this)) == DialogResult.OK ? new[] { d.Rows, d.Columns } : null; };
        }
        TerrainPreset ActivePreset() { return map.Presets.First(p => p.Id == map.ActivePresetId); }
        List<TerrainPreset> AvailablePresets() { return map.Presets.Concat(libraryPresets.Where(p => !map.Presets.Any(m => m.Id == p.Id))).ToList(); }
        void RefreshPresetPicker()
        {
            bool previous = updating; updating = true;
            try
            {
                terrainPicker.Items.Clear(); foreach (var p in AvailablePresets()) terrainPicker.Items.Add(p); terrainPicker.SelectedItem = map.Presets.FirstOrDefault(p => p.Id == map.ActivePresetId);
                foreach (Control control in paletteTiles.Controls.Cast<Control>().ToArray()) control.Dispose(); paletteTiles.Controls.Clear();
                var palette = ActivePreset(); brushIndex = Math.Max(0, Math.Min(brushIndex, palette.Terrains.Count - 1));
                for (int i = 0; i < palette.Terrains.Count; i++)
                {
                    int at = i; var t = palette.Terrains[i]; var b = new Button { Text = t.Name, Width = 92, Height = 65, Margin = new Padding(3), BackColor = ParseColor(t.Color), ForeColor = Contrast(t.Color), FlatStyle = FlatStyle.Flat, AccessibleName = "Teren: " + t.Name }; b.FlatAppearance.BorderColor = Color.White; b.FlatAppearance.BorderSize = at == brushIndex ? 3 : 1;
                    b.Click += (s, e) => { EndCanvasGesture(true); brushIndex = at; RefreshPresetPicker(); }; tips.SetToolTip(b, t.Name + " · wersja " + t.Revision); paletteTiles.Controls.Add(b);
                }
            }
            finally { updating = previous; }
        }
        static Color Contrast(string color) { var c = ParseColor(color); return c.R * .299 + c.G * .587 + c.B * .114 > 145 ? Color.Black : Color.White; }
        void LoadLibraryPresets(bool initial)
        {
            if (!EnsureLibrary()) return; string warning; libraryPresets = PresetCatalog.Load(Path.Combine(LibraryPaths.Root, "Terrains"), out warning);
            if (initial && currentPath == null && !dirty) { var p = libraryPresets.FirstOrDefault(t => t.Id == PresetCatalog.DefaultId); if (p != null) map.Presets[0] = p.Copy(); CaptureSavedState(); }
            RefreshPresetPicker(); if (!String.IsNullOrEmpty(warning)) ShowEditorMessage(warning, "Niektóre zestawy zostały pominięte", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        void CreatePreset()
        { using (var d = new NameDialog("Nowy zestaw", "Nowy zestaw")) if (RunModal(() => d.ShowDialog(this)) == DialogResult.OK) { var p = new TerrainPreset { Name = d.Value }; p.Terrains.Add(PresetCatalog.Neutral()); map.Presets.Add(p); map.ActivePresetId = p.Id; brushIndex = 0; RefreshPresetPicker(); MarkDirty(); } }
        void RenamePreset()
        { var p = ActivePreset(); using (var d = new NameDialog("Nazwa zestawu", p.Name)) if (RunModal(() => d.ShowDialog(this)) == DialogResult.OK) { p.Name = d.Value; RefreshPresetPicker(); MarkDirty(); } }
        void SavePreset()
        {
            if (!EnsureLibrary()) return;
            try { PresetCatalog.Save(ActivePreset(), Path.Combine(LibraryPaths.Root, "Terrains")); LoadLibraryPresets(false); ShowEditorMessage("Zapisano lokalny zestaw terenów.", "Zestaw terenów", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            catch (Exception ex) { ShowEditorMessage("Nie można zapisać zestawu.\n" + ex.Message, "Błąd zestawu", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        void EditTerrain(bool add)
        {
            var p = ActivePreset(); if (!add && p.Terrains.Count == 0) return;
            var old = add ? new TerrainDefinition() : p.Terrains[brushIndex];
            using (var d = new TerrainDialog(old, !add)) if (RunModal(() => d.ShowDialog(this)) == DialogResult.OK)
            {
                var next = old.Copy(); next.Name = d.TerrainName; next.Color = d.TerrainColor;
                if (add) { p.Terrains.Add(next); brushIndex = p.Terrains.Count - 1; }
                else if (next.Name != old.Name || next.Color != old.Color) { next.VariantId = Guid.NewGuid().ToString("N"); next.Revision++; UpdateTerrain(old, next, d.ApplyToMap); }
                RefreshPresetPicker(); MarkDirty();
            }
        }
        void UpdateTerrain(TerrainDefinition old, TerrainDefinition next, bool apply)
        {
            EndCanvasGesture(true); var p = ActivePreset(); int at = p.Terrains.FindIndex(t => t.VariantId == old.VariantId); if (at < 0) throw new InvalidOperationException("Brak wersji terenu."); p.Terrains[at] = next;
            if (!apply) return;
            var action = new EditAction { Label = "Aktualizacja wersji terenu" };
            foreach (var cell in map.Cells.Where(c => c.VariantId == old.VariantId)) { var before = new TerrainState(cell); MapLogic.ApplyTerrain(cell, next); action.Changes.Add(new CellChange { CellId = cell.Id, Before = before, After = new TerrainState(cell) }); }
            if (action.Changes.Count > 0) PushAction(action);
        }
        void RemoveTerrain()
        {
            var p = ActivePreset(); if (p.Terrains.Count == 0) return;
            if (ShowEditorMessage("Usunąć teren z zestawu? Istniejące heksy zachowają swoje dane.", "Usuń teren", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            p.Terrains.RemoveAt(brushIndex); brushIndex = 0; RefreshPresetPicker(); MarkDirty();
        }
    }
}
