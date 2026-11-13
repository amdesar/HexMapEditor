using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class EditorColorDialog : DarkForm
    {
        readonly ColorHexBox hex = new ColorHexBox { MaxLength = 32, AccessibleName = "Kolor HEX" };
        readonly Label error = new Label { Text = "Wpisz # i dokładnie sześć cyfr szesnastkowych.", AutoSize = true, ForeColor = Color.Salmon };
        readonly Panel next = new Panel();
        readonly Button[] recent=new Button[12];
        readonly ToolTip recentTips=new ToolTip();
        readonly Button accept = new Button { Text = "Zatwierdź" };
        readonly HsvSurface saturation;
        readonly HsvSurface hue;
        readonly ScrubNumber[] rgb={new ScrubNumber(),new ScrubNumber(),new ScrubNumber()};
        internal Action<Color> Changed;
        internal Action<bool> ValidityChanged;
        internal bool ColorValid {get{return MapLogic.ValidColor(hex.Text);}}
        internal void SetColor(Color color){ReadColor(color);RefreshColor(true);}
        internal void Embed(){TopLevel=false;FormBorderStyle=FormBorderStyle.None;foreach(Control c in Controls)if(c is Button && !recent.Contains((Button)c))c.Visible=false;}
        double h, s, v;
        bool updating;
        bool hexEditing;
        ColorDraftHistory history;
        internal Action EditStarted,EditCompleted;
        internal Func<bool,bool> ExternalHistory;
        internal bool NativeColorText{get{return hexEditing&&hex.ContainsFocus||rgb.Any(n=>n.IsEditing&&n.Controls[0].ContainsFocus);}}
        void BeginColorEdit(){if(EditStarted!=null)EditStarted();}
        void EndColorEdit(){if(EditCompleted!=null)EditCompleted();else if(history!=null)history.Commit();}
        void FinishHex(){if(!hexEditing)return;hexEditing=false;EndColorEdit();}
        internal void FinishTextEdit(){foreach(var number in rgb)number.CommitText();FinishHex();}
        internal bool MoveColorHistory(bool redo){return ExternalHistory!=null?ExternalHistory(redo):history.Move(redo);}
        protected override void OnDeactivate(EventArgs e){FinishTextEdit();base.OnDeactivate(e);}
        protected override bool ProcessCmdKey(ref Message msg,Keys keys){if(keys==(Keys.Control|Keys.Shift|Keys.Z))return true;if(keys==(Keys.Control|Keys.Z)||keys==(Keys.Control|Keys.Y)){if(NativeColorText)return base.ProcessCmdKey(ref msg,keys);MoveColorHistory(keys==(Keys.Control|Keys.Y));return true;}return base.ProcessCmdKey(ref msg,keys);}

        internal Color SelectedColor { get; private set; }
        internal string Hex { get { return Format(SelectedColor); } }
        static string Format(Color color) { return String.Format("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B); }

        internal EditorColorDialog(Color initial)
        {
            Text = "Wybierz kolor";
            ClientSize = new Size(414, 450);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(34, 38, 48);
            ForeColor = Color.WhiteSmoke;
            Font = new Font("Segoe UI", 9);
            saturation = new HsvSurface(false, (x, y) => { s = x; v = 1 - y; RefreshColor(true); }) { Bounds = new Rectangle(16, 16, 326, 195), AccessibleName = "Nasycenie i jasność" };
            hue = new HsvSurface(true, (x, y) => { h = y * 359.99; RefreshColor(true); }) { Bounds = new Rectangle(354, 16, 44, 195), AccessibleName = "Odcień" };
            foreach(var surface in new[]{saturation,hue}){surface.Started=()=>{BeginColorEdit();};surface.Completed=()=>{EndColorEdit();};}
            hex.SetBounds(55, 225, 108, 25);
            hex.BackColor = Color.FromArgb(48, 53, 65);
            hex.ForeColor = Color.WhiteSmoke;
            var old = new Panel { BackColor = initial, Bounds = new Rectangle(193, 224, 93, 29), BorderStyle = BorderStyle.FixedSingle };
            next.SetBounds(303, 224, 95, 29);
            next.BorderStyle = BorderStyle.FixedSingle;
            var cancel = new Button { Text = "Anuluj", DialogResult = DialogResult.Cancel, Bounds = new Rectangle(302, 410, 96, 29) };
            accept.SetBounds(196, 410, 96, 29);
            foreach (var button in new[] { accept, cancel }) { button.FlatStyle = FlatStyle.Flat; button.BackColor = hex.BackColor; }
            error.Location = new Point(16, 382);
            for(int i=0;i<3;i++){var n=rgb[i];n.Minimum=0;n.Maximum=255;n.DecimalPlaces=0;n.SetBounds(40,282+i*30,74,26);n.TabIndex=3+i;n.AccessibleName="RGB "+"RGB"[i];Controls.Add(n);Controls.Add(LabelAt("RGB"[i]+":",16,286+i*30));n.ValueChanged+=(a,b)=>{if(updating)return;ReadColor(Color.FromArgb((int)rgb[0].Value,(int)rgb[1].Value,(int)rgb[2].Value));RefreshColor(true);};}
            hex.TabIndex=2;accept.TabIndex=6;cancel.TabIndex=7;
            Controls.AddRange(new Control[] { saturation, hue, hex, old, next, error, accept, cancel,
                LabelAt("HEX", 16, 228), LabelAt("Poprzedni", 193, 257), LabelAt("Nowy", 303, 257) });
            for(int i=0;i<12;i++){int at=i;var b=recent[i]=new Button{Bounds=new Rectangle(146+(i%6)*42,289+(i/6)*38,36,30),FlatStyle=FlatStyle.Flat,TabIndex=8+i};b.Click+=(s,e)=>{var colors=RecentColors.Values;if(at<colors.Length){BeginColorEdit();SetColor(colors[at]);EndColorEdit();}};Controls.Add(b);}
            RecentColors.Changed+=RefreshRecent;Disposed+=(s,e)=>{RecentColors.Changed-=RefreshRecent;recentTips.Dispose();};RefreshRecent();
            Controls.Add(LabelAt("Kolory sesji (ostatnie 12)",146,272));
            AcceptButton = accept;
            CancelButton = cancel;
            accept.Click += (sender, e) => { if (MapLogic.ValidColor(hex.Text)){RecentColors.Add(SelectedColor);DialogResult = DialogResult.OK;} };
            hex.TextChanged += (sender, e) =>
            {
                if (updating) return;
                if(!hexEditing){hexEditing=true;BeginColorEdit();}
                accept.Enabled = MapLogic.ValidColor(hex.Text);
                error.Visible = !accept.Enabled;
                if(ValidityChanged!=null)ValidityChanged(accept.Enabled);
                if (accept.Enabled) { ReadColor(ColorTranslator.FromHtml(hex.Text)); RefreshColor(false); }
            };
            ReadColor(initial);
            RefreshColor(true);
            history=new ColorDraftHistory(()=>Hex,value=>SetColor(ColorTranslator.FromHtml(value)));
            foreach(var number in rgb){number.EditStarting+=(a,b)=>BeginColorEdit();number.EditFinished+=(a,b)=>EndColorEdit();}
            hex.Enter+=(a,b)=>{if(!hexEditing){hexEditing=true;BeginColorEdit();}};hex.LostFocus+=(a,b)=>FinishHex();hex.Commit=FinishHex;
            Action<Control> outside=target=>{if(!NumberInputRouter.Within(target,hex))FinishHex();};NumberInputRouter.PointerDown+=outside;Disposed+=(a,b)=>NumberInputRouter.PointerDown-=outside;
            ActiveControl = hex;
        }

        void RefreshRecent(){var colors=RecentColors.Values;for(int i=0;i<12;i++){var b=recent[i];if(b==null)continue;b.Enabled=i<colors.Length;b.BackColor=b.Enabled?colors[i]:Color.FromArgb(48,53,65);b.AccessibleName=b.Enabled?Format(colors[i]):"Puste miejsce historii kolorów";recentTips.SetToolTip(b,b.Enabled?Format(colors[i]):"Brak koloru w tej sesji");}}

        static Label LabelAt(string text, int x, int y) { return new Label { Text = text, Location = new Point(x, y), AutoSize = true }; }

        void ReadColor(Color color)
        {
            double max = Math.Max(color.R, Math.Max(color.G, color.B));
            double min = Math.Min(color.R, Math.Min(color.G, color.B));
            if (max > min) h = color.GetHue();
            s = max == 0 ? 0 : (max - min) / max;
            v = max / 255;
        }

        void RefreshColor(bool writeHex)
        {
            SelectedColor = Hsv(h, s, v);
            saturation.SetPosition(h, s, 1 - v);
            hue.SetPosition(0, .5, h / 360);
            next.BackColor = SelectedColor;
            if (writeHex)
            {
                updating = true;
                hex.Text = Hex;
                updating = false;
            }
            updating=true;rgb[0].Value=SelectedColor.R;rgb[1].Value=SelectedColor.G;rgb[2].Value=SelectedColor.B;updating=false;
            if(Changed!=null)Changed(SelectedColor);
            if(ValidityChanged!=null)ValidityChanged(true);
            accept.Enabled = true;
            error.Visible = false;
        }

        internal static Color Hsv(double h, double s, double v)
        {
            double chroma = v * s;
            double x = chroma * (1 - Math.Abs(h / 60 % 2 - 1));
            double m = v - chroma;
            double r, g, b;
            switch ((int)(h / 60))
            {
                case 0: r = chroma; g = x; b = 0; break;
                case 1: r = x; g = chroma; b = 0; break;
                case 2: r = 0; g = chroma; b = x; break;
                case 3: r = 0; g = x; b = chroma; break;
                case 4: r = x; g = 0; b = chroma; break;
                default: r = chroma; g = 0; b = x; break;
            }
            return Color.FromArgb((int)Math.Round((r + m) * 255), (int)Math.Round((g + m) * 255), (int)Math.Round((b + m) * 255));
        }

        sealed class HsvSurface : Control
        {
            readonly bool hueOnly;
            readonly Action<double, double> choose;
            Bitmap gradient;
            double hue = -1, x, y;
            internal Action Started,Completed;bool gesture;

            internal HsvSurface(bool hueOnly, Action<double, double> choose)
            {
                this.hueOnly = hueOnly;
                this.choose = choose;
                DoubleBuffered = true;
                Cursor = Cursors.Cross;
            }

            internal void SetPosition(double newHue, double x, double y)
            {
                if (hue != newHue && gradient != null) { gradient.Dispose(); gradient = null; }
                hue = newHue; this.x = x; this.y = y;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                if (gradient == null && Width > 1 && Height > 1)
                {
                    gradient = new Bitmap(Width, Height);
                    for (int row = 0; row < Height; row++)
                    for (int col = 0; col < Width; col++)
                    {
                        double ratio = (double)row / (Height - 1);
                        gradient.SetPixel(col, row, hueOnly ? Hsv(ratio * 359.99, 1, 1) : Hsv(hue, (double)col / (Width - 1), 1 - ratio));
                    }
                }
                if (gradient != null) e.Graphics.DrawImageUnscaled(gradient, 0, 0);
                float px = (float)(x * (Width - 1)), py = (float)(y * (Height - 1));
                var mark = hueOnly ? new RectangleF(0, py - 3, Width - 1, 6) : new RectangleF(px - 5, py - 5, 10, 10);
                e.Graphics.DrawRectangle(Pens.Black, mark.X, mark.Y, mark.Width, mark.Height);
                mark.Inflate(-1, -1);
                e.Graphics.DrawRectangle(Pens.White, mark.X, mark.Y, mark.Width, mark.Height);
            }

            void Pick(Point p) { choose(Math.Max(0, Math.Min(1, (double)p.X / Math.Max(1, Width - 1))), Math.Max(0, Math.Min(1, (double)p.Y / Math.Max(1, Height - 1)))); }
            protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { gesture=true;if(Started!=null)Started();Capture = true; Pick(e.Location); } }
            protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (Capture) Pick(e.Location); }
            protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); if (e.Button == MouseButtons.Left) {Capture = false;FinishGesture();} }
            void FinishGesture(){if(!gesture)return;gesture=false;if(Completed!=null)Completed();}
            protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)FinishGesture();}
            protected override void Dispose(bool disposing) { if (disposing && gradient != null) gradient.Dispose(); base.Dispose(disposing); }
        }
    }
}
