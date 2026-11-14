using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class SwitchBox:CheckBox
    {
        public SwitchBox(){FlatStyle=FlatStyle.Flat;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);UseVisualStyleBackColor=false;BackColor=Color.FromArgb(34,38,48);ForeColor=Color.WhiteSmoke;MinimumSize=new Size(32,22);AccessibleRole=AccessibleRole.CheckButton;}
        public override Size GetPreferredSize(Size proposedSize){return SwitchDrawing.Preferred(this);}
        protected override void OnPaint(PaintEventArgs e)
        {
            SwitchDrawing.Paint(e.Graphics,this,Checked,Focused&&ShowFocusCues);
        }
    }
    internal static class SwitchDrawing
    {
        internal static Size Preferred(Control c){var text=TextRenderer.MeasureText(c.Text,c.Font,Size.Empty,TextFormatFlags.SingleLine|TextFormatFlags.NoPadding);return new Size(text.Width+40,Math.Max(22,text.Height+4));}
        internal static void Paint(Graphics graphics,Control control,bool Checked,bool focus)
        {
            var e=new PaintEventArgs(graphics,control.ClientRectangle);var BackColor=control.BackColor;var ForeColor=control.ForeColor;var Font=control.Font;int Height=control.Height,Width=control.Width;bool Enabled=control.Enabled;string Text=control.Text;
            e.Graphics.Clear(BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;float h=Math.Min(16,Font.Height),y=(Height-h)/2f;
            var track=new RectangleF(1,y,28,h);using(var path=new GraphicsPath()){path.AddArc(track.X,track.Y,h,h,90,180);path.AddArc(track.Right-h,track.Y,h,h,270,180);path.CloseFigure();using(var fill=new SolidBrush(Checked?ChoiceStyle.ActiveBack:Color.FromArgb(45,49,60)))e.Graphics.FillPath(fill,path);using(var pen=new Pen(Enabled?(Checked?ChoiceStyle.ActiveBorder:ChoiceStyle.NeutralBorder):Color.FromArgb(91,98,111)))e.Graphics.DrawPath(pen,path);}
            float diameter=h-4;using(var knob=new SolidBrush(Enabled?Color.WhiteSmoke:Checked?Color.FromArgb(161,181,205):Color.FromArgb(123,129,143)))e.Graphics.FillEllipse(knob,Checked?track.Right-diameter-2:track.Left+2,y+2,diameter,diameter);
            var bounds=new RectangleF(37,0,Math.Max(0,Width-37),Height);using(var format=new StringFormat{FormatFlags=StringFormatFlags.NoWrap,LineAlignment=StringAlignment.Center,HotkeyPrefix=System.Drawing.Text.HotkeyPrefix.Show})using(var brush=new SolidBrush(Enabled?ForeColor:Color.FromArgb(153,160,174)))e.Graphics.DrawString(Text,Font,brush,bounds,format);
            if(focus)ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(control.ClientRectangle,-1,-1),Color.Silver,BackColor);
        }
    }
}
