using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class PaintPickerDialog : Form
    {
        readonly ComboBox mode=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList};
        readonly EditorColorDialog color;
        readonly GradientBar bar=new GradientBar();
        readonly GradientSwatch old=new GradientSwatch(),preview=new GradientSwatch();
        readonly TrackBar angle=new TrackBar{Minimum=0,Maximum=360,TickStyle=TickStyle.None};
        readonly Label angleValue=new Label{AutoSize=true};
        readonly Button remove=new Button{Text="Usuń punkt"},accept=new Button{Text="Zatwierdź"};
        GradientDefinition draft;
        string uniform;
        bool updating;
        readonly bool cyclic;
        internal string Hex {get;private set;}
        internal GradientDefinition Gradient {get;private set;}
        internal PaintPickerDialog(Color initial,GradientDefinition gradient,bool cyclic,bool flat=false,bool startGradient=false)
        {
            this.cyclic=cyclic;uniform=ColorTranslator.ToHtml(Color.FromArgb(initial.ToArgb()));uniform=String.Format("#{0:X2}{1:X2}{2:X2}",initial.R,initial.G,initial.B);
            draft=gradient==null?GradientRules.Two(uniform):gradient.Copy();GradientRules.Validate(draft);
            Text=cyclic?"Kolor kreski / wskaźnika":"Kolor wariantu heksa";ClientSize=new Size(760,525);FormBorderStyle=FormBorderStyle.FixedDialog;StartPosition=FormStartPosition.CenterParent;MaximizeBox=MinimizeBox=false;BackColor=Color.FromArgb(34,38,48);ForeColor=Color.WhiteSmoke;Font=new Font("Segoe UI",9);
            mode.Items.AddRange(new object[]{"Jednolity","Gradient"});mode.Bounds=new Rectangle(12,12,215,28);Controls.Add(mode);
            color=new EditorColorDialog(initial);color.Embed();color.Location=new Point(0,48);Controls.Add(color);color.Show();
            old.Color=initial;old.Gradient=gradient==null?null:gradient.Copy();old.Cyclic=cyclic;old.Flat=flat;old.Hexagon=!cyclic;old.Bounds=new Rectangle(442,58,296,95);preview.Bounds=new Rectangle(442,294,296,140);preview.Cyclic=cyclic;preview.Flat=flat;preview.Hexagon=!cyclic;
            bar.Bounds=new Rectangle(442,177,296,55);bar.Draft=draft;bar.Changed=()=>{SyncColor();RefreshPreview();};Controls.AddRange(new Control[]{old,preview,bar,LabelAt("Obecny wygląd",442,35),LabelAt("Nowy wygląd",442,271)});
            var rainbow=new Button{Text="Tęcza",Bounds=new Rectangle(442,235,86,27)};remove.Bounds=new Rectangle(538,235,110,27);var hint=LabelAt(cyclic?"Cykl300px: 80% paleta, 20% płynny powrót.":"0°: lewo→prawo; 90°: góra→dół.",430,450);Controls.AddRange(new Control[]{rainbow,remove,hint});
            angle.Bounds=new Rectangle(440,438,240,32);angleValue.Location=new Point(680,441);if(!cyclic){Controls.Add(angle);Controls.Add(angleValue);hint.Top=475;}
            rainbow.Click+=(s,e)=>{draft=GradientRules.Rainbow();bar.Draft=draft;bar.Selected=0;mode.SelectedIndex=1;SyncColor();RefreshPreview();};remove.Click+=(s,e)=>{bar.RemoveSelected();SyncColor();RefreshPreview();};
            angle.ValueChanged+=(s,e)=>{if(updating)return;draft.Angle=angle.Value;RefreshPreview();};
            var cancel=new Button{Text="Anuluj",DialogResult=DialogResult.Cancel,Bounds=new Rectangle(650,490,96,28)};accept.Bounds=new Rectangle(538,490,105,28);Controls.AddRange(new Control[]{accept,cancel});AcceptButton=accept;CancelButton=cancel;
            accept.Click+=(s,e)=>{if(!color.ColorValid)return;GradientRules.Validate(draft);Gradient=mode.SelectedIndex==1?draft.Copy():null;Hex=Gradient==null?uniform:Gradient.Stops[0].Color;DialogResult=DialogResult.OK;};
            color.Changed=c=>{if(updating)return;if(mode.SelectedIndex==1)draft.Stops[bar.Selected].Color=color.Hex;else uniform=color.Hex;RefreshPreview();};color.ValidityChanged=valid=>accept.Enabled=valid;
            mode.SelectedIndexChanged+=(s,e)=>{SyncColor();RefreshPreview();};mode.SelectedIndex=gradient!=null||startGradient?1:0;
            foreach(var button in Controls.OfType<Button>()){button.FlatStyle=FlatStyle.Flat;button.BackColor=Color.FromArgb(49,54,67);button.ForeColor=ForeColor;}
            SyncColor();RefreshPreview();
        }
        static Label LabelAt(string text,int x,int y){return new Label{Text=text,AutoSize=true,Location=new Point(x,y)};}
        void SyncColor(){updating=true;try{color.SetColor(ColorTranslator.FromHtml(mode.SelectedIndex==1?draft.Stops[bar.Selected].Color:uniform));angle.Value=(int)Math.Round(draft.Angle);}finally{updating=false;}}
        void RefreshPreview(){bool gradient=mode.SelectedIndex==1;bar.Enabled=gradient;remove.Enabled=gradient&&bar.Selected>0&&bar.Selected<draft.Stops.Count-1;angle.Enabled=gradient;angleValue.Text=draft.Angle+"°";preview.Color=ColorTranslator.FromHtml(uniform);preview.Gradient=gradient?draft:null;preview.Invalidate();bar.Invalidate();}
    }
    internal sealed class GradientSwatch : Control
    {
        internal Color Color=Color.Gray;internal GradientDefinition Gradient;internal bool Cyclic,Hexagon,Flat;
        internal GradientSwatch(){DoubleBuffered=true;}
        protected override void OnPaint(PaintEventArgs e)
        {
            if(Cyclic){for(int x=0;x<Width;x++)using(var p=new Pen(Gradient==null?Color:GradientRules.Cycle(Gradient,(double)x/Math.Max(1,Width-1))))e.Graphics.DrawLine(p,x,0,x,Height);}
            else using(var path=Hexagon?TerrainTextures.Mask(new PointF(Width/2f,Height/2f),Math.Min(Width,Height)/2f-3,Flat):Rect()){if(Gradient!=null)GradientRules.Fill(e.Graphics,path,Gradient);else using(var b=new SolidBrush(Color))e.Graphics.FillPath(b,path);}
        }
        GraphicsPath Rect(){var p=new GraphicsPath();p.AddRectangle(ClientRectangle);return p;}
    }
    internal sealed class GradientBar : Control
    {
        internal GradientDefinition Draft;internal int Selected;internal Action Changed;bool dragging;
        internal GradientBar(){DoubleBuffered=true;Cursor=Cursors.Hand;AccessibleName="Punkty gradientu: klik dodaje, przeciągnij punkt, odstęp minimum 0,01";}
        internal bool Add(double t){if(Draft.Stops.Count>=16||Draft.Stops.Any(s=>Math.Abs(s.Position-t)<.01))return false;var c=GradientRules.At(Draft,t);var stop=new GradientStop{Position=t,Color=String.Format("#{0:X2}{1:X2}{2:X2}",c.R,c.G,c.B)};Draft.Stops.Add(stop);Draft.Stops=Draft.Stops.OrderBy(s=>s.Position).ToList();Selected=Draft.Stops.IndexOf(stop);return true;}
        internal void MoveSelected(double t){if(Selected<=0||Selected>=Draft.Stops.Count-1)return;Draft.Stops[Selected].Position=Math.Max(Draft.Stops[Selected-1].Position+.01,Math.Min(Draft.Stops[Selected+1].Position-.01,t));}
        internal void RemoveSelected(){if(Selected<=0||Selected>=Draft.Stops.Count-1)return;Draft.Stops.RemoveAt(Selected);Selected=Math.Min(Selected,Draft.Stops.Count-1);if(Changed!=null)Changed();}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left||Draft==null)return;double t=Math.Max(0,Math.Min(1,(e.X-7.0)/Math.Max(1,Width-14)));int at=Draft.Stops.FindIndex(s=>Math.Abs((Width-14)*s.Position+7-e.X)<=7);if(at>=0)Selected=at;else if(!Add(t))return;dragging=true;Capture=true;if(Changed!=null)Changed();}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(!dragging)return;MoveSelected(Math.Max(0,Math.Min(1,(e.X-7.0)/Math.Max(1,Width-14))));if(Changed!=null)Changed();}
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)dragging=false;}
        protected override void OnPaint(PaintEventArgs e){if(Draft==null)return;for(int x=7;x<Width-7;x++)using(var p=new Pen(GradientRules.At(Draft,(x-7.0)/(Width-14))))e.Graphics.DrawLine(p,x,0,x,29);for(int i=0;i<Draft.Stops.Count;i++){float x=7+(float)Draft.Stops[i].Position*(Width-14);using(var b=new SolidBrush(ColorTranslator.FromHtml(Draft.Stops[i].Color)))e.Graphics.FillRectangle(b,x-6,32,12,18);using(var p=new Pen(i==Selected?Color.Gold:Color.Gray,i==Selected?3:1))e.Graphics.DrawRectangle(p,x-6,32,12,18);}}
    }
}
