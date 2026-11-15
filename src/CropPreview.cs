using System.Drawing;
namespace HexMapEditor
{
    internal static class CropPreview
    {
        internal static void Checker(Graphics g,Rectangle bounds)
        {
            using(var a=new SolidBrush(Color.FromArgb(52,57,68)))using(var b=new SolidBrush(Color.FromArgb(41,46,56)))
                for(int y=bounds.Top;y<bounds.Bottom;y+=16)for(int x=bounds.Left;x<bounds.Right;x+=16)g.FillRectangle(((x+y)/16)%2==0?a:b,x,y,16,16);
        }
        internal static void Surroundings(Graphics g,Bitmap source,CropTransform crop,RectangleF frame,Rectangle viewport)
        {
            var view=crop.Copy();view.Zoom*=frame.Width/viewport.Width;
            CropMath.Draw(g,source,view,viewport,true);
        }
    }
}
