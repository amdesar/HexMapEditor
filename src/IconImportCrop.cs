using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class SquareCropState
    {
        internal readonly CropTransform Transform;
        internal SquareCropState(Size source,CropTransform initial=null)
        {Transform=initial==null?new CropTransform{Zoom=(double)Math.Min(source.Width,source.Height)/Math.Max(source.Width,source.Height)}:initial.Copy();}
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
        internal readonly SquareCropState Draft;
        internal IconAsset Result{get;private set;}
        internal SquareImportDialog(string path):this(GraphicSources.Import(GraphicSources.ReadFile(path,SquareLibraryAsset.InputBytes)),Path.GetFileNameWithoutExtension(path)){}
        internal SquareImportDialog(Bitmap bitmap,string sourceName):this(SquareLibraryAsset.TakeBitmap(bitmap),sourceName){}
        internal SquareImportDialog(GraphicSource original,string sourceName,CropTransform initial=null)
        {
            if(original.Width>2048||original.Height>2048||original.EncodedLength>GraphicLimits.Encoded(SquareLibraryAsset.InputBytes))
                throw new InvalidDataException("Import: źródło może mieć najwyżej 2048 × 2048 i 8 MiB.");
            source=GraphicSources.Decode(original);
            string name=PresetFiles.Shorten(sourceName,200);
            Draft=new SquareCropState(source.Size,initial);
            Text="Kadrowanie importowanej grafiki";ClientSize=new Size(640,660);Font=new Font("Segoe UI",9);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
            var surface=new SquareCropSurface(source,Draft){Bounds=new Rectangle(16,40,608,536)};
            var hint=new Label{Text="Przeciągaj: przesunięcie • Kółko: skala • Obrót suwakiem. Kadr 512×512.",Bounds=new Rectangle(16,10,608,24)};
            var rotation=new TrackBar{Minimum=0,Maximum=360,Value=(int)Math.Round(Draft.Transform.Angle),TickFrequency=45,Bounds=new Rectangle(16,582,390,32)};var angle=new Label{Bounds=new Rectangle(417,588,72,24),Text=rotation.Value+"°"};rotation.ValueChanged+=(s,e)=>{Draft.Transform.Angle=CropMath.Angle(rotation.Value);angle.Text=rotation.Value+"°";surface.Invalidate();};
            var accept=new EditorButton{Text="Importuj",Bounds=new Rectangle(400,620,105,28)};var cancel=new EditorButton{Text="Anuluj",DialogResult=DialogResult.Cancel,Bounds=new Rectangle(516,620,108,28)};
            DarkTextBox nameBox=null;
            if(initial==null)
            {
                nameBox=new DarkTextBox { Text=name,MaxLength=200,Bounds=new Rectangle(76,621,309,27),AccessibleName="Nazwa grafiki" };
                Controls.Add(new Label { Text="Nazwa",Bounds=new Rectangle(16,621,58,27),TextAlign=ContentAlignment.MiddleLeft });
                Controls.Add(nameBox);
            }
            accept.Click+=(s,e)=>{try{
                string accepted=nameBox==null?name:nameBox.Text.Trim();
                if(accepted.Length==0)throw new InvalidDataException("Podaj nazwę grafiki.");
                using(var image=Draft.Render(source))Result=SquareLibraryAsset.Prepare(image,original,accepted,Draft.Transform);
                DialogResult=DialogResult.OK;
            }catch(Exception ex){MessageBox.Show(this,ex.Message,"Nie można zapisać kadru",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};
            var reset=new EditorButton{Text="Obrót 0°",Bounds=new Rectangle(512,582,110,28)};reset.Click+=(s,e)=>{Draft.Transform.Angle=0;rotation.Value=0;angle.Text="0°";surface.Invalidate();};Controls.AddRange(new Control[]{hint,surface,rotation,angle,reset,accept,cancel});AcceptButton=accept;CancelButton=cancel;Disposed+=(s,e)=>source.Dispose();
        }
    }
    internal sealed class SquareCropSurface:Control
    {
        readonly Bitmap source;readonly SquareCropState state;Point previous;bool dragging;
        internal RectangleF Frame{get{float side=Math.Max(1,Math.Min(Width,Height)-16);return new RectangleF((Width-side)/2f,(Height-side)/2f,side,side);}}
        internal SquareCropSurface(Bitmap source,SquareCropState state){this.source=source;this.state=state;DoubleBuffered=true;Cursor=Cursors.SizeAll;TabStop=true;AccessibleName="Kwadratowy kadr importu: przeciąganie i kółko";}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left&&e.Button!=MouseButtons.Middle)return;Focus();previous=e.Location;dragging=Capture=true;}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(!dragging)return;state.Pan(e.X-previous.X,e.Y-previous.Y,source.Size,Frame.Width);previous=e.Location;Invalidate();}
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button==MouseButtons.Left||e.Button==MouseButtons.Middle)Capture=false;}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)dragging=false;}
        protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);state.Zoom(Math.Pow(1.13,e.Delta/120.0));Invalidate();}
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;CropPreview.Checker(g,ClientRectangle);CropPreview.Surroundings(g,source,state.Transform,Frame,ClientRectangle);
            using(var outside=new Region(ClientRectangle))using(var shade=new SolidBrush(Color.FromArgb(165,20,24,32)))using(var p=new Pen(ChoiceStyle.ActiveBorder,2)){outside.Exclude(Frame);g.FillRegion(shade,outside);g.DrawRectangle(p,Frame.X,Frame.Y,Frame.Width,Frame.Height);}
        }
    }
    public sealed partial class MainWindow
    {
        IconAsset CropImportedIcon(string path,IWin32Window owner){using(var dialog=new SquareImportDialog(path))return RunModal(()=>dialog.ShowDialog(owner))==DialogResult.OK?dialog.Result:null;}
        IconAsset ChooseSquareGraphicFile(IWin32Window owner,string title)
        {
            using(var file=new OpenFileDialog{Title=title,Filter="Bitmapy PNG/JPG/BMP/GIF|*.png;*.jpg;*.jpeg;*.bmp;*.gif",RestoreDirectory=true})
                return RunModal(()=>file.ShowDialog(owner))==DialogResult.OK?CropImportedIcon(file.FileName,owner):null;
        }
        IconAsset CropClipboardIcon(IWin32Window owner,string name)
        {
            var bytes=ReadClipboardGraphic(8*1024*1024,2048);
            using(var dialog=new SquareImportDialog(GraphicSources.Import(bytes),name))
                return RunModal(()=>dialog.ShowDialog(owner))==DialogResult.OK?dialog.Result:null;
        }
        static byte[] ReadClipboardGraphic(int limit,int side)
        {
            var bytes=ClipboardRaster.Read(System.Windows.Clipboard.GetDataObject(),limit,side,(long)side*side);
            if(bytes==null)throw new InvalidDataException("Schowek nie zawiera obsługiwanej grafiki.");
            return bytes;
        }
    }
}
