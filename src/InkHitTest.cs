using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace HexMapEditor
{
    internal static class InkHitTest
    {
        static double PointDistance(PointF p,PointF a,PointF b){double x=b.X-(double)a.X,y=b.Y-(double)a.Y,d=x*x+y*y,t=d==0?0:Math.Max(0,Math.Min(1,((p.X-a.X)*x+(p.Y-a.Y)*y)/d));x=p.X-a.X-t*x;y=p.Y-a.Y-t*y;return x*x+y*y;}
        static double Cross(PointF a,PointF b,PointF c){return (b.X-(double)a.X)*(c.Y-a.Y)-(b.Y-(double)a.Y)*(c.X-a.X);}
        static double Distance(PointF a,PointF b,PointF c,PointF d)
        {
            if(Math.Max(a.X,b.X)>=Math.Min(c.X,d.X)&&Math.Max(c.X,d.X)>=Math.Min(a.X,b.X)&&Math.Max(a.Y,b.Y)>=Math.Min(c.Y,d.Y)&&Math.Max(c.Y,d.Y)>=Math.Min(a.Y,b.Y)&&Cross(a,b,c)*Cross(a,b,d)<=0&&Cross(c,d,a)*Cross(c,d,b)<=0)return 0;
            return Math.Min(Math.Min(PointDistance(a,c,d),PointDistance(b,c,d)),Math.Min(PointDistance(c,a,b),PointDistance(d,a,b)));
        }
        internal static bool Any(IList<InkStroke> strokes,PointF a,PointF b,float diameter)
        {
            float radius=diameter/2;var box=RectangleF.FromLTRB(Math.Min(a.X,b.X)-radius,Math.Min(a.Y,b.Y)-radius,Math.Max(a.X,b.X)+radius,Math.Max(a.Y,b.Y)+radius);
            foreach(var stroke in strokes)
            {
                if(!box.IntersectsWith(InkGeometry.Bounds(stroke)))continue;
                if(stroke.Surface==null)
                {
                    double r=(stroke.Width+diameter)/2.0;
                    for(int i=0;i<stroke.Points.Count;i++)if(Distance(a,b,stroke.Points[Math.Max(0,i-1)].Point,stroke.Points[i].Point)<r*r)return true;
                    continue;
                }
                var flat=InkPathCache.Flat(stroke);
                if(flat==null) // Rare oversized flattening: localized native intersection, not a full WPF Exclude.
                {
                    using(var cut=InkSurface.DrawingPath(InkSurface.Capsule(a,b,diameter)))using(var region=new Region(InkPathCache.Get(stroke)))using(var pixel=new Bitmap(1,1))using(var g=Graphics.FromImage(pixel)){region.Intersect(cut);if(!region.IsEmpty(g))return true;}continue;
                }
                if(flat.IsVisible(a)||flat.IsVisible(b))return true;
                var points=flat.PathPoints;var types=flat.PathTypes;PointF first=PointF.Empty,last=PointF.Empty;
                // Conservative only within the existing geometry tolerance; worker remains authoritative.
                double tolerance=Math.Max(.0005,stroke.Width/65536.0),limit=(radius+tolerance)*(radius+tolerance);
                for(int i=0;i<points.Length;i++)
                {
                    if((types[i]&7)==0)first=last=points[i];else{if(Distance(a,b,last,points[i])<limit)return true;last=points[i];}
                    if((types[i]&128)!=0&&Distance(a,b,last,first)<limit)return true;
                }
            }
            return false;
        }
    }
}
