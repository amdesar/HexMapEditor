using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        TableLayoutPanel leftSections;
        Panel terrainScroll;
        Button terrainUp, terrainDown, renamePresetButton;
        int terrainOffset;
        FlowLayoutPanel iconPreview;
        Button deleteIconButton;
        ComboBox shapePicker;
        NumericUpDown sizePicker, heightPicker;
        CheckBox fillPicker;
        Panel Section(string title, int row)
        {
            var section = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = new Padding(6, 0, 6, 0), AccessibleName = title };
            var header = new Label { Text = title, Dock = DockStyle.Top, Height = 26, Padding = new Padding(0, 2, 0, 0), Font = new Font(Font, FontStyle.Bold) };
            header.Paint += (s, e) => { using (var p = new Pen(Color.FromArgb(65, 72, 86))) e.Graphics.DrawLine(p, 0, header.Height - 7, header.Width, header.Height - 7); };
            var content = new Panel { Dock = DockStyle.Fill }; section.Controls.Add(content); section.Controls.Add(header); leftSections.Controls.Add(section, 0, row); return content;
        }
        Button IconAction(Control parent, string icon, string help, Action action)
        { var b = SmallButton(parent, "", action, 32); b.Image = EditorIcons.Small(icon); b.AccessibleName = help; tips.SetToolTip(b, help); return b; }
        void BuildSections()
        {
            leftSections = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Margin = Padding.Empty, Padding = Padding.Empty };
            foreach (var style in new[] { new RowStyle(SizeType.Absolute, 166), new RowStyle(SizeType.Absolute, 100), new RowStyle(SizeType.Percent, 100), new RowStyle(SizeType.Absolute, 90), new RowStyle(SizeType.Absolute, 90) }) leftSections.RowStyles.Add(style);
            left.Controls.Add(leftSections);
            var toolbox = Section("Przybornik", 0);
            paletteTools = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 56, WrapContents = true }; toolbox.Controls.Add(paletteTools);
            string[] names={"Zaznaczanie","Prostokąt","Lasso","Rączka","Pędzel","Wiadro","Mgła wojny","Wypełnienie mgłą"},icons={"select","rectangle","lasso","hand","brush","bucket","fog","fogbucket"};
            var tools=new[]{EditorTool.Select,EditorTool.RectangleSelect,EditorTool.Lasso,EditorTool.Hand,EditorTool.Brush,EditorTool.Bucket,EditorTool.FogBrush,EditorTool.FogBucket};
            for(int i=0;i<tools.Length;i++){var tool=tools[i];var b=ShellButton(paletteTools,"",icons[i],names[i],()=>SelectTool(tool),48);b.Height=26;toolButtons.Add(tool,b);}
            var shapeLabel = new Label { Text = "Kształt", Width = 52, Height = 22 }; toolbox.Controls.Add(shapeLabel);
            shapePicker = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = "Kształt pędzla" }; shapePicker.Items.AddRange(new object[] { "Punkt", "Kwadrat", "Prostokąt", "Koło", "Heksagon", "Pięciokąt foremny", "Linia pozioma", "Linia pionowa" }); shapePicker.SelectedIndex = 0; toolbox.Controls.Add(shapePicker);
            var sizeLabel = new Label { Text = "Rozmiar", Width = 52, Height = 22 }; toolbox.Controls.Add(sizeLabel);
            sizePicker = new NumericUpDown { Minimum = 1, Maximum = 15, Value = 1, Width = 60, AccessibleName = "Rozmiar lub szerokość pędzla" }; heightPicker = new NumericUpDown { Minimum = 1, Maximum = 15, Value = 1, Width = 60, Visible = false, AccessibleName = "Wysokość prostokąta" }; toolbox.Controls.Add(sizePicker); toolbox.Controls.Add(heightPicker);
            fillPicker = new CheckBox { Text = "Wypełnienie", Checked = true, AutoSize = true, AccessibleName = "Wypełnienie figury; wyłącz, aby malować obrys" }; toolbox.Controls.Add(fillPicker);
            tips.SetToolTip(sizePicker, "1–15. Heksagon: promień w pierścieniach + pole środkowe. Pozostałe: wymiar figury w odstępach siatki. Punkt zawsze 1 heks."); tips.SetToolTip(heightPicker, "Drugi wymiar prostokąta: wysokość 1–15");
            Action settings = () => { EndCanvasGesture(true); brushShape = (BrushShape)shapePicker.SelectedIndex; brushSize = (int)sizePicker.Value; brushHeight = (int)heightPicker.Value; brushFilled = fillPicker.Checked; heightPicker.Visible = brushShape == BrushShape.Rectangle; UpdateBrushAvailability(); tips.SetToolTip(sizeLabel, brushShape == BrushShape.Point ? "Punkt obejmuje zawsze jeden heks; rozmiar i wypełnienie nie mają zastosowania." : "Wymiar figury w jednostkach siatki: 1–15."); canvas.Invalidate(); };
            shapePicker.SelectedIndexChanged += (s, e) => settings(); sizePicker.ValueChanged += (s, e) => settings(); heightPicker.ValueChanged += (s, e) => settings(); fillPicker.CheckedChanged += (s, e) => settings(); settings();
            fogPreviewButton=new CheckBox{Text="Podgląd bez mgły",AutoSize=false,Height=18,AccessibleName="Tymczasowy podgląd bez mgły",Font=new Font(Font,FontStyle.Bold)};toolbox.Controls.Add(fogPreviewButton);fogPreviewButton.CheckedChanged+=(s,e)=>{if(fogPreviewButton.Checked!=previewWithoutFog)SetFogPreview(fogPreviewButton.Checked);};
            toolbox.Layout+=(s,e)=>{int w=toolbox.ClientSize.Width;foreach(Control b in paletteTools.Controls){b.Width=(w-8)/4;b.Height=26;}shapeLabel.Location=new Point(0,59);shapePicker.SetBounds(52,57,Math.Max(80,w-52),22);sizeLabel.Location=new Point(0,82);sizePicker.Location=new Point(52,80);heightPicker.Location=new Point(w-60,80);fillPicker.Location=new Point(0,103);fogPreviewButton.SetBounds(0,122,w,18);};
            var presets = Section("Presety", 1);
            presetPickerRow = new Panel { Dock = DockStyle.Top, Height = 28 }; presets.Controls.Add(presetPickerRow);
            terrainPicker.DropDownStyle = ComboBoxStyle.DropDownList; terrainPicker.DisplayMember = "Name"; terrainPicker.AccessibleName = "Presety"; presetPickerRow.Controls.Add(terrainPicker);
            terrainPicker.SelectedIndexChanged += (s, e) => { if (!updating && terrainPicker.SelectedItem != null) SelectPreset(((TerrainPreset)terrainPicker.SelectedItem).Id); };
            refreshPresetsButton = IconAction(presetPickerRow, "refresh", "Odśwież presety", () => LoadLibraryPresets(false));
            presetActions = new FlowLayoutPanel { Height = 32, WrapContents = false }; presets.Controls.Add(presetActions);
            IconAction(presetActions, "new", "Nowy preset", CreatePreset); renamePresetButton = IconAction(presetActions, "pencil", "Edytuj nazwę", RenamePreset); IconAction(presetActions, "save", "Zapisz preset", SavePreset); deletePresetButton = IconAction(presetActions, "trash", "Usuń preset", RemovePreset);
            presetStateLabel.AutoEllipsis = true; presets.Controls.Add(presetStateLabel);
            presets.Layout += (s, e) => { int w = presets.ClientSize.Width; terrainPicker.SetBounds(0, 1, Math.Max(80, w - 32), 24); refreshPresetsButton.SetBounds(w - 28, 0, 28, 26); presetActions.SetBounds(0, 25, w, 28); foreach (Control b in presetActions.Controls) b.Width = (w - 16) / 4; presetStateLabel.SetBounds(0, 54, w, 20); };

            var terrains = Section("Tereny", 2); addTerrainButton = SmallButton(terrains, "Dodaj teren", () => EditTerrain(true), 208); addTerrainButton.Dock = DockStyle.Top; addTerrainButton.Height = 26;
            var terrainList = new Panel { Dock = DockStyle.Fill }; terrains.Controls.Add(terrainList); terrainList.BringToFront();
            terrainUp = TerrainScrollButton(terrainList, true);
            terrainDown = TerrainScrollButton(terrainList, false);
            terrainScroll = new WheelViewport(delta => ScrollTerrains(delta)) { Dock = DockStyle.Fill, AccessibleName = "Lista terenów — osobne przewijanie" }; terrainList.Controls.Add(terrainScroll); terrainScroll.BringToFront();
            paletteTiles.AutoSize = true; paletteTiles.AutoSizeMode = AutoSizeMode.GrowAndShrink; paletteTiles.FlowDirection = FlowDirection.TopDown; paletteTiles.WrapContents = false; paletteTiles.Margin = Padding.Empty; terrainScroll.Controls.Add(paletteTiles); terrainScroll.Layout += (s, e) => LayoutPalette(); paletteTiles.SizeChanged += (s, e) => LayoutPalette(); paletteTiles.ControlAdded += (s, e) => LayoutPalette(); paletteTiles.ControlRemoved += (s, e) => LayoutPalette();

            var iconSection = Section("Ikony", 3); var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 30, WrapContents = false }; iconSection.Controls.Add(actions);
            IconAction(actions, "new", "Dodaj ikonę z pliku", ImportIcon); IconAction(actions, "pencil", "Edytuj ikonę", EditLibraryIcon); deleteIconButton = IconAction(actions, "trash", "Usuń wybraną ikonę z biblioteki", DeleteLibraryIcon); var more = SmallButton(actions, "…", OpenIconLibrary, 40); more.AccessibleName = "Pełna biblioteka ikon"; tips.SetToolTip(more, more.AccessibleName);
            iconPreview = new BufferedFlowPanel { Dock = DockStyle.Fill, WrapContents = true, AutoScroll = false, Padding = Padding.Empty }; iconSection.Controls.Add(iconPreview); iconPreview.BringToFront();
            var objects=Section("Obiekty",4);var objectActions=new FlowLayoutPanel{Dock=DockStyle.Top,Height=30,WrapContents=false};objects.Controls.Add(objectActions);
            IconAction(objectActions,"new","Dodaj wzorzec obiektu",AddObjectTemplate);IconAction(objectActions,"pencil","Edytuj wzorzec obiektu",EditObjectTemplate);deleteObjectButton=IconAction(objectActions,"trash","Usuń wzorzec obiektu",DeleteObjectTemplate);SmallButton(objectActions,"…",OpenObjectLibrary,32);IconAction(objectActions,"labels","Lista instancji na mapie",OpenObjectInstances);
            objectPreview=new BufferedFlowPanel{Dock=DockStyle.Fill,AutoScroll=false,WrapContents=true};objects.Controls.Add(objectPreview);objectPreview.BringToFront();
            SelectTool(EditorTool.Select); LayoutPalette(); RefreshPresetPicker(); UpdateMaterialIndicator();
            canvas.MouseLeave += (s, e) => { previewCell = -1; previousStrokePoint = null; objectPreviousCursor=null; canvas.Invalidate(); };
            Disposed += (s, e) => { foreach (var b in documentBitmaps.Values) b.Dispose(); foreach (var b in libraryBitmaps.Values) b.Dispose(); };
        }
        bool layingOutPalette;
        Button TerrainScrollButton(Control parent, bool up)
        {
            var button = SmallButton(parent, "", () => ScrollTerrainRow(up ? -1 : 1), 208);
            button.Dock = up ? DockStyle.Top : DockStyle.Bottom; button.Height = 18; button.Margin = Padding.Empty; button.Padding = Padding.Empty;
            button.Image = EditorIcons.ScrollArrow(up); button.ImageAlign = ContentAlignment.MiddleCenter;
            button.AccessibleName = up ? "Przewiń tereny w górę" : "Przewiń tereny w dół"; tips.SetToolTip(button, button.AccessibleName); return button;
        }
        void ScrollTerrainRow(int direction)
        {
            var row = paletteTiles.Controls.Cast<TerrainTile>().FirstOrDefault();
            ScrollTerrains(direction * (row == null ? 36 : row.Height + row.Margin.Vertical));
        }
        void ScrollTerrains(int delta) { terrainOffset += delta; LayoutPalette(); }
        void LayoutPalette()
        {
            if (layingOutPalette || terrainScroll == null) return; layingOutPalette = true;
            try { int width = Math.Max(150, terrainScroll.ClientSize.Width); paletteTiles.Width = width; foreach (TerrainTile tile in paletteTiles.Controls) tile.ResizeRow(width); int max = Math.Max(0, paletteTiles.PreferredSize.Height - terrainScroll.ClientSize.Height); terrainOffset = Math.Max(0, Math.Min(terrainOffset, max)); paletteTiles.Location = new Point(0, -terrainOffset); terrainUp.Enabled = terrainOffset > 0; terrainDown.Enabled = terrainOffset < max; }
            finally { layingOutPalette = false; }
        }
    }
    internal sealed class WheelViewport : BufferedPanel
    {
        readonly Action<int> scroll;
        public WheelViewport(Action<int> scroll) { this.scroll = scroll; }
        protected override void OnMouseWheel(MouseEventArgs e) { scroll(-e.Delta * 36 / 120); var handled = e as HandledMouseEventArgs; if (handled != null) handled.Handled = true; }
    }
}
