using System;
using System.Drawing;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class BackgroundDialog : Form
    {
        MapBackground draft;
        readonly BackgroundCropSurface crop = new BackgroundCropSurface { Bounds = new Rectangle(12, 48, 450, 320) };
        readonly PictureBox preview = new PictureBox { Bounds = new Rectangle(477, 48, 290, 320), SizeMode = PictureBoxSizeMode.Zoom };
        readonly TrackBar opacity = Slider(0, 100, 100), saturation = Slider(0, 200, 100), blur = Slider(0, 24, 0);
        readonly Label values = new Label { Bounds = new Rectangle(15, 453, 620, 25) };
        readonly Button remove = new Button { Text = "Usuń tło", Bounds = new Rectangle(150, 10, 110, 28) };
        bool updating;
        internal Func<bool> CanAccept;
        internal MapBackground Result { get; private set; }
        static TrackBar Slider(int min, int max, int value) { return new TrackBar { Minimum = min, Maximum = max, Value = value, TickStyle = TickStyle.None, Width = 185, Height = 35 }; }
        internal BackgroundDialog(MapBackground initial)
        {
            draft = initial == null ? null : initial.Copy();
            Text = "Tło mapy"; ClientSize = new Size(780, 526); FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent; MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(34, 38, 48); ForeColor = Color.WhiteSmoke; Font = new Font("Segoe UI", 9);
            var import = new Button { Text = "Z pliku…", Bounds = new Rectangle(12, 10, 125, 28) };
            var hint = new Label { Text = "Przeciągaj narożniki kadru lub zaznacz nowy prostokąt.", Bounds = new Rectangle(280, 15, 490, 22) };
            opacity.Location = new Point(12, 403); saturation.Location = new Point(272, 403); blur.Location = new Point(532, 403);
            var accept = new Button { Text = "Zatwierdź", Bounds = new Rectangle(546, 484, 106, 29) };
            var cancel = new Button { Text = "Anuluj", DialogResult = DialogResult.Cancel, Bounds = new Rectangle(660, 484, 106, 29) };
            import.Click += (s, e) => Import();
            remove.Click += (s, e) => { draft = null; UpdateSettings(); };
            crop.Changed = RefreshPreview;
            foreach (var slider in new[] { opacity, saturation, blur }) slider.ValueChanged += (s, e) =>
            {
                if (updating || draft == null) return;
                draft.Opacity = opacity.Value; draft.Saturation = saturation.Value; draft.Blur = blur.Value;
                RefreshPreview();
            };
            accept.Click += (s, e) => { if (CanAccept == null || CanAccept()) { Result = draft == null ? null : draft.Copy(); DialogResult = DialogResult.OK; } };
            Controls.AddRange(new Control[] { crop, preview, import, remove, hint, opacity, saturation, blur, accept, cancel, values,
                new Label { Text = "Krycie", Location = new Point(12, 381), AutoSize = true },
                new Label { Text = "Nasycenie", Location = new Point(272, 381), AutoSize = true },
                new Label { Text = "Rozmycie", Location = new Point(532, 381), AutoSize = true } });
            AcceptButton = accept; CancelButton = cancel; UpdateSettings();
            Disposed += (s, e) => { if (preview.Image != null) preview.Image.Dispose(); };
        }
        void Import()
        {
            using (var file = new OpenFileDialog { Filter = "Grafiki|*.png;*.jpg;*.jpeg;*.bmp;*.gif", RestoreDirectory = true })
            {
                if (file.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    var source = GraphicSources.Import(file.FileName);
                    draft = new MapBackground { Source = source, SourceId = source.Id };
                    UpdateSettings();
                }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "Import tła", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            }
        }
        void UpdateSettings()
        {
            updating = true;
            opacity.Value = draft == null ? 100 : draft.Opacity; saturation.Value = draft == null ? 100 : draft.Saturation; blur.Value = draft == null ? 0 : draft.Blur;
            opacity.Enabled = saturation.Enabled = blur.Enabled = remove.Enabled = draft != null;
            crop.SetContent(draft); updating = false; RefreshPreview();
        }
        void RefreshPreview()
        {
            var old = preview.Image;
            preview.Image = draft == null ? null : BackgroundRenderer.Render(draft, preview.ClientSize);
            if (old != null) old.Dispose();
            // PictureBox previews opacity against the same canvas background.
            preview.BackColor = Color.FromArgb(25, 29, 37);
            if (draft != null && draft.Opacity < 100)
            {
                var rendered = preview.Image;
                var mixed = new Bitmap(rendered.Width, rendered.Height);
                using (var g = Graphics.FromImage(mixed))
                using (var attributes = new System.Drawing.Imaging.ImageAttributes())
                {
                    g.Clear(preview.BackColor);
                    var matrix = new System.Drawing.Imaging.ColorMatrix(); matrix.Matrix33 = draft.Opacity / 100f; attributes.SetColorMatrix(matrix);
                    g.DrawImage(rendered, new Rectangle(0, 0, mixed.Width, mixed.Height), 0, 0, rendered.Width, rendered.Height, GraphicsUnit.Pixel, attributes);
                }
                preview.Image = mixed; rendered.Dispose();
            }
            values.Text = draft == null ? "Brak tła." : String.Format("Krycie {0}%     Nasycenie {1}%     Rozmycie {2} px", draft.Opacity, draft.Saturation, draft.Blur);
        }
    }

    internal sealed class BackgroundCropSurface : Control
    {
        MapBackground settings;
        Bitmap image;
        PointF anchor;
        int corner;
        internal Action Changed;
        internal BackgroundCropSurface() { DoubleBuffered = true; BackColor = Color.FromArgb(23, 27, 34); }
        RectangleF ImageBounds
        {
            get
            {
                if (image == null) return RectangleF.Empty;
                float scale = Math.Min((Width - 12f) / image.Width, (Height - 12f) / image.Height);
                return new RectangleF((Width - image.Width * scale) / 2, (Height - image.Height * scale) / 2, image.Width * scale, image.Height * scale);
            }
        }
        internal void SetContent(MapBackground settings)
        {
            if (image != null) image.Dispose();
            this.settings = settings; image = settings == null ? null : GraphicSources.Decode(settings.Source); Invalidate();
        }
        PointF Relative(Point point)
        {
            var box = ImageBounds;
            return new PointF(Math.Max(0, Math.Min(1, (point.X - box.X) / box.Width)), Math.Max(0, Math.Min(1, (point.Y - box.Y) / box.Height)));
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (image == null || e.Button != MouseButtons.Left || !ImageBounds.Contains(e.Location)) return;
            var box = ImageBounds;
            var corners = new[] { new PointF((float)settings.Left, (float)settings.Top), new PointF((float)(settings.Left + settings.Width), (float)settings.Top),
                new PointF((float)(settings.Left + settings.Width), (float)(settings.Top + settings.Height)), new PointF((float)settings.Left, (float)(settings.Top + settings.Height)) };
            corner = -1;
            for (int i = 0; i < corners.Length; i++)
                if (Math.Abs(e.X - box.X - corners[i].X * box.Width) < 14 && Math.Abs(e.Y - box.Y - corners[i].Y * box.Height) < 14) corner = i;
            anchor = corner < 0 ? Relative(e.Location) : corners[(corner + 2) % 4];
            Capture = true;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!Capture || settings == null) return;
            var point = Relative(e.Location);
            double left = Math.Min(anchor.X, point.X), top = Math.Min(anchor.Y, point.Y);
            double width = Math.Abs((double)point.X - anchor.X), height = Math.Abs((double)point.Y - anchor.Y);
            if (width < .02 || height < .02) return;
            settings.Left = left; settings.Top = top; settings.Width = width; settings.Height = height; Invalidate();
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && Capture) { Capture = false; if (Changed != null) Changed(); }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (image == null) return;
            var box = ImageBounds; e.Graphics.DrawImage(image, box);
            var crop = new RectangleF(box.X + (float)settings.Left * box.Width, box.Y + (float)settings.Top * box.Height,
                (float)settings.Width * box.Width, (float)settings.Height * box.Height);
            using (var region = new Region(box))
            using (var brush = new SolidBrush(Color.FromArgb(160, Color.Black)))
            {
                region.Exclude(crop); e.Graphics.FillRegion(brush, region);
            }
            e.Graphics.DrawRectangle(Pens.Gold, crop.X, crop.Y, crop.Width, crop.Height);
            foreach (float x in new[] { crop.Left, crop.Right }) foreach (float y in new[] { crop.Top, crop.Bottom })
                e.Graphics.FillRectangle(Brushes.Gold, x - 4, y - 4, 8, 8);
        }
        protected override void Dispose(bool disposing) { if (disposing && image != null) image.Dispose(); base.Dispose(disposing); }
    }
}
