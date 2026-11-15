using System;
using System.Drawing;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class TerrainCropDialog : DarkForm
    {
        readonly GraphicDefinition draft;
        readonly CropSurface surface;
        readonly ComboBox orientation = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        readonly TrackBar rotation = new TrackBar { Minimum = 0, Maximum = 360, TickFrequency = 45, Width = 390 };
        readonly Label angle = new Label { AutoSize = true };
        bool activeFlat, updating, initialSync;
        internal GraphicDefinition Result { get; private set; }
        internal TerrainCropDialog(GraphicDefinition initial, bool flat = false, bool newImport = false)
        {
            draft = initial.Copy(); initialSync = newImport; draft.Linked = initialSync; activeFlat = flat;
            Text = "Kadrowanie grafiki"; ClientSize = new Size(640, 590);
            FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = MinimizeBox = false; BackColor = Color.FromArgb(34,38,48); ForeColor = Color.WhiteSmoke; Font = new Font("Segoe UI",9);
            var hint = new Label { Text = "Przeciągaj obraz pod maską. Kółko: zoom. Suwak: obrót.", Bounds = new Rectangle(16,10,605,24) };
            orientation.Items.AddRange(new object[] { "Pointy Top", "Flat Top" }); orientation.Bounds = new Rectangle(100,36,170,26); orientation.SelectedIndex = flat ? 1 : 0;
            var image = GraphicSources.Preview(draft.Source, GraphicLimits.CropPreviewSide);
            surface = new CropSurface(draft,image,flat) { Bounds = new Rectangle(16,70,608,408), Active = true };
            surface.Edited = ignored => RefreshView();
            Disposed += (s,e) => image.Dispose();
            orientation.SelectedIndexChanged += (s,e) => SwitchOrientation(orientation.SelectedIndex == 1);
            rotation.Location = new Point(16,490); angle.Location = new Point(417,498);
            rotation.ValueChanged += (s,e) => {
                if (updating) return;
                double current = (activeFlat ? draft.Flat : draft.Pointy).Angle, delta = rotation.Value-current;
                if(delta>180)delta-=360; if(delta< -180)delta+=360;
                CropMath.Rotate(draft,activeFlat,delta); RefreshView();
            };
            var reset = new Button { Text = "Obrót 0°", Bounds = new Rectangle(512,488,110,28) };
            reset.Click += (s,e) => { CropMath.Rotate(draft,activeFlat,-(activeFlat ? draft.Flat : draft.Pointy).Angle); RefreshView(); };
            var accept = new Button { Text = "Zatwierdź", Bounds = new Rectangle(400,546,105,30) };
            var cancel = new Button { Text = "Anuluj", Bounds = new Rectangle(516,546,105,30), DialogResult = DialogResult.Cancel };
            accept.Click += (s,e) => {
                // Legacy Linked metadata must not create an invisible recrop/history step on OK.
                bool unchanged = !newImport && MapLogic.Serializer().Serialize(new[]{draft.Pointy,draft.Flat}) == MapLogic.Serializer().Serialize(new[]{initial.Pointy,initial.Flat});
                draft.Linked=false; draft.Seal(); Result=unchanged ? initial.Copy() : draft.Copy(); DialogResult=DialogResult.OK;
            };
            Controls.AddRange(new Control[] { hint, new Label { Text="Orientacja",Bounds=new Rectangle(16,39,80,22) },orientation,surface,rotation,angle,reset,accept,cancel });
            var tips = new ToolTip(); tips.SetToolTip(orientation,newImport ? "Nowy import: do pierwszego przełączenia oba kadry otrzymują wspólne zmiany. Potem są niezależne." : "Kadry są niezależne od początku. Przełączenie nie zmienia orientacji mapy.");
            Disposed += (s,e) => tips.Dispose();
            AcceptButton=accept; CancelButton=cancel; RefreshView();
        }
        internal void SwitchOrientation(bool flat)
        {
            if (activeFlat != flat) { initialSync=false; draft.Linked=false; activeFlat=flat; surface.Flat=flat; }
            RefreshView();
        }
        void RefreshView()
        {
            surface.Invalidate(); updating=true;
            double value=(activeFlat ? draft.Flat : draft.Pointy).Angle;
            rotation.Value=Math.Min(359,(int)System.Math.Round(value)); angle.Text=value.ToString("0")+"°"; updating=false;
        }
    }
    internal sealed class CropSurface : Control
    {
        readonly GraphicDefinition graphic;
        readonly Bitmap image;
        bool flat;
        bool dragging;
        Point previous;
        internal Action<bool> Edited;
        internal bool Active;
        internal bool Flat { get { return flat; } set { flat=value; AccessibleName=flat ? "Kadr Flat Top" : "Kadr Pointy Top"; Invalidate(); } }
        float Side { get { return Math.Max(1, Math.Min(Width, Height) - 12); } }
        RectangleF Square { get { return new RectangleF((Width - Side) / 2, (Height - Side) / 2, Side, Side); } }
        internal CropSurface(GraphicDefinition graphic, Bitmap image, bool flat)
        {
            this.graphic = graphic; this.image = image; this.flat = flat;
            DoubleBuffered = true; BackColor = Color.FromArgb(23, 27, 34); Cursor = Cursors.SizeAll; TabStop = true;
            AccessibleName = flat ? "Kadr Flat Top" : "Kadr Pointy Top";
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Middle) return;
            Focus(); previous = e.Location; dragging = Capture = true; Edited(flat);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e); if (!dragging) return;
            var crop = flat ? graphic.Flat : graphic.Pointy;
            double factor = Side / Math.Min(graphic.Source.Width, graphic.Source.Height) * crop.Zoom;
            double dx = (e.X - previous.X) / factor, dy = (e.Y - previous.Y) / factor, radians = crop.Angle * Math.PI / 180;
            CropMath.Pan(graphic, flat, -(dx * Math.Cos(radians) + dy * Math.Sin(radians)) / graphic.Source.Width,
                -(-dx * Math.Sin(radians) + dy * Math.Cos(radians)) / graphic.Source.Height);
            previous = e.Location; Edited(flat);
        }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle) Capture = false; }
        protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e); if (!Capture) dragging = false; }
        protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); CropMath.Zoom(graphic, flat, Math.Pow(1.13, e.Delta / 120.0)); Edited(flat); }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); if (CanFocus) Focus(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var square = Square; var g = e.Graphics; CropPreview.Checker(g,ClientRectangle);CropPreview.Surroundings(g,image,flat?graphic.Flat:graphic.Pointy,square,ClientRectangle);
            using (var region = new Region(ClientRectangle))
            using (var mask = TerrainTextures.Mask(new PointF(Width / 2f, Height / 2f), Side / 2, flat))
            using (var shade = new SolidBrush(Color.FromArgb(180, BackColor)))
            using (var pen = new Pen(Active ? ChoiceStyle.ActiveBorder : Color.WhiteSmoke, Active ? 2 : 1))
            { region.Exclude(mask); g.FillRegion(shade, region); g.DrawPath(pen, mask); }
        }
    }
}
