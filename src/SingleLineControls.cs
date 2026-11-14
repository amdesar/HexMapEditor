using System.Drawing;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class SingleLineButton : Button
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            using(var b=new SolidBrush(ClientRectangle.Contains(PointToClient(MousePosition))&&Enabled?Color.FromArgb(65,73,90):BackColor))e.Graphics.FillRectangle(b,ClientRectangle);
            using(var p=new Pen(Enabled?FlatAppearance.BorderColor:Color.FromArgb(57,61,70)))e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);
            if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(3,3,Width-6,Height-6));
            TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(Padding.Left+2,2,Width-Padding.Horizontal-4,Height-4),Enabled?ForeColor:SystemColors.GrayText,
                TextFormatFlags.SingleLine|TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
        }
    }
    internal sealed class EllipsisNameLabel : Control
    {
        internal EllipsisNameLabel(){DoubleBuffered=true;}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,ForeColor,TextFormatFlags.SingleLine|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding);}
    }
}
