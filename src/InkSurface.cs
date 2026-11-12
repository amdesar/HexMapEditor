using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using Media = System.Windows.Media;
using WPoint = System.Windows.Point;

namespace HexMapEditor
{
    // Immutable vector results; no bitmap of the world and no erasing by cutting stroke axes.
    internal static class InkSurface
    {
        static double Tolerance(double width){return Math.Max(.0005,width/65536);}
        internal static Media.PathGeometry Capsule(PointF from,PointF to,float diameter)
        {
            if(from==to)return Media.PathGeometry.CreateFromGeometry(new Media.EllipseGeometry(new WPoint(from.X,from.Y),diameter/2.0,diameter/2.0));
            var line=new Media.LineGeometry(new WPoint(from.X,from.Y),new WPoint(to.X,to.Y));
            return line.GetWidenedPathGeometry(Pen(diameter),Tolerance(diameter),Media.ToleranceType.Absolute);
        }
        static Media.Pen Pen(double width){return new Media.Pen(Media.Brushes.Black,width){StartLineCap=Media.PenLineCap.Round,EndLineCap=Media.PenLineCap.Round,LineJoin=Media.PenLineJoin.Round};}
        internal static Media.PathGeometry Geometry(InkStroke stroke)
        {
            if(stroke.Surface!=null)
            {
                if(stroke.CachedSurface!=null&&stroke.CachedSurfaceText==stroke.Surface)return stroke.CachedSurface;
                if(stroke.Surface.Length>2*1024*1024)throw new InvalidDataException("Opis pojedynczej powierzchni przekracza 2 MiB.");
                var parsed=Media.PathGeometry.CreateFromGeometry(Media.Geometry.Parse(stroke.Surface));parsed.Freeze();stroke.CachedSurfaceText=stroke.Surface;return stroke.CachedSurface=parsed;
            }
            if(stroke.Points.Count==1)return Capsule(stroke.Points[0].Point,stroke.Points[0].Point,stroke.Width);
            var path=new Media.StreamGeometry();using(var context=path.Open())
            {context.BeginFigure(new WPoint(stroke.Points[0].X,stroke.Points[0].Y),false,false);context.PolyLineTo(stroke.Points.Skip(1).Select(p=>new WPoint(p.X,p.Y)).ToList(),true,true);}
            return path.GetWidenedPathGeometry(Pen(stroke.Width),Tolerance(stroke.Width),Media.ToleranceType.Absolute);
        }
        internal static InkStroke Subtract(InkStroke stroke,Media.Geometry cut,float diameter)
        {
            var geometry=Geometry(stroke);double tolerance=Tolerance(Math.Min(stroke.Width,diameter));
            if(InkTiming.Measure(()=>geometry.FillContainsWithDetail(cut,tolerance,Media.ToleranceType.Absolute),(m,t)=>m.Contains+=t)==Media.IntersectionDetail.Empty)return stroke;
            var remaining=InkTiming.Measure(()=>Media.Geometry.Combine(geometry,cut,Media.GeometryCombineMode.Exclude,null,tolerance,Media.ToleranceType.Absolute),(m,t)=>m.Exclude+=t);
            if(remaining.IsEmpty()||remaining.Bounds.IsEmpty)return null;
            remaining.Freeze();
            var next=new InkStroke{Id=stroke.Id,Color=stroke.Color,Width=stroke.Width,Surface=InkTiming.Measure(()=>remaining.ToString(CultureInfo.InvariantCulture),(m,t)=>m.Serialize+=t),Gradient=stroke.Gradient,CycleWorld=stroke.CycleWorld,ColorTrace=stroke.ColorTrace};
            if(stroke.Gradient!=null)next.Points=stroke.Points;
            next.CachedSurface=remaining;next.CachedSurfaceText=next.Surface;return next;
        }
        internal static int PointCount(Media.PathGeometry path)
        {
            int count=0;foreach(var figure in path.Figures){count++;foreach(var segment in figure.Segments)
            {
                if(segment is Media.LineSegment)count++;
                else if(segment is Media.BezierSegment)count+=3;
                else if(segment is Media.PolyLineSegment)count+=((Media.PolyLineSegment)segment).Points.Count;
                else if(segment is Media.PolyBezierSegment)count+=((Media.PolyBezierSegment)segment).Points.Count;
                else throw new InvalidDataException("Nieobsługiwany segment powierzchni rysunku.");
            }}return count;
        }
        internal static RectangleF Bounds(InkStroke stroke)
        {var r=Geometry(stroke).Bounds;return r.IsEmpty?RectangleF.Empty:new RectangleF((float)r.X,(float)r.Y,(float)r.Width,(float)r.Height);}
        internal static void Validate(InkStroke stroke)
        {
            if(stroke.ValidatedSurfaceText==stroke.Surface&&stroke.ValidatedWidth==stroke.Width)return;
            var geometry=Geometry(stroke);var b=geometry.Bounds;double edge=10000000.0+stroke.Width/2.0+1;
            if(geometry.IsEmpty()||b.IsEmpty||!new[]{b.Left,b.Top,b.Right,b.Bottom}.All(v=>GraphicSources.Finite(v)&&Math.Abs(v)<=edge))throw new InvalidDataException("Nieprawidłowa obwiednia powierzchni rysunku.");
            foreach(var f in geometry.Figures)
            {if(!f.IsClosed||!f.IsFilled)throw new InvalidDataException("Powierzchnia musi mieć zamknięte kontury.");}
            int count=PointCount(geometry);
            using(var path=DrawingPath(geometry))foreach(var p in path.PathPoints)if(!ObjectGeometry.Finite(p.X)||!ObjectGeometry.Finite(p.Y)||Math.Abs(p.X)>edge||Math.Abs(p.Y)>edge)throw new InvalidDataException("Nieprawidłowe punkty powierzchni.");
            stroke.CachedSurfacePointCount=count;stroke.ValidatedWidth=stroke.Width;stroke.ValidatedSurfaceText=stroke.Surface;
        }
        static PointF Point(WPoint p){return new PointF((float)p.X,(float)p.Y);}
        internal static GraphicsPath DrawingPath(Media.PathGeometry geometry)
        {
            var path=new GraphicsPath(geometry.FillRule==Media.FillRule.Nonzero?FillMode.Winding:FillMode.Alternate);
            try
            {
                foreach(var figure in geometry.Figures)
                {
                    path.StartFigure();var current=Point(figure.StartPoint);
                    foreach(var segment in figure.Segments)
                    {
                        var line=segment as Media.LineSegment;var lines=segment as Media.PolyLineSegment;var curve=segment as Media.BezierSegment;var curves=segment as Media.PolyBezierSegment;
                        if(line!=null){var end=Point(line.Point);path.AddLine(current,end);current=end;}
                        else if(lines!=null){foreach(var p in lines.Points){var end=Point(p);path.AddLine(current,end);current=end;}}
                        else if(curve!=null){var end=Point(curve.Point3);path.AddBezier(current,Point(curve.Point1),Point(curve.Point2),end);current=end;}
                        else if(curves!=null){for(int i=0;i<curves.Points.Count;i+=3){var end=Point(curves.Points[i+2]);path.AddBezier(current,Point(curves.Points[i]),Point(curves.Points[i+1]),end);current=end;}}
                        else throw new InvalidDataException("Nieobsługiwany segment powierzchni.");
                    }
                    if(figure.IsClosed)path.CloseFigure();
                }
                return path;
            }catch{path.Dispose();throw;}
        }
    }
}
