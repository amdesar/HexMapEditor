using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal class EditorDialog : DarkForm
    {
        protected new readonly TableLayoutPanel Layout;
        protected readonly Button Ok = new Button { Text = "OK", Width = 92, Height = 30 };
        protected EditorDialog(string title, int height)
        {
            Text = title; ClientSize = new Size(420, height); Font = new Font("Segoe UI", 9); FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent; MaximizeBox = false; MinimizeBox = false;
            Layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 3 }; Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110)); Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); Controls.Add(Layout);
        }
        protected void Row(int row, string label, Control input, bool span)
        { Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); Layout.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row); input.Dock = DockStyle.Fill; input.Margin = new Padding(3, 5, 3, 5); Layout.Controls.Add(input, 1, row); if (span) Layout.SetColumnSpan(input, 2); }
        protected void Buttons(int row, Action accept)
        {
            var panel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill }; var cancel = new Button { Text = "Anuluj", DialogResult = DialogResult.Cancel, Width = 92, Height = 30 }; Ok.Click += (s, e) => accept(); panel.Controls.Add(Ok); panel.Controls.Add(cancel); Layout.Controls.Add(panel, 0, row); Layout.SetColumnSpan(panel, 3); Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); AcceptButton = Ok; CancelButton = cancel;
        }
    }
    internal sealed class NameDialog : EditorDialog
    {
        readonly TextBox name = new DarkTextBox(); public string Value { get { return name.Text.Trim(); } }
        public NameDialog(string title, string initial) : base(title, 120) { name.Text = initial; name.MaxLength = 200; Row(0, "Nazwa", name, true); Buttons(1, () => { if (Value.Length > 0) DialogResult = DialogResult.OK; }); }
    }
    internal sealed class SavePresetDialog : EditorDialog
    {
        readonly RadioButton overwrite = new RadioButton { Text = "Nadpisz bieżący preset", AutoSize = true }, asNew = new RadioButton { Text = "Zapisz jako nowy preset", AutoSize = true };
        readonly TextBox name = new DarkTextBox { MaxLength = 200 };
        readonly SwitchBox terrains=new SwitchBox{Text="Tereny",Checked=true,AutoSize=true},icons=new SwitchBox{Text="Ikony",Checked=true,AutoSize=true},objects=new SwitchBox{Text="Obiekty",Checked=true,AutoSize=true};
        internal PresetSections Sections{get{return (terrains.Checked?PresetSections.Terrains:0)|(icons.Checked?PresetSections.Icons:0)|(objects.Checked?PresetSections.Objects:0);}}
        public bool AsNew { get { return asNew.Checked; } } public string PresetName { get { return name.Text.Trim(); } }
        public SavePresetDialog(TerrainPreset preset, bool saved,bool all=false) : base("Zapisz preset", 410)
        {
            overwrite.Enabled = saved && preset.Id != PresetCatalog.DefaultId; overwrite.Checked = overwrite.Enabled; asNew.Checked = !overwrite.Enabled;
            name.Text = preset.Name + (preset.Id == PresetCatalog.DefaultId || saved ? " — kopia" : ""); name.Enabled = asNew.Checked;
            Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); Layout.Controls.Add(overwrite, 0, 0); Layout.SetColumnSpan(overwrite, 3);
            Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); Layout.Controls.Add(asNew, 0, 1); Layout.SetColumnSpan(asNew, 3);
            Row(2, "Nowa nazwa", name, true);
            var reason = new Label { Text = preset.Id == PresetCatalog.DefaultId ? "Domyślny jest chronionym wzorcem. Zapisz roboczą paletę jako nowy preset z własnym ID." : saved ? "Nadpisanie zapisuje roboczą paletę do bieżącego pliku presetu. Sesyjne Undo/Redo odtwarza pliki z ochroną konfliktów (do 300 wpisów, bez historii po restarcie)." : "Ten preset nie został jeszcze zapisany. Najpierw zapisz go jako nowy preset.", Dock = DockStyle.Fill };
            Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64)); Layout.Controls.Add(reason, 0, 3); Layout.SetColumnSpan(reason, 3); asNew.CheckedChanged += (s, e) => name.Enabled = asNew.Checked;
            var sections=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};sections.Controls.AddRange(new Control[]{terrains,icons,objects});Layout.Controls.Add(sections,0,4);Layout.SetColumnSpan(sections,3);Layout.RowStyles.Add(new RowStyle(SizeType.Absolute,34));
            var hint=new Label{Dock=DockStyle.Fill};Layout.Controls.Add(hint,0,5);Layout.SetColumnSpan(hint,3);Layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
            Buttons(6, () => { if (Sections!=PresetSections.None&&(!AsNew || PresetName.Length > 0)) DialogResult = DialogResult.OK; }); Ok.Text = "Zapisz";
            Action state=()=>{Ok.Enabled=Sections!=PresetSections.None;hint.Text=Ok.Enabled?(all?"Zapis podczas zamykania obejmuje wszystkie trzy sekcje.":""):"Wybierz przynajmniej jedną sekcję lub utwórz nowy pusty preset.";};foreach(var s in new[]{terrains,icons,objects}){s.Enabled=!all;s.CheckedChanged+=(sender,e)=>state();}state();
        }
    }
    internal sealed class TableSizeDialog : EditorDialog
    {
        readonly ScrubNumber rows = new ScrubNumber { Minimum = 1, Maximum = 100, Value = 2 }, columns = new ScrubNumber { Minimum = 1, Maximum = 100, Value = 2 };
        public int Rows { get { return (int)rows.Value; } } public int Columns { get { return (int)columns.Value; } }
        public TableSizeDialog() : base("Wstaw tabelę (1–100)", 155) { Row(0, "Wiersze", rows, true); Row(1, "Kolumny", columns, true); Buttons(2, () => DialogResult = DialogResult.OK); }
    }
}
