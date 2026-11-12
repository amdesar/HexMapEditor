using System;
using System.Drawing;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class BackgroundDialog : Form
    {
        MapBackground draft;
        readonly BackgroundCropSurface crop = new BackgroundCropSurface { Bounds = new Rectangle(12, 58, 365, 310) };
        readonly CanvasAspectPreview preview = new CanvasAspectPreview { Bounds = new Rectangle(403, 58, 365, 310) };
        readonly TrackBar opacity = Slider(0, 100, 100), saturation = Slider(0, 200, 100), blur = Slider(0, 24, 0);
        readonly Label values = new Label { Bounds = new Rectangle(15, 453, 620, 25) };
        readonly Button remove = new Button { Text = "Usuń tło", Bounds = new Rectangle(286, 10, 110, 28) };
        bool updating;
        readonly Size viewport;
        Bitmap previewSource, filtered, covered;
        string filteredKey, coveredKey;
        bool previewQueued, adjusting;
        internal bool LibraryOnly {set{remove.Visible=!value;}}
        internal Func<bool> CanAccept;
        internal Func<BackgroundEntry> ChooseLibrary;
        internal Func<MapBackground,string,bool> SaveAndApply;
        internal MapBackground Initial {get;private set;}
        internal MapBackground Result { get; private set; }
        static TrackBar Slider(int min, int max, int value) { return new TrackBar { Minimum = min, Maximum = max, Value = value, TickStyle = TickStyle.None, Width = 185, Height = 35 }; }
        internal BackgroundDialog(MapBackground initial, Size viewport)
        {
            Initial=initial==null?null:initial.Copy();
            this.viewport = new Size(Math.Max(1,viewport.Width),Math.Max(1,viewport.Height));
            preview.Aspect = this.viewport;
            draft = initial == null ? null : initial.Copy();
            Text = "Tło mapy"; ClientSize = new Size(780, 526); FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent; MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(34, 38, 48); ForeColor = Color.WhiteSmoke; Font = new Font("Segoe UI", 9);
            var import = new Button { Text = "Z pliku…", Bounds = new Rectangle(149, 10, 125, 28) };
            var library = new Button { Text = "Z biblioteki…", Bounds = new Rectangle(12,10,125,28) };
            remove.FlatStyle=FlatStyle.Flat;remove.ForeColor=Color.FromArgb(244,113,113);remove.FlatAppearance.BorderColor=remove.ForeColor;
            remove.FlatAppearance.MouseOverBackColor=Color.FromArgb(80,42,48);remove.FlatAppearance.MouseDownBackColor=Color.FromArgb(100,44,52);
            library.Click+=(s,e)=>{if(ChooseLibrary==null)return;var chosen=ChooseLibrary();if(chosen==null)return;draft=chosen.Settings.Copy();draft.Source=draft.Source.Pin();draft.LibraryId=chosen.Id;UpdateSettings();};
            var hint = new Label { Text = "LPM: kadr/uchwyty. MMB: przesuń cały kadr.", Bounds = new Rectangle(407,15,365,22) };
            opacity.Location = new Point(12, 403); saturation.Location = new Point(272, 403); blur.Location = new Point(532, 403);
            var accept = new Button { Text = "Zatwierdź", Bounds = new Rectangle(546, 484, 106, 29) };
            var cancel = new Button { Text = "Anuluj", DialogResult = DialogResult.Cancel, Bounds = new Rectangle(660, 484, 106, 29) };
            import.Click += (s, e) => Import();
            remove.Click += (s, e) => { draft = null; UpdateSettings(); };
            crop.Changed = QueuePreview;
            foreach (var slider in new[] { opacity, saturation, blur }) slider.ValueChanged += (s, e) =>
            {
                if (updating || draft == null) return;
                draft.Opacity = opacity.Value; draft.Saturation = saturation.Value; draft.Blur = blur.Value;
                QueuePreview();
            };
            foreach (var slider in new[] { opacity, saturation, blur })
            {
                slider.MouseDown += (s,e) => adjusting=true;
                slider.MouseUp += (s,e) => FinishPreview();
                slider.KeyDown += (s,e) => adjusting=true;
                slider.KeyUp += (s,e) => FinishPreview();
                slider.MouseCaptureChanged += (s,e) => { if (!slider.Capture && Control.MouseButtons==MouseButtons.None) FinishPreview(); };
            }
            accept.Click += (s, e) => { if (CanAccept != null && !CanAccept())return;var result=draft==null?null:draft.Copy();if(SaveAndApply!=null&&!SaveAndApply(result,draft==null?null:draft.LibraryId))return;Result=result;DialogResult=DialogResult.OK; };
            var reset = new Button { Text = "100%", Bounds = new Rectangle(462,403,58,26), AccessibleName = "Przywróć nasycenie 100%" };
            reset.Click += (s,e) => saturation.Value = 100;
            Controls.AddRange(new Control[] { crop, preview, import, remove, library, hint, opacity, saturation, blur, accept, cancel, values, reset,
                new Label { Text = "Podgląd na płótnie", Bounds = new Rectangle(403,35,365,22) },
                new Label { Text = "Krycie", Location = new Point(12, 381), AutoSize = true },
                new Label { Text = "Nasycenie", Location = new Point(272, 381), AutoSize = true },
                new Label { Text = "Rozmycie", Location = new Point(532, 381), AutoSize = true } });
            AcceptButton = accept; CancelButton = cancel; UpdateSettings();
            Disposed += (s, e) => { if (preview.Image != null) preview.Image.Dispose(); if (previewSource != null) previewSource.Dispose(); if (filtered != null) filtered.Dispose(); if (covered != null) covered.Dispose(); };
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
            if (previewSource != null) previewSource.Dispose();
            previewSource = draft == null ? null : GraphicSources.Preview(draft.Source, GraphicLimits.CropPreviewSide);
            if (filtered != null) filtered.Dispose(); filtered = null; filteredKey = null;
            if (covered != null) covered.Dispose(); covered = null; coveredKey = null;
            crop.SetContent(draft); updating = false; RefreshPreview();
        }
        void QueuePreview()
        {
            if (previewQueued || IsDisposed || Disposing) return;
            if (!IsHandleCreated) { RefreshPreview(); return; }
            previewQueued = true;
            BeginInvoke((Action)(() => { previewQueued = false; if (!IsDisposed && !Disposing) RefreshPreview(); }));
        }
        void FinishPreview() { if (!adjusting) return; adjusting=false; QueuePreview(); }
        void RefreshPreview()
        {
            var old = preview.Image;
            if (draft != null)
            {
                var geometry = MapLogic.Serializer().Serialize(new { draft.SourceId,draft.Left,draft.Top,draft.Width,draft.Height });
                var key = geometry + ":" + draft.Saturation + ":" + draft.Blur + ":" + adjusting;
                if (key != filteredKey)
                {
                    Bitmap next;
                    if (adjusting)
                    {
                        if (coveredKey != geometry)
                        {
                            var plain=draft.Copy(); plain.Saturation=100; plain.Blur=0;
                            var cover=BackgroundRenderer.Render(plain,viewport,previewSource,160);
                            if(covered!=null)covered.Dispose(); covered=cover; coveredKey=geometry;
                        }
                        next=BackgroundRenderer.PreviewEffects(covered,draft.Saturation,draft.Blur,Math.Min(1,160f/Math.Max(viewport.Width,viewport.Height)));
                    }
                    else next = BackgroundRenderer.Render(draft, viewport, previewSource, Math.Min(preview.Width,preview.Height));
                    if (filtered != null) filtered.Dispose(); filtered = next; filteredKey = key;
                }
                preview.Image = (Bitmap)filtered.Clone();
            }
            else preview.Image = null;
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

    internal sealed class CanvasAspectPreview : PictureBox
    {
        internal Size Aspect;
        internal Rectangle Destination
        {
            get { double factor=Math.Min((double)ClientSize.Width/Math.Max(1,Aspect.Width),(double)ClientSize.Height/Math.Max(1,Aspect.Height));
                var size=new Size((int)Math.Round(Aspect.Width*factor),(int)Math.Round(Aspect.Height*factor));
                return new Rectangle((ClientSize.Width-size.Width)/2,(ClientSize.Height-size.Height)/2,size.Width,size.Height); }
        }
        protected override void OnPaint(PaintEventArgs e) { if(Image!=null)e.Graphics.DrawImage(Image,Destination); }
    }

    internal sealed class BackgroundCropSurface : Control
    {
        MapBackground settings;
        Bitmap image;
        PointF anchor;
        int handle;bool moving;
        MapBackground original;
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
            this.settings = settings; image = settings == null ? null : GraphicSources.Preview(settings.Source, GraphicLimits.CropPreviewSide); Invalidate();
        }
        PointF Relative(Point point)
        {
            var box = ImageBounds;
            return new PointF(Math.Max(0, Math.Min(1, (point.X - box.X) / box.Width)), Math.Max(0, Math.Min(1, (point.Y - box.Y) / box.Height)));
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (image == null || (e.Button != MouseButtons.Left && e.Button != MouseButtons.Middle) || !RectangleF.Inflate(ImageBounds,10,10).Contains(e.Location)) return;
            moving=e.Button==MouseButtons.Middle;handle = moving?-1:HitHandle(e.Location);
            original = settings.Copy();
            var corners = Handles();
            anchor = handle < 0 ? Relative(e.Location) : corners[(handle + 2) % 4];
            if(moving)Cursor=Cursors.SizeAll;
            Capture = true;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!Capture || settings == null) { int hit = image == null ? -1 : HitHandle(e.Location); Cursor = hit==0 || hit==2 ? Cursors.SizeNWSE : hit==1 || hit==3 ? Cursors.SizeNESW : hit==4 || hit==6 ? Cursors.SizeNS : hit==5 || hit==7 ? Cursors.SizeWE : Cursors.Cross; return; }
            var point = Relative(e.Location);
            if(moving)
            {
                settings.Left=Math.Max(0,Math.Min(1-original.Width,original.Left+point.X-anchor.X));
                settings.Top=Math.Max(0,Math.Min(1-original.Height,original.Top+point.Y-anchor.Y));
                Invalidate();return;
            }
            double left = Math.Min(anchor.X, point.X), top = Math.Min(anchor.Y, point.Y);
            double width = Math.Abs((double)point.X - anchor.X), height = Math.Abs((double)point.Y - anchor.Y);
            if (handle >= 4)
            {
                left=original.Left; top=original.Top; double right=original.Left+original.Width,bottom=original.Top+original.Height;
                if(handle==4)top=Math.Min(point.Y,bottom-.02);
                if(handle==5)right=Math.Max(point.X,left+.02);
                if(handle==6)bottom=Math.Max(point.Y,top+.02);
                if(handle==7)left=Math.Min(point.X,right-.02);
                width=right-left;height=bottom-top;
            }
            if (width < .02 || height < .02) return;
            settings.Left = left; settings.Top = top; settings.Width = width; settings.Height = height; Invalidate();
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if ((e.Button == MouseButtons.Left || e.Button==MouseButtons.Middle) && Capture) { Capture = false; }
        }
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture){moving=false;Cursor=Cursors.Cross;if(Changed!=null)Changed();}}
        PointF[] Handles()
        {
            float l=(float)settings.Left,t=(float)settings.Top,r=(float)(settings.Left+settings.Width),b=(float)(settings.Top+settings.Height);
            return new[]{new PointF(l,t),new PointF(r,t),new PointF(r,b),new PointF(l,b),new PointF((l+r)/2,t),new PointF(r,(t+b)/2),new PointF((l+r)/2,b),new PointF(l,(t+b)/2)};
        }
        int HitHandle(Point point)
        {
            var box=ImageBounds;var points=Handles();
            for(int i=0;i<points.Length;i++)if(Math.Abs(point.X-box.X-points[i].X*box.Width)<=10 && Math.Abs(point.Y-box.Y-points[i].Y*box.Height)<=10)return i;
            return -1;
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
            foreach (var point in Handles()) e.Graphics.FillRectangle(Brushes.Gold, box.X+point.X*box.Width-4,box.Y+point.Y*box.Height-4,8,8);
        }
        protected override void Dispose(bool disposing) { if (disposing && image != null) image.Dispose(); base.Dispose(disposing); }
    }
}
