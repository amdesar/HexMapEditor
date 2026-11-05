using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class ResizeDialog : EditorDialog
    {
        readonly NumericUpDown topDelta = NewAmount(), bottomDelta = NewAmount(), leftDelta = NewAmount(), rightDelta = NewAmount();
        readonly Label result = new Label();
        public int TopDelta { get { return (int)topDelta.Value; } } public int BottomDelta { get { return (int)bottomDelta.Value; } }
        public int LeftDelta { get { return (int)leftDelta.Value; } } public int RightDelta { get { return (int)rightDelta.Value; } }
        static NumericUpDown NewAmount() { return new NumericUpDown { Minimum = -199, Maximum = 199, Value = 0 }; }
        public ResizeDialog(int columns, int rows) : base("Rozmiar mapy", 330)
        {
            Layout.Controls.Add(new Label { Text = "Bieżący rozmiar: " + columns + " × " + rows + "\nDodatnie dodają, ujemne usuwają, 0 bez zmiany.", Dock = DockStyle.Fill }, 0, 0); Layout.SetColumnSpan(Layout.GetControlFromPosition(0, 0), 3); Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            Row(1, "Góra", topDelta, true); Row(2, "Dół", bottomDelta, true); Row(3, "Lewo", leftDelta, true); Row(4, "Prawo", rightDelta, true);
            result.Dock = DockStyle.Fill; Layout.Controls.Add(result, 0, 5); Layout.SetColumnSpan(result, 3); Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            Action update = () => { try { var size = MapLogic.ResizeDimensions(columns, rows, TopDelta, BottomDelta, LeftDelta, RightDelta); result.Text = "Wynik: " + size.Width + " × " + size.Height + (TopDelta == 0 && BottomDelta == 0 && LeftDelta == 0 && RightDelta == 0 ? " (bez zmian)" : ""); result.ForeColor = SystemColors.ControlText; Ok.Enabled = true; } catch (InvalidDataException ex) { result.Text = ex.Message; result.ForeColor = Color.Maroon; Ok.Enabled = false; } };
            foreach (var amount in new[] { topDelta, bottomDelta, leftDelta, rightDelta }) amount.ValueChanged += (s, e) => update();
            Buttons(6, () => { update(); if (Ok.Enabled) DialogResult = DialogResult.OK; }); update();
        }
    }
    internal sealed class ResizeWarningDialog : Form
    {
        internal readonly TextBox coordinates = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, WordWrap = true, MaxLength = int.MaxValue, Dock = DockStyle.Fill, AccessibleName = "Pełna lista współrzędnych heksów z danymi przed zmianą" };
        public ResizeWarningDialog(int removed, string[] dataCoordinates)
        {
            Text = "Usunięcie danych mapy"; Size = new Size(560, 430); MinimumSize = new Size(440, 300); StartPosition = FormStartPosition.CenterParent; MaximizeBox = false; MinimizeBox = false; Font = new Font("Segoe UI", 9);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 3 }; layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); Controls.Add(layout);
            layout.Controls.Add(new Label { Text = "Usuwanych heksów: " + removed + "\nHeksów z danymi: " + dataCoordinates.Length + "\nWspółrzędne poniżej dotyczą mapy PRZED operacją.\nCzy kontynuować usuwanie? Operację można cofnąć.", Dock = DockStyle.Fill }, 0, 0);
            coordinates.Text = String.Join(", ", dataCoordinates); layout.Controls.Add(coordinates, 0, 1);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) }; var no = new Button { Text = "Nie", DialogResult = DialogResult.No, Width = 90, Height = 30 }; var yes = new Button { Text = "Tak, usuń", DialogResult = DialogResult.Yes, Width = 100, Height = 30 }; buttons.Controls.Add(no); buttons.Controls.Add(yes); layout.Controls.Add(buttons, 0, 2);
            AcceptButton = no; CancelButton = no; Shown += (s, e) => { var screen = Screen.FromControl(this).WorkingArea; MaximumSize = new Size(screen.Width - 32, screen.Height - 32); no.Select(); };
        }
    }
}
