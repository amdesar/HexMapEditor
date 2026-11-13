using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class SwitchBox:CheckBox
    {
        public SwitchBox(){FlatStyle=FlatStyle.Flat;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);UseVisualStyleBackColor=false;BackColor=Color.FromArgb(34,38,48);ForeColor=Color.WhiteSmoke;MinimumSize=new Size(32,22);AccessibleRole=AccessibleRole.CheckButton;}
        public override Size GetPreferredSize(Size proposedSize){int width=MaximumSize.Width>0?Math.Max(1,MaximumSize.Width-38):10000;var text=TextRenderer.MeasureText(Text,Font,new Size(width,10000),TextFormatFlags.WordBreak|TextFormatFlags.NoPadding);return new Size(text.Width+38,Math.Max(22,text.Height+4));}
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;float h=Math.Min(16,Font.Height),y=(Height-h)/2f;
            var track=new RectangleF(1,y,28,h);using(var path=new GraphicsPath()){path.AddArc(track.X,track.Y,h,h,90,180);path.AddArc(track.Right-h,track.Y,h,h,270,180);path.CloseFigure();using(var fill=new SolidBrush(Checked?ChoiceStyle.ActiveBack:Color.FromArgb(45,49,60)))e.Graphics.FillPath(fill,path);using(var pen=new Pen(Enabled?(Checked?ChoiceStyle.ActiveBorder:ChoiceStyle.NeutralBorder):Color.FromArgb(91,98,111)))e.Graphics.DrawPath(pen,path);}
            float diameter=h-4;using(var knob=new SolidBrush(Enabled?Color.WhiteSmoke:Checked?Color.FromArgb(161,181,205):Color.FromArgb(123,129,143)))e.Graphics.FillEllipse(knob,Checked?track.Right-diameter-2:track.Left+2,y+2,diameter,diameter);
            var bounds=new RectangleF(37,0,Math.Max(0,Width-37),Height);using(var format=new StringFormat{LineAlignment=StringAlignment.Center,HotkeyPrefix=System.Drawing.Text.HotkeyPrefix.Show})using(var brush=new SolidBrush(Enabled?ForeColor:Color.FromArgb(153,160,174)))e.Graphics.DrawString(Text,Font,brush,bounds,format);
            if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(ClientRectangle,-1,-1),Color.Silver,BackColor);
        }
    }
}
