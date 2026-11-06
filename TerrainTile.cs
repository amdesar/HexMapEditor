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
            Width = 182; Height = 42; Margin = new Padding(3); TabStop = false;
            Choose.SetBounds(0, 0, 126, 42); Choose.FlatStyle = FlatStyle.Flat; Choose.AutoEllipsis = true; Choose.Padding = new Padding(3); Choose.TabIndex = 0; Choose.TextAlign = ContentAlignment.MiddleCenter;
            Edit.SetBounds(130, 5, 24, 32); Delete.SetBounds(158, 5, 24, 32);
            Edit.Image = EditorIcons.Small("pencil"); Delete.Image = EditorIcons.Small("trash"); Edit.TabIndex = 1; Delete.TabIndex = 2;
            foreach (var b in new[] { Edit, Delete }) { b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0; b.BackColor = Color.FromArgb(49, 54, 67); b.Cursor = Cursors.Hand; }
            Controls.Add(Choose); Controls.Add(Edit); Controls.Add(Delete); Edit.BringToFront(); Delete.BringToFront();
            Choose.Click += (s, e) => choose(TerrainId); Edit.Click += (s, e) => edit(TerrainId); Delete.Click += (s, e) => delete(TerrainId);
            Choose.KeyDown += (s, e) => { if (e.KeyCode == Keys.F2 || e.KeyCode == Keys.Delete) { e.Handled = true; e.SuppressKeyPress = true; if (e.KeyCode == Keys.F2) edit(TerrainId); else delete(TerrainId); } };
            Disposed += (s, e) => tips.Dispose();
        }
        public void UpdateTerrain(TerrainDefinition terrain, bool selected)
        {
            TerrainId = terrain.TerrainId; Choose.Text = terrain.Name; var color = ColorTranslator.FromHtml(terrain.Color); Choose.BackColor = color; Choose.ForeColor = color.R * .299 + color.G * .587 + color.B * .114 > 145 ? Color.Black : Color.White;
            Choose.FlatAppearance.BorderColor = Color.White; Choose.FlatAppearance.BorderSize = selected ? 3 : 1; Choose.AccessibleName = "Teren: " + terrain.Name; Choose.AccessibleDescription = selected ? "Wybrany" : "Niewybrany";
            Edit.AccessibleName = "Edytuj teren " + terrain.Name; Delete.AccessibleName = "Usuń teren " + terrain.Name;
            tips.SetToolTip(Choose, terrain.Name + " · wersja " + terrain.Revision); tips.SetToolTip(Edit, Edit.AccessibleName + " (F2)"); tips.SetToolTip(Delete, Delete.AccessibleName + " (Delete)");
        }
    }
}
