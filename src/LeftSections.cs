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
            foreach (var style in new[] { new RowStyle(SizeType.Absolute, 137), new RowStyle(SizeType.Absolute, 118), new RowStyle(SizeType.Percent, 100), new RowStyle(SizeType.Absolute, 194), new RowStyle(SizeType.Absolute, 40) }) leftSections.RowStyles.Add(style);
            left.Controls.Add(leftSections);
            var toolbox = Section("Przybornik", 0);
            paletteTools = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, WrapContents = false }; toolbox.Controls.Add(paletteTools);
            string[] names = { "Zaznaczanie", "Przesuwanie", "Pędzel", "Wiadro", "Mgła wojny", "Wypełnienie mgłą" }, icons = { "select", "hand", "brush", "bucket", "fog", "fogbucket" };
            for (int i = 0; i < names.Length; i++) { var tool = (EditorTool)i; var b = ShellButton(paletteTools, "", icons[i], names[i], () => SelectTool(tool), 32); toolButtons.Add(tool, b); }
            var shapeLabel = new Label { Text = "Kształt", Width = 52, Height = 22 }; toolbox.Controls.Add(shapeLabel);
            shapePicker = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = "Kształt pędzla" }; shapePicker.Items.AddRange(new object[] { "Punkt", "Kwadrat", "Prostokąt", "Koło", "Heksagon", "Pięciokąt foremny", "Linia pozioma", "Linia pionowa" }); shapePicker.SelectedIndex = 0; toolbox.Controls.Add(shapePicker);
            var sizeLabel = new Label { Text = "Rozmiar", Width = 52, Height = 22 }; toolbox.Controls.Add(sizeLabel);
            sizePicker = new NumericUpDown { Minimum = 1, Maximum = 15, Value = 1, Width = 60, AccessibleName = "Rozmiar lub szerokość pędzla" }; heightPicker = new NumericUpDown { Minimum = 1, Maximum = 15, Value = 1, Width = 60, Visible = false, AccessibleName = "Wysokość prostokąta" }; toolbox.Controls.Add(sizePicker); toolbox.Controls.Add(heightPicker);
            fillPicker = new CheckBox { Text = "Wypełnienie", Checked = true, AutoSize = true, AccessibleName = "Wypełnienie figury; wyłącz, aby malować obrys" }; toolbox.Controls.Add(fillPicker);
            tips.SetToolTip(sizePicker, "1–15. Heksagon: promień w pierścieniach + pole środkowe. Pozostałe: wymiar figury w odstępach siatki. Punkt zawsze 1 heks."); tips.SetToolTip(heightPicker, "Drugi wymiar prostokąta: wysokość 1–15");
            Action settings = () => { EndCanvasGesture(true); brushShape = (BrushShape)shapePicker.SelectedIndex; brushSize = (int)sizePicker.Value; brushHeight = (int)heightPicker.Value; brushFilled = fillPicker.Checked; heightPicker.Visible = brushShape == BrushShape.Rectangle; sizePicker.Enabled = brushShape != BrushShape.Point; fillPicker.Enabled = brushShape != BrushShape.Horizontal && brushShape != BrushShape.Vertical && brushShape != BrushShape.Point; tips.SetToolTip(sizeLabel, brushShape == BrushShape.Point ? "Punkt obejmuje zawsze jeden heks; rozmiar i wypełnienie nie mają zastosowania." : "Wymiar figury w jednostkach siatki: 1–15."); canvas.Invalidate(); };
            shapePicker.SelectedIndexChanged += (s, e) => settings(); sizePicker.ValueChanged += (s, e) => settings(); heightPicker.ValueChanged += (s, e) => settings(); fillPicker.CheckedChanged += (s, e) => settings(); settings();
            toolbox.Layout += (s, e) => { int w = toolbox.ClientSize.Width; foreach (Control b in paletteTools.Controls) b.Width = (w - 12) / 6; shapeLabel.Location = new Point(0, 39); shapePicker.SetBounds(52, 37, Math.Max(80, w - 52), 24); sizeLabel.Location = new Point(0, 65); sizePicker.Location = new Point(52, 63); heightPicker.Location = new Point(w - 60, 63); fillPicker.Location = new Point(0, 89); };

            var presets = Section("Presety", 1);
            presetPickerRow = new Panel { Dock = DockStyle.Top, Height = 28 }; presets.Controls.Add(presetPickerRow);
            terrainPicker.DropDownStyle = ComboBoxStyle.DropDownList; terrainPicker.DisplayMember = "Name"; terrainPicker.AccessibleName = "Presety"; presetPickerRow.Controls.Add(terrainPicker);
            terrainPicker.SelectedIndexChanged += (s, e) => { if (!updating && terrainPicker.SelectedItem != null) SelectPreset(((TerrainPreset)terrainPicker.SelectedItem).Id); };
            refreshPresetsButton = IconAction(presetPickerRow, "refresh", "Odśwież presety", () => LoadLibraryPresets(false));
            presetActions = new FlowLayoutPanel { Height = 32, WrapContents = false }; presets.Controls.Add(presetActions);
            IconAction(presetActions, "new", "Nowy preset", CreatePreset); renamePresetButton = IconAction(presetActions, "pencil", "Edytuj nazwę", RenamePreset); IconAction(presetActions, "save", "Zapisz preset", SavePreset); deletePresetButton = IconAction(presetActions, "trash", "Usuń preset", RemovePreset);
            presetStateLabel.AutoEllipsis = true; presets.Controls.Add(presetStateLabel);
            presets.Layout += (s, e) => { int w = presets.ClientSize.Width; terrainPicker.SetBounds(0, 1, Math.Max(80, w - 32), 24); refreshPresetsButton.SetBounds(w - 28, 0, 28, 26); presetActions.SetBounds(0, 28, w, 32); foreach (Control b in presetActions.Controls) b.Width = (w - 16) / 4; presetStateLabel.SetBounds(0, 62, w, 30); };

            var terrains = Section("Tereny", 2); addTerrainButton = SmallButton(terrains, "Dodaj teren", () => EditTerrain(true), 208); addTerrainButton.Dock = DockStyle.Top; addTerrainButton.Height = 26;
            var terrainList = new Panel { Dock = DockStyle.Fill }; terrains.Controls.Add(terrainList); terrainList.BringToFront();
            terrainUp = SmallButton(terrainList, "▲", () => ScrollTerrains(-72), 208); terrainUp.Dock = DockStyle.Top; terrainUp.Height = 18; terrainUp.AccessibleName = "Przewiń tereny w górę"; tips.SetToolTip(terrainUp, terrainUp.AccessibleName);
            terrainDown = SmallButton(terrainList, "▼", () => ScrollTerrains(72), 208); terrainDown.Dock = DockStyle.Bottom; terrainDown.Height = 18; terrainDown.AccessibleName = "Przewiń tereny w dół"; tips.SetToolTip(terrainDown, terrainDown.AccessibleName);
            terrainScroll = new WheelViewport(delta => ScrollTerrains(delta)) { Dock = DockStyle.Fill, AccessibleName = "Lista terenów — osobne przewijanie" }; terrainList.Controls.Add(terrainScroll); terrainScroll.BringToFront();
            paletteTiles.AutoSize = true; paletteTiles.AutoSizeMode = AutoSizeMode.GrowAndShrink; paletteTiles.FlowDirection = FlowDirection.TopDown; paletteTiles.WrapContents = false; paletteTiles.Margin = Padding.Empty; terrainScroll.Controls.Add(paletteTiles); terrainScroll.Layout += (s, e) => LayoutPalette(); paletteTiles.SizeChanged += (s, e) => LayoutPalette(); paletteTiles.ControlAdded += (s, e) => LayoutPalette(); paletteTiles.ControlRemoved += (s, e) => LayoutPalette();

            var iconSection = Section("Ikony", 3); var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 30, WrapContents = false }; iconSection.Controls.Add(actions);
            IconAction(actions, "new", "Dodaj ikonę z pliku", ImportIcon); IconAction(actions, "pencil", "Edytuj ikonę", EditLibraryIcon); deleteIconButton = IconAction(actions, "trash", "Usuń wybraną ikonę z biblioteki", DeleteLibraryIcon); var more = SmallButton(actions, "…", OpenIconLibrary, 40); more.AccessibleName = "Pełna biblioteka ikon"; tips.SetToolTip(more, more.AccessibleName);
            iconPreview = new BufferedFlowPanel { Dock = DockStyle.Fill, WrapContents = true, AutoScroll = false, Padding = Padding.Empty }; iconSection.Controls.Add(iconPreview); iconPreview.BringToFront();
            Section("Obiekty", 4).Controls.Add(new Label { Text = "W przyszłym etapie", Dock = DockStyle.Fill, ForeColor = Color.Silver });
            SelectTool(EditorTool.Select); LayoutPalette(); RefreshPresetPicker(); UpdateMaterialIndicator();
            canvas.MouseLeave += (s, e) => { previewCell = -1; previousStrokePoint = null; canvas.Invalidate(); };
            Disposed += (s, e) => { foreach (var b in documentBitmaps.Values) b.Dispose(); foreach (var b in libraryBitmaps.Values) b.Dispose(); };
        }
        bool layingOutPalette;
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
