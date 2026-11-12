using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace HexMapEditor
{
    internal struct CometPoint
    {
        internal PointF Point;internal double Age,Phase;internal GradientSampler Palette;internal Color Color;
        internal CometPoint(PointF point,double age){Point=point;Age=age;Phase=0;Palette=null;Color=Color.Empty;}
        internal CometPoint(PointF point,double age,double phase,GradientSampler palette,Color color){Point=point;Age=age;Phase=phase;Palette=palette;Color=color;}
    }
    // A bounded scratch tile. Max coverage, not repeated SourceOver, at every join.
    internal sealed class CometRenderer : IDisposable
    {
        const int Side=256;
        readonly Bitmap tile=new Bitmap(Side,Side,PixelFormat.Format32bppArgb);
        readonly int[] pixels=new int[Side*Side];
        internal void Draw(Graphics graphics,Rectangle viewport,IList<CometPoint> points,PointF? head,float diameter,Color color)
        {
            DrawColored(graphics,viewport,System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(points,p=>new CometPoint(p.Point,p.Age,0,null,color))),head.HasValue?(CometPoint?)new CometPoint(head.Value,0,0,null,color):null,diameter);
        }
        internal void DrawColored(Graphics graphics,Rectangle viewport,IList<CometPoint> points,CometPoint? head,float diameter)
        {
            if(points.Count==0&&!head.HasValue)return;
            float left=head.HasValue?head.Value.Point.X:points[0].Point.X,right=left,top=head.HasValue?head.Value.Point.Y:points[0].Point.Y,bottom=top;
            foreach(var p in points){left=Math.Min(left,p.Point.X);right=Math.Max(right,p.Point.X);top=Math.Min(top,p.Point.Y);bottom=Math.Max(bottom,p.Point.Y);}
            var bounds=Rectangle.Intersect(viewport,Rectangle.Ceiling(RectangleF.FromLTRB(left-diameter/2-1,top-diameter/2-1,right+diameter/2+1,bottom+diameter/2+1)));
            bounds=Rectangle.Intersect(bounds,Rectangle.Ceiling(graphics.VisibleClipBounds));if(bounds.IsEmpty)return;
            for(int y=bounds.Top;y<bounds.Bottom;y+=Side)for(int x=bounds.Left;x<bounds.Right;x+=Side)
            {
                var area=new Rectangle(x,y,Math.Min(Side,bounds.Right-x),Math.Min(Side,bounds.Bottom-y));Array.Clear(pixels,0,pixels.Length);
                for(int i=1;i<points.Count;i++)Segment(area,points[i-1],points[i],diameter,false);
                if(points.Count==1)Segment(area,points[0],points[0],diameter,false);
                if(head.HasValue){var p=head.Value;Segment(area,p,p,diameter,true);}
                var data=tile.LockBits(new Rectangle(0,0,Side,Side),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
                try{Marshal.Copy(pixels,0,data.Scan0,pixels.Length);}finally{tile.UnlockBits(data);}
                graphics.DrawImage(tile,area,new Rectangle(0,0,area.Width,area.Height),GraphicsUnit.Pixel);
            }
        }
        void Segment(Rectangle area,CometPoint a,CometPoint b,float diameter,bool head)
        {
            // Preserve a predecessor across the expiry boundary without rendering its dead tail.
            if(a.Age>=750&&b.Age>=750)return;
            if(a.Age>750){double t=(a.Age-750)/(a.Age-b.Age);a=new CometPoint(new PointF((float)(a.Point.X+(b.Point.X-a.Point.X)*t),(float)(a.Point.Y+(b.Point.Y-a.Point.Y)*t)),750,a.Phase+(b.Phase-a.Phase)*t,a.Palette??b.Palette,a.Color);}
            double la=Math.Max(0,1-a.Age/750),lb=Math.Max(0,1-b.Age/750),ra=diameter*.5*Math.Pow(la,2.3),rb=diameter*.5*Math.Pow(lb,2.3),r=Math.Max(ra,rb)+1;
            double dx=b.Point.X-a.Point.X,dy=b.Point.Y-a.Point.Y,len2=dx*dx+dy*dy;
            int y0=Math.Max(area.Top,(int)Math.Floor(Math.Min(a.Point.Y,b.Point.Y)-r)),y1=Math.Min(area.Bottom-1,(int)Math.Ceiling(Math.Max(a.Point.Y,b.Point.Y)+r));
            for(int y=y0;y<=y1;y++)
            {
                double low=0,high=1;if(Math.Abs(dy)>.00001){low=Math.Max(0,Math.Min((y+.5-r-a.Point.Y)/dy,(y+.5+r-a.Point.Y)/dy));high=Math.Min(1,Math.Max((y+.5-r-a.Point.Y)/dy,(y+.5+r-a.Point.Y)/dy));if(low>high)continue;}
                int x0=Math.Max(area.Left,(int)Math.Floor(Math.Min(a.Point.X+dx*low,a.Point.X+dx*high)-r)),x1=Math.Min(area.Right-1,(int)Math.Ceiling(Math.Max(a.Point.X+dx*low,a.Point.X+dx*high)+r));
                for(int x=x0;x<=x1;x++)
                {
                    double t=len2<.00001?1:Math.Max(0,Math.Min(1,((x+.5-a.Point.X)*dx+(y+.5-a.Point.Y)*dy)/len2));
                    double px=a.Point.X+dx*t,py=a.Point.Y+dy*t,radius=ra+(rb-ra)*t,distance=Math.Sqrt((x+.5-px)*(x+.5-px)+(y+.5-py)*(y+.5-py));
                    double coverage=Math.Max(0,Math.Min(1,radius+.5-distance));int alpha=(int)Math.Round((head?255:210*(la+(lb-la)*t))*coverage);int index=(y-area.Top)*Side+x-area.Left;
                    if(alpha>((pixels[index]>>24)&255)){var palette=b.Palette??a.Palette;var color=palette==null?GradientRules.Lerp(a.Color,b.Color,t):palette.Cycle(a.Phase+(b.Phase-a.Phase)*t);pixels[index]=(alpha<<24)|(color.ToArgb()&0xffffff);}
                }
            }
        }
        public void Dispose(){tile.Dispose();}
    }
}
