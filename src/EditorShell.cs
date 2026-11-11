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
        internal static Bitmap ScrollArrow(bool up)
        {
            Bitmap image; string key = up ? "terrain-up" : "terrain-down";
            if (cache.TryGetValue(key, out image)) return image;
            if (up) { image = new Bitmap(9,5); using (var g = Graphics.FromImage(image)) g.FillPolygon(Brushes.WhiteSmoke, new[] { new Point(0,4), new Point(4,0), new Point(8,4) }); }
            else { image = (Bitmap)ScrollArrow(true).Clone(); image.RotateFlip(RotateFlipType.RotateNoneFlipY); }
            cache.Add(key,image); return image;
        }
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
                else if(key=="rectangle"){p.DashStyle=DashStyle.Dash;g.DrawRectangle(p,3,4,16,14);}
                else if(key=="lasso"){g.DrawEllipse(p,3,3,16,12);g.DrawBezier(p,new Point(8,14),new Point(2,22),new Point(16,21),new Point(12,15));}
                else if (key == "hand") { g.DrawEllipse(p, 5, 8, 12, 12); for (int x = 5; x <= 17; x += 4) g.DrawLine(p, x, 10, x, 2 + Math.Abs(x - 11) / 2); }
                else if (key == "brush") { using (var handle = new Pen(Color.FromArgb(218, 170, 91), 4)) g.DrawLine(handle, 18, 2, 10, 12); g.FillPolygon(Brushes.WhiteSmoke, new[] { new Point(8, 10), new Point(13, 14), new Point(10, 20), new Point(2, 20), new Point(5, 16) }); }
                else if (key == "fog" || key == "fogbucket") { g.FillEllipse(Brushes.SlateGray, 1, 9, 20, 10); g.FillEllipse(Brushes.LightSlateGray, 4, 4, 10, 12); g.FillEllipse(Brushes.LightSlateGray, 10, 7, 10, 11); if (key == "fogbucket") g.DrawLine(p, 2, 21, 20, 21); }
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
        FlowLayoutPanel paletteTools, presetActions;
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
            ShellButton(file, "Biblioteka", "library", "Pokaż lokalizację biblioteki", () => ShowEditorMessage("Lokalna biblioteka:\n" + LibraryPaths.Root + "\n\nPresety terenów można zapisywać w panelu po lewej. Stany obiektów są dostępne w menu kafelków w szczegółach instancji.", "Biblioteka", MessageBoxButtons.OK, MessageBoxIcon.Information), 98);
            ShellButton(file, "Tło", "open", "Tło mapy", EditBackground, 65);
            ConfigureButton(orientationToggle, "Pointy Top", "pointy", "Zmień orientację", () => ChangeOrientation(map.Orientation == "PointyTop" ? "FlatTop" : "PointyTop"), 128); view.Controls.Add(orientationToggle);
            ConfigureButton(labelToggle, "Osie", "labels", "Zmień tryb etykiet", CycleLabelMode, 110); view.Controls.Add(labelToggle);
            ConfigureButton(zoomButton, "100%", "fit", "Ustaw skalę 100%", SetZoom100, 62); zoomButton.Image = null; view.Controls.Add(zoomButton);
            BuildViewMenu(view);
            var fitMenu=new ContextMenuStrip();fitMenu.Items.Add("Dopasuj mapę",null,(s,e)=>FitMap());fitMenu.Items.Add("Dopasuj wszystko",null,(s,e)=>FitEverything());
            fitMenu.BackColor=Color.FromArgb(34,38,48);fitMenu.ForeColor=Color.WhiteSmoke;fitMenu.Renderer=new ToolStripProfessionalRenderer(new FitMenuColors());
            fitMenu.Opening+=(s,e)=>EndCanvasGesture(true);fitMenu.Closed+=(s,e)=>{waitingForMouseRelease=Control.MouseButtons!=MouseButtons.None;requireFreshCanvasDown=true;};
            var fitStrip = new ToolStrip { AutoSize = false, Size = new Size(94, 32), Margin = new Padding(1),
                Padding = Padding.Empty, GripStyle = ToolStripGripStyle.Hidden, CanOverflow = false,
                BackColor = Color.FromArgb(49, 54, 67), Renderer = new FitButtonRenderer() };
            var fitButton = new ToolStripDropDownButton("Dopasuj", EditorIcons.Small("fit"))
            {
                AutoSize = false, Size = new Size(94, 32), Margin = Padding.Empty, ShowDropDownArrow = false,
                ForeColor = ForeColor, AccessibleName = "Dopasuj mapę / Dopasuj wszystko",
                ToolTipText = "Dopasuj mapę / Dopasuj wszystko", DropDown = fitMenu
            };
            fitStrip.Items.Add(fitButton);
            view.Controls.Add(fitStrip);
            ShellButton(view, "Rozmiar", "labels", "Dodaj lub usuń wiersze i kolumny", ResizeProject, 90);
            mapName.TextAlign = ContentAlignment.MiddleCenter; mapName.Font = new Font(Font, FontStyle.Bold); mapName.AutoEllipsis = true; mapName.AccessibleName = "Nazwa projektu"; shellToolbar.Controls.Add(mapName);
            var status = new Panel { Dock = DockStyle.Bottom, Height = 27, BackColor = shellToolbar.BackColor }; statusLabel.Dock = DockStyle.Fill; statusLabel.Padding = new Padding(10, 4, 0, 0); status.Controls.Add(statusLabel); Controls.Add(status);
            canvas.BackColor = Color.FromArgb(25, 28, 36); canvas.TabStop = true; Controls.Add(canvas); canvas.SendToBack();
            canvas.Paint += PaintMap; canvas.MouseDown += CanvasMouseDown; canvas.MouseMove += CanvasMouseMove; canvas.MouseUp += CanvasMouseUp; canvas.MouseWheel += CanvasMouseWheel;
            canvas.MouseCaptureChanged += (s, e) => { if (!canvas.Capture && gestureButton != MouseButtons.None) EndCanvasGesture(true); };
            left.BackColor = shellToolbar.BackColor; left.AutoScroll = false; Controls.Add(left);
            leftToggle.FlatStyle = FlatStyle.Flat; StyleButton(leftToggle); leftToggle.AccessibleName = "Zwiń lub rozwiń paletę"; tips.SetToolTip(leftToggle, leftToggle.AccessibleName); leftToggle.Click += (s, e) => { leftCollapsed = !leftCollapsed; PositionOverlays(); canvas.Invalidate(); }; Controls.Add(leftToggle);
            BuildPalettePanel(); BuildDetailsPanel(); Controls.Add(right);
            left.BringToFront(); right.BringToFront(); leftToggle.BringToFront();
        }
        void LayoutMainToolbar()
        {
            fileGroup.Width = fileGroup.Controls.Cast<Control>().Sum(c => c.Width + c.Margin.Horizontal);
            viewGroup.Width = viewGroup.Controls.Cast<Control>().Sum(c => c.Width + c.Margin.Horizontal);
            fileGroup.Location = new Point(4, 4); viewGroup.Location = new Point(shellToolbar.ClientSize.Width - viewGroup.Width - 4, 4);
            int titleHeight = mapName.Font.Height + 2;
            mapName.Bounds = new Rectangle(fileGroup.Right + 8, 4 + (34 - titleHeight) / 2, Math.Max(0, viewGroup.Left - fileGroup.Right - 16), titleHeight);
        }
        void BuildPalettePanel() { BuildSections(); }
        Button SmallButton(Control parent, string text, Action action, int width)
        { var b = new Button { Text = text, Width = width, Height = 28, Margin = new Padding(2), FlatStyle = FlatStyle.Flat, AccessibleName = text }; StyleButton(b); b.Click += (s, e) => action(); parent.Controls.Add(b); return b; }
        void SelectTool(EditorTool tool)
        {
            EndCanvasGesture(true); currentTool = tool;
            foreach (var pair in toolButtons) { pair.Value.BackColor = pair.Key == tool ? Color.FromArgb(93, 111, 156) : Color.FromArgb(49, 54, 67); pair.Value.FlatAppearance.BorderSize = pair.Key == tool ? 3 : 1; pair.Value.AccessibleDescription = pair.Key == tool ? "Aktywne" : "Nieaktywne"; }
            UpdateBrushAvailability();
        }
        void BuildDetailsPanel()
        {
            right.BackColor = Color.FromArgb(34, 38, 48); right.Visible = false;
            var header = new Panel { Dock = DockStyle.Top, Height = 38 }; var title = detailTitle = new Label { Text = "Szczegóły heksu", Dock = DockStyle.Fill, Padding = new Padding(14, 10, 0, 0) }; header.Controls.Add(title);
            rightClose.Text = "Zamknij"; rightClose.Dock = DockStyle.Right; rightClose.Width = 85; StyleButton(rightClose); rightClose.Click += (s, e) => CloseDetailsPanel(); header.Controls.Add(rightClose);
            var scroll = new DetailsScrollPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(14) }; var content = new Panel { Location = new Point(14, 14), Height = 770, Width = 375 };
            scroll.Controls.Add(content); right.Controls.Add(scroll); right.Controls.Add(header);
            hexDetailScroll=scroll;
            InitializePanelResize(header);
            detailCoordinate.SetBounds(0, 0, 360, 24); detailTerrain.SetBounds(0, 26, 360, 28); detailTerrain.AutoEllipsis = true; content.Controls.Add(detailCoordinate); content.Controls.Add(detailTerrain);
            detailCoordinate.Anchor = detailTerrain.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            detailIcon.SetBounds(0,54,360,24);detailIcon.AutoEllipsis=true;detailIcon.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;content.Controls.Add(detailIcon);
            content.Controls.Add(new Label { Text = "Własna nazwa heksu", Location = new Point(0, 86), AutoSize = true });
            cellName.SetBounds(0, 110, 360, 26); cellName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; cellName.AccessibleName = "Własna nazwa heksu"; content.Controls.Add(cellName);
            cellName.TextChanged += (s, e) => NameEdited(); cellName.LostFocus += (s, e) => textHistory.BreakGroups();
            cellName.HistoryUndo = () => TextUndo("name", false);
            cellName.MouseDown += (s, e) => textHistory.BreakGroups();
            cellName.KeyDown += (s, e) => { if (e.Control || new[] { Keys.Left, Keys.Right, Keys.Up, Keys.Down, Keys.Home, Keys.End }.Contains(e.KeyCode)) textHistory.BreakGroups(); };
            var history = new FlowLayoutPanel { Location = new Point(0, 138), Width = 360, Height = 34, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; SmallButton(history, "Cofnij nazwę", () => TextUndo("name", false), 116); SmallButton(history, "Ponów nazwę", () => TextUndo("name", true), 116); content.Controls.Add(history);
            content.Controls.Add(new Label { Text = "Opis (tekst, listy i tabele)", Location = new Point(0, 178), AutoSize = true });
            cellDescription.SetBounds(0, 204, 360, 570); cellDescription.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; content.Controls.Add(cellDescription);content.Height=800;
            cellDescription.Changed = DescriptionEdited; cellDescription.HistoryRequested = repeat => TextUndo("description", repeat);
            cellDescription.SaveRequested = asNew => { if (asNew) SaveAsProject(); else SaveProject(); };
            cellDescription.BreakHistoryGroup = () => textHistory.BreakGroups();
            cellDescription.Error = message => ShowEditorMessage(message, "Opis heksu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            cellDescription.TableSizeRequested = () => { using (var d = new TableSizeDialog()) return RunModal(() => d.ShowDialog(this)) == DialogResult.OK ? new[] { d.Rows, d.Columns } : null; };
            // Establish anchors at the design width before responsive layout begins.
            scroll.Content = content; scroll.PerformLayout();
            BuildObjectDetails();
        }
    }
}
