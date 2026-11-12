using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal static class InkSizeMath
    {
        internal const float MaxWorldWidth=2000000000f;
        internal static float World(float logical,float zoom,float dpi)
        {
            if(!ObjectGeometry.Finite(logical)||logical<.5f||logical>200||!ObjectGeometry.Finite(zoom)||zoom<.000001f||!ObjectGeometry.Finite(dpi)||dpi<=0||dpi>960)throw new ArgumentOutOfRangeException("Rozmiar/zoom/DPI rysika poza zakresem.");
            return logical*(dpi/96f)/zoom;
        }
    }
    internal sealed class InkCanvas : BufferedPanel
    {
        bool pointerHidden;
        [DllImport("user32.dll")]static extern IntPtr SetCursor(IntPtr cursor);
        internal bool PointerHidden
        {
            get{return pointerHidden;}
            set{pointerHidden=value;if(IsHandleCreated&&ClientRectangle.Contains(PointToClient(Control.MousePosition)))SetCursor(value?IntPtr.Zero:Cursor.Handle);}
        }
        protected override void WndProc(ref Message m){if(m.Msg==0x20&&pointerHidden){SetCursor(IntPtr.Zero);m.Result=new IntPtr(1);return;}base.WndProc(ref m);}
        protected override void Dispose(bool disposing){if(disposing)PointerHidden=false;base.Dispose(disposing);}
    }
    internal sealed class NumberDrag : Control
    {
        readonly NumericUpDown number;readonly decimal step;readonly int pixels;
        int startX;decimal startValue;bool dragging;
        internal NumberDrag(NumericUpDown number,decimal step,int pixels)
        {
            this.number=number;this.step=step;this.pixels=pixels;Size=new Size(18,23);Cursor=Cursors.SizeWE;AccessibleName="Przeciągnij poziomo rozmiar";TabStop=false;DoubleBuffered=true;
            MouseDown+=(s,e)=>{if(e.Button!=MouseButtons.Left||!number.Enabled)return;startX=e.X;startValue=number.Value;dragging=true;Capture=true;};
            MouseMove+=(s,e)=>{if(dragging)MoveBy(e.X-startX);};MouseUp+=(s,e)=>{if(e.Button==MouseButtons.Left){dragging=false;Capture=false;}};MouseCaptureChanged+=(s,e)=>{if(!Capture)dragging=false;};LostFocus+=(s,e)=>{dragging=false;Capture=false;};
        }
        internal void MoveBy(int delta){number.Value=Math.Max(number.Minimum,Math.Min(number.Maximum,startValue+Math.Truncate((decimal)delta/pixels)*step));}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var p=new Pen(Enabled?Color.Silver:Color.DimGray,1.5f)){float y=Height/2f;e.Graphics.DrawLine(p,2,y,Width-3,y);e.Graphics.DrawLines(p,new[]{new PointF(6,y-4),new PointF(2,y),new PointF(6,y+4)});e.Graphics.DrawLines(p,new[]{new PointF(Width-7,y-4),new PointF(Width-3,y),new PointF(Width-7,y+4)});}}
    }
}
