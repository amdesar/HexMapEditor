using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
namespace HexMapEditor
{
    // Transient import draft. Only its final square PNG is a library asset.
    internal sealed class SquareCropState
    {
        internal readonly CropTransform Transform=new CropTransform();
        internal SquareCropState(Size source){Transform.Zoom=(double)Math.Min(source.Width,source.Height)/Math.Max(source.Width,source.Height);}
        internal void Pan(double dx,double dy,Size source,double side)
        {
            double factor=side/Math.Min(source.Width,source.Height)*Transform.Zoom,r=Transform.Angle*Math.PI/180;
            Transform.X-=(dx*Math.Cos(r)+dy*Math.Sin(r))/factor/source.Width;
            Transform.Y-=(-dx*Math.Sin(r)+dy*Math.Cos(r))/factor/source.Height;
            Transform.X=Math.Max(-4,Math.Min(5,Transform.X));Transform.Y=Math.Max(-4,Math.Min(5,Transform.Y));
        }
        internal void Zoom(double factor){Transform.Zoom=Math.Max(.02,Math.Min(CropMath.MaxZoom,Transform.Zoom*factor));}
        internal Bitmap Render(Bitmap source,int side=512){var result=new Bitmap(side,side,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(result)){g.Clear(Color.Transparent);CropMath.Draw(g,source,Transform,new RectangleF(0,0,side,side));}return result;}
    }
    internal sealed class SquareImportDialog:DarkForm
    {
        readonly Bitmap source;
        readonly string name;
        internal readonly SquareCropState Draft;
        internal IconAsset Result{get;private set;}
        internal SquareImportDialog(string path)
        {
            source=IconAssets.ReadImport(path);name=Path.GetFileNameWithoutExtension(path);name=name.Substring(0,Math.Min(200,name.Length));Draft=new SquareCropState(source.Size);
            Text="Kadrowanie importowanej grafiki";ClientSize=new Size(760,760);Font=new Font("Segoe UI",9);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
            var surface=new SquareCropSurface(source,Draft){Bounds=new Rectangle(16,40,728,640)};
            var hint=new Label{Text="Przeciągaj: przesunięcie • Kółko: skala • Obrót suwakiem. Zapis tylko wnętrza kwadratu512×512.",Bounds=new Rectangle(16,10,728,24)};
            var rotation=new TrackBar{Minimum=0,Maximum=360,TickFrequency=45,Bounds=new Rectangle(16,684,440,32)};var angle=new Label{Bounds=new Rectangle(462,690,72,24),Text="0°"};rotation.ValueChanged+=(s,e)=>{Draft.Transform.Angle=CropMath.Angle(rotation.Value);angle.Text=rotation.Value+"°";surface.Invalidate();};
            var accept=new EditorButton{Text="Importuj",Bounds=new Rectangle(526,720,104,28)};var cancel=new EditorButton{Text="Anuluj",DialogResult=DialogResult.Cancel,Bounds=new Rectangle(640,720,104,28)};
            accept.Click+=(s,e)=>{using(var image=Draft.Render(source)){var asset=IconAssets.FromBitmap(image,name);using(var valid=IconAssets.Decode(asset)){}Result=asset;}DialogResult=DialogResult.OK;};
            Controls.AddRange(new Control[]{hint,surface,rotation,angle,accept,cancel});AcceptButton=accept;CancelButton=cancel;Disposed+=(s,e)=>source.Dispose();
        }
    }
    internal sealed class SquareCropSurface:Control
    {
        readonly Bitmap source;readonly SquareCropState state;Point previous;bool dragging;
        internal RectangleF Frame{get{float side=Math.Max(1,Math.Min(Width,Height)-80);return new RectangleF((Width-side)/2f,(Height-side)/2f,side,side);}}
        internal SquareCropSurface(Bitmap source,SquareCropState state){this.source=source;this.state=state;DoubleBuffered=true;Cursor=Cursors.SizeAll;TabStop=true;AccessibleName="Kwadratowy kadr importu: przeciąganie i kółko";}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left&&e.Button!=MouseButtons.Middle)return;Focus();previous=e.Location;dragging=Capture=true;}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(!dragging)return;state.Pan(e.X-previous.X,e.Y-previous.Y,source.Size,Frame.Width);previous=e.Location;Invalidate();}
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button==MouseButtons.Left||e.Button==MouseButtons.Middle)Capture=false;}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)dragging=false;}
        protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);state.Zoom(Math.Pow(1.13,e.Delta/120.0));Invalidate();}
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;for(int y=0;y<Height;y+=16)for(int x=0;x<Width;x+=16)using(var b=new SolidBrush(((x+y)/16)%2==0?Color.FromArgb(52,57,68):Color.FromArgb(41,46,56)))g.FillRectangle(b,x,y,16,16);
            var surrounding=state.Transform.Copy();surrounding.Zoom*=Frame.Width/Width;CropMath.Draw(g,source,surrounding,ClientRectangle,true);
            using(var outside=new Region(ClientRectangle))using(var shade=new SolidBrush(Color.FromArgb(165,20,24,32)))using(var p=new Pen(ChoiceStyle.ActiveBorder,2)){outside.Exclude(Frame);g.FillRegion(shade,outside);g.DrawRectangle(p,Frame.X,Frame.Y,Frame.Width,Frame.Height);}
        }
    }
    public sealed partial class MainWindow
    {
        IconAsset CropImportedIcon(string path){using(var dialog=new SquareImportDialog(path))return RunModal(()=>dialog.ShowDialog(this))==DialogResult.OK?dialog.Result:null;}
    }
}
