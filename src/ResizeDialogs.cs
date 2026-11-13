using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class ResizeDiagram : Control
    {
        public ResizeDiagram() { DoubleBuffered = true; AccessibleName = "Schemat mapy; zmiany czterech brzegów"; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            float radius = 19, cx = ClientSize.Width / 2f, cy = ClientSize.Height / 2f;
            using (var pen = new Pen(Color.SteelBlue, 2)) using (var fill = new SolidBrush(Color.FromArgb(225, 234, 243)))
            {
                for (int row = -1; row <= 1; row++) for (int col = -1; col <= 1; col++)
                {
                    float x = cx + col * radius * 1.74f + (row == 0 ? 0 : radius * .87f), y = cy + row * radius * 1.5f;
                    var points = Enumerable.Range(0, 6).Select(i => { double a = (i * 60 - 90) * Math.PI / 180; return new PointF(x + radius * (float)Math.Cos(a), y + radius * (float)Math.Sin(a)); }).ToArray();
                    e.Graphics.FillPolygon(fill, points); e.Graphics.DrawPolygon(pen, points);
                }
            }
        }
    }
    internal sealed class ResizeDialog : DarkForm
    {
        readonly ScrubNumber topDelta = NewAmount(), bottomDelta = NewAmount(), leftDelta = NewAmount(), rightDelta = NewAmount();
        readonly Label result = new Label();
        public int TopDelta { get { return (int)topDelta.Value; } } public int BottomDelta { get { return (int)bottomDelta.Value; } }
        public int LeftDelta { get { return (int)leftDelta.Value; } } public int RightDelta { get { return (int)rightDelta.Value; } }
        static ScrubNumber NewAmount() { return new ScrubNumber { Minimum = -199, Maximum = 199, Value = 0, Width = 108 }; }
        static Control Field(string name, ScrubNumber amount)
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 }; layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.Controls.Add(new Label { Text = name, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 0, 0);
            amount.Anchor = AnchorStyles.None; amount.AccessibleName = name; layout.Controls.Add(amount, 0, 1); return layout;
        }
        public ResizeDialog(int columns, int rows)
        {
            Text = "Rozmiar mapy"; ClientSize = new Size(520, 430); Font = new Font("Segoe UI", 9); FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent; MaximizeBox = false; MinimizeBox = false;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 3, RowCount = 7 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            foreach (int height in new[] { 28, 58, 126, 58, 38, 44, 42 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height)); Controls.Add(layout);
            var current = new Label { Text = "Bieżący rozmiar: " + columns + " × " + rows, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }; layout.Controls.Add(current, 0, 0); layout.SetColumnSpan(current, 3);
            layout.Controls.Add(Field("Góra", topDelta), 1, 1); var left = Field("Lewo", leftDelta); left.Dock = DockStyle.None; left.Anchor = AnchorStyles.Left | AnchorStyles.Right; left.Height = 58; layout.Controls.Add(left, 0, 2);
            layout.Controls.Add(new ResizeDiagram { Dock = DockStyle.Fill }, 1, 2); var right = Field("Prawo", rightDelta); right.Dock = DockStyle.None; right.Anchor = AnchorStyles.Left | AnchorStyles.Right; right.Height = 58; layout.Controls.Add(right, 2, 2); layout.Controls.Add(Field("Dół", bottomDelta), 1, 3);
            var legend = new Label { Text = "Dodatnie dodają · Ujemne usuwają · 0 bez zmiany\nUsuwanie dotyczy brzegów mapy sprzed operacji.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }; layout.Controls.Add(legend, 0, 4); layout.SetColumnSpan(legend, 3);
            result.Dock = DockStyle.Fill; result.TextAlign = ContentAlignment.MiddleCenter; layout.Controls.Add(result, 0, 5); layout.SetColumnSpan(result, 3);
            var ok = new Button { Text = "OK", Width = 92, Height = 30 }; var cancel = new Button { Text = "Anuluj", Width = 92, Height = 30, DialogResult = DialogResult.Cancel };
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft }; buttons.Controls.Add(ok); buttons.Controls.Add(cancel); layout.Controls.Add(buttons, 0, 6); layout.SetColumnSpan(buttons, 3); AcceptButton = ok; CancelButton = cancel;
            Action update = () => { try { var size = MapLogic.ResizeDimensions(columns, rows, TopDelta, BottomDelta, LeftDelta, RightDelta); result.Text = "Wynik: " + size.Width + " × " + size.Height + (TopDelta == 0 && BottomDelta == 0 && LeftDelta == 0 && RightDelta == 0 ? " (bez zmian)" : ""); result.ForeColor = SystemColors.ControlText; ok.Enabled = true; } catch (InvalidDataException ex) { result.Text = ex.Message; result.ForeColor = Color.Maroon; ok.Enabled = false; } };
            foreach (var amount in new[] { topDelta, bottomDelta, leftDelta, rightDelta }) amount.ValueChanged += (s, e) => update();
            ok.Click += (s, e) => { ValidateChildren(); update(); if (ok.Enabled) DialogResult = DialogResult.OK; }; update();
            topDelta.TabIndex = 0; bottomDelta.TabIndex = 1; leftDelta.TabIndex = 2; rightDelta.TabIndex = 3;
        }
    }
    internal sealed class ResizeWarningDialog : DarkForm
    {
        internal readonly TextBox coordinates;
        internal readonly List<TextBox> GroupCoordinates = new List<TextBox>();
        public ResizeWarningDialog(int removed, string[] dataCoordinates) : this(removed, new List<RemovalGroup> { new RemovalGroup { Kind = CellContentKind.Text, Coordinates = dataCoordinates } }) { }
        public ResizeWarningDialog(int removed, List<RemovalGroup> groups)
        {
            Text = "Usunięcie danych mapy"; Size = new Size(600, 460); MinimumSize = new Size(480, 330); StartPosition = FormStartPosition.CenterParent; MaximizeBox = false; MinimizeBox = false; Font = new Font("Segoe UI", 9);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 3 }; layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); Controls.Add(layout);
            layout.Controls.Add(new Label { Text = "Usuwanych heksów: " + removed + "\nHeksów z danymi: " + groups.Sum(g => g.Coordinates.Length) + "\nPełne współrzędne w grupach dotyczą mapy PRZED operacją.\nCzy kontynuować usuwanie? Operację można cofnąć.", Dock = DockStyle.Fill }, 0, 0);
            var tabs = new TabControl { Dock = DockStyle.Fill }; layout.Controls.Add(tabs, 0, 1);
            foreach (var group in groups)
            {
                var text = new DarkTextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, WordWrap = true, MaxLength = int.MaxValue, Dock = DockStyle.Fill, AccessibleName = group.Title + ": pełne współrzędne przed zmianą", Text = String.Join(", ", group.Coordinates) };
                var tab = new TabPage(group.Title + " (" + group.Coordinates.Length + ")") { Padding = new Padding(5) }; tab.Controls.Add(text); tabs.TabPages.Add(tab); GroupCoordinates.Add(text);
            }
            coordinates = GroupCoordinates.FirstOrDefault();
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) }; var no = new Button { Text = "Nie", DialogResult = DialogResult.No, Width = 90, Height = 30 }; var yes = new Button { Text = "Tak, usuń", DialogResult = DialogResult.Yes, Width = 100, Height = 30 }; buttons.Controls.Add(no); buttons.Controls.Add(yes); layout.Controls.Add(buttons, 0, 2);
            AcceptButton = no; CancelButton = no; Shown += (s, e) => { var screen = Screen.FromControl(this).WorkingArea; MaximumSize = new Size(screen.Width - 32, screen.Height - 32); no.Select(); };
        }
    }
}
