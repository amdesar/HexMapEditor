using System;
using System.Drawing;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class BufferedFlowPanel : FlowLayoutPanel
    {
        public BufferedFlowPanel() { DoubleBuffered = true; }
    }
    internal sealed class TerrainTile : UserControl
    {
        internal readonly Button Choose = new Button(), Edit = new Button(), Delete = new Button();
        readonly ToolTip tips = new ToolTip();
        public string TerrainId;
        public TerrainTile(Action<string> choose, Action<string> edit, Action<string> delete)
        {
            Width = 88; Height = 52; Margin = new Padding(3); TabStop = false;
            Choose.Dock = DockStyle.Fill; Choose.FlatStyle = FlatStyle.Flat; Choose.AutoEllipsis = false; Choose.Padding = new Padding(2, 0, 2, 16); Choose.TabIndex = 0; Choose.Font = new Font("Segoe UI", 8f); Choose.TextAlign = ContentAlignment.TopCenter;
            Edit.SetBounds(Width - 40, Height - 20, 19, 19); Delete.SetBounds(Width - 20, Height - 20, 19, 19);
            Edit.Image = EditorIcons.Small("pencil"); Delete.Image = EditorIcons.Small("trash"); Edit.TabIndex = 1; Delete.TabIndex = 2;
            foreach (var b in new[] { Edit, Delete }) { b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0; b.BackColor = Color.FromArgb(49, 54, 67); b.Cursor = Cursors.Hand; b.Visible = false; }
            Controls.Add(Choose); Controls.Add(Edit); Controls.Add(Delete); Edit.BringToFront(); Delete.BringToFront();
            Choose.Click += (s, e) => choose(TerrainId); Edit.Click += (s, e) => edit(TerrainId); Delete.Click += (s, e) => delete(TerrainId);
            foreach (Control c in new Control[] { this, Choose, Edit, Delete }) { c.MouseEnter += (s, e) => Reveal(true); c.MouseLeave += (s, e) => Reveal(ContainsFocus || ClientRectangle.Contains(PointToClient(Cursor.Position))); c.Enter += (s, e) => Reveal(true); }
            Leave += (s, e) => Reveal(ClientRectangle.Contains(PointToClient(Cursor.Position)));
            Choose.KeyDown += (s, e) => { if (e.KeyCode == Keys.F2 || e.KeyCode == Keys.Delete) { e.Handled = true; e.SuppressKeyPress = true; if (e.KeyCode == Keys.F2) edit(TerrainId); else delete(TerrainId); } };
            Disposed += (s, e) => tips.Dispose();
        }
        void Reveal(bool visible) { Edit.Visible = visible; Delete.Visible = visible; }
        public void UpdateTerrain(TerrainDefinition terrain, bool selected)
        {
            TerrainId = terrain.TerrainId; Choose.Text = terrain.Name; var color = ColorTranslator.FromHtml(terrain.Color); Choose.BackColor = color; Choose.ForeColor = color.R * .299 + color.G * .587 + color.B * .114 > 145 ? Color.Black : Color.White;
            Choose.FlatAppearance.BorderColor = Color.White; Choose.FlatAppearance.BorderSize = selected ? 3 : 1; Choose.AccessibleName = "Teren: " + terrain.Name; Choose.AccessibleDescription = selected ? "Wybrany" : "Niewybrany";
            Edit.AccessibleName = "Edytuj teren " + terrain.Name; Delete.AccessibleName = "Usuń teren " + terrain.Name;
            tips.SetToolTip(Choose, terrain.Name + " · wersja " + terrain.Revision); tips.SetToolTip(Edit, Edit.AccessibleName + " (F2)"); tips.SetToolTip(Delete, Delete.AccessibleName + " (Delete)");
        }
    }
}
