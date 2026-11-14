using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class TerrainPreview : Control
    {
        readonly bool flat;
        Bitmap image;
        internal GradientDefinition Gradient;
        internal TerrainPreview(bool flat) { this.flat = flat; DoubleBuffered = true; }
        internal void ShowFill(Color color, GraphicDefinition asset)
        {
            if (image != null) image.Dispose();
            image = asset == null ? null : TerrainTextures.Decode(asset, flat);
            BackColor = color;
            Invalidate();
        }
        protected override void OnPaintBackground(PaintEventArgs e) { e.Graphics.Clear(Parent == null ? SystemColors.Control : Parent.BackColor); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var center = new PointF(Width / 2f, Height / 2f);
            float radius = Math.Max(1, Math.Min(Width, Height) / 2f - 3);
            PaintHex(e.Graphics, center, radius, flat, BackColor, image,true,Gradient);
        }
        internal static void PaintHex(Graphics g, PointF center, float radius, bool flat, Color color, Bitmap image, bool outline = true, GradientDefinition gradient=null)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var mask = TerrainTextures.Mask(center, radius, flat))
            using (var fill = new SolidBrush(color))
            {
                g.FillPath(fill, mask);
                if(gradient!=null)GradientRules.Fill(g,mask,gradient);
                if (image != null)
                    TerrainTextures.Draw(g, mask, image, new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2));
                if (outline) g.DrawPath(Pens.Gray, mask);
            }
        }
        protected override void Dispose(bool disposing) { if (disposing && image != null) image.Dispose(); base.Dispose(disposing); }
    }

    internal sealed class TerrainGraphicPicker : DarkForm
    {
        readonly ListView list = new ListView { Dock = DockStyle.Fill, View = View.LargeIcon, MultiSelect = false, HideSelection = false };
        readonly ImageList images = new ImageList { ImageSize = new Size(88, 72), ColorDepth = ColorDepth.Depth32Bit };
        readonly List<Bitmap> thumbnails = new List<Bitmap>();
        readonly Func<List<TerrainGraphic>> items;
        readonly bool flat;
        [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam);
        internal Func<TerrainGraphic> Import;
        internal Func<TerrainGraphic, TerrainGraphic> Edit;
        internal Action<TerrainGraphic> Delete;
        internal TerrainGraphic Selected { get { return list.SelectedItems.Count == 1 ? (TerrainGraphic)list.SelectedItems[0].Tag : null; } }

        internal TerrainGraphicPicker(Func<List<TerrainGraphic>> items, bool flat, bool manage = false)
        {
            this.items = items; this.flat = flat;
            Text = "Biblioteka grafik terenów";
            var area=Screen.FromControl(this).WorkingArea;
            ClientSize = new Size(Math.Min(4*120+SystemInformation.VerticalScrollBarWidth+8,Math.Max(320,area.Width-50)),Math.Min(450,Math.Max(220,area.Height-80)));
            MinimumSize = new Size(Math.Min(480,Width),Math.Min(300,Height));
            Shown+=(s,e)=>UpdateIconSpacing();list.Resize+=(s,e)=>UpdateIconSpacing();
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9);
            BackColor=Color.FromArgb(34,38,48); ForeColor=Color.WhiteSmoke; list.BackColor=BackColor;list.ForeColor=ForeColor;
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 47, Padding = new Padding(6) };
            var add = new EditorButton { Text = "Importuj…", Width = 95 };
            var edit = new EditorButton { Text = "Edytuj…", Width = 85 };
            var remove = new EditorButton { Text = "Usuń…", Width = 85, Destructive=true };
            var select = new EditorButton { Text = "Wybierz", Width = 85 };
            var cancel = new EditorButton { Text = "Anuluj", Width = 85, DialogResult = DialogResult.Cancel };
            buttons.Controls.AddRange(new Control[] { add, edit, remove, select, cancel });
            foreach(Button b in buttons.Controls){b.FlatStyle=FlatStyle.Flat;b.BackColor=Color.FromArgb(49,54,67);b.ForeColor=ForeColor;}
            select.Visible=!manage; cancel.Text=manage?"Zamknij":"Anuluj";
            Controls.Add(list);
            Controls.Add(buttons);
            list.LargeImageList = images;
            add.Click += (s, e) => Run(() => { var entry = Import(); if (entry != null) RefreshList(entry.Id); });
            edit.Click += (s, e) => Run(() => { if (Selected != null) RefreshList(Edit(Selected).Id); });
            remove.Click += (s, e) => Run(() => { if (Selected != null) { var item = Selected; Delete(item); RefreshList(item.Id); } });
            select.Click += (s, e) => { if (Selected != null) DialogResult = DialogResult.OK; };
            list.DoubleClick += (s, e) => { if(!manage)select.PerformClick(); };
            list.SelectedIndexChanged += (s, e) => edit.Enabled = remove.Enabled = select.Enabled = Selected != null;
            if(!manage)AcceptButton = select;
            CancelButton = cancel;
            RefreshList(null);
            edit.Enabled = remove.Enabled = select.Enabled = false;
        }

        void UpdateIconSpacing()
        {
            if(!list.IsHandleCreated)return;
            int spacing=120;
            SendMessage(list.Handle,0x1035,IntPtr.Zero,new IntPtr(spacing|(114<<16)));
            list.ArrangeIcons(ListViewAlignment.Top);
        }

        void Run(Action action)
        {
            try { action(); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        void RefreshList(string id)
        {
            list.BeginUpdate();
            try
            {
                list.Items.Clear();
                images.Images.Clear();
                foreach (var thumbnail in thumbnails) thumbnail.Dispose();
                thumbnails.Clear();
                foreach (var entry in items())
                {
                    var thumbnail = new Bitmap(88, 72);
                    thumbnails.Add(thumbnail);
                    using (var g = Graphics.FromImage(thumbnail))
                        g.DrawImageUnscaled(TerrainGraphicThumbnails.Get(entry.Asset,flat),0,0);
                    images.Images.Add(thumbnail);
                    var row = list.Items.Add(entry.Name, images.Images.Count - 1);
                    row.Tag = entry;
                    if (entry.Id == id) { row.Selected = true; row.Focused = true; row.EnsureVisible(); }
                }
            }
            finally { list.EndUpdate(); }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { list.LargeImageList = null; images.Dispose(); foreach (var thumbnail in thumbnails) thumbnail.Dispose(); }
            base.Dispose(disposing);
        }
    }

    internal sealed class TerrainGraphicEditDialog : EditorDialog
    {
        readonly TextBox name = new DarkTextBox { MaxLength = 200 };
        internal Func<bool, GraphicDefinition> ChooseImage;
        internal GraphicDefinition Asset { get; private set; }
        internal string GraphicName { get { return name.Text.Trim(); } }

        internal TerrainGraphicEditDialog(TerrainGraphic item, bool flat) : base("Edytuj grafikę terenu", 300)
        {
            name.Text = item.Name;
            Asset = item.Asset;
            var actions = new FlowLayoutPanel { WrapContents = false };
            var replace = new EditorButton { Text = "Z pliku…", Width = 100 };
            var recrop = new EditorButton { Text = "Kadruj wycinek…", Width = 130 };
            actions.Controls.AddRange(new Control[] { replace, recrop });
            var preview = new TerrainPreview(flat) { Dock = DockStyle.Fill, AccessibleName = "Podgląd grafiki terenu" };
            preview.ShowFill(Color.SlateGray, Asset);
            Row(0, "Nazwa", name, true);
            Row(1, "Grafika", actions, true);
            Layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Layout.Controls.Add(preview, 0, 2); Layout.SetColumnSpan(preview, 3);
            replace.Click += (s, e) => Replace(false, preview);
            recrop.Click += (s, e) => Replace(true, preview);
            Buttons(3, () => { if (GraphicName.Length != 0) DialogResult = DialogResult.OK; });
        }

        void Replace(bool recrop, TerrainPreview preview)
        {
            try
            {
                var next = ChooseImage(recrop);
                if (next == null) return;
                Asset = next;
                preview.ShowFill(Color.SlateGray, Asset);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
    }
}
