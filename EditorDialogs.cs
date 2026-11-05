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
    internal sealed class TableSizeDialog : EditorDialog
    {
        readonly NumericUpDown rows = new NumericUpDown { Minimum = 1, Maximum = 100, Value = 2 }, columns = new NumericUpDown { Minimum = 1, Maximum = 100, Value = 2 };
        public int Rows { get { return (int)rows.Value; } } public int Columns { get { return (int)columns.Value; } }
        public TableSizeDialog() : base("Wstaw tabelę (1–100)", 155) { Row(0, "Wiersze", rows, true); Row(1, "Kolumny", columns, true); Buttons(2, () => DialogResult = DialogResult.OK); }
    }
    internal sealed class ResizeDialog : EditorDialog
    {
        readonly ComboBox side = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList }, operation = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        readonly NumericUpDown amount = new NumericUpDown { Minimum = 1, Maximum = 199, Value = 1 };
        public string Side { get { return new[] { "top", "bottom", "left", "right" }[side.SelectedIndex]; } } public int Delta { get { return (int)amount.Value * (operation.SelectedIndex == 0 ? 1 : -1); } }
        public ResizeDialog(int columns, int rows) : base("Rozmiar mapy · " + columns + " × " + rows, 225)
        { side.Items.AddRange(new[] { "Góra", "Dół", "Lewo", "Prawo" }); side.SelectedIndex = 0; operation.Items.AddRange(new[] { "Dodaj", "Usuń" }); operation.SelectedIndex = 0; Row(0, "Strona", side, true); Row(1, "Operacja", operation, true); Row(2, "Liczba", amount, true); Buttons(3, () => DialogResult = DialogResult.OK); }
    }
}
