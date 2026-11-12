using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace HexMapEditor
{
    public sealed class InkPoint
    {
        public float X { get; set; }
        public float Y { get; set; }
        public InkPoint() { }
        internal InkPoint(PointF p) { X=p.X;Y=p.Y; }
        internal PointF Point { get { return new PointF(X,Y); } }
    }
    public sealed class InkStroke
    {
        public string Id { get; set; }
        public string Color { get; set; }
        public float Width { get; set; }
        public List<InkPoint> Points { get; set; }
        internal RectangleF? CachedBounds;
        public InkStroke() { Id=Guid.NewGuid().ToString("N");Color="#FFD04A";Width=3;Points=new List<InkPoint>(); }
    }
    internal static class InkGeometry
    {
        internal const int MaxStrokes=5000, MaxPoints=200000, MaxStrokePoints=10000;
        internal static void Validate(List<InkStroke> strokes)
        {
            if(strokes==null||strokes.Count>MaxStrokes||strokes.Sum(s=>(long)(s==null||s.Points==null?0:s.Points.Count))>MaxPoints)throw new InvalidDataException("Limit rysunków: 5000 fragmentów / 200000 punktów.");
            var ids=new HashSet<string>();foreach(var s in strokes)
                if(s==null||String.IsNullOrWhiteSpace(s.Id)||s.Id.Length>200||!ids.Add(s.Id)||!MapLogic.ValidColor(s.Color)||!ObjectGeometry.Finite(s.Width)||s.Width<=0||s.Width>InkSizeMath.MaxWorldWidth||s.Points==null||s.Points.Count<1||s.Points.Count>MaxStrokePoints||s.Points.Any(p=>p==null||!ObjectGeometry.Finite(p.X)||!ObjectGeometry.Finite(p.Y)||Math.Abs(p.X)>10000000||Math.Abs(p.Y)>10000000))throw new InvalidDataException("Nieprawidłowa kreska (grubość świata >0, do 2 mld; do 10000 punktów)." );
        }
        internal static RectangleF Bounds(InkStroke s)
        {if(!s.CachedBounds.HasValue)s.CachedBounds=RectangleF.FromLTRB(s.Points.Min(p=>p.X)-s.Width/2,s.Points.Min(p=>p.Y)-s.Width/2,s.Points.Max(p=>p.X)+s.Width/2,s.Points.Max(p=>p.Y)+s.Width/2);return s.CachedBounds.Value;}
        struct Interval { internal double A,B;internal Interval(double a,double b){A=Math.Max(0,a);B=Math.Min(1,b);} }
        static PointF At(PointF a,PointF b,double t){return new PointF((float)(a.X+(b.X-a.X)*t),(float)(a.Y+(b.Y-a.Y)*t));}
        static void Circle(List<Interval> cuts,PointF a,PointF b,PointF center,double radius)
        {
            double dx=b.X-a.X,dy=b.Y-a.Y,x=a.X-center.X,y=a.Y-center.Y,aa=dx*dx+dy*dy,bb=2*(x*dx+y*dy),cc=x*x+y*y-radius*radius;
            if(aa<1e-14){if(cc<=0)cuts.Add(new Interval(0,1));return;}
            double d=bb*bb-4*aa*cc;if(d<0)return;double root=Math.Sqrt(d);var i=new Interval((-bb-root)/(2*aa),(-bb+root)/(2*aa));if(i.B>i.A)cuts.Add(i);
        }
        static bool Range(double start,double delta,double min,double max,ref double lo,ref double hi)
        {
            if(Math.Abs(delta)<1e-14)return start>=min&&start<=max;
            double a=(min-start)/delta,b=(max-start)/delta;lo=Math.Max(lo,Math.Min(a,b));hi=Math.Min(hi,Math.Max(a,b));return hi>lo;
        }
        static List<Interval> CutIntervals(PointF a,PointF b,PointF u,PointF v,double radius)
        {
            var cuts=new List<Interval>();Circle(cuts,a,b,u,radius);Circle(cuts,a,b,v,radius);
            double dx=v.X-u.X,dy=v.Y-u.Y,length=Math.Sqrt(dx*dx+dy*dy);
            if(length>1e-7){dx/=length;dy/=length;double lo=0,hi=1;
                if(Range((a.X-u.X)*dx+(a.Y-u.Y)*dy,(b.X-a.X)*dx+(b.Y-a.Y)*dy,0,length,ref lo,ref hi)&&Range(-(a.X-u.X)*dy+(a.Y-u.Y)*dx,-(b.X-a.X)*dy+(b.Y-a.Y)*dx,-radius,radius,ref lo,ref hi))cuts.Add(new Interval(lo,hi));}
            return cuts.OrderBy(i=>i.A).ToList();
        }
        internal static List<InkStroke> Erase(List<InkStroke> source,PointF from,PointF to,float diameter)
        {
            var result=new List<InkStroke>();
            foreach(var stroke in source)
            {
                double radius=(diameter+stroke.Width)/2;
                var sweep=RectangleF.FromLTRB(Math.Min(from.X,to.X)-(float)radius,Math.Min(from.Y,to.Y)-(float)radius,Math.Max(from.X,to.X)+(float)radius,Math.Max(from.Y,to.Y)+(float)radius);
                if(!sweep.IntersectsWith(Bounds(stroke))){result.Add(stroke);continue;}
                bool touched=false;
                for(int n=1;n<stroke.Points.Count&&!touched;n++)touched=CutIntervals(stroke.Points[n-1].Point,stroke.Points[n].Point,from,to,radius).Count>0;
                if(stroke.Points.Count>1&&!touched){result.Add(stroke);continue;}
                if(stroke.Points.Count==1){if(CutIntervals(stroke.Points[0].Point,stroke.Points[0].Point,from,to,radius).Count==0)result.Add(stroke);continue;}
                List<InkPoint> piece=null;bool originalId=true;
                Action flush=()=>{if(piece==null||piece.Count==0)return;result.Add(new InkStroke{Id=originalId?stroke.Id:Guid.NewGuid().ToString("N"),Color=stroke.Color,Width=stroke.Width,Points=piece});originalId=false;piece=null;};
                for(int i=1;i<stroke.Points.Count;i++)
                {
                    var a=stroke.Points[i-1].Point;var b=stroke.Points[i].Point;var cuts=CutIntervals(a,b,from,to,radius);double cursor=0;
                    foreach(var cut in cuts){if(cut.A>cursor+1e-7){if(piece==null)piece=new List<InkPoint>{new InkPoint(At(a,b,cursor))};piece.Add(new InkPoint(At(a,b,cut.A)));}if(cut.B>cursor){flush();cursor=Math.Max(cursor,cut.B);}}
                    if(cursor<1-1e-7){if(piece==null)piece=new List<InkPoint>{new InkPoint(At(a,b,cursor))};piece.Add(new InkPoint(b));}else flush();
                }
                flush();
            }
            Validate(result);return result;
        }
    }
}
