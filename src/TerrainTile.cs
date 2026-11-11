using System;
using System.Drawing;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class TerrainChoiceButton : Button
    {
        internal Image FillImage;
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var inner = Rectangle.Inflate(ClientRectangle, -3, -3);
            if (FillImage != null && inner.Width > 0 && inner.Height > 0)
            {
                float factor = Math.Max((float)inner.Width / FillImage.Width, (float)inner.Height / FillImage.Height);
                var state = e.Graphics.Save();
                e.Graphics.SetClip(inner);
                e.Graphics.DrawImage(FillImage, inner.X + (inner.Width - FillImage.Width * factor) / 2,
                    inner.Y + (inner.Height - FillImage.Height * factor) / 2, FillImage.Width * factor, FillImage.Height * factor);
                e.Graphics.Restore(state);
            }
            var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
            var outline = ForeColor == Color.Black ? Color.White : Color.Black;
            for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) if (x != 0 || y != 0)
                TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(inner.X+x, inner.Y+y, inner.Width, inner.Height), outline, flags);
            TextRenderer.DrawText(e.Graphics, Text, Font, inner, ForeColor, flags);
            if (Focused) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(inner, -1, -1));
        }
    }
    internal sealed class BufferedFlowPanel : FlowLayoutPanel
    {
        public BufferedFlowPanel() { DoubleBuffered = true; }
    }
    internal sealed class TerrainTile : UserControl
    {
        internal readonly TerrainChoiceButton Choose = new TerrainChoiceButton();
        internal readonly Button Edit = new Button(), Delete = new Button();
        readonly ToolTip tips = new ToolTip();
        string mosaicKey;
        public string TerrainId;
        public TerrainTile(Action<string> choose, Action<string> edit, Action<string> delete)
        {
            Width = 196; Height = 32; Margin = new Padding(0, 2, 0, 2); TabStop = false;
            Choose.FlatStyle = FlatStyle.Flat; Choose.AutoEllipsis = true; Choose.Padding = new Padding(3); Choose.TabIndex = 0; Choose.TextAlign = ContentAlignment.MiddleCenter;
            Choose.Font = new Font(Font, FontStyle.Bold);
            ResizeRow(Width);
            Edit.Image = EditorIcons.Small("pencil"); Delete.Image = EditorIcons.Small("trash"); Edit.TabIndex = 1; Delete.TabIndex = 2;
            foreach (var b in new[] { Edit, Delete }) { b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0; b.BackColor = Color.FromArgb(49, 54, 67); b.Cursor = Cursors.Hand; }
            Controls.Add(Choose); Controls.Add(Edit); Controls.Add(Delete); Edit.BringToFront(); Delete.BringToFront();
            Choose.Click += (s, e) => choose(TerrainId); Edit.Click += (s, e) => edit(TerrainId); Delete.Click += (s, e) => delete(TerrainId);
            Choose.KeyDown += (s, e) => { if (e.KeyCode == Keys.F2 || e.KeyCode == Keys.Delete) { e.Handled = true; e.SuppressKeyPress = true; if (e.KeyCode == Keys.F2) edit(TerrainId); else delete(TerrainId); } };
            Disposed += (s, e) => { tips.Dispose(); Choose.Font.Dispose(); if (Choose.FillImage != null) Choose.FillImage.Dispose(); };
        }
        internal void ResizeRow(int width)
        {
            Width = width; Choose.SetBounds(0, 0, width - 56, 32);
            Edit.SetBounds(width - 52, 2, 24, 28); Delete.SetBounds(width - 24, 2, 24, 28);
        }
        public void UpdateTerrain(TerrainDefinition terrain, bool selected, bool flat = false)
        {
            TerrainId = terrain.TerrainId; Choose.Text = terrain.Name; var color = ColorTranslator.FromHtml(terrain.Color); Choose.BackColor = color;
            string key = TerrainMosaic.Key(terrain) + ":" + flat + ":" + Choose.ClientSize;
            if (mosaicKey != key)
            {
                var old = Choose.FillImage;
                Choose.FillImage = null;
                if (old != null) old.Dispose();
                mosaicKey = key;
                Choose.FillImage = TerrainMosaic.Render(terrain, Choose.ClientSize, flat);
                double luminance = color.R * .299 + color.G * .587 + color.B * .114;
                var image = Choose.FillImage as Bitmap;
                if (image != null)
                {
                    double sum = 0;
                    for (int y = 0; y < 4; y++) for (int x = 0; x < 8; x++)
                    {
                        var pixel = image.GetPixel((x*2+1)*image.Width/16, (int)((.42+y*.04)*image.Height));
                        double alpha = pixel.A / 255.0;
                        sum += alpha*(pixel.R*.299+pixel.G*.587+pixel.B*.114)+(1-alpha)*luminance;
                    }
                    luminance = sum / 32;
                }
                Choose.ForeColor = luminance > 145 ? Color.Black : Color.White;
            }
            else { /* Preserve the cached single text color for an unchanged graphic. */ }
            Choose.FlatAppearance.BorderColor = selected ? Color.Gold : Color.Gray; Choose.FlatAppearance.BorderSize = selected ? 3 : 1; Choose.AccessibleName = "Teren: " + terrain.Name; Choose.AccessibleDescription = selected ? "Wybrany" : "Niewybrany";
            Edit.AccessibleName = "Edytuj teren " + terrain.Name; Delete.AccessibleName = "Usuń teren " + terrain.Name;
            tips.SetToolTip(Choose, terrain.Name + " · wersja " + terrain.Revision + " · warianty: " + terrain.Variants.Count); tips.SetToolTip(Edit, "Edytuj teren (F2)"); tips.SetToolTip(Delete, "Usuń teren (Delete)");
            Choose.Invalidate();
        }
    }
}
