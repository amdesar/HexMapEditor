using System;
using System.Drawing;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal class EditorButton:Button
    {
        bool destructive;
        internal bool Destructive{get{return destructive;}set{destructive=value;UpdateColors();}}
        internal EditorButton(){UseVisualStyleBackColor=false;FlatStyle=FlatStyle.Flat;BackColor=Color.FromArgb(49,54,67);ForeColor=Color.WhiteSmoke;FlatAppearance.BorderColor=ChoiceStyle.NeutralBorder;EnabledChanged+=(s,e)=>{UpdateColors();Invalidate();};}
        void UpdateColors(){if(destructive){ForeColor=Enabled?Color.FromArgb(244,113,113):Color.FromArgb(153,160,174);FlatAppearance.BorderColor=Enabled?Color.FromArgb(204,91,91):Color.FromArgb(76,83,98);}}
        protected override void OnPaint(PaintEventArgs e)
        {
            if(Enabled){base.OnPaint(e);return;}
            e.Graphics.Clear(BackColor);using(var p=new Pen(Color.FromArgb(76,83,98)))e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);
            if(Image!=null)ControlPaint.DrawImageDisabled(e.Graphics,Image,(Width-Image.Width)/2,(Height-Image.Height)/2,BackColor);
            TextRenderer.DrawText(e.Graphics,Text,Font,Rectangle.Inflate(ClientRectangle,-3,-2),Color.FromArgb(153,160,174),TextFormatFlags.SingleLine|TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        }
    }
    internal sealed class RadioSwitch:RadioButton
    {
        internal RadioSwitch(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);AutoSize=true;BackColor=Color.FromArgb(34,38,48);ForeColor=Color.WhiteSmoke;MinimumSize=new Size(32,22);AccessibleRole=AccessibleRole.RadioButton;}
        public override Size GetPreferredSize(Size proposed){return SwitchDrawing.Preferred(this);}
        protected override void OnPaint(PaintEventArgs e){SwitchDrawing.Paint(e.Graphics,this,Checked,Focused&&ShowFocusCues);}
    }
}
