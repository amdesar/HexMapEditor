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
        public static Bitmap Small(string key)
        {
            Bitmap image; if (!cache.TryGetValue("small:" + key, out image)) { image = new Bitmap(Draw(key), new Size(18, 18)); cache.Add("small:" + key, image); } return image;
        }
        public static Bitmap Draw(string key)
        {
            Bitmap existing; if (cache.TryGetValue(key, out existing)) return existing;
            var image = new Bitmap(22, 22);
            using (var g = Graphics.FromImage(image)) using (var p = new Pen(Color.WhiteSmoke, 1.8f))
            {
                if (key == "trash") p.Color = Color.FromArgb(244, 113, 113);
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
                else if (key == "pencil") { g.DrawPolygon(p, new[] { new Point(3, 19), new Point(5, 12), new Point(15, 2), new Point(20, 7), new Point(10, 17) }); g.DrawLine(p, 12, 5, 17, 10); }
                else if (key == "trash") { g.DrawRectangle(p, 6, 7, 11, 13); g.DrawLine(p, 3, 5, 20, 5); g.DrawLine(p, 9, 2, 14, 2); g.DrawLine(p, 9, 10, 9, 17); g.DrawLine(p, 14, 10, 14, 17); }
                else if (key == "fit") { foreach (int x in new[] { 2, 20 }) foreach (int y in new[] { 2, 20 }) { g.DrawLine(p, x, y, x == 2 ? 8 : 14, y); g.DrawLine(p, x, y, x, y == 2 ? 8 : 14); } }
                else if (key == "refresh") { g.DrawArc(p, 4, 4, 14, 14, 40, 285); g.DrawLines(p, new[] { new Point(12, 2), new Point(18, 4), new Point(17, 10) }); }
                else { g.DrawRectangle(p, 3, 3, 16, 16); g.DrawLine(p, 7, 8, 15, 8); g.DrawLine(p, 7, 13, 15, 13); }
            }
            cache.Add(key, image); return image;
        }
    }

    public sealed partial class MainWindow
    {
        Panel shellToolbar;
        FlowLayoutPanel fileGroup, viewGroup;
        readonly Label detailCoordinate = new Label(), detailTerrain = new Label();
        readonly FlowLayoutPanel paletteTiles = new BufferedFlowPanel();
        readonly Label presetStateLabel = new Label();
        readonly Dictionary<EditorTool, Button> toolButtons = new Dictionary<EditorTool, Button>();
        FlowLayoutPanel paletteBody, paletteTools, presetActions;
        Panel presetPickerRow;
        Button refreshPresetsButton, addTerrainButton;
        void ConfigureButton(Button button, string text, string icon, string help, Action action, int width)
        {
            button.Text = text; button.Image = EditorIcons.Draw(icon); button.TextImageRelation = TextImageRelation.ImageBeforeText;
            button.Width = width; button.Height = 32; button.Margin = new Padding(1); button.Padding = new Padding(2, 0, 2, 0); button.FlatStyle = FlatStyle.Flat;
            button.Image = EditorIcons.Small(icon); button.ImageAlign = ContentAlignment.MiddleCenter; button.TextAlign = ContentAlignment.MiddleCenter;
            button.AccessibleName = help; tips.SetToolTip(button, help); StyleButton(button); button.Click += (s, e) => action();
        }
        Button ShellButton(Control parent, string text, string icon, string help, Action action, int width)
        { var button = new Button(); ConfigureButton(button, text, icon, help, action, width); parent.Controls.Add(button); return button; }
        void InitializeShell()
        {
            shellToolbar = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Color.FromArgb(34, 38, 48) }; Controls.Add(shellToolbar);
            var file = fileGroup = new FlowLayoutPanel { Location = new Point(4, 4), Height = 34, WrapContents = false };
            var view = viewGroup = new FlowLayoutPanel { Height = 34, WrapContents = false };
            shellToolbar.Controls.Add(file); shellToolbar.Controls.Add(view);
            ShellButton(file, "Nowa", "new", "Nowa mapa", NewProject, 70); ShellButton(file, "Otwórz", "open", "Otwórz mapę", OpenProject, 80);
            ConfigureButton(saveButton, "Zapisz", "save", "Zapisz (Ctrl+S)", () => SaveProject(), 86); file.Controls.Add(saveButton);
            ShellButton(file, "Zapisz jako", "saveas", "Zapisz jako (Ctrl+Shift+S)", () => SaveAsProject(), 100);
            ConfigureButton(undoButton, "", "undo", "Cofnij operację mapy", Undo, 32); file.Controls.Add(undoButton);
            ConfigureButton(redoButton, "", "redo", "Ponów operację mapy", Redo, 32); file.Controls.Add(redoButton);
            ShellButton(file, "Biblioteka", "library", "Pokaż lokalizację biblioteki", () => ShowEditorMessage("Lokalna biblioteka:\n" + LibraryPaths.Root + "\n\nPresety terenów można zapisywać w panelu po lewej. Pozostałe katalogi są przygotowane na przyszłe funkcje.", "Biblioteka", MessageBoxButtons.OK, MessageBoxIcon.Information), 98);
            ConfigureButton(orientationToggle, "Pointy Top", "pointy", "Zmień orientację", () => ChangeOrientation(map.Orientation == "PointyTop" ? "FlatTop" : "PointyTop"), 128); view.Controls.Add(orientationToggle);
            ConfigureButton(labelToggle, "Osie", "labels", "Zmień tryb etykiet", CycleLabelMode, 110); view.Controls.Add(labelToggle);
            ConfigureButton(zoomButton, "100%", "fit", "Ustaw skalę 100%", SetZoom100, 62); zoomButton.Image = null; view.Controls.Add(zoomButton);
            ConfigureButton(fitButton, "Dopasuj", "fit", "Dopasuj mapę (prawy panel jest nakładką)", FitMap, 94); view.Controls.Add(fitButton);
            ShellButton(view, "Rozmiar", "labels", "Dodaj lub usuń wiersze i kolumny", ResizeProject, 90);
            mapName.TextAlign = ContentAlignment.MiddleCenter; mapName.Font = new Font(Font, FontStyle.Bold); mapName.AutoEllipsis = true; mapName.AccessibleName = "Nazwa projektu"; shellToolbar.Controls.Add(mapName);
            var status = new Panel { Dock = DockStyle.Bottom, Height = 27, BackColor = shellToolbar.BackColor }; statusLabel.Dock = DockStyle.Fill; statusLabel.Padding = new Padding(10, 4, 0, 0); status.Controls.Add(statusLabel); Controls.Add(status);
            canvas.BackColor = Color.FromArgb(25, 28, 36); canvas.TabStop = true; Controls.Add(canvas); canvas.SendToBack();
            canvas.Paint += PaintMap; canvas.MouseDown += CanvasMouseDown; canvas.MouseMove += CanvasMouseMove; canvas.MouseUp += CanvasMouseUp; canvas.MouseWheel += CanvasMouseWheel;
            canvas.MouseCaptureChanged += (s, e) => { if (!canvas.Capture && gestureButton != MouseButtons.None) EndCanvasGesture(true); };
            left.BackColor = shellToolbar.BackColor; left.AutoScroll = true; Controls.Add(left);
            leftToggle.FlatStyle = FlatStyle.Flat; StyleButton(leftToggle); leftToggle.AccessibleName = "Zwiń lub rozwiń paletę"; tips.SetToolTip(leftToggle, leftToggle.AccessibleName); leftToggle.Click += (s, e) => { leftCollapsed = !leftCollapsed; PositionOverlays(); canvas.Invalidate(); }; Controls.Add(leftToggle);
            BuildPalettePanel(); BuildDetailsPanel(); Controls.Add(right);
        }
        void LayoutMainToolbar()
        {
            fileGroup.Width = fileGroup.Controls.Cast<Control>().Sum(c => c.Width + c.Margin.Horizontal);
            viewGroup.Width = viewGroup.Controls.Cast<Control>().Sum(c => c.Width + c.Margin.Horizontal);
            fileGroup.Location = new Point(4, 4); viewGroup.Location = new Point(shellToolbar.ClientSize.Width - viewGroup.Width - 4, 4);
            int titleHeight = mapName.Font.Height + 2;
            mapName.Bounds = new Rectangle(fileGroup.Right + 8, 4 + (34 - titleHeight) / 2, Math.Max(0, viewGroup.Left - fileGroup.Right - 16), titleHeight);
        }
        void BuildPalettePanel()
        {
            var body = paletteBody = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(6), Margin = Padding.Empty };
            left.Controls.Add(body);
            var tools = paletteTools = new FlowLayoutPanel { Height = 38, WrapContents = false, Margin = new Padding(0, 2, 0, 2) };
            var names = new[] { "Zaznaczanie", "Przesuwanie", "Pędzel", "Wiadro" }; var icons = new[] { "select", "hand", "brush", "bucket" };
            for (int i = 0; i < 4; i++) { var tool = (EditorTool)i; var b = ShellButton(tools, "", icons[i], names[i], () => SelectTool(tool), 44); toolButtons.Add(tool, b); } body.Controls.Add(tools); SelectTool(EditorTool.Select);
            body.Controls.Add(new Label { Text = "Presety", AutoSize = true, Margin = new Padding(0, 6, 0, 3) });
            presetPickerRow = new Panel { Height = 28, Margin = new Padding(0, 0, 0, 2) }; body.Controls.Add(presetPickerRow);
            terrainPicker.DropDownStyle = ComboBoxStyle.DropDownList; terrainPicker.DisplayMember = "Name"; terrainPicker.AccessibleName = "Presety";
            terrainPicker.SelectedIndexChanged += (s, e) => { if (!updating && terrainPicker.SelectedItem != null) SelectPreset(((TerrainPreset)terrainPicker.SelectedItem).Id); }; presetPickerRow.Controls.Add(terrainPicker);
            refreshPresetsButton = SmallButton(presetPickerRow, "", () => LoadLibraryPresets(false), 28); refreshPresetsButton.Image = EditorIcons.Small("refresh"); refreshPresetsButton.AccessibleName = "Odśwież presety"; tips.SetToolTip(refreshPresetsButton, "Odśwież presety — zachowaj roboczą paletę projektu");
            var actions = presetActions = new FlowLayoutPanel { Height = 64, WrapContents = true, Margin = new Padding(0, 0, 0, 2) };
            SmallButton(actions, "Nowy preset", CreatePreset, 90); SmallButton(actions, "Zmień nazwę", RenamePreset, 90); SmallButton(actions, "Zapisz preset", SavePreset, 90); deletePresetButton = SmallButton(actions, "Usuń preset", RemovePreset, 90); body.Controls.Add(actions);
            presetStateLabel.Height = 34; presetStateLabel.AutoEllipsis = true; presetStateLabel.Margin = new Padding(0, 2, 0, 2); body.Controls.Add(presetStateLabel);
            addTerrainButton = SmallButton(body, "Dodaj teren", () => EditTerrain(true), 184); addTerrainButton.Margin = new Padding(0, 2, 0, 4);
            paletteTiles.AutoSize = true; paletteTiles.FlowDirection = FlowDirection.TopDown; paletteTiles.WrapContents = false; paletteTiles.Margin = Padding.Empty; body.Controls.Add(paletteTiles);
            left.Layout += (s, e) => LayoutPalette(); paletteTiles.ControlAdded += (s, e) => LayoutPalette();
            LayoutPalette();
            RefreshPresetPicker();
        }
        bool layingOutPalette;
        void LayoutPalette()
        {
            if (layingOutPalette || paletteBody == null) return;
            layingOutPalette = true;
            try
            {
                int width = Math.Max(160, left.ClientSize.Width - paletteBody.Padding.Horizontal);
                paletteBody.SuspendLayout();
                try
                {
                    paletteTools.Width = presetPickerRow.Width = presetActions.Width = presetStateLabel.Width = addTerrainButton.Width = paletteTiles.Width = width;
                    for (int i = 0; i < paletteTools.Controls.Count; i++) paletteTools.Controls[i].Width = (width - 8) / 4;
                    terrainPicker.SetBounds(0, 1, width - 32, 24); refreshPresetsButton.SetBounds(width - 28, 0, 28, 26);
                    foreach (Control b in presetActions.Controls) b.Width = (width - 8) / 2;
                    foreach (TerrainTile tile in paletteTiles.Controls) tile.ResizeRow(width);
                }
                finally { paletteBody.ResumeLayout(true); }
            }
            finally { layingOutPalette = false; }
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
            var scroll = new DetailsScrollPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(14) }; var content = new Panel { Location = new Point(14, 14), Height = 770, Width = 375 };
            scroll.Controls.Add(content); right.Controls.Add(scroll); right.Controls.Add(header);
            InitializePanelResize(header);
            detailCoordinate.SetBounds(0, 0, 360, 24); detailTerrain.SetBounds(0, 26, 360, 28); detailTerrain.AutoEllipsis = true; content.Controls.Add(detailCoordinate); content.Controls.Add(detailTerrain);
            detailCoordinate.Anchor = detailTerrain.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            content.Controls.Add(new Label { Text = "Własna nazwa heksu", Location = new Point(0, 60), AutoSize = true });
            cellName.SetBounds(0, 84, 360, 26); cellName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; cellName.AccessibleName = "Własna nazwa heksu"; content.Controls.Add(cellName);
            cellName.TextChanged += (s, e) => NameEdited(); cellName.LostFocus += (s, e) => textHistory.BreakGroups();
            cellName.HistoryUndo = () => TextUndo("name", false);
            cellName.MouseDown += (s, e) => textHistory.BreakGroups();
            cellName.KeyDown += (s, e) => { if (e.Control || new[] { Keys.Left, Keys.Right, Keys.Up, Keys.Down, Keys.Home, Keys.End }.Contains(e.KeyCode)) textHistory.BreakGroups(); };
            var history = new FlowLayoutPanel { Location = new Point(0, 112), Width = 360, Height = 34, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; SmallButton(history, "Cofnij nazwę", () => TextUndo("name", false), 116); SmallButton(history, "Ponów nazwę", () => TextUndo("name", true), 116); content.Controls.Add(history);
            content.Controls.Add(new Label { Text = "Opis (tekst, listy i tabele)", Location = new Point(0, 152), AutoSize = true });
            cellDescription.SetBounds(0, 178, 360, 570); cellDescription.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; content.Controls.Add(cellDescription);
            cellDescription.Changed = DescriptionEdited; cellDescription.HistoryRequested = repeat => TextUndo("description", repeat);
            cellDescription.SaveRequested = asNew => { if (asNew) SaveAsProject(); else SaveProject(); };
            cellDescription.BreakHistoryGroup = () => textHistory.BreakGroups();
            cellDescription.Error = message => ShowEditorMessage(message, "Opis heksu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            cellDescription.TableSizeRequested = () => { using (var d = new TableSizeDialog()) return RunModal(() => d.ShowDialog(this)) == DialogResult.OK ? new[] { d.Rows, d.Columns } : null; };
            // Establish anchors at the design width before responsive layout begins.
            scroll.Content = content; scroll.PerformLayout();
        }
    }
}
