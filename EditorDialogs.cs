using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal class EditorDialog : Form
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
        readonly TextBox name = new TextBox(); public string Value { get { return name.Text.Trim(); } }
        public NameDialog(string title, string initial) : base(title, 120) { name.Text = initial; name.MaxLength = 200; Row(0, "Nazwa", name, true); Buttons(1, () => { if (Value.Length > 0) DialogResult = DialogResult.OK; }); }
    }
    internal sealed class TerrainDialog : EditorDialog
    {
        readonly TextBox name = new TextBox(); readonly Button color = new Button(); readonly CheckBox apply = new CheckBox { Text = "Zastosuj do bieżącej mapy", AutoSize = true };
        public string TerrainName { get { return name.Text.Trim(); } } public string TerrainColor { get; private set; } public bool ApplyToMap { get { return apply.Checked; } }
        public TerrainDialog(TerrainDefinition terrain, bool existing) : base(existing ? "Edycja terenu" : "Nowy teren", 205)
        {
            name.Text = terrain.Name; name.MaxLength = 200; TerrainColor = terrain.Color; color.Text = terrain.Color; color.BackColor = ColorTranslator.FromHtml(terrain.Color); color.ForeColor = Color.Black;
            Row(0, "Nazwa", name, true); Row(1, "Kolor", color, true); color.Click += (s, e) => { using (var d = new ColorDialog { Color = ColorTranslator.FromHtml(TerrainColor), FullOpen = true }) if (d.ShowDialog(this) == DialogResult.OK) { TerrainColor = "#" + d.Color.R.ToString("X2") + d.Color.G.ToString("X2") + d.Color.B.ToString("X2"); color.Text = TerrainColor; color.BackColor = d.Color; } };
            apply.Checked = false; apply.Enabled = existing; Layout.Controls.Add(apply, 0, 2); Layout.SetColumnSpan(apply, 3); Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); Buttons(3, () => { if (TerrainName.Length > 0) DialogResult = DialogResult.OK; });
        }
    }
    internal sealed class SavePresetDialog : EditorDialog
    {
        readonly RadioButton overwrite = new RadioButton { Text = "Nadpisz bieżący preset", AutoSize = true }, asNew = new RadioButton { Text = "Zapisz jako nowy preset", AutoSize = true };
        readonly TextBox name = new TextBox { MaxLength = 200 };
        public bool AsNew { get { return asNew.Checked; } } public string PresetName { get { return name.Text.Trim(); } }
        public SavePresetDialog(TerrainPreset preset, bool saved) : base("Zapisz zestaw", 255)
        {
            overwrite.Enabled = saved && preset.Id != PresetCatalog.DefaultId; overwrite.Checked = overwrite.Enabled; asNew.Checked = !overwrite.Enabled;
            name.Text = preset.Name + (preset.Id == PresetCatalog.DefaultId || saved ? " — kopia" : ""); name.Enabled = asNew.Checked;
            Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); Layout.Controls.Add(overwrite, 0, 0); Layout.SetColumnSpan(overwrite, 3);
            Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); Layout.Controls.Add(asNew, 0, 1); Layout.SetColumnSpan(asNew, 3);
            Row(2, "Nowa nazwa", name, true);
            var reason = new Label { Text = preset.Id == PresetCatalog.DefaultId ? "Domyślny jest chronionym wzorcem. Zapisz roboczą paletę jako nowy preset z własnym ID." : saved ? "Nadpisanie zapisuje roboczą paletę do bieżącego pliku presetu. Historia nie cofa plików biblioteki." : "Ten zestaw nie został jeszcze zapisany. Najpierw zapisz go jako nowy preset.", Dock = DockStyle.Fill };
            Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64)); Layout.Controls.Add(reason, 0, 3); Layout.SetColumnSpan(reason, 3); asNew.CheckedChanged += (s, e) => name.Enabled = asNew.Checked;
            Buttons(4, () => { if (!AsNew || PresetName.Length > 0) DialogResult = DialogResult.OK; }); Ok.Text = "Zapisz";
        }
    }
    internal sealed class TableSizeDialog : EditorDialog
    {
        readonly NumericUpDown rows = new NumericUpDown { Minimum = 1, Maximum = 100, Value = 2 }, columns = new NumericUpDown { Minimum = 1, Maximum = 100, Value = 2 };
        public int Rows { get { return (int)rows.Value; } } public int Columns { get { return (int)columns.Value; } }
        public TableSizeDialog() : base("Wstaw tabelę (1–100)", 155) { Row(0, "Wiersze", rows, true); Row(1, "Kolumny", columns, true); Buttons(2, () => DialogResult = DialogResult.OK); }
    }
}
