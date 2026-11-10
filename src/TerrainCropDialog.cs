using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class TerrainCropDialog : Form
    {
        readonly CropSurface surface;
        internal IconAsset Result { get; private set; }

        internal TerrainCropDialog(Bitmap source, string name, bool flat)
        {
            Text = "Kadrowanie grafiki terenu";
            ClientSize = new Size(480, 494);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(34, 38, 48);
            ForeColor = Color.WhiteSmoke;
            Font = new Font("Segoe UI", 9);
            var hint = new Label { Text = "Przeciągnij obraz pod maską. Kółko zmienia przybliżenie.", Location = new Point(16, 14), AutoSize = true };
            var orientation = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(16, 40), Width = 155, AccessibleName = "Orientacja maski" };
            orientation.Items.AddRange(new object[] { "Pointy Top", "Flat Top" });
            orientation.SelectedIndex = flat ? 1 : 0;
            var fill = new Button { Text = "Wypełnij", Bounds = new Rectangle(184, 38, 100, 26) };
            surface = new CropSurface(source, flat) { Bounds = new Rectangle(16, 73, 448, 361), TabStop = true, AccessibleName = "Podgląd kadru heksa" };
            var accept = new Button { Text = "Zatwierdź", Bounds = new Rectangle(250, 449, 104, 30) };
            var cancel = new Button { Text = "Anuluj", Bounds = new Rectangle(360, 449, 104, 30), DialogResult = DialogResult.Cancel };
            orientation.SelectedIndexChanged += (s, e) => surface.ShowFlat(orientation.SelectedIndex == 1);
            fill.Click += (s, e) => surface.ResetCrop();
            accept.Click += (s, e) => { Result = surface.Crop(name); DialogResult = DialogResult.OK; };
            Controls.AddRange(new Control[] { hint, orientation, fill, surface, accept, cancel });
            AcceptButton = accept;
            CancelButton = cancel;
        }
    }

    internal sealed class CropSurface : Control
    {
        const float Side = 340;
        readonly Bitmap source;
        bool flat, dragging;
        Point previous;
        float scale;
        PointF offset;

        internal CropSurface(Bitmap source, bool flat)
        {
            this.source = source;
            this.flat = flat;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(23, 27, 34);
            Cursor = Cursors.SizeAll;
            ResetCrop();
        }

        float MinScale { get { return Side / Math.Min(source.Width, source.Height); } }
        RectangleF Square { get { return new RectangleF((Width - Side) / 2, (Height - Side) / 2, Side, Side); } }
        RectangleF ImageRectangle { get { return new RectangleF(offset.X, offset.Y, source.Width * scale, source.Height * scale); } }

        internal void ShowFlat(bool value) { flat = value; Invalidate(); }
        internal void ResetCrop()
        {
            scale = MinScale;
            offset = new PointF((Side - source.Width * scale) / 2, (Side - source.Height * scale) / 2);
            Clamp();
        }

        void Clamp()
        {
            offset.X = Math.Min(0, Math.Max(Side - source.Width * scale, offset.X));
            offset.Y = Math.Min(0, Math.Max(Side - source.Height * scale, offset.Y));
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Middle) return;
            Focus();
            previous = e.Location;
            dragging = Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!dragging) return;
            offset.X += e.X - previous.X;
            offset.Y += e.Y - previous.Y;
            previous = e.Location;
            Clamp();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle) Capture = false;
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (!Capture) dragging = false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            float next = Math.Max(MinScale, Math.Min(MinScale * 12, scale * (float)Math.Pow(1.13, e.Delta / 120.0)));
            var square = Square;
            float x = e.X - square.X, y = e.Y - square.Y;
            offset = new PointF(x - (x - offset.X) * next / scale, y - (y - offset.Y) * next / scale);
            scale = next;
            Clamp();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if (CanFocus) Focus();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            var square = Square;
            var image = ImageRectangle;
            image.Offset(square.Location);
            var state = g.Save();
            g.SetClip(square, CombineMode.Intersect);
            g.DrawImage(source, image);
            using (var shade = new SolidBrush(Color.FromArgb(180, BackColor))) g.FillRectangle(shade, square);
            g.Restore(state);
            using (var hex = TerrainTextures.Mask(new PointF(square.X + Side / 2, square.Y + Side / 2), Side / 2, flat))
            {
                using (var checker = new HatchBrush(HatchStyle.LargeCheckerBoard, Color.SlateGray, Color.LightSlateGray)) g.FillPath(checker, hex);
                TerrainTextures.Draw(g, hex, source, image);
                using (var pen = new Pen(Color.WhiteSmoke, 1.5f)) g.DrawPath(pen, hex);
            }
        }

        internal IconAsset Crop(string name)
        {
            int size = TerrainTextures.Resolution;
            float factor = size / Side;
            var image = ImageRectangle;
            using (var result = new Bitmap(size, size, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(result))
                using (var attributes = new ImageAttributes())
                {
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    attributes.SetWrapMode(WrapMode.TileFlipXY);
                    var destination = new[] { new PointF(image.Left * factor, image.Top * factor), new PointF(image.Right * factor, image.Top * factor), new PointF(image.Left * factor, image.Bottom * factor) };
                    g.DrawImage(source, destination, new RectangleF(0, 0, source.Width, source.Height), GraphicsUnit.Pixel, attributes);
                }
                return IconAssets.FromBitmap(result, name);
            }
        }
    }
}
