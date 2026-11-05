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
    public enum EditorTool { Select, Hand, Brush, Bucket }

    internal static class LibraryPaths
    {
        public static string Root { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Library"); } }
        public static string Hexmaps { get { return Path.Combine(Root, "Hexmaps"); } }
        public static void EnsureDirectories(string applicationDirectory)
        {
            string root = Path.Combine(applicationDirectory, "Library");
            foreach (string folder in new[] { "Backgrounds", "Terrains", "Icons", "Objects", "Hexmaps", Path.Combine("Exports", "Images"), Path.Combine("Exports", "Text") })
                Directory.CreateDirectory(Path.Combine(root, folder));
        }
    }

    internal static class OrientationNames
    {
        public static string Display(string orientation) { return orientation == "FlatTop" ? "Flat Top" : "Pointy Top"; }
    }

    public sealed class CellData
    {
        public string Id { get; set; }
        public string TerrainId { get; set; }
        public string VariantId { get; set; }
        public int TerrainRevision { get; set; }
        public string Terrain { get; set; }
        public string Color { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string NotesFormat { get; set; }
        public string RichDescription { get; set; }
        public CellData() { Id = Guid.NewGuid().ToString("N"); TerrainId = PresetCatalog.NeutralId; VariantId = PresetCatalog.NeutralVariant; TerrainRevision = 1; Terrain = "Teren neutralny"; Color = "#777F91"; Name = ""; Description = ""; NotesFormat = "plainText"; }
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
        public int RowParity { get; set; }
        public int ColumnParity { get; set; }
        public string ActivePresetId { get; set; }
        public List<TerrainPreset> Presets { get; set; }
        public MapData() { FormatVersion = 2; Name = "Nowa mapa"; Columns = 15; Rows = 10; Orientation = "PointyTop"; LabelMode = "Axes"; Cells = new List<CellData>(); ActivePresetId = PresetCatalog.DefaultId; Presets = new List<TerrainPreset> { PresetCatalog.Default() }; }
    }

    internal sealed class TerrainState
    {
        public readonly string Terrain;
        public readonly string Color;
        public readonly string TerrainId, VariantId;
        public readonly int Revision;
        public TerrainState(CellData cell) { Terrain = cell.Terrain; Color = cell.Color; TerrainId = cell.TerrainId; VariantId = cell.VariantId; Revision = cell.TerrainRevision; }
        public bool Matches(CellData cell) { return VariantId == cell.VariantId && TerrainId == cell.TerrainId && Revision == cell.TerrainRevision && String.Equals(Terrain, cell.Terrain, StringComparison.Ordinal) && String.Equals(Color, cell.Color, StringComparison.OrdinalIgnoreCase); }
        public void ApplyTo(CellData cell) { cell.Terrain = Terrain; cell.Color = Color; cell.TerrainId = TerrainId; cell.VariantId = VariantId; cell.TerrainRevision = Revision; }
    }

    internal sealed class CellChange
    {
        public int Index;
        public string CellId;
        public TerrainState Before;
        public TerrainState After;
    }

    internal sealed class EditAction
    {
        public List<CellChange> Changes = new List<CellChange>();
        public string Label;
        public ResizeState BeforeResize, AfterResize;
        public PaletteState BeforePalette, AfterPalette;
    }

    public sealed partial class MainWindow : Form
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
        readonly Label mapName = new Label();
        readonly HistoryNameBox cellName = new HistoryNameBox();
        readonly RichNoteEditor cellDescription = new RichNoteEditor();
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
        MouseButtons gestureButton = MouseButtons.None;
        EditorTool gestureTool;
        bool waitingForMouseRelease, requireFreshCanvasDown;
        int modalDepth;
        EditorTool currentTool = EditorTool.Select;
        Point dragOrigin;
        PointF panOrigin;
        PointF pan = new PointF(500, 340);
        float scale = 1f;
        const float BaseRadius = 28f;
        const string AppVersion = "0.2.1";
        readonly SavedMapState savedState = new SavedMapState();
        readonly TextHistoryStore textHistory = new TextHistoryStore();
        string editorCellId;
        List<TerrainPreset> libraryPresets = new List<TerrainPreset>();
        readonly Dictionary<int, TerrainState> strokeBefore = new Dictionary<int, TerrainState>();
        readonly Stack<EditAction> undo = new Stack<EditAction>();
        readonly Stack<EditAction> redo = new Stack<EditAction>();

        public MainWindow()
        {
            Text = "HexMapEditor " + AppVersion; MinimumSize = new Size(1180, 680); Size = new Size(1440, 940);
            StartPosition = FormStartPosition.CenterScreen; BackColor = Color.FromArgb(25, 28, 36); ForeColor = Color.FromArgb(228, 231, 238); Font = new Font("Segoe UI", 9f); KeyPreview = true;
            InitializeShell();
            Shown += (s, e) => { PositionOverlays(); FitMap(); UpdateUI(); LoadLibraryPresets(true); };
            Resize += (s, e) => PositionOverlays(); FormClosing += OnFormClosing; KeyDown += OnKeyDown;
            CaptureSavedState(); UpdateUI();
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

        void PositionOverlays()
        {
            if (shellToolbar == null) return;
            LayoutMainToolbar();
            int top = shellToolbar.Height, bottom = 27, height = Math.Max(0, ClientSize.Height - top - bottom);
            canvas.Bounds = new Rectangle(0, top, ClientSize.Width, height);
            left.Bounds = new Rectangle(0, top + 30, 220, Math.Max(0, height - 30));
            leftToggle.Bounds = new Rectangle(0, top, leftCollapsed ? 28 : 220, leftCollapsed ? height : 30);
            leftToggle.Text = leftCollapsed ? "›" : "Zwiń panel  ‹"; left.Visible = !leftCollapsed;
            int width = ClampRightWidth(requestedRightWidth); right.Bounds = new Rectangle(Math.Max(0, ClientSize.Width - width), top, width, height);
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

        void MarkDirty() { dirty = savedState.CheckAll(map); UpdateUI(); }
        void CaptureSavedState() { savedSnapshot = json.Serialize(map); savedState.Capture(map); dirty = false; }
        void MarkCellDirty(CellData cell) { dirty = savedState.CheckCell(cell); UpdateCaption(); canvas.Invalidate(); }
        static string ProjectTitle(string path, string initial)
        {
            if (String.IsNullOrEmpty(path)) return String.IsNullOrWhiteSpace(initial) ? "Nowa mapa" : initial;
            string name = Path.GetFileName(path); return name.EndsWith(".hexmap", StringComparison.OrdinalIgnoreCase) ? name.Substring(0, name.Length - 7) : name;
        }
        void UpdateCaption()
        {
            Text = "HexMapEditor " + AppVersion + (dirty ? " *" : ""); mapName.Text = ProjectTitle(currentPath, map.Name) + (dirty ? " *" : ""); tips.SetToolTip(mapName, ProjectTitle(currentPath, map.Name));
            saveButton.Text = dirty ? "Zapisz *" : "Zapisz"; saveButton.BackColor = dirty ? Color.FromArgb(83, 92, 122) : Color.FromArgb(49, 54, 67);
        }

        void UpdateUI()
        {
            updating = true;
            try
            {
                UpdateCaption(); undoButton.Enabled = undo.Count > 0; redoButton.Enabled = redo.Count > 0;
                zoomButton.Text = Math.Round(scale * 100) + "%"; tips.SetToolTip(zoomButton, "Skala względem bazowego rozmiaru rysowania. Kliknij, aby ustawić 100%.");
                statusLabel.Text = "Kursor: " + (selected >= 0 ? Coordinate(selected) : "—") + "     " + map.Columns + " × " + map.Rows + " pól     " + OrientationNames.Display(map.Orientation) + "     Łącznie heksów: " + map.Cells.Count;
                dimensionsLabel.Text = map.Columns + " × " + map.Rows;
                orientationToggle.Text = OrientationNames.Display(map.Orientation); orientationToggle.Image = EditorIcons.Small(map.Orientation == "FlatTop" ? "flat" : "pointy");
                tips.SetToolTip(orientationToggle, map.Orientation == "FlatTop" ? "Flat Top: pozioma krawędź na górze" : "Pointy Top: wierzchołek na górze");
                labelToggle.Text = map.LabelMode == "InCells" ? "W polach" : map.LabelMode == "None" ? "Brak" : "Osie";
                if (selected >= 0 && selected < map.Cells.Count)
                {
                    var cell = map.Cells[selected];
                    if (editorCellId != cell.Id) { textHistory.BreakGroups(); editorCellId = cell.Id; cellName.Text = cell.Name ?? ""; cellDescription.SetContent(cell.RichDescription, cell.Description); }
                    detailCoordinate.Text = "Współrzędne: " + Coordinate(selected); detailTerrain.Text = "Teren: " + cell.Terrain;
                }
                else { selected = -1; rightPanelOpen = false; editorCellId = null; }
                right.Visible = rightPanelOpen && selected >= 0;
            }
            finally { updating = false; }
            PositionOverlays(); canvas.Invalidate();
        }

        bool ConfirmDiscard()
        {
            if (!dirty) return true;
            var result = ShowEditorMessage("Są niezapisane zmiany. Czy zapisać je przed kontynuowaniem?", "Niezapisane zmiany", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
            if (result == DialogResult.Cancel) return false;
            if (result == DialogResult.No) return true;
            return SaveProject();
        }

        void NewProject()
        {
            if (!ConfirmDiscard()) return;
            var choices = AvailablePresets();
            using (var f = new NewMapDialog(map.Orientation, choices))
            {
                if (RunModal(() => f.ShowDialog(this)) != DialogResult.OK) return;
                map = NewMap(f.Columns, f.Rows, f.Orientation); map.Name = f.ProjectName;
                var preset = f.Preset.Copy(); map.Presets = new List<TerrainPreset> { preset }; map.ActivePresetId = preset.Id;
                currentPath = null; selected = -1; rightPanelOpen = false; editorCellId = null; undo.Clear(); redo.Clear(); textHistory.Clear();
                CaptureSavedState(); RefreshPresetPicker(); RecalculateFitTransform(); UpdateUI();
            }
        }

        void ChangeOrientation(string orientation)
        {
            if (map.Orientation == orientation) return;
            EndCanvasGesture(true);
            map.Orientation = orientation; RecalculateFitTransform(); MarkDirty();
        }

        void OpenProject()
        {
            if (!ConfirmDiscard()) return;
            using (var d = new OpenFileDialog { Filter = "Projekt HexMapEditor (*.hexmap)|*.hexmap|Wszystkie pliki (*.*)|*.*", Title = "Otwórz mapę", RestoreDirectory = true })
            {
                if (RunModal(() => d.ShowDialog(this)) != DialogResult.OK) return;
                string error; bool migrated;
                if (!TryLoadProject(d.FileName, out error, out migrated)) ShowEditorMessage("Nie można otworzyć projektu.\n\n" + error, "Błąd odczytu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else if (migrated) ShowEditorMessage("Wczytano mapę formatu 1 bez utraty zwykłych opisów. Następny zapis użyje formatu 2, którego wersje 0.1.x nie odczytają. Zapisz jako pozwala zachować oryginał.", "Format mapy", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        bool TryLoadProject(string path, out string error, out bool migrated)
        {
            error = null; migrated = false;
            try
            {
                if (new FileInfo(path).Length > 128L * 1024 * 1024) throw new InvalidDataException("Plik przekracza limit odczytu 128 MiB.");
                var raw = json.DeserializeObject(File.ReadAllText(path, Encoding.UTF8)) as Dictionary<string, object>;
                if (raw == null || !raw.ContainsKey("FormatVersion") || !(raw["FormatVersion"] is int)) throw new InvalidDataException("Brak jawnego numeru formatu mapy.");
                if ((int)raw["FormatVersion"] == 2)
                {
                    var cells = raw.ContainsKey("Cells") ? raw["Cells"] as object[] : null;
                    if (cells == null || cells.Any(c => !(c is Dictionary<string, object>) || new[] { "Id", "TerrainId", "VariantId", "TerrainRevision" }.Any(key => !((Dictionary<string, object>)c).ContainsKey(key)))) throw new InvalidDataException("Format 2 wymaga utrwalonych identyfikatorów heksów i wersji terenów.");
                }
                var loaded = json.ConvertToType<MapData>(raw); migrated = loaded != null && loaded.FormatVersion == 1; loaded = MapLogic.Migrate(loaded); ValidateMap(loaded);
                map = loaded; currentPath = Path.GetFullPath(path); selected = -1; rightPanelOpen = false; editorCellId = null; undo.Clear(); redo.Clear(); textHistory.Clear();
                CaptureSavedState(); RefreshPresetPicker(); RecalculateFitTransform(); UpdateUI(); return true;
            }
            catch (Exception ex) { error = ex.Message; migrated = false; return false; }
        }

        bool SaveProject() { return String.IsNullOrEmpty(currentPath) ? SaveAsProject() : SaveTo(currentPath); }

        bool SaveAsProject()
        {
            if (String.IsNullOrEmpty(currentPath) && !EnsureLibrary()) return false;
            using (var d = CreateSaveDialog())
            {
                if (RunModal(() => d.ShowDialog(this)) != DialogResult.OK) return false;
                return SaveAsTo(d.FileName, path => ShowEditorMessage("Plik już istnieje:\n\n" + path + "\n\nCzy chcesz go zastąpić?", "Potwierdź nadpisanie", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2));
            }
        }

        SaveFileDialog CreateSaveDialog()
        {
            return new SaveFileDialog { Filter = "Projekt HexMapEditor (*.hexmap)|*.hexmap", DefaultExt = "hexmap", AddExtension = false, OverwritePrompt = false, RestoreDirectory = true, InitialDirectory = String.IsNullOrEmpty(currentPath) ? LibraryPaths.Hexmaps : Path.GetDirectoryName(Path.GetFullPath(currentPath)), FileName = ProjectFileName(ProjectTitle(currentPath, map.Name)), Title = "Zapisz mapę jako" };
        }

        bool SaveAsTo(string path, Func<string, DialogResult> confirmOverwrite)
        {
            string finalPath = Path.GetFullPath(EnsureProjectExtension(path));
            bool exists = File.Exists(finalPath);
            if (exists && confirmOverwrite(finalPath) != DialogResult.Yes) return false;
            // If a new target appears after this check, File.Move fails instead of replacing it.
            return SaveTo(finalPath, exists);
        }

        bool EnsureLibrary()
        {
            try { LibraryPaths.EnsureDirectories(AppDomain.CurrentDomain.BaseDirectory); return true; }
            catch (Exception ex) { ShowEditorMessage("Nie można przygotować lokalnej biblioteki:\n\n" + LibraryPaths.Root + "\n\n" + ex.Message + "\n\nDane nie zostały przeniesione do innego katalogu.", "Błąd biblioteki", MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }
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
            return SaveTo(path, true);
        }

        bool SaveTo(string path, bool allowOverwrite)
        {
            string error;
            if (TrySaveTo(path, allowOverwrite, out error)) return true;
            ShowEditorMessage("Zapis nie powiódł się. Poprzedni plik pozostał bez zmian.\n\n" + error, "Błąd zapisu", MessageBoxButtons.OK, MessageBoxIcon.Error); return false;
        }

        bool TrySaveTo(string path, bool allowOverwrite, out string error)
        {
            error = null;
            string temp = null;
            try
            {
                ValidateMap(map);
                string directory = Path.GetDirectoryName(Path.GetFullPath(path));
                temp = Path.Combine(directory, ".hexmap-" + Guid.NewGuid().ToString("N") + ".tmp");
                byte[] bytes = new UTF8Encoding(false).GetBytes(json.Serialize(map));
                if (bytes.LongLength > 128L * 1024 * 1024) throw new InvalidDataException("Mapa przekracza limit pliku 128 MiB. Zmniejsz zawartość opisów przed zapisem.");
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                if (allowOverwrite && File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
                currentPath = path; CaptureSavedState(); UpdateUI(); return true;
            }
            catch (Exception ex)
            {
                if (temp != null) try { if (File.Exists(temp)) File.Delete(temp); } catch { }
                error = ex.Message; return false;
            }
        }

        static string SafeFileName(string name) { foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_'); return String.IsNullOrWhiteSpace(name) ? "Nowa mapa" : name; }

        static void ValidateMap(MapData m)
        {
            if (m == null) throw new InvalidDataException("Brak danych mapy.");
            if (m.FormatVersion != 2) throw new InvalidDataException("Nieobsługiwana wersja formatu: " + m.FormatVersion + ".");
            if (m.Columns < 1 || m.Columns > 200 || m.Rows < 1 || m.Rows > 200) throw new InvalidDataException("Wymiary mapy muszą mieścić się w zakresie 1–200.");
            if (m.Orientation != "PointyTop" && m.Orientation != "FlatTop") throw new InvalidDataException("Nieprawidłowa orientacja mapy.");
            if (m.LabelMode != "Axes" && m.LabelMode != "InCells" && m.LabelMode != "None") throw new InvalidDataException("Nieprawidłowy tryb etykiet.");
            if (m.RowParity < 0 || m.RowParity > 1 || m.ColumnParity < 0 || m.ColumnParity > 1 || m.Cells == null || m.Cells.Count != m.Columns * m.Rows) throw new InvalidDataException("Nieprawidłowa siatka mapy.");
            var ids = new HashSet<string>(); var variants = new Dictionary<string, string>();
            foreach (var c in m.Cells)
            {
                if (c == null || String.IsNullOrWhiteSpace(c.Id) || !ids.Add(c.Id) || String.IsNullOrWhiteSpace(c.Terrain) || !MapLogic.ValidColor(c.Color) || String.IsNullOrWhiteSpace(c.TerrainId) || String.IsNullOrWhiteSpace(c.VariantId) || c.TerrainRevision < 1) throw new InvalidDataException("Nieprawidłowe dane heksu lub terenu.");
                CheckVariant(variants, c.VariantId, c.TerrainId, c.TerrainRevision, c.Terrain, c.Color);
                if (c.NotesFormat == "richDocument1") { NoteCodec.Read(c.RichDescription); if (c.Description != NoteCodec.Plain(c.RichDescription)) throw new InvalidDataException("Zwykły tekst nie odpowiada formatowanemu opisowi."); }
                else if (c.NotesFormat != "plainText" || !String.IsNullOrEmpty(c.RichDescription)) throw new InvalidDataException("Nieobsługiwany format notatek.");
            }
            if (m.Presets == null || m.Presets.Count == 0 || m.Presets.Count > 100 || !m.Presets.Any(p => p.Id == m.ActivePresetId)) throw new InvalidDataException("Brak aktywnej palety projektu.");
            var presets = new HashSet<string>(); foreach (var p in m.Presets) { PresetCatalog.Validate(p); if (!presets.Add(p.Id)) throw new InvalidDataException("Powtórzony identyfikator zestawu."); foreach (var t in p.Terrains) CheckVariant(variants, t.VariantId, t.TerrainId, t.Revision, t.Name, t.Color); }
        }
        static void CheckVariant(Dictionary<string, string> variants, string variant, string terrain, int revision, string name, string color)
        {
            string signature = MapLogic.Serializer().Serialize(new[] { terrain, revision.ToString(System.Globalization.CultureInfo.InvariantCulture), name, color.ToUpperInvariant() }); string previous;
            if (variants.TryGetValue(variant, out previous) && previous != signature) throw new InvalidDataException("Sprzeczne właściwości tej samej wersji terenu: " + variant);
            variants[variant] = signature;
        }

        void OnFormClosing(object sender, FormClosingEventArgs e) { if (!ConfirmDiscard()) e.Cancel = true; }
        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.Shift && e.KeyCode == Keys.S) { SaveAsProject(); e.Handled = true; e.SuppressKeyPress = true; }
            else if (e.Control && !e.Shift && e.KeyCode == Keys.S) { SaveProject(); e.Handled = true; e.SuppressKeyPress = true; }
            else if (e.Control && (e.KeyCode == Keys.Z || e.KeyCode == Keys.Y))
            {
                bool repeat = e.KeyCode == Keys.Y;
                if (cellName.ContainsFocus || ActiveControl == cellName) TextUndo("name", repeat);
                else if (cellDescription.ContainsFocus) TextUndo("description", repeat);
                else if (ActiveControl is TextBoxBase) return;
                else if (repeat) Redo(); else Undo();
                e.Handled = true; e.SuppressKeyPress = true;
            }
        }

        string Coordinate(int index)
        {
            return MapLogic.Coordinate(index, map.Columns);
        }

        static Color ParseColor(string value) { return ColorTranslator.FromHtml(value); }
        PointF Center(int col, int row)
        {
            float r = BaseRadius;
            if (map.Orientation == "FlatTop") return new PointF(1.5f * r * col, (float)(Math.Sqrt(3) * r * (row + ((col + map.ColumnParity) & 1) * 0.5f)));
            return new PointF((float)(Math.Sqrt(3) * r * (col + ((row + map.RowParity) & 1) * 0.5f)), 1.5f * r * row);
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
            float leftInset = leftCollapsed ? 28f : 220f;
            bool axes = map.LabelMode == "Axes" && map.Cells.Count <= 1400;
            var viewport = RectangleF.FromLTRB(leftInset + 32 + (axes ? 28 : 0), 32 + (axes ? 24 : 0), canvas.ClientSize.Width - 32, canvas.ClientSize.Height - 32);
            scale = Math.Max(.000001f, Math.Min(Math.Max(1, viewport.Width) / Math.Max(bounds.Width, 1), Math.Max(1, viewport.Height) / Math.Max(bounds.Height, 1)));
            pan = new PointF(viewport.Left + viewport.Width / 2f - (bounds.Left + bounds.Width / 2f) * scale, viewport.Top + viewport.Height / 2f - (bounds.Top + bounds.Height / 2f) * scale);
            return true;
        }

        RectangleF MapBounds()
        {
            var points = new List<PointF>();
            foreach (int c in new[] { 0, Math.Min(1, map.Columns - 1), map.Columns - 1 }) foreach (int r in new[] { 0, Math.Min(1, map.Rows - 1), map.Rows - 1 }) points.Add(Center(c, r));
            float xRadius = map.Orientation == "FlatTop" ? BaseRadius : (float)Math.Sqrt(3) * BaseRadius / 2f;
            float yRadius = map.Orientation == "FlatTop" ? (float)Math.Sqrt(3) * BaseRadius / 2f : BaseRadius;
            return RectangleF.FromLTRB(points.Min(p => p.X) - xRadius, points.Min(p => p.Y) - yRadius, points.Max(p => p.X) + xRadius, points.Max(p => p.Y) + yRadius);
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

        T RunModal<T>(Func<T> show)
        {
            EndRightResize();
            EndCanvasGesture(true);
            modalDepth++;
            try { return show(); }
            finally
            {
                modalDepth--;
                EndCanvasGesture(false);
                waitingForMouseRelease = Control.MouseButtons != MouseButtons.None;
                requireFreshCanvasDown = true;
            }
        }

        DialogResult ShowEditorMessage(string text, string title, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1)
        {
            return RunModal(() => MessageBox.Show(this, text, title, buttons, icon, defaultButton));
        }

        void EndCanvasGesture(bool commitStroke)
        {
            bool brushStroke = gestureButton == MouseButtons.Left && gestureTool == EditorTool.Brush;
            gestureButton = MouseButtons.None;
            dragging = false; panGesture = false; dragOrigin = Point.Empty; panOrigin = PointF.Empty;
            if (commitStroke && brushStroke) FinishStroke(); else strokeBefore.Clear();
            canvas.Capture = false;
        }

        void CanvasMouseDown(object sender, MouseEventArgs e)
        {
            if (modalDepth > 0 || rightResizing || waitingForMouseRelease || gestureButton != MouseButtons.None) return;
            if (requireFreshCanvasDown && e.Clicks != 1) return;
            if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Middle) return;
            requireFreshCanvasDown = false;
            gestureButton = e.Button; gestureTool = currentTool;
            dragging = false; dragOrigin = e.Location; panOrigin = pan;
            canvas.Focus();
            canvas.Capture = true;
            if (e.Button == MouseButtons.Middle || gestureTool == EditorTool.Hand) { panGesture = true; return; }
            if (gestureTool == EditorTool.Bucket) { int hit = HitTest(e.Location); if (hit >= 0) FillRegion(hit); return; }
            if (gestureTool == EditorTool.Brush)
            {
                strokeBefore.Clear(); int hit = HitTest(e.Location); if (hit >= 0) PaintCell(hit); canvas.Capture = true;
            }
        }

        void CanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (modalDepth > 0 || rightResizing) return;
            int hit = HitTest(e.Location); coordinateLabel.Text = "Kursor: " + (hit >= 0 ? Coordinate(hit) : "—"); statusLabel.Text = "Kursor: " + (hit >= 0 ? Coordinate(hit) : "—") + "     " + map.Columns + " × " + map.Rows + " pól     " + OrientationNames.Display(map.Orientation) + "     Łącznie heksów: " + map.Cells.Count;
            if (e.Button == MouseButtons.None) { waitingForMouseRelease = false; if (gestureButton != MouseButtons.None) EndCanvasGesture(true); return; }
            // A pressed button alone is not an editing gesture: its Down must belong to this canvas.
            if (waitingForMouseRelease || gestureButton == MouseButtons.None || (e.Button & gestureButton) == MouseButtons.None) return;
            if (panGesture) { pan = new PointF(panOrigin.X + e.X - dragOrigin.X, panOrigin.Y + e.Y - dragOrigin.Y); canvas.Invalidate(); return; }
            if (gestureTool == EditorTool.Select && (Math.Abs(e.X - dragOrigin.X) + Math.Abs(e.Y - dragOrigin.Y) > 3))
            {
                if (!dragging) { dragging = true; panOrigin = pan; }
                pan = new PointF(panOrigin.X + e.X - dragOrigin.X, panOrigin.Y + e.Y - dragOrigin.Y); canvas.Invalidate();
            }
            if (gestureTool == EditorTool.Brush) { if (Math.Abs(e.X - dragOrigin.X) + Math.Abs(e.Y - dragOrigin.Y) > 3) dragging = true; if (hit >= 0) PaintCell(hit); }
        }

        void CanvasMouseUp(object sender, MouseEventArgs e)
        {
            if (Control.MouseButtons == MouseButtons.None) waitingForMouseRelease = false;
            if (modalDepth > 0 || waitingForMouseRelease || gestureButton == MouseButtons.None || e.Button != gestureButton) return;
            if (panGesture || gestureTool == EditorTool.Brush || gestureTool == EditorTool.Bucket) { EndCanvasGesture(true); return; }
            if (!dragging)
            {
                int hit = HitTest(e.Location);
                if (hit >= 0 && hit == selected && rightPanelOpen) { selected = -1; rightPanelOpen = false; }
                else if (hit >= 0) { selected = hit; rightPanelOpen = true; }
                UpdateUI();
            }
            EndCanvasGesture(true);
        }

        void CanvasMouseWheel(object sender, MouseEventArgs e)
        {
            if (rightResizing || modalDepth > 0) return;
            float old = scale, next = Math.Max(Math.Min(old, .01f), Math.Min(Math.Max(old, 128f), old * (e.Delta > 0 ? 1.12f : 1f / 1.12f)));
            float factor = next / old; pan = new PointF(e.X - (e.X - pan.X) * factor, e.Y - (e.Y - pan.Y) * factor); scale = next; UpdateUI();
        }

        void PaintCell(int index)
        {
            if (index < 0 || index >= map.Cells.Count) return;
            var palette = ActivePreset(); if (palette.Terrains.Count == 0) return;
            var terrain = palette.Terrains[Math.Max(0, Math.Min(brushIndex, palette.Terrains.Count - 1))]; var cell = map.Cells[index];
            if (cell.VariantId == terrain.VariantId && cell.Terrain == terrain.Name && String.Equals(cell.Color, terrain.Color, StringComparison.OrdinalIgnoreCase)) return;
            if (!strokeBefore.ContainsKey(index)) strokeBefore[index] = new TerrainState(cell);
            MapLogic.ApplyTerrain(cell, terrain); dirty = true; canvas.Invalidate();
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
                action.Changes.Add(new CellChange { Index = item.Key, CellId = cell.Id, Before = item.Value, After = new TerrainState(cell) });
            }
            strokeBefore.Clear();
            if (action.Changes.Count == 0) { MarkDirty(); return; }
            undo.Push(action); while (undo.Count > 300) { var keep = undo.Reverse().Skip(1).ToList(); undo.Clear(); foreach (var a in keep) undo.Push(a); }
            redo.Clear(); MarkDirty();
        }

        void Undo() { EndCanvasGesture(true); if (undo.Count == 0) return; var action = undo.Pop(); ApplyAction(action, false); redo.Push(action); MarkDirty(); }
        void Redo() { EndCanvasGesture(true); if (redo.Count == 0) return; var action = redo.Pop(); ApplyAction(action, true); undo.Push(action); MarkDirty(); }
    }

    internal sealed class BufferedPanel : Panel
    {
        public BufferedPanel() { DoubleBuffered = true; ResizeRedraw = true; }
    }

    internal sealed class NewMapDialog : EditorDialog
    {
        public int Columns { get; private set; }
        public int Rows { get; private set; }
        public string Orientation { get; private set; }
        public string ProjectName { get; private set; }
        public TerrainPreset Preset { get; private set; }
        readonly NumericUpDown cols = new NumericUpDown { Minimum = 1, Maximum = 200, Value = 15 };
        readonly NumericUpDown rows = new NumericUpDown { Minimum = 1, Maximum = 200, Value = 10 };
        readonly ComboBox orient = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        readonly ComboBox presets = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Name" };
        readonly TextBox initialName = new TextBox { Text = "Nowa mapa", MaxLength = 200 };
        public NewMapDialog(string o) : this(o, new List<TerrainPreset> { PresetCatalog.Default() }) { }
        public NewMapDialog(string o, List<TerrainPreset> choices) : base("Nowa mapa", 270)
        {
            Columns = 15; Rows = 10; Orientation = "PointyTop"; ProjectName = "Nowa mapa"; Preset = choices[0];
            Row(0, "Nazwa", initialName, true); Row(1, "Kolumny", cols, false); Row(2, "Wiersze", rows, false);
            var rangeFont = new Font(Font, FontStyle.Italic); Disposed += (s, e) => rangeFont.Dispose();
            foreach (int row in new[] { 1, 2 }) Layout.Controls.Add(new Label { Name = row == 1 ? "columnsRange" : "rowsRange", Text = "(zakres: 1–200)", Font = rangeFont, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(5, 0, 0, 0) }, 2, row);
            orient.Items.AddRange(new[] { "Pointy Top", "Flat Top" }); orient.SelectedIndex = o == "FlatTop" ? 1 : 0; Row(3, "Orientacja", orient, true);
            foreach (var p in choices) presets.Items.Add(p); presets.SelectedIndex = 0; Row(4, "Zestaw", presets, true);
            Buttons(5, () => { if (String.IsNullOrWhiteSpace(initialName.Text)) return; Columns = (int)cols.Value; Rows = (int)rows.Value; Orientation = orient.SelectedIndex == 1 ? "FlatTop" : "PointyTop"; ProjectName = initialName.Text.Trim(); Preset = (TerrainPreset)presets.SelectedItem; DialogResult = DialogResult.OK; }); Ok.Text = "Utwórz";
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
