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
        // When present this vector surface, including its holes, is authoritative.
        public string Surface { get; set; }
        internal System.Windows.Media.PathGeometry CachedSurface;
        internal string CachedSurfaceText;
        internal RectangleF? CachedBounds;
        public InkStroke() { Id=Guid.NewGuid().ToString("N");Color="#FFD04A";Width=3;Points=new List<InkPoint>(); }
    }
    internal static class InkGeometry
    {
        internal const int MaxStrokes=5000, MaxPoints=200000, MaxStrokePoints=10000;
        internal static int PointCost(InkStroke stroke){return stroke.Points.Count+(stroke.Surface==null?0:InkSurface.PointCount(InkSurface.Geometry(stroke)));}
        internal static void Validate(List<InkStroke> strokes)
        {
            if(strokes==null||strokes.Count>MaxStrokes)throw new InvalidDataException("Limit rysunków: 5000 powierzchni / 200000 punktów kontrolnych.");
            var ids=new HashSet<string>();long points=0,chars=0;
            foreach(var s in strokes)
            {
                if(s==null||String.IsNullOrWhiteSpace(s.Id)||s.Id.Length>200||!ids.Add(s.Id)||!MapLogic.ValidColor(s.Color)||!ObjectGeometry.Finite(s.Width)||s.Width<=0||s.Width>InkSizeMath.MaxWorldWidth||s.Points==null||s.Points.Count>MaxStrokePoints||s.Surface==null&&s.Points.Count<1||s.Points.Any(p=>p==null||!ObjectGeometry.Finite(p.X)||!ObjectGeometry.Finite(p.Y)||Math.Abs(p.X)>10000000||Math.Abs(p.Y)>10000000))throw new InvalidDataException("Nieprawidłowa geometria rysunku.");
                if(s.Surface!=null)InkSurface.Validate(s);
                int count=PointCost(s);points+=count;chars+=s.Surface==null?0:s.Surface.Length;
                if(count>MaxStrokePoints||points>MaxPoints||chars>16*1024*1024)throw new InvalidDataException("Limit rysunku: 10000 punktów kontrolnych powierzchni / 200000 dokumentu / 16 MiB opisu powierzchni.");
            }
        }
        internal static RectangleF Bounds(InkStroke s)
        {
            if(!s.CachedBounds.HasValue)s.CachedBounds=s.Surface!=null?InkSurface.Bounds(s):RectangleF.FromLTRB(s.Points.Min(p=>p.X)-s.Width/2,s.Points.Min(p=>p.Y)-s.Width/2,s.Points.Max(p=>p.X)+s.Width/2,s.Points.Max(p=>p.Y)+s.Width/2);
            return s.CachedBounds.Value;
        }
        internal static List<InkStroke> Erase(List<InkStroke> source,PointF from,PointF to,float diameter)
        {
            if(!ObjectGeometry.Finite(diameter)||diameter<=0||diameter>InkSizeMath.MaxWorldWidth)throw new InvalidDataException("Nieprawidłowa średnica gumki.");
            var result=new List<InkStroke>();var cut=InkSurface.Capsule(from,to,diameter);var sweep=RectangleF.FromLTRB(Math.Min(from.X,to.X)-diameter/2,Math.Min(from.Y,to.Y)-diameter/2,Math.Max(from.X,to.X)+diameter/2,Math.Max(from.Y,to.Y)+diameter/2);
            foreach(var stroke in source)
            {
                if(!sweep.IntersectsWith(Bounds(stroke))){result.Add(stroke);continue;}
                var next=InkSurface.Subtract(stroke,cut,diameter);if(next!=null)result.Add(next);
            }
            Validate(result);return result;
        }
        internal static void Draw(System.Drawing.Graphics graphics,InkStroke stroke)
        {
            if(stroke.Surface!=null){using(var path=InkSurface.DrawingPath(InkSurface.Geometry(stroke)))using(var brush=new SolidBrush(ColorTranslator.FromHtml(stroke.Color)))graphics.FillPath(brush,path);return;}
            using(var pen=new Pen(ColorTranslator.FromHtml(stroke.Color),stroke.Width){StartCap=System.Drawing.Drawing2D.LineCap.Round,EndCap=System.Drawing.Drawing2D.LineCap.Round,LineJoin=System.Drawing.Drawing2D.LineJoin.Round})
            {
                if(stroke.Points.Count==1){var p=stroke.Points[0];using(var b=new SolidBrush(pen.Color))graphics.FillEllipse(b,p.X-stroke.Width/2,p.Y-stroke.Width/2,stroke.Width,stroke.Width);}
                else graphics.DrawLines(pen,stroke.Points.Select(p=>p.Point).ToArray());
            }
        }
    }
}
