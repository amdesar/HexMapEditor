using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace HexMapEditor
{
    public enum HexOrientation { PointyTop, FlatTop }
    public enum LabelMode { Axes, InCells, None }
    public enum EditorTool { Select, Hand, Brush }

    public sealed class CellData
    {
        public string Terrain { get; set; }
        public string Color { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string NotesFormat { get; set; }
        public CellData() { Terrain = "Teren neutralny"; Color = "#777F91"; Name = ""; Description = ""; NotesFormat = "plainText"; }
    }

    public sealed class MapData
    {
        public int FormatVersion { get; set; }
        public string Name { get; set; }
        public int Columns { get; set; }
        public int Rows { get; set; }
        public string Orientation { get; set; }
        public string LabelMode { get; set; }
        public List<CellData> Cells { get; set; }
        public MapData() { FormatVersion = 1; Name = "Nowa mapa"; Columns = 15; Rows = 10; Orientation = "PointyTop"; LabelMode = "Axes"; Cells = new List<CellData>(); }
    }

    internal sealed class TerrainState
    {
        public readonly string Terrain;
        public readonly string Color;
        public TerrainState(CellData cell) { Terrain = cell.Terrain; Color = cell.Color; }
        public bool Matches(CellData cell) { return String.Equals(Terrain, cell.Terrain, StringComparison.Ordinal) && String.Equals(Color, cell.Color, StringComparison.OrdinalIgnoreCase); }
        public void ApplyTo(CellData cell) { cell.Terrain = Terrain; cell.Color = Color; }
    }

    internal sealed class CellChange
    {
        public int Index;
        public TerrainState Before;
        public TerrainState After;
    }

    internal sealed class EditAction
    {
        public List<CellChange> Changes = new List<CellChange>();
        public string Label;
    }

    public sealed class MainWindow : Form
    {
        static readonly string[] TerrainNames = { "Teren neutralny", "Woda", "Równina", "Góry", "Pustynia", "Czerń / Pustka" };
        static readonly string[] TerrainColors = { "#777F91", "#287DB3", "#72A65B", "#8D8174", "#D1A94E", "#252735" };
        readonly JavaScriptSerializer json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 64 };
        readonly Panel canvas = new BufferedPanel();
        readonly Panel left = new Panel();
        readonly Panel right = new Panel();
        readonly Button leftToggle = new Button();
        readonly Button rightClose = new Button();
        readonly ComboBox terrainPicker = new ComboBox();
        readonly Button orientationToggle = new Button();
        readonly Button labelToggle = new Button();
        readonly TextBox mapName = new TextBox();
        readonly TextBox cellName = new TextBox();
        readonly TextBox cellDescription = new TextBox();
        readonly Label statusLabel = new Label();
        readonly Label dimensionsLabel = new Label();
        readonly Label coordinateLabel = new Label();
        readonly Button saveButton = new Button();
        readonly Button undoButton = new Button();
        readonly Button redoButton = new Button();
        readonly Button zoomButton = new Button();
        readonly Button fitButton = new Button();
        readonly ToolTip tips = new ToolTip();
        MapData map = NewMap(15, 10, "PointyTop");
        string currentPath;
        string savedSnapshot;
        int selected = -1;
        int brushIndex = 2;
        bool dirty, leftCollapsed, updating, dragging, panGesture;
        bool rightPanelOpen;
        EditorTool currentTool = EditorTool.Select;
        Point dragOrigin;
        PointF panOrigin;
        PointF pan = new PointF(500, 340);
        float scale = 1f;
        const float BaseRadius = 28f;
        const string AppVersion = "0.1.2";
        readonly Dictionary<int, TerrainState> strokeBefore = new Dictionary<int, TerrainState>();
        readonly Stack<EditAction> undo = new Stack<EditAction>();
        readonly Stack<EditAction> redo = new Stack<EditAction>();

        public MainWindow()
        {
            Text = "HexMapEditor " + AppVersion;
            MinimumSize = new Size(1180, 620);
            Size = new Size(1440, 900);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(25, 28, 36);
            ForeColor = Color.FromArgb(228, 231, 238);
            Font = new Font("Segoe UI", 9f);
            KeyPreview = true;

            var toolbar = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = Color.FromArgb(34, 38, 48), Padding = new Padding(10, 8, 10, 5) };
            Controls.Add(toolbar);
            var files = new FlowLayoutPanel { Dock = DockStyle.Left, Width = 510, WrapContents = false, BackColor = Color.Transparent };
            toolbar.Controls.Add(files);
            AddButton(files, "Nowa", NewProject);
            AddButton(files, "Otwórz", OpenProject);
            saveButton = AddButton(files, "Zapisz", () => SaveProject());
            AddButton(files, "Zapisz jako", () => SaveAsProject());
            undoButton = AddButton(files, "Cofnij", Undo);
            redoButton = AddButton(files, "Ponów", Redo);

            var view = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 650, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, BackColor = Color.Transparent };
            toolbar.Controls.Add(view);
            fitButton = AddButton(view, "Dopasuj", FitMap);
            zoomButton = AddButton(view, "100%", SetZoom100);
            zoomButton.Click += (s, e) => SetZoom100();
            labelToggle = AddButton(view, "Osie", CycleLabelMode); labelToggle.Width = 88; labelToggle.AutoSize = false;
            orientationToggle = AddButton(view, "Wierzchołkiem", () => ChangeOrientation(map.Orientation == "PointyTop" ? "FlatTop" : "PointyTop")); orientationToggle.Width = 154; orientationToggle.AutoSize = false;
            dimensionsLabel.Text = "15 × 10"; dimensionsLabel.Width = 64; dimensionsLabel.Height = 32; dimensionsLabel.Margin = new Padding(3); dimensionsLabel.TextAlign = ContentAlignment.MiddleCenter; dimensionsLabel.ForeColor = Color.FromArgb(210, 215, 225); view.Controls.Add(dimensionsLabel);
            var library = new Button { Text = "Biblioteka", Width = 84, Height = 32, Margin = new Padding(3), FlatStyle = FlatStyle.Flat };
            StyleButton(library); library.Click += (s, e) => { try { var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Library"); Directory.CreateDirectory(dir); System.Diagnostics.Process.Start("explorer.exe", dir); } catch (Exception ex) { MessageBox.Show(this, "Nie można otworzyć biblioteki: " + ex.Message, "Biblioteka", MessageBoxButtons.OK, MessageBoxIcon.Error); } }; view.Controls.Add(library);
            var nameWrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 8, 0), BackColor = Color.Transparent };
            toolbar.Controls.Add(nameWrap);
            mapName.Dock = DockStyle.Top; mapName.Height = 27; mapName.TextAlign = HorizontalAlignment.Center; mapName.BackColor = Color.FromArgb(44, 48, 59); mapName.ForeColor = ForeColor; mapName.BorderStyle = BorderStyle.FixedSingle; nameWrap.Controls.Add(mapName);
            mapName.TextChanged += (s, e) => { if (!updating) { map.Name = mapName.Text; MarkDirty(); } };

            statusLabel.Dock = DockStyle.Bottom; statusLabel.Height = 27; statusLabel.Padding = new Padding(12, 0, 0, 0); statusLabel.TextAlign = ContentAlignment.MiddleLeft; statusLabel.BackColor = Color.FromArgb(34, 38, 48); Controls.Add(statusLabel);
            canvas.Dock = DockStyle.None; canvas.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; canvas.BackColor = Color.FromArgb(21, 24, 31); canvas.TabStop = true; canvas.Paint += PaintMap; canvas.MouseDown += CanvasMouseDown; canvas.MouseMove += CanvasMouseMove; canvas.MouseUp += CanvasMouseUp; canvas.MouseWheel += CanvasMouseWheel; canvas.MouseLeave += (s, e) => { coordinateLabel.Text = "Kursor: —"; }; Controls.Add(canvas);
            left = BuildLeftPanel(); left.Width = 208; left.BackColor = Color.FromArgb(34, 38, 48); left.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom; Controls.Add(left);
            leftToggle.Text = "Zwiń panel  ‹"; leftToggle.Dock = DockStyle.None; leftToggle.Anchor = AnchorStyles.Left | AnchorStyles.Top; leftToggle.Height = 34; StyleButton(leftToggle); leftToggle.Click += (s, e) => { leftCollapsed = !leftCollapsed; left.Visible = !leftCollapsed; leftToggle.Text = leftCollapsed ? "›" : "Zwiń panel  ‹"; leftToggle.Width = leftCollapsed ? 36 : 208; PositionOverlays(); leftToggle.BringToFront(); }; Controls.Add(leftToggle);
            right = BuildRightPanel(); right.Width = 360; right.Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom; right.Visible = false; Controls.Add(right);
            rightClose.Text = "Zamknij panel  ×"; rightClose.Dock = DockStyle.Top; rightClose.Height = 36; StyleButton(rightClose); rightClose.Click += (s, e) => CloseDetailsPanel(); right.Controls.Add(rightClose);
            Shown += (s, e) => { PositionOverlays(); FitMap(); UpdateUI(); };
            Resize += (s, e) => PositionOverlays();
            FormClosing += OnFormClosing;
            KeyDown += OnKeyDown;
            savedSnapshot = json.Serialize(map);
            UpdateUI();
        }

        static MapData NewMap(int cols, int rows, string orientation)
        {
            var m = new MapData { Columns = cols, Rows = rows, Orientation = orientation, LabelMode = "Axes" };
            for (int i = 0; i < cols * rows; i++) m.Cells.Add(new CellData());
            return m;
        }

        Button AddButton(Control parent, string text, Action action)
        {
            var b = new Button { Text = text, AutoSize = true, MinimumSize = new Size(68, 32), Height = 32, Margin = new Padding(3), FlatStyle = FlatStyle.Flat, AutoEllipsis = false };
            StyleButton(b); b.Click += (s, e) => action(); parent.Controls.Add(b); return b;
        }

        ComboBox AddPicker(Control parent, string[] values, int width)
        {
            var c = new ComboBox { Width = width, Height = 32, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(3), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(44, 48, 59), ForeColor = ForeColor };
            c.Items.AddRange(values); c.SelectedIndex = 0; parent.Controls.Add(c); return c;
        }

        void StyleButton(Button b) { b.BackColor = Color.FromArgb(49, 54, 67); b.ForeColor = Color.FromArgb(236, 238, 243); b.FlatAppearance.BorderColor = Color.FromArgb(74, 81, 98); b.FlatAppearance.MouseOverBackColor = Color.FromArgb(65, 73, 90); b.Cursor = Cursors.Hand; }

        Panel BuildLeftPanel()
        {
            var p = new Panel { Padding = new Padding(10), AutoScroll = true };
            var content = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent, Width = 184 };
            p.Controls.Add(content);
            var toolTitle = new Label { Text = "PRZYBORNIK", Width = 184, Height = 30, ForeColor = Color.FromArgb(162, 173, 194), TextAlign = ContentAlignment.MiddleLeft };
            content.Controls.Add(toolTitle);
            var tools = new FlowLayoutPanel { Width = 188, Height = 40, WrapContents = false, BackColor = Color.Transparent, Margin = new Padding(0) };
            foreach (var item in new[] { "Zaznacz", "Rączka", "Pędzel" })
            {
                var b = new RadioButton { Text = item, Appearance = Appearance.Button, Width = item == "Zaznacz" ? 72 : 57, Height = 32, Margin = new Padding(1), TextAlign = ContentAlignment.MiddleCenter, Checked = item == "Zaznacz", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(49, 54, 67), ForeColor = Color.White };
                b.CheckedChanged += (s, e) => { if (b.Checked) { currentTool = item == "Rączka" ? EditorTool.Hand : item == "Pędzel" ? EditorTool.Brush : EditorTool.Select; canvas.Cursor = currentTool == EditorTool.Hand ? Cursors.Hand : currentTool == EditorTool.Brush ? Cursors.Cross : Cursors.Default; } };
                tools.Controls.Add(b);
            }
            content.Controls.Add(tools);
            var label = new Label { Text = "TEREN", Width = 184, Height = 32, ForeColor = Color.FromArgb(162, 173, 194), TextAlign = ContentAlignment.MiddleLeft };
            content.Controls.Add(label);
            terrainPicker.Width = 184; terrainPicker.Height = 30; terrainPicker.DropDownStyle = ComboBoxStyle.DropDownList; terrainPicker.BackColor = Color.FromArgb(44, 48, 59); terrainPicker.ForeColor = ForeColor; terrainPicker.Items.AddRange(TerrainNames); terrainPicker.SelectedIndex = brushIndex; terrainPicker.SelectedIndexChanged += (s, e) => { if (terrainPicker.SelectedIndex >= 0) brushIndex = terrainPicker.SelectedIndex; };
            content.Controls.Add(terrainPicker);
            var palette = new FlowLayoutPanel { Width = 188, Height = 176, WrapContents = true, BackColor = Color.Transparent, Margin = new Padding(0, 10, 0, 0) };
            for (int i = 0; i < TerrainNames.Length; i++)
            {
                int terrain = i;
                var swatch = new Button { Text = TerrainNames[i], Width = 86, Height = 48, Margin = new Padding(2), FlatStyle = FlatStyle.Flat, BackColor = ParseColor(TerrainColors[i]), ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter };
                swatch.FlatAppearance.BorderColor = Color.FromArgb(90, 100, 120); swatch.Click += (s, e) => { brushIndex = terrain; terrainPicker.SelectedIndex = terrain; }; palette.Controls.Add(swatch);
            }
            content.Controls.Add(palette);
            return p;
        }

        Panel BuildRightPanel()
        {
            var p = new Panel { Padding = new Padding(0), BackColor = Color.FromArgb(38, 42, 53) };
            var title = new Label { Text = "SZCZEGÓŁY HEKSU", Bounds = new Rectangle(12, 43, 336, 28), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, ForeColor = Color.FromArgb(162, 173, 194), TextAlign = ContentAlignment.MiddleLeft }; p.Controls.Add(title);
            var coord = new Label { Name = "cellCoord", Bounds = new Rectangle(12, 78, 336, 27), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Font = new Font(Font, FontStyle.Bold), Text = "—" }; p.Controls.Add(coord);
            var nameLabel = new Label { Text = "Nazwa własna", Bounds = new Rectangle(12, 111, 336, 22), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; p.Controls.Add(nameLabel);
            cellName.Bounds = new Rectangle(12, 134, 336, 28); cellName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; cellName.BackColor = Color.FromArgb(48, 53, 66); cellName.ForeColor = ForeColor; cellName.BorderStyle = BorderStyle.FixedSingle; p.Controls.Add(cellName);
            var terrainView = new Label { Name = "cellTerrain", Bounds = new Rectangle(12, 170, 336, 27), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Text = "Teren: —", TextAlign = ContentAlignment.MiddleLeft }; p.Controls.Add(terrainView);
            var descriptionLabel = new Label { Text = "Opis", Bounds = new Rectangle(12, 205, 336, 24), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; p.Controls.Add(descriptionLabel);
            cellDescription.Multiline = true; cellDescription.ScrollBars = ScrollBars.Vertical; cellDescription.Bounds = new Rectangle(12, 231, 336, 220); cellDescription.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; cellDescription.BackColor = Color.FromArgb(48, 53, 66); cellDescription.ForeColor = ForeColor; cellDescription.BorderStyle = BorderStyle.FixedSingle; p.Controls.Add(cellDescription);
            cellName.TextChanged += (s, e) => { if (!updating && selected >= 0) { map.Cells[selected].Name = cellName.Text; map.Cells[selected].NotesFormat = "plainText"; MarkDirty(); canvas.Invalidate(); } };
            cellDescription.TextChanged += (s, e) => { if (!updating && selected >= 0) { map.Cells[selected].Description = cellDescription.Text; map.Cells[selected].NotesFormat = "plainText"; MarkDirty(); } };
            return p;
        }

        void PositionOverlays()
        {
            int top = 88, bottom = 27;
            canvas.Bounds = new Rectangle(0, top, ClientSize.Width, Math.Max(0, ClientSize.Height - top - bottom));
            left.Bounds = new Rectangle(0, top, 208, Math.Max(0, ClientSize.Height - top - bottom));
            leftToggle.Bounds = new Rectangle(leftCollapsed ? 0 : 0, top, leftCollapsed ? 36 : 208, 34);
            int panelWidth = Math.Min(360, ClientSize.Width);
            right.Bounds = new Rectangle(Math.Max(0, ClientSize.Width - panelWidth), top, panelWidth, Math.Max(0, ClientSize.Height - top - bottom));
            left.BringToFront(); if (right.Visible) right.BringToFront(); leftToggle.BringToFront();
        }

        void CycleLabelMode()
        {
            map.LabelMode = map.LabelMode == "Axes" ? "InCells" : map.LabelMode == "InCells" ? "None" : "Axes";
            MarkDirty();
        }

        void CloseDetailsPanel()
        {
            selected = -1; rightPanelOpen = false; right.Visible = false; UpdateUI();
        }

        void MarkDirty() { dirty = !String.Equals(json.Serialize(map), savedSnapshot, StringComparison.Ordinal); UpdateUI(); }

        void UpdateUI()
        {
            updating = true;
            Text = "HexMapEditor " + AppVersion + (dirty ? " *" : ""); tips.SetToolTip(mapName, map.Name);
            saveButton.Text = dirty ? "Zapisz *" : "Zapisz"; saveButton.BackColor = dirty ? Color.FromArgb(83, 92, 122) : Color.FromArgb(49, 54, 67);
            undoButton.Enabled = undo.Count > 0; redoButton.Enabled = redo.Count > 0;
            zoomButton.Text = Math.Round(scale * 100) + "%"; tips.SetToolTip(zoomButton, "Skala względem bazowego rozmiaru rysowania. Kliknij, aby ustawić 100%.");
            statusLabel.Text = "Kursor: " + (selected >= 0 ? Coordinate(selected) : "—") + "     " + map.Columns + " × " + map.Rows + " pól     " + map.Orientation + "     Łącznie heksów: " + map.Cells.Count;
            dimensionsLabel.Text = map.Columns + " × " + map.Rows;
            mapName.Text = map.Name;
            orientationToggle.Text = map.Orientation == "FlatTop" ? "Płaskim bokiem" : "Wierzchołkiem";
            tips.SetToolTip(orientationToggle, map.Orientation == "FlatTop" ? "Orientacja: płaskim bokiem do góry. Kliknij, aby zmienić na wierzchołkiem do góry." : "Orientacja: wierzchołkiem do góry. Kliknij, aby zmienić na płaskim bokiem do góry.");
            labelToggle.Text = map.LabelMode == "InCells" ? "W polach" : map.LabelMode == "None" ? "Brak" : "Osie";
            tips.SetToolTip(labelToggle, "Etykiety: " + labelToggle.Text + ". Kliknij, aby przełączyć tryb.");
            if (selected >= 0 && selected < map.Cells.Count)
            {
                var cell = map.Cells[selected];
                cellName.Text = cell.Name; cellDescription.Text = cell.Description;
                right.Controls["cellCoord"].Text = "Współrzędne: " + Coordinate(selected);
                right.Controls["cellTerrain"].Text = "Teren: " + cell.Terrain;
            }
            else { selected = -1; rightPanelOpen = false; }
            right.Visible = rightPanelOpen && selected >= 0;
            PositionOverlays();
            if (right.Visible) { right.BringToFront(); leftToggle.BringToFront(); }
            updating = false;
            canvas.Invalidate();
        }

        bool ConfirmDiscard()
        {
            if (!dirty) return true;
            var result = MessageBox.Show(this, "Są niezapisane zmiany. Czy zapisać je przed kontynuowaniem?", "Niezapisane zmiany", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
            if (result == DialogResult.Cancel) return false;
            if (result == DialogResult.No) return true;
            return SaveProject();
        }

        void NewProject()
        {
            if (!ConfirmDiscard()) return;
            using (var f = new NewMapDialog(map.Columns, map.Rows, map.Orientation))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;
                map = NewMap(f.Columns, f.Rows, f.Orientation); currentPath = null; savedSnapshot = json.Serialize(map); dirty = false; selected = -1; rightPanelOpen = false; undo.Clear(); redo.Clear();
                RecalculateFitTransform(); UpdateUI();
            }
        }

        void ChangeOrientation(string orientation)
        {
            if (map.Orientation == orientation) return;
            map.Orientation = orientation; RecalculateFitTransform(); MarkDirty();
        }

        void OpenProject()
        {
            if (!ConfirmDiscard()) return;
            using (var d = new OpenFileDialog { Filter = "Projekt HexMapEditor (*.hexmap)|*.hexmap|Wszystkie pliki (*.*)|*.*", Title = "Otwórz mapę" })
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    var text = File.ReadAllText(d.FileName, Encoding.UTF8);
                    var loaded = json.Deserialize<MapData>(text);
                    if (loaded != null && String.IsNullOrEmpty(loaded.LabelMode)) loaded.LabelMode = "Axes";
                    ValidateMap(loaded);
                    map = loaded; currentPath = d.FileName; savedSnapshot = json.Serialize(map); dirty = false; selected = -1; rightPanelOpen = false; undo.Clear(); redo.Clear();
                    RecalculateFitTransform(); UpdateUI();
                }
                catch (Exception ex) { MessageBox.Show(this, "Nie można otworzyć projektu. Plik jest uszkodzony albo ma nieobsługiwaną wersję.\n\n" + ex.Message, "Błąd odczytu", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }

        bool SaveProject() { return String.IsNullOrEmpty(currentPath) ? SaveAsProject() : SaveTo(currentPath); }

        bool SaveAsProject()
        {
            using (var d = new SaveFileDialog { Filter = "Projekt HexMapEditor (*.hexmap)|*.hexmap", DefaultExt = "hexmap", AddExtension = true, FileName = ProjectFileName(map.Name), Title = "Zapisz mapę jako" })
            {
                if (d.ShowDialog(this) != DialogResult.OK) return false;
                return SaveTo(EnsureProjectExtension(d.FileName));
            }
        }

        static string ProjectFileName(string name)
        {
            string safe = SafeFileName(name);
            return safe.EndsWith(".hexmap", StringComparison.OrdinalIgnoreCase) ? safe : safe + ".hexmap";
        }

        static string EnsureProjectExtension(string path)
        {
            return path.EndsWith(".hexmap", StringComparison.OrdinalIgnoreCase) ? path : path + ".hexmap";
        }

        bool SaveTo(string path)
        {
            string temp = null;
            try
            {
                ValidateMap(map);
                string directory = Path.GetDirectoryName(Path.GetFullPath(path));
                temp = Path.Combine(directory, ".hexmap-" + Guid.NewGuid().ToString("N") + ".tmp");
                byte[] bytes = new UTF8Encoding(false).GetBytes(json.Serialize(map));
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
                currentPath = path; savedSnapshot = json.Serialize(map); dirty = false; UpdateUI(); return true;
            }
            catch (Exception ex)
            {
                if (temp != null) try { if (File.Exists(temp)) File.Delete(temp); } catch { }
                MessageBox.Show(this, "Zapis nie powiódł się. Poprzedni plik pozostał bez zmian.\n\n" + ex.Message, "Błąd zapisu", MessageBoxButtons.OK, MessageBoxIcon.Error); return false;
            }
        }

        static string SafeFileName(string name) { foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_'); return String.IsNullOrWhiteSpace(name) ? "Nowa mapa" : name; }

        static void ValidateMap(MapData m)
        {
            if (m == null) throw new InvalidDataException("Brak danych mapy.");
            if (m.FormatVersion != 1) throw new InvalidDataException("Nieobsługiwana wersja formatu: " + m.FormatVersion + ".");
            if (m.Columns < 1 || m.Columns > 200 || m.Rows < 1 || m.Rows > 200) throw new InvalidDataException("Wymiary mapy muszą mieścić się w zakresie 1–200.");
            if (m.Orientation != "PointyTop" && m.Orientation != "FlatTop") throw new InvalidDataException("Nieprawidłowa orientacja mapy.");
            if (m.LabelMode != "Axes" && m.LabelMode != "InCells" && m.LabelMode != "None") throw new InvalidDataException("Nieprawidłowy tryb etykiet.");
            if (m.Cells == null || m.Cells.Count != m.Columns * m.Rows) throw new InvalidDataException("Liczba pól nie zgadza się z wymiarami mapy.");
            foreach (var c in m.Cells)
            {
                uint color;
                if (c == null || String.IsNullOrWhiteSpace(c.Terrain) || String.IsNullOrEmpty(c.Color) || c.Color.Length != 7 || !c.Color.StartsWith("#") || !UInt32.TryParse(c.Color.Substring(1), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out color)) throw new InvalidDataException("Dane terenu lub kolor pola są nieprawidłowe.");
            }
        }

        void OnFormClosing(object sender, FormClosingEventArgs e) { if (!ConfirmDiscard()) e.Cancel = true; }
        void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Let the native text control handle Undo. TextBox has no Ctrl+Y Redo.
            if (e.Control && (e.KeyCode == Keys.Z || e.KeyCode == Keys.Y) && ActiveControl is TextBoxBase) return;
            if (e.Control && e.Shift && e.KeyCode == Keys.S) { SaveAsProject(); e.Handled = true; e.SuppressKeyPress = true; }
            else if (e.Control && !e.Shift && e.KeyCode == Keys.S) { SaveProject(); e.Handled = true; e.SuppressKeyPress = true; }
            else if (e.Control && e.KeyCode == Keys.Z) { Undo(); e.Handled = true; e.SuppressKeyPress = true; }
            else if (e.Control && e.KeyCode == Keys.Y) { Redo(); e.Handled = true; e.SuppressKeyPress = true; }
        }

        string Coordinate(int index)
        {
            int col = index % map.Columns, row = index / map.Columns;
            string letters = ""; int n = col + 1;
            while (n > 0) { n--; letters = (char)('A' + n % 26) + letters; n /= 26; }
            return letters + (row + 1);
        }

        static Color ParseColor(string value) { return ColorTranslator.FromHtml(value); }
        PointF Center(int col, int row)
        {
            float r = BaseRadius;
            if (map.Orientation == "FlatTop") return new PointF(1.5f * r * col, (float)(Math.Sqrt(3) * r * (row + (col & 1) * 0.5f)));
            return new PointF((float)(Math.Sqrt(3) * r * (col + (row & 1) * 0.5f)), 1.5f * r * row);
        }

        void FitMap()
        {
            if (!RecalculateFitTransform()) return;
            UpdateUI();
        }

        bool RecalculateFitTransform()
        {
            if (canvas.ClientSize.Width < 20 || canvas.ClientSize.Height < 20) return false;
            RectangleF bounds = MapBounds();
            float leftInset = leftCollapsed ? 36f : 208f;
            float viewWidth = Math.Max(100, canvas.ClientSize.Width - leftInset);
            float centerX = leftInset + viewWidth / 2f;
            scale = Math.Max(.08f, Math.Min(1.7f, Math.Min((viewWidth - 80) / Math.Max(bounds.Width, 1), (canvas.ClientSize.Height - 80) / Math.Max(bounds.Height, 1))));
            pan = new PointF(centerX - (bounds.Left + bounds.Width / 2f) * scale, canvas.ClientSize.Height / 2f - (bounds.Top + bounds.Height / 2f) * scale);
            return true;
        }

        RectangleF MapBounds()
        {
            float r = BaseRadius, sqrt3 = (float)Math.Sqrt(3);
            if (map.Orientation == "FlatTop")
            {
                float maxX = 1.5f * r * (map.Columns - 1);
                float maxY = sqrt3 * r * (map.Rows - 1 + (map.Columns > 1 ? 0.5f : 0f));
                return RectangleF.FromLTRB(-r, -sqrt3 * r / 2f, maxX + r, maxY + sqrt3 * r / 2f);
            }
            float pointyMaxX = sqrt3 * r * (map.Columns - 1 + (map.Rows > 1 ? 0.5f : 0f));
            float pointyMaxY = 1.5f * r * (map.Rows - 1);
            return RectangleF.FromLTRB(-sqrt3 * r / 2f, -r, pointyMaxX + sqrt3 * r / 2f, pointyMaxY + r);
        }

        void SetZoom100()
        {
            var center = new PointF(canvas.ClientSize.Width / 2f, canvas.ClientSize.Height / 2f);
            float factor = 1f / scale; pan = new PointF(center.X - (center.X - pan.X) * factor, center.Y - (center.Y - pan.Y) * factor); scale = 1f; UpdateUI();
        }

        PointF ScreenCenter(int col, int row) { var p = Center(col, row); return new PointF(pan.X + p.X * scale, pan.Y + p.Y * scale); }

        void PaintMap(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(canvas.BackColor);
            int margin = (int)(BaseRadius * scale * 2.5f);
            for (int row = 0; row < map.Rows; row++) for (int col = 0; col < map.Columns; col++)
            {
                var c = ScreenCenter(col, row);
                if (c.X < -margin || c.Y < -margin || c.X > canvas.Width + margin || c.Y > canvas.Height + margin) continue;
                int i = row * map.Columns + col;
                using (var path = HexPath(c, BaseRadius * scale))
                using (var brush = new SolidBrush(ParseColor(map.Cells[i].Color)))
                using (var pen = new Pen(i == selected ? Color.FromArgb(255, 239, 190, 89) : Color.FromArgb(135, 19, 23, 30), i == selected ? 2.4f : 1.2f))
                { e.Graphics.FillPath(brush, path); e.Graphics.DrawPath(pen, path); }
                if (map.LabelMode == "InCells" && scale > .28f) using (var b = new SolidBrush(Color.FromArgb(220, 245, 245, 245))) using (var f = new Font("Segoe UI", Math.Max(6f, 8f * scale))) { var rect = new RectangleF(c.X - 15 * scale, c.Y + BaseRadius * .42f * scale, 30 * scale, 12 * scale); e.Graphics.DrawString(Coordinate(i), f, b, rect, new StringFormat { Alignment = StringAlignment.Center }); }
            }
            if (map.LabelMode == "Axes" && map.Columns * map.Rows <= 1400)
            {
                using (var b = new SolidBrush(Color.FromArgb(185, 188, 198, 213))) using (var f = new Font("Segoe UI", 8f))
                {
                    RectangleF bounds = MapBounds();
                    float labelsTop = pan.Y + bounds.Top * scale - 20;
                    float labelsLeft = pan.X + bounds.Left * scale - 24;
                    for (int c = 0; c < map.Columns; c++) { var p = ScreenCenter(c, 0); string s = Coordinate(c); e.Graphics.DrawString(s.Substring(0, s.Length - 1), f, b, p.X - 16, labelsTop); }
                    for (int r = 0; r < map.Rows; r++) { var p = ScreenCenter(0, r); e.Graphics.DrawString((r + 1).ToString(), f, b, labelsLeft, p.Y - 8); }
                }
            }
        }

        GraphicsPath HexPath(PointF center, float radius)
        {
            var pts = new PointF[6]; float offset = map.Orientation == "FlatTop" ? 0 : -30;
            if (map.Orientation == "PointyTop") offset = -90;
            for (int i = 0; i < 6; i++) { double a = Math.PI / 180 * (60 * i + offset); pts[i] = new PointF(center.X + radius * (float)Math.Cos(a), center.Y + radius * (float)Math.Sin(a)); }
            var path = new GraphicsPath(); path.AddPolygon(pts); return path;
        }

        int HitTest(Point p)
        {
            double best = Double.MaxValue; int found = -1; float threshold = BaseRadius * scale * 1.04f;
            for (int row = 0; row < map.Rows; row++) for (int col = 0; col < map.Columns; col++)
            {
                var c = ScreenCenter(col, row); double dx = p.X - c.X, dy = p.Y - c.Y, d = dx * dx + dy * dy;
                if (d < best) { best = d; found = row * map.Columns + col; }
            }
            return best <= threshold * threshold ? found : -1;
        }

        void CanvasMouseDown(object sender, MouseEventArgs e)
        {
            canvas.Focus();
            if (e.Button == MouseButtons.Middle || (e.Button == MouseButtons.Left && currentTool == EditorTool.Hand)) { panGesture = true; dragOrigin = e.Location; panOrigin = pan; return; }
            if (e.Button != MouseButtons.Left) return;
            dragging = false; dragOrigin = e.Location;
            canvas.Capture = true;
            if (currentTool == EditorTool.Brush)
            {
                strokeBefore.Clear(); int hit = HitTest(e.Location); if (hit >= 0) PaintCell(hit); canvas.Capture = true;
            }
        }

        void CanvasMouseMove(object sender, MouseEventArgs e)
        {
            int hit = HitTest(e.Location); coordinateLabel.Text = "Kursor: " + (hit >= 0 ? Coordinate(hit) : "—"); statusLabel.Text = "Kursor: " + (hit >= 0 ? Coordinate(hit) : "—") + "     " + map.Columns + " × " + map.Rows + " pól     " + map.Orientation + "     Łącznie heksów: " + map.Cells.Count;
            if (panGesture && (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Left)) { pan = new PointF(panOrigin.X + e.X - dragOrigin.X, panOrigin.Y + e.Y - dragOrigin.Y); canvas.Invalidate(); return; }
            if (e.Button == MouseButtons.Left && currentTool == EditorTool.Select && (Math.Abs(e.X - dragOrigin.X) + Math.Abs(e.Y - dragOrigin.Y) > 3))
            {
                if (!dragging) { dragging = true; panOrigin = pan; }
                pan = new PointF(panOrigin.X + e.X - dragOrigin.X, panOrigin.Y + e.Y - dragOrigin.Y); canvas.Invalidate();
            }
            if (e.Button == MouseButtons.Left && currentTool == EditorTool.Brush) { if (Math.Abs(e.X - dragOrigin.X) + Math.Abs(e.Y - dragOrigin.Y) > 3) dragging = true; if (hit >= 0) PaintCell(hit); }
        }

        void CanvasMouseUp(object sender, MouseEventArgs e)
        {
            if (panGesture) { panGesture = false; return; }
            if (e.Button != MouseButtons.Left) return;
            if (currentTool == EditorTool.Brush) { FinishStroke(); return; }
            if (currentTool == EditorTool.Hand) return;
            if (!dragging)
            {
                int hit = HitTest(e.Location);
                if (hit >= 0 && hit == selected && rightPanelOpen) { selected = -1; rightPanelOpen = false; }
                else if (hit >= 0) { selected = hit; rightPanelOpen = true; }
                UpdateUI();
            }
        }

        void CanvasMouseWheel(object sender, MouseEventArgs e)
        {
            float old = scale, next = Math.Max(.08f, Math.Min(4f, old * (e.Delta > 0 ? 1.12f : 1f / 1.12f)));
            float factor = next / old; pan = new PointF(e.X - (e.X - pan.X) * factor, e.Y - (e.Y - pan.Y) * factor); scale = next; UpdateUI();
        }

        void PaintCell(int index)
        {
            if (index < 0 || index >= map.Cells.Count) return;
            if (String.Equals(map.Cells[index].Terrain, TerrainNames[brushIndex], StringComparison.Ordinal) && String.Equals(map.Cells[index].Color, TerrainColors[brushIndex], StringComparison.OrdinalIgnoreCase)) return;
            if (!strokeBefore.ContainsKey(index)) strokeBefore[index] = new TerrainState(map.Cells[index]);
            map.Cells[index].Terrain = TerrainNames[brushIndex]; map.Cells[index].Color = TerrainColors[brushIndex]; dirty = true; canvas.Invalidate();
        }

        void FinishStroke()
        {
            if (strokeBefore.Count == 0) return;
            var action = new EditAction { Label = "Pociągnięcie pędzla" };
            foreach (var item in strokeBefore)
            {
                var cell = map.Cells[item.Key];
                // Preserve the original HTML color spelling for a net-empty stroke.
                if (item.Value.Matches(cell)) { item.Value.ApplyTo(cell); continue; }
                action.Changes.Add(new CellChange { Index = item.Key, Before = item.Value, After = new TerrainState(cell) });
            }
            strokeBefore.Clear();
            if (action.Changes.Count == 0) { MarkDirty(); return; }
            undo.Push(action); while (undo.Count > 300) { var keep = undo.Reverse().Skip(1).ToList(); undo.Clear(); foreach (var a in keep) undo.Push(a); }
            redo.Clear(); MarkDirty();
        }

        void Undo() { if (undo.Count == 0) return; var a = undo.Pop(); foreach (var c in a.Changes) c.Before.ApplyTo(map.Cells[c.Index]); redo.Push(a); MarkDirty(); }
        void Redo() { if (redo.Count == 0) return; var a = redo.Pop(); foreach (var c in a.Changes) c.After.ApplyTo(map.Cells[c.Index]); undo.Push(a); MarkDirty(); }
    }

    internal sealed class BufferedPanel : Panel
    {
        public BufferedPanel() { DoubleBuffered = true; ResizeRedraw = true; }
    }

    internal sealed class NewMapDialog : Form
    {
        public int Columns { get; private set; }
        public int Rows { get; private set; }
        public string Orientation { get; private set; }
        readonly NumericUpDown cols = new NumericUpDown { Minimum = 1, Maximum = 200, Value = 15, Width = 90 };
        readonly NumericUpDown rows = new NumericUpDown { Minimum = 1, Maximum = 200, Value = 10, Width = 90 };
        readonly ComboBox orient = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        public NewMapDialog(int c, int r, string o)
        {
            Columns = 15; Rows = 10; Orientation = "PointyTop";
            Text = "Nowa mapa"; FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent; MaximizeBox = false; MinimizeBox = false; ClientSize = new Size(380, 215); BackColor = Color.FromArgb(34, 38, 48); ForeColor = Color.White; Font = new Font("Segoe UI", 9f);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, RowCount = 4 }; layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57)); Controls.Add(layout);
            layout.Controls.Add(new Label { Text = "Kolumny", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0); layout.Controls.Add(cols, 1, 0); cols.Value = c;
            layout.Controls.Add(new Label { Text = "Wiersze", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 1); layout.Controls.Add(rows, 1, 1); rows.Value = r;
            layout.Controls.Add(new Label { Text = "Orientacja", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 2); orient.Items.AddRange(new[] { "Wierzchołkiem do góry", "Płaskim bokiem do góry" }); orient.SelectedIndex = o == "FlatTop" ? 1 : 0; layout.Controls.Add(orient, 1, 2);
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill }; var ok = new Button { Text = "Utwórz", DialogResult = DialogResult.OK, Width = 92, Height = 32 }; var cancel = new Button { Text = "Anuluj", DialogResult = DialogResult.Cancel, Width = 92, Height = 32 }; buttons.Controls.Add(ok); buttons.Controls.Add(cancel); layout.Controls.Add(buttons, 0, 3); layout.SetColumnSpan(buttons, 2); AcceptButton = ok; CancelButton = cancel;
            ok.Click += (s, e) => { Columns = (int)cols.Value; Rows = (int)rows.Value; Orientation = orient.SelectedIndex == 1 ? "FlatTop" : "PointyTop"; };
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            try { Application.Run(new MainWindow()); }
            catch (Exception ex) { MessageBox.Show("Aplikacja napotkała błąd:\n\n" + ex.ToString(), "HexMapEditor — błąd", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
}
