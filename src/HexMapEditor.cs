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
    public enum EditorTool { Select, Hand, Brush, Bucket, FogBrush, FogBucket, RectangleSelect, Lasso, Stylus, Eraser, DarkBrush, DarkBucket }

    internal static class LibraryPaths
    {
        public static string Root { get { return AppStorage.Current.LibraryRoot; } }
        public static string Hexmaps { get { return AppStorage.Current.MapRoot; } }
        public static string Presets { get { return Path.Combine(Root, "Terrains", "Presets"); } }
        public static string Backgrounds { get { return Path.Combine(Root, "Backgrounds"); } }
        public static string TerrainGraphics { get { return Path.Combine(Root, "Terrains", "Graphics"); } }
        public static void EnsureDirectories() { EnsureDirectories(Root, Hexmaps); }
        public static void EnsureDirectories(string dataDirectory)
        {
            string root = Path.Combine(dataDirectory, "Library");
            EnsureDirectories(root, Path.Combine(root, "Hexmaps"));
        }
        static void EnsureDirectories(string root, string maps)
        {
            foreach (string folder in new[] { "Backgrounds", "Terrains", Path.Combine("Terrains", "Presets"), Path.Combine("Terrains", "Graphics"), "Icons", "Objects", Path.Combine("Objects", "States"), Path.Combine("Exports", "Images"), Path.Combine("Exports", "Text") })
                Directory.CreateDirectory(Path.Combine(root, folder));
            Directory.CreateDirectory(maps);
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
        public string TextureId { get; set; }
        public string FillVariantId { get; set; }
        public GradientDefinition Gradient {get;set;}
        public FillVariant FogFill { get; set; }
        internal GraphicDefinition Texture;
        public string Color { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string NotesFormat { get; set; }
        public string RichDescription { get; set; }
        public string IconId { get; set; }
        public string IconEntryId { get; set; }
        public string IconName { get; set; }
        public bool Fog { get; set; }
        public bool Darkness { get; set; }
        public FillVariant DarkFill { get; set; }
        public CellData() { Id = Guid.NewGuid().ToString("N"); TerrainId = UnpaintedTerrain.Id; VariantId = UnpaintedTerrain.Version; TerrainRevision = 1; Terrain = "Niepomalowany"; Color = "#777F91"; Name = ""; Description = ""; NotesFormat = "plainText"; FillVariantId = UnpaintedTerrain.Fill; FogFill = FillRules.FogDefault(); DarkFill = FillRules.DarkDefault(); }
        internal CellData SnapshotCopy(){var copy=(CellData)MemberwiseClone();copy.Gradient=Gradient==null?null:Gradient.Copy();return copy;}
    }

    public sealed class MapData
    {
        internal ProjectFileVersion LoadedFileVersion;
        public int FormatVersion { get; set; }
        // Keep this small metadata field before the variable-length map payload.
        public string LastSavedAppVersion { get; set; }
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
        public List<IconAsset> IconAssets { get; set; }
        public List<GraphicDefinition> TerrainAssets { get; set; }
        public List<GraphicSource> GraphicSources { get; set; }
        public List<FillVariant> FogPalette { get; set; }
        public List<InkStroke> Ink { get; set; }
        public List<FillVariant> DarkPalette { get; set; }
        public MapBackground Background { get; set; }
        public List<MapObject> Objects { get; set; }
        public List<ObjectStateDefinition> ObjectStates { get; set; }
        public List<string> ExcludedStateIds { get; set; }
        public List<ObjectGroup> Groups { get; set; }
        public MapData() { FormatVersion = 12; Ink = new List<InkStroke>(); DarkPalette = new List<FillVariant>{FillRules.DarkDefault()}; Name = "Nowa mapa"; Columns = 15; Rows = 10; Orientation = "PointyTop"; LabelMode = "Axes"; Cells = new List<CellData>(); ActivePresetId = PresetCatalog.DefaultId; Presets = new List<TerrainPreset> { PresetCatalog.Default() }; IconAssets = new List<IconAsset>(); TerrainAssets = new List<GraphicDefinition>(); GraphicSources = new List<GraphicSource>(); FogPalette = new List<FillVariant> { FillRules.FogDefault() }; Objects = new List<MapObject>(); ObjectStates=ObjectStateDefinition.Defaults(); ExcludedStateIds = new List<string>(); Groups=new List<ObjectGroup>(); }
    }

    internal sealed class TerrainState
    {
        public readonly string Terrain;
        public readonly string Color;
        public readonly string TerrainId, VariantId;
        readonly GraphicDefinition texture;
        readonly string textureId;
        readonly string fillVariantId;
        readonly GradientDefinition gradient;
        public readonly int Revision;
        public TerrainState(CellData cell) { Terrain = cell.Terrain; Color = cell.Color; TerrainId = cell.TerrainId; VariantId = cell.VariantId; Revision = cell.TerrainRevision; texture = cell.Texture; textureId = cell.TextureId; fillVariantId = cell.FillVariantId; gradient=cell.Gradient==null?null:cell.Gradient.Copy(); }
        public bool Matches(CellData cell) { return GradientRules.Same(gradient,cell.Gradient) && VariantId == cell.VariantId && TerrainId == cell.TerrainId && Revision == cell.TerrainRevision && textureId == cell.TextureId && fillVariantId == cell.FillVariantId && String.Equals(Terrain, cell.Terrain, StringComparison.Ordinal) && String.Equals(Color, cell.Color, StringComparison.OrdinalIgnoreCase); }
        public void ApplyTo(CellData cell) { cell.Terrain = Terrain; cell.Color = Color; cell.TerrainId = TerrainId; cell.VariantId = VariantId; cell.TerrainRevision = Revision; cell.Texture = texture; cell.TextureId = textureId; cell.FillVariantId = fillVariantId;cell.Gradient=gradient==null?null:gradient.Copy(); }
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
        internal PresetSections QuickSections;
        internal string NamedCellId,BeforeCellName,AfterCellName;
        internal string QuickPresetId;internal QuickSnapshot BeforeQuick,AfterQuick;
        public List<CellChange> Changes = new List<CellChange>();
        public List<LayerChange> Layers = new List<LayerChange>();
        internal List<ErasedCellText> ErasedText;
        public IconLibraryChange LibraryChange;
        public PresetFileChange PresetChange;
        public ObjectLibraryChange ObjectLibraryChange;
        public StateFileChange StateFileChange;
        public List<ObjectChange> Objects = new List<ObjectChange>();
        public string BeforeOrientation, AfterOrientation;
        public List<ObjectGroup> BeforeGroups, AfterGroups;
        public List<ObjectStateDefinition> BeforeStates, AfterStates;
        public List<string> BeforeExcludedStates, AfterExcludedStates;
        public List<IconAsset> RequiredAssets = new List<IconAsset>();
        public TerrainGraphicChange TerrainGraphicChange;
        public BackgroundFileChange BackgroundFileChange;
        public List<FillVariant> BeforeFogPalette, AfterFogPalette;
        public List<FillVariant> BeforeDarkPalette, AfterDarkPalette;
        public List<InkStroke> BeforeInk, AfterInk;
        public bool BackgroundChange;
        public MapBackground BeforeBackground, AfterBackground;
        public string Label;
        public ResizeState BeforeResize, AfterResize;
        public PaletteState BeforePalette, AfterPalette;
    }

    public sealed partial class MainWindow : DarkForm
    {
        static readonly string[] TerrainNames = { "Teren neutralny", "Woda", "Równina", "Góry", "Pustynia", "Czerń / Pustka" };
        static readonly string[] TerrainColors = { "#777F91", "#287DB3", "#72A65B", "#8D8174", "#D1A94E", "#252735" };
        readonly JavaScriptSerializer json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 64 };
        readonly Panel canvas = new InkCanvas();
        readonly Panel left = new Panel();
        readonly DetailsDock right = new DetailsDock();
        readonly Button leftToggle = new Button();
        readonly ComboBox terrainPicker = new ComboBox();
        readonly Button orientationToggle = new Button();
        readonly Button labelToggle = new Button();
        readonly RichNoteEditor cellDescription;
        internal static RichNoteEditor CreateNoteEditor(){StartupTrace.Write("editor.notes.begin");try{var editor=new RichNoteEditor();StartupTrace.Write("editor.notes.ready");return editor;}catch(Exception ex){StartupTrace.Write("editor.notes.failed",null,ex);throw;}}
        readonly Label statusLabel = new Label();
        readonly Label dimensionsLabel = new Label();
        readonly Label coordinateLabel = new Label();
        readonly Button saveButton = new Button();
        readonly Button undoButton = new Button();
        readonly Button redoButton = new Button();
        readonly Button zoomButton = new SingleLineButton();
        readonly ToolTip tips = new ToolTip();
        int selected = -1;
        int brushIndex = 2;
        bool leftCollapsed, updating, dragging, panGesture;
        Point brushMouse=new Point(-100,-100);
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
        internal static string Version {get{return AppVersion;}}
        const string AppVersion = ReleaseMetadata.Version;
        string editorCellId;
        List<TerrainPreset> libraryPresets = new List<TerrainPreset>();
        readonly Dictionary<int, TerrainState> strokeBefore = new Dictionary<int, TerrainState>();

        internal Action<Exception> InitialDisplayComplete;
        internal Exception InitialDisplayError;
        protected override void OnLoad(EventArgs e){try{base.OnLoad(e);}catch(Exception ex){if(InitialDisplayComplete==null)throw;InitialDisplayError=ex;StartupTrace.Write("editor.load.failed",this,ex);}}
        protected override void OnShown(EventArgs e){try{if(InitialDisplayError!=null)throw InitialDisplayError;base.OnShown(e);}catch(Exception ex){if(InitialDisplayComplete==null)throw;InitialDisplayError=ex;InitialDisplayComplete(ex);return;}if(InitialDisplayComplete!=null)InitialDisplayComplete(null);}
        public MainWindow() : this(NewMap(15,10,"PointyTop"),null,false) {}
        internal MainWindow(MapData initial,string path):this(initial,path,true) {}
        MainWindow(MapData initial,string path,bool prepareLibraries):this(initial,path,prepareLibraries,null){}
        internal MainWindow(MapData initial,string path,bool prepareLibraries,RichNoteEditor notes)
        {
            StartupTrace.Write("editor.construct.begin");
            cellDescription=notes??CreateNoteEditor();
            activeDocument=new DocumentSession(initial,path);documents.Add(activeDocument);
            Text = "HexMapEditor " + AppVersion; MinimumSize = new Size(1180, 748); Size = new Size(1440, 940);
            StartPosition = FormStartPosition.CenterScreen; BackColor = Color.FromArgb(25, 28, 36); ForeColor = Color.FromArgb(228, 231, 238); Font = new Font("Segoe UI", 9f); KeyPreview = true;
            LoadProgramIcon();
            StartupTrace.Write("editor.construct.icon",this);
            InitializeShell(); InitInk(); InitBackgroundResize(); InitCameraInteraction();
            AttachMapDrop();
            Disposed+=(s,e)=>{if(saveButton.Image!=null&&!ReferenceEquals(saveButton.Image,EditorIcons.Small("save")))saveButton.Image.Dispose();};
            TrackCommandFocus(this);
            Deactivate+=(s,e)=>CancelNumberDrags(this);
            StartupTrace.Write("editor.construct.shell",this);
            Application.AddMessageFilter(this); Disposed += (s,e) => Application.RemoveMessageFilter(this);
            Shown += (s,e)=>{PositionOverlays();FitMap();UpdateUI();if(!prepareLibraries){LoadLibraryPresets(true);LoadIcons();LoadObjects();LoadStateLibrary();if(path!=null){AdoptLibraryPanel(activeDocument);RefreshPresetPicker();}}};
            Resize += (s, e) => PositionOverlays(); FormClosing += OnFormClosing; KeyDown += OnKeyDown;
            if(prepareLibraries){LoadLibraryPresets(true);LoadIcons();LoadObjects();LoadStateLibrary();if(path!=null){AdoptLibraryPanel(activeDocument);RefreshPresetPicker();}}
            StartupTrace.Write("editor.construct.libraries",this);
            ConfigureLeftMinimum();PositionOverlays();FitMap();CaptureSavedState(); UpdateUI();
            InitAutoSave();
            StartupTrace.Write("editor.construct.ready",this);
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
            int top = shellToolbar.Height, bottom = BottomHeight, height = Math.Max(0, ClientSize.Height - top - bottom);
            int inset = leftCollapsed ? 18 : LeftPanelWidth+18;
            using (new CanvasUpdate(canvas, canvas.Left != inset))
            {
            if (canvas.Left != inset) pan = new PointF(pan.X + canvas.Left - inset, pan.Y);
            canvas.Bounds = new Rectangle(inset, top, Math.Max(0, ClientSize.Width - inset), height);
            left.Bounds = new Rectangle(0, top, LeftPanelWidth, height);
            leftToggle.Bounds = new Rectangle(leftCollapsed ? 0 : LeftPanelWidth, top, 18, height);
            leftToggle.Text = leftCollapsed ? "›" : "‹"; left.Visible = !leftCollapsed;
            PositionRightPanel(top,height,false);
            }
        }

        void CycleLabelMode()
        {
            map.LabelMode = map.LabelMode == "Axes" ? "InCells" : map.LabelMode == "InCells" ? "None" : "Axes";
            MarkDirty();
        }

        void CloseDetailsPanel()
        {
            EndObjectPropertyEdit(); textHistory.BreakGroups(); selected = -1; selectedObjects.Clear(); groupSelectionOrder.Clear(); selectedGroupId=null; rightPanelOpen = false; UpdateUI();
        }

        void MarkDirty() { dirty = savedState.CheckAll(map); UpdateUI(); }
        void CaptureSavedState() { savedState.Capture(map); dirty = false; }
        void MarkCellDirty(CellData cell) { documentRevision++; dirty = savedState.CheckCell(cell); UpdateCaption(); canvas.Invalidate(); }
        static string ProjectTitle(string path, string initial)
        {
            if (String.IsNullOrEmpty(path)) return String.IsNullOrWhiteSpace(initial) ? "Nowa mapa" : initial;
            string name = Path.GetFileName(path); return name.EndsWith(".hexmap", StringComparison.OrdinalIgnoreCase) ? name.Substring(0, name.Length - 7) : name;
        }
        void UpdateCaption()
        {
            Text = HasDocuments?"HexMapEditor — " + ProjectTitle(currentPath,map.Name) + (HasUnsavedChanges ? " *" : ""):"HexMapEditor";
            UpdateSaveAccent();UpdateCommandState();UpdateTabTitle(activeDocument);
        }

        internal int UiRefreshCount;
        void UpdateUI()
        {
            UiRefreshCount++;
            updating = true;
            right.SuspendLayout();
            try
            {
                UpdateCaption();
                UpdateZoomCaption();
                UpdateMapStatus(HasDocuments&&statusCell<map.Cells.Count?statusCell:-1);
                dimensionsLabel.Text = map.Columns + " × " + map.Rows;
                orientationToggle.Text = OrientationNames.Display(map.Orientation); orientationToggle.Image = EditorIcons.Small(map.Orientation == "FlatTop" ? "flat" : "pointy");
                tips.SetToolTip(orientationToggle, map.Orientation == "FlatTop" ? "Flat Top: pozioma krawędź na górze" : "Pointy Top: wierzchołek na górze");
                labelToggle.Text = map.LabelMode == "InCells" ? "W heksach" : map.LabelMode == "None" ? "Brak" : "Osie";
                tips.SetToolTip(labelToggle, map.LabelMode == "InCells" ? "Pełne współrzędne wewnątrz heksów (od 80% skali, jeśli cały napis mieści się w polu i oknie)." : map.LabelMode == "None" ? "Bez etykiet współrzędnych." : "Współrzędne na osiach mapy.");
                bool hasSelection = selected >= 0 || selectedObjects.Count > 0;
                if (!hasSelection)
                {
                    rightPanelOpen=false;rightCollapsed=false;
                    editorCellId=null;textHistory.BreakGroups();
                }
                bool active=rightPanelOpen&&hasSelection;
                bool group=CurrentGroup()!=null;
                bool instance=!group&&selectedObjects.Count==1;
                bool hex=selectedObjects.Count==0&&selected>=0&&selected<map.Cells.Count;
                bool multiple=!group&&selectedObjects.Count>1;
                hexDetailName.CancelIf(!active||!hex||rightCollapsed);
                objectName.CancelIf(!active||!instance||rightCollapsed);
                groupName.CancelIf(!active||!group||rightCollapsed);
                if(active&&hex)UpdateHexDetails();
                if(active&&(instance||group)){editorCellId=null;UpdateObjectDetails();}
                hexDetailScroll.Visible=active&&!rightCollapsed&&hex;
                objectDetailPanel.Visible=active&&!rightCollapsed&&(instance||group);
                rightToggle.Visible=active&&!multiple;
                rightToggle.Text=rightCollapsed?"‹":"›";
                rightResizeGrip.Visible=active&&!rightCollapsed&&!multiple;
                right.Visible=active&&!multiple;
                UpdateDetailsCommands(active,multiple);
            }
            finally { right.ResumeLayout(true); updating = false; }
            PositionOverlays();
            canvas.Invalidate();
        }

        internal Func<DialogResult> ConfirmUnsavedForTest;
        bool ConfirmDiscard(DocumentSession document, bool mustSave=false)
        {
            if (!document.Unsaved) return true;
            PaintBeforeClosePrompt();
            string name=ProjectTitle(document.Path,document.Map.Name);
            var result=ConfirmUnsavedForTest!=null?ConfirmUnsavedForTest():ShowEditorMessage(
                "Mapa „"+name+"” zawiera niezapisane zmiany. Czy chcesz je zapisać przed kontynuowaniem?", "Niezapisana mapa",
                mustSave?MessageBoxButtons.OKCancel:MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning,
                mustSave?MessageBoxDefaultButton.Button2:MessageBoxDefaultButton.Button1);
            if(result==DialogResult.No)return !mustSave;
            return (result==DialogResult.Yes||(mustSave&&result==DialogResult.OK))&&SaveDocument(document,false);
        }

        void NewProject()
        {
            if(!CanChangeDocument())return;
            var choices = AvailablePresets();
            var created=RunModal(()=>ChooseNewProject(this,map.Orientation,choices));if(created==null)return;
            created.ObjectStates=NewStateDefinitions();
            AdoptNewProject(created,null);
        }
        void StartNewProject(int columns, int rows, string orientation, string name, TerrainPreset sourcePreset)
        {
            var data=CreateProjectData(columns,rows,orientation,name,sourcePreset);data.ObjectStates=NewStateDefinitions();AdoptNewProject(data,null);
        }
        void AdoptNewProject(MapData data,string path)
        {
            AddDocument(data,path);
        }

        internal static MapData CreateProjectData(int columns,int rows,string orientation,string name,TerrainPreset sourcePreset)
        {
            var result=NewMap(columns,rows,orientation);result.Name=name;
            var preset=sourcePreset.Copy();result.Presets=new List<TerrainPreset>{preset};result.ActivePresetId=preset.Id;return result;
        }
        internal static MapData ChooseNewProject(IWin32Window owner,string orientation,List<TerrainPreset> choices)
        {
            using(var dialog=new NewMapDialog(orientation,choices))
                return dialog.ShowDialog(owner)==DialogResult.OK?CreateProjectData(dialog.Columns,dialog.Rows,dialog.Orientation,dialog.ProjectName,dialog.Preset):null;
        }
        void ChangeOrientation(string orientation)
        {
            if(BlockInkCommit())return;
            if (map.Orientation == orientation) return;
            EndCanvasGesture(true);
            var geometry = new MapData { Orientation=orientation, RowParity=map.RowParity, ColumnParity=map.ColumnParity };
            var action = new EditAction { Label="Orientacja mapy", BeforeOrientation=map.Orientation, AfterOrientation=orientation, Objects=TransformObjects(geometry,0,0) };
            ApplyAction(action,true); PushAction(action); RecalculateFitTransform(); MarkDirty();
        }

        void OpenProject()
        {
            if(!CanChangeDocument())return;
            using (var d = new OpenFileDialog { Filter = "Projekt HexMapEditor (*.hexmap)|*.hexmap|Wszystkie pliki (*.*)|*.*", Title = "Otwórz mapę", RestoreDirectory = true })
            {
                if (RunModal(() => d.ShowDialog(this)) != DialogResult.OK) return;
                string error; bool migrated;
                if (!TryLoadProject(d.FileName, out error, out migrated)) ShowEditorMessage("Nie można otworzyć projektu.\n\n" + error, "Błąd odczytu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        internal static MapData ReadProjectData(string path)
        {
                if (new FileInfo(path).Length > GraphicLimits.MapBytes) throw new InvalidDataException("Plik przekracza limit odczytu 256 MiB.");
                var json=MapLogic.Serializer();
                byte[] fileBytes=File.ReadAllBytes(path);
                string fileText;using(var reader=new StreamReader(new MemoryStream(fileBytes),Encoding.UTF8,true))fileText=reader.ReadToEnd();
                var raw = json.DeserializeObject(fileText) as Dictionary<string, object>;
                if (raw == null || !raw.ContainsKey("FormatVersion") || !(raw["FormatVersion"] is int)) throw new InvalidDataException("Brak jawnego numeru formatu mapy.");
                int format = (int)raw["FormatVersion"];
                if (format != 12) throw new InvalidDataException(ProjectMetadata.IncompatibleMessage(raw));
                GraphicSchema.Map(raw);
                if (!raw.ContainsKey("TerrainAssets") || !raw.ContainsKey("GraphicSources") || !raw.ContainsKey("FogPalette") ||
                    !raw.ContainsKey("Background")) throw new InvalidDataException("Brak tabeli grafik, źródeł lub ustawień mgły/tła.");
                {
                    var cells = raw.ContainsKey("Cells") ? raw["Cells"] as object[] : null;
                    var objects=raw.ContainsKey("Objects")?raw["Objects"] as object[]:null;
                    string[] objectFields={"Id","TemplateId","Name","AssetId","X","Y","Size","Side","Locked","RevealsFog","RevealRadius","LightEnabled","LightRadius"};
                    if(objects==null||objects.Any(o=>!(o is Dictionary<string,object>)||objectFields.Any(key=>!((Dictionary<string,object>)o).ContainsKey(key))))throw new InvalidDataException("Niepełne dane instancji obiektu.");
                    ValidateGroupFields(raw, format);
                    if (cells == null || !raw.ContainsKey("Objects") || !raw.ContainsKey("IconAssets") || cells.Any(c => !(c is Dictionary<string, object>) || new[] { "Id", "TerrainId", "VariantId", "TerrainRevision", "IconId", "Fog", "FillVariantId", "FogFill" }.Any(key => !((Dictionary<string, object>)c).ContainsKey(key)))) throw new InvalidDataException("Brak jawnych danych heksów, zasobów, obiektów, stanów lub grup.");
                }
                var loaded = json.ConvertToType<MapData>(raw); ValidateMap(loaded);
                loaded.TerrainAssets.Clear(); // References now belong to the cells and palette variants.
                loaded.LoadedFileVersion=ProjectFileVersion.Bytes(fileBytes);return loaded;

        }
        bool TryLoadProject(string path, out string error, out bool migrated)
        {
            error=null;migrated=false;
            try
            {
                path=Path.GetFullPath(path);
                var existing=FindDocument(path);
                if(existing!=null&&ActivateDocument(existing)){RecentProjects.Shared.Touch(existing.Path);return true;}
                if(existing==null&&CanChangeDocument()&&AddDocument(ReadProjectData(path),path))return true;
                error="Dokończ bieżącą operację przed zmianą karty.";return false;
            }
            catch (Exception ex) { error = ex.Message; migrated = false; return false; }
        }
        internal void OpenExternalProject(string path)
        {
            string error;bool migrated;
            if(!TryLoadProject(path,out error,out migrated))ShowEditorMessage(error,"Nie można otworzyć projektu",MessageBoxButtons.OK,MessageBoxIcon.Warning);
        }

        bool SaveProject(){return SaveDocument(activeDocument,false);}
        bool SaveAsProject(){return SaveDocument(activeDocument,true);}
        bool SaveDocument(DocumentSession document,bool asNew)
        {
            if(InkCommitBusy)return false;
            if(IsAutoSaving(document)){statusLabel.Text=AutoSaveBusyMessage;return false;}
            if(!asNew&&document.Path!=null)return SaveTo(document,document.Path,true,false);
            if(document.Path==null&&!EnsureLibrary())return false;
            using (var d = CreateSaveDialog(document))
            {
                if (RunModal(() => ProjectCreation.ShowSaveDialog(d,this)) != DialogResult.OK) return false;
                return SaveAsTo(document,d.FileName,path=>ShowEditorMessage("Plik już istnieje:\n\n"+path+"\n\nCzy chcesz go zastąpić?","Potwierdź nadpisanie",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2));
            }
        }

        SaveFileDialog CreateSaveDialog(DocumentSession document)
        {
            return new SaveFileDialog { Filter = "Projekt HexMapEditor (*.hexmap)|*.hexmap", DefaultExt = "hexmap", AddExtension = false, OverwritePrompt = false, RestoreDirectory = true, InitialDirectory = document.Path==null?LibraryPaths.Hexmaps:Path.GetDirectoryName(document.Path), FileName = ProjectFileName(ProjectTitle(document.Path, document.Map.Name)), Title = "Zapisz mapę jako" };
        }

        bool SaveAsTo(string path, Func<string, DialogResult> confirmOverwrite)
        {return SaveAsTo(activeDocument,path,confirmOverwrite);}
        bool SaveAsTo(DocumentSession document,string path,Func<string,DialogResult> confirmOverwrite)
        {
            string finalPath = Path.GetFullPath(EnsureProjectExtension(path));
            var opened=FindDocument(finalPath);
            if(opened!=null&&!ReferenceEquals(opened,document))
            {
                statusLabel.Text="Ten plik jest już otwarty w innej karcie. Wybierz inną ścieżkę.";
                return false;
            }
            bool exists = File.Exists(finalPath);
            if (exists && confirmOverwrite(finalPath) != DialogResult.Yes) return false;
            // If a new target appears after this check, File.Move fails instead of replacing it.
            return SaveTo(document,finalPath,exists,true);
        }

        bool EnsureLibrary()
        {
            try { LibraryPaths.EnsureDirectories(); return true; }
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

        bool SaveTo(DocumentSession document,string path,bool allowOverwrite,bool explicitOverwrite)
        {
            string error;
            if (TrySaveCore(document,path,allowOverwrite,explicitOverwrite,out error)) return true;
            ShowEditorMessage("Zapis nie powiódł się. Poprzedni plik pozostał bez zmian.\n\n" + error, "Błąd zapisu", MessageBoxButtons.OK, MessageBoxIcon.Error); return false;
        }

        bool TrySaveTo(string path, bool allowOverwrite, out string error)
        {return TrySaveCore(activeDocument,path,allowOverwrite,false,out error);}
        bool TrySaveCore(DocumentSession document,string path,bool allowOverwrite,bool explicitOverwrite,out string error)
        {
            if(ReferenceEquals(document,activeDocument))EndCanvasGesture(true);
            error=null;if(InkCommitBusy||IsAutoSaving(document)){error=IsAutoSaving(document)?AutoSaveBusyMessage:InkBusyMessage;return false;}
            try
            {
                path=Path.GetFullPath(path);
                var opened=FindDocument(path);
                if(opened!=null&&!ReferenceEquals(opened,document))throw new IOException("Ten plik jest już otwarty w innej karcie.");
                var expected=!explicitOverwrite&&SameProjectPath(path,document.Path)?document.Version:null;
                var version=WritePreparedProject(PrepareProjectBytes(document.Map,PresetsForSave(document)),path,allowOverwrite,expected);
                document.Path=path;document.FirstSave=false;document.Version=version;document.AutoSaveConflict=false;
                document.Saved.Capture(document.Map);document.Dirty=false;
                UpdateTabTitle(document);if(ReferenceEquals(document,activeDocument))UpdateCaption();
                RecentProjects.Shared.Touch(path);return true;
            }
            catch (Exception ex) { error=ex.Message;return false; }
        }
        internal static ProjectFileVersion WriteProjectData(MapData data,string path,bool allowOverwrite)
        {return WritePreparedProject(PrepareProjectBytes(data),path,allowOverwrite,null);}
        internal static byte[] PrepareProjectBytes(MapData data,IEnumerable<TerrainPreset> presets=null)
        {
            var output=MapForSave(data,presets);ValidateMap(output);var serializer=MapLogic.Serializer();
            output.LastSavedAppVersion=Version;
            byte[] bytes=new UTF8Encoding(false).GetBytes(serializer.Serialize(output));
            if(bytes.LongLength>GraphicLimits.MapBytes)throw new InvalidDataException("Mapa przekracza limit pliku 256 MiB.");return bytes;
        }
        internal static ProjectFileVersion WritePreparedProject(byte[] bytes,string path,bool allowOverwrite,ProjectFileVersion expected)
        {
            string temp=null,parked=null;
            try
            {
                string directory=Path.GetDirectoryName(Path.GetFullPath(path));
                if(bytes.LongLength>GraphicLimits.MapBytes)throw new InvalidDataException("Mapa przekracza limit pliku 256 MiB.");
                temp=Path.Combine(directory,".hexmap-"+Guid.NewGuid().ToString("N")+".tmp");
                using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(expected!=null)
                {
                    ProjectFileVersion.Check(path,expected);parked=Path.Combine(directory,".hexmap-checked-"+Guid.NewGuid().ToString("N")+".tmp");
                    File.Move(path,parked);ProjectFileVersion.Check(parked,expected);File.Move(temp,path);temp=null;
                }
                else{if(allowOverwrite&&File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);temp=null;}
                if(parked!=null){try{File.Delete(parked);}catch(IOException){}catch(UnauthorizedAccessException){}parked=null;}
                return ProjectFileVersion.Bytes(bytes);
            }
            catch(Exception error){if(parked!=null&&File.Exists(parked)){try{File.Move(parked,path);parked=null;}catch(Exception restore){throw new IOException("Nie nadpisano zajętej ścieżki. Zachowano dane transakcji: "+parked+". "+restore.Message,error);}}throw;}
            finally{if(temp!=null)try{File.Delete(temp);}catch(IOException){ }catch(UnauthorizedAccessException){ }}
        }

        static string SafeFileName(string name) { foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_'); return String.IsNullOrWhiteSpace(name) ? "Nowa mapa" : name; }

        static void ValidateMap(MapData m)
        {
            if (m == null) throw new InvalidDataException("Brak danych mapy.");
            if (m.FormatVersion != 12) throw new InvalidDataException("Nieobsługiwana wersja formatu: " + m.FormatVersion);
            var textures = TerrainTextures.ReadTable(m.TerrainAssets, m.GraphicSources);
            if (m.Background != null)
            {
                GraphicSource source;
                if (!m.GraphicSources.Any(s => s.Id == m.Background.SourceId)) throw new InvalidDataException("Brak źródła tła.");
                source = m.GraphicSources.Single(s => s.Id == m.Background.SourceId);
                m.Background.Source = source; BackgroundRenderer.Validate(m.Background);
            }
            FillRules.Attach(m.FogPalette, textures); FillRules.ValidatePool(m.FogPalette);
            FillRules.Attach(m.DarkPalette, textures); FillRules.ValidatePool(m.DarkPalette); InkGeometry.Validate(m.Ink);
            IconAssets.Validate(m.IconAssets); var iconIds = new HashSet<string>(m.IconAssets.Select(a => a.Id));
            ObjectGeometry.Validate(m,iconIds);
            if (m.Columns < 1 || m.Columns > 200 || m.Rows < 1 || m.Rows > 200) throw new InvalidDataException("Wymiary mapy muszą mieścić się w zakresie 1–200.");
            if (m.Orientation != "PointyTop" && m.Orientation != "FlatTop") throw new InvalidDataException("Nieprawidłowa orientacja mapy.");
            if (m.LabelMode != "Axes" && m.LabelMode != "InCells" && m.LabelMode != "None") throw new InvalidDataException("Nieprawidłowy tryb etykiet.");
            if (m.RowParity < 0 || m.RowParity > 1 || m.ColumnParity < 0 || m.ColumnParity > 1 || m.Cells == null || m.Cells.Count != m.Columns * m.Rows) throw new InvalidDataException("Nieprawidłowa siatka mapy.");
            var ids = new HashSet<string>(); var variants = new Dictionary<string, string>();
            long noteCharacters=0;
            foreach (var c in m.Cells)
            {
                if (c == null || String.IsNullOrWhiteSpace(c.Id) || !ids.Add(c.Id) || String.IsNullOrWhiteSpace(c.Terrain) || !MapLogic.ValidColor(c.Color) || String.IsNullOrWhiteSpace(c.TerrainId) || String.IsNullOrWhiteSpace(c.VariantId) || c.TerrainRevision < 1) throw new InvalidDataException("Nieprawidłowe dane heksu lub terenu.");
                c.Texture = TerrainTextures.Resolve(textures, c.TextureId);
                if (String.IsNullOrWhiteSpace(c.FillVariantId)) throw new InvalidDataException("Brak utrwalonego wariantu wyglądu.");
                if ((c.TerrainId == UnpaintedTerrain.Id || c.VariantId == UnpaintedTerrain.Version) && !MapLogic.IsUnpainted(c)) throw new InvalidDataException("Sprzeczny stan niepomalowanego pola.");
                GradientRules.Validate(c.Gradient);if(c.Gradient!=null&&c.TextureId!=null)throw new InvalidDataException("Heks ma równocześnie grafikę i gradient.");
                FillRules.Attach(new[] { c.FogFill }, textures); FillRules.Validate(c.FogFill); FillRules.Attach(new[]{c.DarkFill},textures); FillRules.Validate(c.DarkFill);
                CheckVariant(variants, c.VariantId, c.TerrainId, c.TerrainRevision, c.Terrain, c.Color, c.TextureId);
                if (c.IconId != null && !iconIds.Contains(c.IconId)) throw new InvalidDataException("Brak osadzonego zasobu ikony.");
                if ((c.IconName != null && (String.IsNullOrWhiteSpace(c.IconName) || c.IconName.Length > 200)) || (c.IconEntryId != null && (String.IsNullOrWhiteSpace(c.IconEntryId) || c.IconEntryId.Length > 200)) || (c.IconId == null && (c.IconName != null || c.IconEntryId != null))) throw new InvalidDataException("Nieprawidłowe metadane wystąpienia ikony.");
                noteCharacters+=NoteCodec.LengthOf(c.RichDescription,c.Description);NoteCodec.CheckMapBudget(noteCharacters);
                if (c.NotesFormat == "richDocument1") { NoteCodec.Read(c.RichDescription); if (c.Description != NoteCodec.Plain(c.RichDescription)) throw new InvalidDataException("Zwykły tekst nie odpowiada formatowanemu opisowi."); }
                else if (c.NotesFormat != "plainText" || !String.IsNullOrEmpty(c.RichDescription)) throw new InvalidDataException("Nieobsługiwany format notatek.");
            }
            if (m.Presets == null || m.Presets.Count == 0 || m.Presets.Count > 100 || !m.Presets.Any(p => p.Id == m.ActivePresetId)) throw new InvalidDataException("Brak aktywnej palety projektu.");
            var presets = new HashSet<string>();
            foreach (var p in m.Presets)
            {
                TerrainTextures.Attach(p, textures);PresetSets.Attach(p,m.IconAssets);
                PresetCatalog.Validate(p);
                if (!presets.Add(p.Id)) throw new InvalidDataException("Powtórzony identyfikator presetu.");
                foreach (var t in p.Terrains) CheckVariant(variants, t.VariantId, t.TerrainId, t.Revision, t.Name, t.Color, t.TextureId);
            }
            GradientRules.Budget(m.Presets.SelectMany(p=>p.Terrains).SelectMany(t=>t.Variants).Select(f=>f.Gradient).Concat(m.Cells.SelectMany(c=>new[]{c.Gradient,c.FogFill==null?null:c.FogFill.Gradient,c.DarkFill==null?null:c.DarkFill.Gradient})).Concat(m.FogPalette.Select(f=>f.Gradient)).Concat(m.DarkPalette.Select(f=>f.Gradient)).Concat(m.Ink.Select(s=>s.Gradient)),100000,"dokumentu");
        }
        static void CheckVariant(Dictionary<string, string> variants, string variant, string terrain, int revision, string name, string color, string textureId)
        {
            // A name is metadata, not a surface boundary. Appearance positions have their own IDs.
            string signature = MapLogic.Serializer().Serialize(new[] { terrain, revision.ToString(System.Globalization.CultureInfo.InvariantCulture) }); string previous;
            if (variants.TryGetValue(variant, out previous) && previous != signature) throw new InvalidDataException("Sprzeczne właściwości tej samej wersji terenu: " + variant);
            variants[variant] = signature;
        }

        void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if(returningToWelcome)return;
            if(reviewingExit){e.Cancel=true;return;}
            if(autoSaving){CancelAutoSave();e.Cancel=true;return;}
            if(!FinishDocumentInteraction()){e.Cancel=true;return;}
            reviewingExit=true;
            try { e.Cancel=!ReviewWindowExit(closingForUpdate); }
            finally { reviewingExit=false;if(e.Cancel)HidePresetReview(); }
        }
        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if(!HasDocuments)return;
            if(InkCommitBusy&&!(e.Alt&&e.KeyCode==Keys.F4)){if(e.KeyCode==Keys.Escape)CancelInkCommit();else BlockInkCommit();e.Handled=true;e.SuppressKeyPress=true;return;}
            if(e.KeyCode==Keys.Escape&&gestureButton==MouseButtons.Left&&gestureTool==EditorTool.Eraser){EndCanvasGesture(false);e.Handled=true;e.SuppressKeyPress=true;return;}
            if ((e.KeyCode==Keys.Escape || e.KeyCode==Keys.Delete && selectedObjects.Count>0 && !left.ContainsFocus) && !TextInputFocused(this)) { if(e.KeyCode==Keys.Delete)ConfirmDeleteSelectedObjects();else {EndCanvasGesture(true);selectedObjects.Clear();UpdateUI();} e.Handled=true;e.SuppressKeyPress=true;return; }
            if (e.KeyData == (Keys.Control|Keys.Shift|Keys.S)) { ExecuteCommand(EditorCommand.SaveAs); e.Handled = true; e.SuppressKeyPress = true; }
            else if (e.KeyData == (Keys.Control|Keys.S)) { ExecuteCommand(EditorCommand.Save); e.Handled = true; e.SuppressKeyPress = true; }
            else if (HandleHistoryShortcut(e.KeyData)) { e.Handled = true; e.SuppressKeyPress = true; }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            return HandleLayerShortcut(keyData) || HandleHistoryShortcut(keyData) || base.ProcessCmdKey(ref msg, keyData);
        }

        bool HandleHistoryShortcut(Keys keyData)
        {
            if(!HasDocuments)return false;
            var key = keyData & Keys.KeyCode;
            if ((key != Keys.Z && key != Keys.Y) || (keyData & Keys.Control) == 0 || (keyData & Keys.Alt) != 0) return false;
            if ((keyData & Keys.Modifiers) != Keys.Control) return true;

            var local=LocalToolNumber(FocusedEditorControl());
            if(local!=null){local.StepLocal(key==Keys.Y);return true;}
            for(var c=FocusedEditorControl();c!=null;c=c.Parent){var n=c as ScrubNumber;if(n!=null&&!n.IsEditing){if(key==Keys.Y)Redo();else Undo();return true;}}
            if(FocusedEditorControl() is TextBoxBase)return false;

            bool repeat = key == Keys.Y;
            if (cellDescription.ContainsFocus) TextUndo("description", repeat);
            else if (objectDetailPanel.ContainsFocus || !TextInputFocused(this))
            {
                if (repeat) Redo();
                else Undo();
            }
            else return false;
            return true;
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
            return FitBounds(MapBounds());
        }
        bool FitBounds(RectangleF bounds)
        {
            if(inkBefore!=null)EndCanvasGesture(true);
            bool axes = map.LabelMode == "Axes" && map.Cells.Count <= 1400;
            var viewport = RectangleF.FromLTRB(32 + (axes ? 28 : 0), 32 + (axes ? 24 : 0), canvas.ClientSize.Width - 32, canvas.ClientSize.Height - 32);
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
            SetZoomPercent(100);
        }

        PointF ScreenCenter(int col, int row) { var p = Center(col, row); return new PointF(pan.X + p.X * scale, pan.Y + p.Y * scale); }

        void PaintMap(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(canvas.BackColor);
            DrawBackground(e.Graphics);
            int margin = (int)(BaseRadius * scale * 2.5f);
            for (int row = 0; row < map.Rows; row++) for (int col = 0; col < map.Columns; col++)
            {
                var c = ScreenCenter(col, row);
                if (c.X < -margin || c.Y < -margin || c.X > canvas.Width + margin || c.Y > canvas.Height + margin) continue;
                if (!e.ClipRectangle.IntersectsWith(Rectangle.Ceiling(new RectangleF(c.X - BaseRadius * scale - 3, c.Y - BaseRadius * scale - 3, 2 * BaseRadius * scale + 6, 2 * BaseRadius * scale + 6)))) continue;
                int i = row * map.Columns + col;
                using (var path = HexPath(c, BaseRadius * scale))
                using (var brush = new SolidBrush(ParseColor(hideTerrain ? TerrainColors[0] : map.Cells[i].Color)))
                using (var pen = new Pen(i == selected ? ChoiceStyle.ActiveBorder : Color.FromArgb(135, 19, 23, 30), i == selected ? 2.4f : 1.2f))
                {
                    e.Graphics.FillPath(brush, path);
                    if(!hideTerrain&&lowQuality){using(var simple=new SolidBrush(RepresentativeColors.Get(map.Cells[i].Color,map.Cells[i].Texture,map.Cells[i].Gradient,map.Orientation=="FlatTop")))e.Graphics.FillPath(simple,path);}
                    if(!hideTerrain&&!lowQuality&&map.Cells[i].Gradient!=null)GradientRules.Fill(e.Graphics,path,map.Cells[i].Gradient);
                    if (!hideTerrain && !lowQuality && map.Cells[i].Texture != null)
                        DrawTerrainTexture(e.Graphics, path, c, BaseRadius * scale, map.Cells[i].Texture);
                    if (!hideIcons) DrawCellIcon(e.Graphics, map.Cells[i], c);
                    if (map.LabelMode == "InCells") CellLabels.Draw(e.Graphics, Coordinate(i), c, BaseRadius * scale, scale, canvas.ClientRectangle);
                    if (!CellMasked(i)) e.Graphics.DrawPath(pen, path);
                }
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
            if (!hideObjects) DrawObjects(e.Graphics);
            DrawDarkness(e.Graphics, e.ClipRectangle);
            DrawFog(e.Graphics, e.ClipRectangle);
            DrawBrushPreview(e.Graphics); DrawInk(e.Graphics);
        }

        void DrawFog(Graphics graphics, Rectangle clip)
        {
            if (previewWithoutFog) return;
            for (int i = 0; i < map.Cells.Count; i++)
            {
                if (!map.Cells[i].Fog) continue;
                var center = ScreenCenter(i % map.Columns, i / map.Columns); float radius = BaseRadius * scale;
                if (!clip.IntersectsWith(Rectangle.Ceiling(new RectangleF(center.X-radius-3,center.Y-radius-3,2*radius+6,2*radius+6)))) continue;
                using (var path = HexPath(center, radius))
                {
                    var fill = map.Cells[i].FogFill;
                    if(lowQuality){using(var simple=new SolidBrush(RepresentativeColors.Get(fill.Color,fill.Graphic,fill.Gradient,map.Orientation=="FlatTop")))graphics.FillPath(simple,path);}
                    else if (fill.Graphic != null) DrawTerrainTexture(graphics, path, center, radius, fill.Graphic);
                    else if(fill.Gradient!=null)GradientRules.Fill(graphics,path,fill.Gradient);
                    else using (var brush = new SolidBrush(ColorTranslator.FromHtml(fill.Color))) graphics.FillPath(brush, path);
                    using (var pen = new Pen(i == selected ? ChoiceStyle.ActiveBorder : Color.FromArgb(135,19,23,30), i == selected ? 2.4f : 1.2f)) graphics.DrawPath(pen,path);
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
            if(BlockInkCommit())return default(T);
            EndRightResize();
            EndCanvasGesture(true);
            if(BlockInkCommit())return default(T);
            modalDepth++;
            try { return show(); }
            finally
            {
                modalDepth--;
                EndCanvasGesture(false);
                waitingForMouseRelease = Control.MouseButtons != MouseButtons.None;
                requireFreshCanvasDown = true;
                ArmCanvasInput();
            }
        }

        DialogResult ShowEditorMessage(string text, string title, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1)
        {
            return ShowLibraryMessage(this,text,title,buttons,icon,defaultButton);
        }

        DialogResult ShowLibraryMessage(IWin32Window owner,string text,string title,MessageBoxButtons buttons,MessageBoxIcon icon,MessageBoxDefaultButton defaultButton=MessageBoxDefaultButton.Button1)
        { return RunModal(()=>MessageBox.Show(owner,text,title,buttons,icon,defaultButton)); }

        void EndCanvasGesture(bool commitStroke)
        {
            EndCameraInteraction(); EndObjectPropertyEdit();
            FinishObjectGesture(commitStroke); FinishInk(commitStroke);
            bool brushStroke = gestureButton == MouseButtons.Left && (gestureTool == EditorTool.Brush || (gestureTool == EditorTool.FogBrush || gestureTool == EditorTool.DarkBrush) || gestureTool == EditorTool.Bucket || (gestureTool == EditorTool.FogBucket || gestureTool == EditorTool.DarkBucket));
            gestureButton = MouseButtons.None;
            dragging = false; panGesture = false; dragOrigin = Point.Empty; panOrigin = PointF.Empty;
            if (commitStroke && brushStroke) FinishStroke(); else { strokeBefore.Clear(); layerBefore.Clear(); strokeVisited.Clear(); }
            strokeStarted = false;
            bucketOrigin = null; bucketProcessed.Clear(); previousStrokePoint = null;
            canvas.Capture = false;
        }

        void CanvasMouseDown(object sender, MouseEventArgs e)
        {
            brushMouse=e.Location;
            if(BlockInkCommit())return;
            if(e.Button==MouseButtons.Middle&&inkBefore!=null)EndCanvasGesture(true);
            if (modalDepth > 0 || rightResizing || waitingForMouseRelease || gestureButton != MouseButtons.None) return;
            if (requireFreshCanvasDown && e.Clicks != 1) return;
            if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Middle && !(e.Button==MouseButtons.Right&&(currentTool==EditorTool.Stylus||currentTool==EditorTool.Eraser))) return;
            requireFreshCanvasDown = false; EndObjectPropertyEdit();
            if (e.Button != MouseButtons.Middle && !(currentTool==EditorTool.Eraser?GuardEraserLayers(e.Button):GuardCanvasLayer())) return;
            // Numeric editors can commit on focus loss and end an old gesture. Finish that first.
            canvas.Focus();
            gestureButton = e.Button; gestureTool = currentTool;
            dragging = false; dragOrigin = e.Location; panOrigin = pan;
            canvas.Capture = true;
            if (e.Button == MouseButtons.Middle) { BeginCameraInteraction(false); panGesture = true; return; }
            if (BeginInk(e)) return;
            if (ObjectMouseDown(e)) return;
            if (gestureTool==EditorTool.Hand) {BeginCameraInteraction(false);panGesture=true;return;}
            if ((gestureTool==EditorTool.Brush || gestureTool==EditorTool.Bucket) && (noMaterial || objectMaterial)) return;
            strokeStarted = false;
            previousStrokePoint = null;
            if (gestureTool == EditorTool.Bucket || (gestureTool == EditorTool.FogBucket || gestureTool == EditorTool.DarkBucket)) { int hit = HitTest(e.Location); if (hit >= 0) { BeginMaterialStroke(hit, gestureTool); VisitBucketRegion(hit); } return; }
            if (gestureTool == EditorTool.Brush || (gestureTool == EditorTool.FogBrush || gestureTool == EditorTool.DarkBrush))
            {
                strokeBefore.Clear(); layerBefore.Clear(); strokeVisited.Clear(); int hit = HitTest(e.Location);var targets=StampAt(e.Location);if(targets.Count>0){BeginMaterialStroke(hit>=0?hit:targets[0],gestureTool);PaintAlong(e.Location,hit);} canvas.Capture = true;
            }
        }

        void CanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (autoSaving || InkCommitBusy || modalDepth > 0 || rightResizing) return;
            brushMouse=e.Location;
            int hit = HitTest(e.Location); UpdateMapStatus(hit);
            if (previewCell != hit || hit<0&&(currentTool==EditorTool.Brush||currentTool==EditorTool.FogBrush||currentTool==EditorTool.DarkBrush)) { previewCell = hit; canvas.Invalidate(); }
            if(currentTool==EditorTool.Eraser||currentTool==EditorTool.Stylus){inkCursor=e.Location;canvas.Invalidate();}
            if (e.Button == MouseButtons.None) { waitingForMouseRelease = false; if (gestureButton != MouseButtons.None) EndCanvasGesture(true); return; }
            // A pressed button alone is not an editing gesture: its Down must belong to this canvas.
            if (waitingForMouseRelease || gestureButton == MouseButtons.None || (e.Button & gestureButton) == MouseButtons.None) return;
            if (panGesture) { if(Math.Abs(e.X-dragOrigin.X)+Math.Abs(e.Y-dragOrigin.Y)>3)dragging=true; pan = new PointF(panOrigin.X + e.X - dragOrigin.X, panOrigin.Y + e.Y - dragOrigin.Y); canvas.Invalidate(); return; }
            if(gestureTool==EditorTool.Stylus||gestureTool==EditorTool.Eraser){MoveInk(e.Location);return;}
            if (ObjectMouseMove(e)) return;
            if ((gestureTool==EditorTool.Brush || gestureTool==EditorTool.Bucket) && (noMaterial || objectMaterial)) return;
            if (gestureTool == EditorTool.Select && (Math.Abs(e.X - dragOrigin.X) + Math.Abs(e.Y - dragOrigin.Y) > 3))
            {
                if (!dragging) { BeginCameraInteraction(false); dragging = true; panOrigin = pan; }
                pan = new PointF(panOrigin.X + e.X - dragOrigin.X, panOrigin.Y + e.Y - dragOrigin.Y); canvas.Invalidate();
            }
            if (gestureTool == EditorTool.Brush || (gestureTool == EditorTool.FogBrush || gestureTool == EditorTool.DarkBrush)) { if (Math.Abs(e.X - dragOrigin.X) + Math.Abs(e.Y - dragOrigin.Y) > 3) dragging = true; if (canvas.ClientRectangle.Contains(e.Location)) {if(!strokeStarted){var targets=StampAt(e.Location);if(targets.Count>0)BeginMaterialStroke(hit>=0?hit:targets[0],gestureTool);}if(strokeStarted)PaintAlong(e.Location,hit);}else previousStrokePoint=null; }
            if ((gestureTool == EditorTool.Bucket || (gestureTool == EditorTool.FogBucket || gestureTool == EditorTool.DarkBucket)) && hit >= 0 && canvas.ClientRectangle.Contains(e.Location)) { if (!strokeStarted) BeginMaterialStroke(hit, gestureTool); VisitBucketRegion(hit); }
        }

        void CanvasMouseUp(object sender, MouseEventArgs e)
        {
            if (Control.MouseButtons == MouseButtons.None) waitingForMouseRelease = false;
            if (modalDepth > 0 || waitingForMouseRelease || gestureButton == MouseButtons.None || e.Button != gestureButton) return;
            if (!panGesture && ObjectMouseUp(e)) { EndCanvasGesture(true); return; }
            if (panGesture && !dragging && e.Button==MouseButtons.Left && ObjectGeometry.Hit(map,World(e.Location))<0) { selected=-1; if(selectedObjects.Count==0)rightPanelOpen=false;UpdateUI(); }
            if (panGesture || !SelectionTool(gestureTool)) { EndCanvasGesture(true); return; }
            if (!dragging)
            {
                int hit = HitTest(e.Location);
                if (hit < 0) {selected=-1;rightPanelOpen=false;}
                else if (hit == selected && rightPanelOpen) { selected = -1; rightPanelOpen = false; }
                else if (hit >= 0) { selected = hit; rightPanelOpen = true; }
                UpdateUI();
            }
            EndCanvasGesture(true);
        }

        void CanvasMouseWheel(object sender, MouseEventArgs e)
        {
            if (rightResizing || modalDepth > 0 || BlockInkCommit()) return;
            if(inkBefore!=null)EndCanvasGesture(true);
            if(BlockInkCommit())return;WheelCamera(e);
        }

        void PaintCell(int index)
        {
            if (index < 0 || index >= map.Cells.Count) return;
            var palette = ActivePreset(); if (palette.Terrains.Count == 0) return;
            var terrain = palette.Terrains[Math.Max(0, Math.Min(brushIndex, palette.Terrains.Count - 1))]; var cell = map.Cells[index];
            var appearance = NextFill(terrain.Variants);
            if (cell.VariantId == terrain.VariantId && cell.FillVariantId == appearance.Id && cell.TextureId == appearance.GraphicId && cell.Terrain == terrain.Name && String.Equals(cell.Color, appearance.Color, StringComparison.OrdinalIgnoreCase)) return;
            if (!strokeBefore.ContainsKey(index)) strokeBefore[index] = new TerrainState(cell);
            MapLogic.ApplyTerrain(cell, terrain, appearance); dirty = true; canvas.Invalidate();
        }

        void FinishStroke()
        {
            if (strokeBefore.Count == 0 && layerBefore.Count == 0) return;
            var action = new EditAction { Label = "Pociągnięcie pędzla" };
            foreach (var item in strokeBefore)
            {
                var cell = map.Cells[item.Key];
                // Preserve the original HTML color spelling for a net-empty stroke.
                if (item.Value.Matches(cell)) { item.Value.ApplyTo(cell); continue; }
                action.Changes.Add(new CellChange { Index = item.Key, CellId = cell.Id, Before = item.Value, After = new TerrainState(cell) });
            }
            strokeBefore.Clear();
            action.Layers.AddRange(layerBefore.Values); layerBefore.Clear(); strokeVisited.Clear();
            if(strokeFog)ReconcileFog(action,map,true);
            if (action.Changes.Count == 0 && action.Layers.Count == 0) { MarkDirty(); return; }
            PushAction(action); MarkDirty();
        }

        bool TryHistory(bool after, out string error)
        {
            if(IsAutoSaving(activeDocument)){error=AutoSaveBusyMessage;return false;}EndCanvasGesture(true); if(InkCommitBusy){error=InkBusyMessage;return false;} var from = after ? redo : undo; var to = after ? undo : redo; error = null; if (from.Count == 0) return true;
            try { var action = from.Peek(); ApplyAction(action, after); documentRevision++; from.Pop(); to.Push(action); MarkDirty(); return true; }
            catch (Exception ex) { error = ex.Message; return false; }
        }
        void Undo() { string error; if (!TryHistory(false, out error)) ShowEditorMessage(error, "Nie można cofnąć", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        void Redo() { string error; if (!TryHistory(true, out error)) ShowEditorMessage(error, "Nie można ponowić", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    internal class BufferedPanel : Panel
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
        readonly ScrubNumber cols = new ScrubNumber { Minimum = 1, Maximum = 200, Value = 15 };
        readonly ScrubNumber rows = new ScrubNumber { Minimum = 1, Maximum = 200, Value = 10 };
        readonly ComboBox orient = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        readonly ComboBox presets = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Name" };
        readonly TextBox initialName = new DarkTextBox { Text = "Nowa mapa", MaxLength = 200 };
        internal Func<bool> AcceptProject;
        internal CreatedProject CreatedProject;
        public NewMapDialog(string o) : this(o, new List<TerrainPreset> { PresetCatalog.Default() }) { }
        public NewMapDialog(string o, List<TerrainPreset> choices) : base("Nowa mapa", 270)
        {
            Columns = 15; Rows = 10; Orientation = "PointyTop"; ProjectName = "Nowa mapa"; Preset = choices[0];
            Row(0, "Nazwa", initialName, true); Row(1, "Kolumny", cols, false); Row(2, "Wiersze", rows, false);
            var rangeFont = new Font(Font, FontStyle.Italic); Disposed += (s, e) => rangeFont.Dispose();
            foreach (int row in new[] { 1, 2 }) Layout.Controls.Add(new Label { Name = row == 1 ? "columnsRange" : "rowsRange", Text = "(zakres: 1–200)", Font = rangeFont, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(5, 0, 0, 0) }, 2, row);
            orient.Items.AddRange(new[] { "Pointy Top", "Flat Top" }); orient.SelectedIndex = o == "FlatTop" ? 1 : 0; Row(3, "Orientacja", orient, true);
            foreach (var p in choices) presets.Items.Add(p); presets.SelectedIndex = 0; Row(4, "Preset", presets, true);
            Buttons(5, () => { if (String.IsNullOrWhiteSpace(initialName.Text)) return; Columns = (int)cols.Value; Rows = (int)rows.Value; Orientation = orient.SelectedIndex == 1 ? "FlatTop" : "PointyTop"; ProjectName = initialName.Text; Preset = (TerrainPreset)presets.SelectedItem; if(AcceptProject==null||AcceptProject())DialogResult = DialogResult.OK; }); Ok.Text = "Utwórz";
        }
    }
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException+=(sender,error)=>StartupTrace.Write("program.unhandled",null,error.ExceptionObject as Exception);
            try
            {
                string path=args.Length>0?args[0]:null;
                VerifiedInstaller pending = null;
                using(var installGuard=AppStorage.Current.Installed?new System.Threading.Mutex(false,AppStorage.InstallMutex):null)
                using(var executableGuard=AppStorage.Current.Installed?File.Open(Application.ExecutablePath,FileMode.Open,FileAccess.Read,FileShare.Read):null)
                using(var launch=new ProjectLaunch())
                {
                    if(launch.Primary)
                    {
                        using(var context = new StartupContext(path,launch))
                        {
                            Application.Run(context);
                            pending = context.PendingInstaller;
                        }
                    }
                    else launch.Forward(path);
                }
                if(pending!=null)
                {
                    using(pending)
                    {
                        try { pending.Launch(); }
                        catch(Exception error) {
                            MessageBox.Show("Nie udało się uruchomić aktualizacji. Możesz ponownie uruchomić edytor lub pobrać właściwy instalator ze strony wydania.\n\n"+error.Message,
                                "Aktualizacja HexMapEditor",MessageBoxButtons.OK,MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex) { StartupTrace.Write("program.exception",null,ex);MessageBox.Show("Aplikacja napotkała błąd:\n\n" + ex.ToString(), "HexMapEditor — błąd", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
}
