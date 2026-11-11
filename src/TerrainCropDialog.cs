using System;
using System.Drawing;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class TerrainCropDialog : Form
    {
        readonly GraphicDefinition draft;
        readonly CropSurface pointy, flat;
        readonly TrackBar rotation = new TrackBar { Minimum = 0, Maximum = 360, TickFrequency = 45, Width = 330 };
        readonly Label angle = new Label { AutoSize = true };
        bool activeFlat, updating;
        internal GraphicDefinition Result { get; private set; }
        internal TerrainCropDialog(GraphicDefinition initial)
        {
            draft = initial.Copy(); Text = "Kadrowanie grafiki"; ClientSize = new Size(820, 514);
            FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = MinimizeBox = false; BackColor = Color.FromArgb(34, 38, 48); ForeColor = Color.WhiteSmoke; Font = new Font("Segoe UI", 9);
            var hint = new Label { Text = "Przeciągnij obraz pod maską. Kółko zmienia zoom. Kliknięty podgląd wyznacza obrót.", Bounds = new Rectangle(16, 10, 790, 22) };
            var linked = new CheckBox { Text = "Połącz kadry", Checked = draft.Linked, AutoSize = true, Location = new Point(16, 424) };
            var image = GraphicSources.Decode(draft.Source);
            pointy = new CropSurface(draft, image, false) { Bounds = new Rectangle(16, 58, 386, 354) };
            flat = new CropSurface(draft, image, true) { Bounds = new Rectangle(418, 58, 386, 354) };
            Disposed += (s, e) => image.Dispose();
            pointy.Edited = isFlat => RefreshViews(isFlat); flat.Edited = isFlat => RefreshViews(isFlat);
            var reset = new Button { Text = "Obrót 0°", Bounds = new Rectangle(686, 419, 115, 28) };
            rotation.Location = new Point(275, 417); angle.Location = new Point(616, 425);
            rotation.ValueChanged += (s, e) =>
            {
                if (updating) return;
                double current = (activeFlat ? draft.Flat : draft.Pointy).Angle;
                double delta = rotation.Value - current;
                if (delta > 180) delta -= 360;
                if (delta < -180) delta += 360;
                CropMath.Rotate(draft, activeFlat, delta); RefreshViews(activeFlat);
            };
            linked.CheckedChanged += (s, e) => { draft.Linked = linked.Checked; RefreshViews(activeFlat); };
            reset.Click += (s, e) => { CropMath.Rotate(draft, activeFlat, -(activeFlat ? draft.Flat : draft.Pointy).Angle); RefreshViews(activeFlat); };
            var accept = new Button { Text = "Zatwierdź", Bounds = new Rectangle(580, 470, 105, 30) };
            var cancel = new Button { Text = "Anuluj", Bounds = new Rectangle(696, 470, 105, 30), DialogResult = DialogResult.Cancel };
            accept.Click += (s, e) => { draft.Seal(); Result = draft.Copy(); DialogResult = DialogResult.OK; };
            Controls.AddRange(new Control[] { hint, pointy, flat, linked, rotation, angle, reset, accept, cancel,
                new Label { Text = "Pointy Top", Bounds = new Rectangle(16, 35, 200, 22) },
                new Label { Text = "Flat Top", Bounds = new Rectangle(418, 35, 200, 22) } });
            AcceptButton = accept; CancelButton = cancel; RefreshViews(false);
        }
        void RefreshViews(bool isFlat)
        {
            activeFlat = isFlat; pointy.Active = !isFlat; flat.Active = isFlat; pointy.Invalidate(); flat.Invalidate();
            updating = true; double value = (isFlat ? draft.Flat : draft.Pointy).Angle;
            rotation.Value = Math.Min(359, (int)Math.Round(value)); angle.Text = value.ToString("0") + "°"; updating = false;
        }
    }
    internal sealed class CropSurface : Control
    {
        readonly GraphicDefinition graphic;
        readonly Bitmap image;
        readonly bool flat;
        bool dragging;
        Point previous;
        internal Action<bool> Edited;
        internal bool Active;
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
            double factor = Side / Math.Min(image.Width, image.Height) * crop.Zoom;
            double dx = (e.X - previous.X) / factor, dy = (e.Y - previous.Y) / factor, radians = crop.Angle * Math.PI / 180;
            CropMath.Pan(graphic, flat, -(dx * Math.Cos(radians) + dy * Math.Sin(radians)) / image.Width,
                -(-dx * Math.Sin(radians) + dy * Math.Cos(radians)) / image.Height);
            previous = e.Location; Edited(flat);
        }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle) Capture = false; }
        protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e); if (!Capture) dragging = false; }
        protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); CropMath.Zoom(graphic, flat, Math.Pow(1.13, e.Delta / 120.0)); Edited(flat); }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); if (CanFocus) Focus(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var square = Square; var g = e.Graphics; CropMath.Draw(g, image, flat ? graphic.Flat : graphic.Pointy, square);
            using (var region = new Region(square))
            using (var mask = TerrainTextures.Mask(new PointF(Width / 2f, Height / 2f), Side / 2, flat))
            using (var shade = new SolidBrush(Color.FromArgb(180, BackColor)))
            using (var pen = new Pen(Active ? Color.Gold : Color.WhiteSmoke, Active ? 2 : 1))
            { region.Exclude(mask); g.FillRegion(shade, region); g.DrawPath(pen, mask); }
        }
    }
}
