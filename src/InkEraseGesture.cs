using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using Media=System.Windows.Media;
namespace HexMapEditor
{
    // Retain every sampled turn. Preview excludes the same union of swept discs; commit does one
    // boolean per intersected stroke, rather than serializing every intermediate contour at every move.
    internal sealed class InkEraseGesture : IDisposable
    {
        internal readonly GraphicsPath Preview=new GraphicsPath(FillMode.Winding);
        readonly Media.GeometryGroup cuts=new Media.GeometryGroup{FillRule=Media.FillRule.Nonzero};
        readonly List<RectangleF> cutBounds=new List<RectangleF>();
        readonly float diameter;
        readonly IList<InkStroke> hitSource;
        readonly List<PointF> froms=new List<PointF>(),tos=new List<PointF>();
        bool touched;
        RectangleF bounds;
        int count;
        internal InkEraseGesture(float diameter,IList<InkStroke> source=null){this.diameter=diameter;hitSource=source;}
        internal void Add(PointF from,PointF to)
        {
            if(++count>InkGeometry.MaxStrokePoints)throw new InvalidDataException("Limit gestu gumki: 10000 próbek.");
            if(hitSource!=null&&!touched){touched=InkHitTest.Any(hitSource,from,to,diameter);if(!touched)return;}
            froms.Add(from);tos.Add(to);
            var cut=InkSurface.Capsule(from,to,diameter);cuts.Children.Add(cut);
            using(var path=InkSurface.DrawingPath(cut))Preview.AddPath(path,false);
            var area=RectangleF.FromLTRB(Math.Min(from.X,to.X)-diameter/2,Math.Min(from.Y,to.Y)-diameter/2,Math.Max(from.X,to.X)+diameter/2,Math.Max(from.Y,to.Y)+diameter/2);
            cutBounds.Add(area);
            bounds=cutBounds.Count==1?area:RectangleF.Union(bounds,area);
        }
        internal List<InkStroke> Apply(IList<InkStroke> source)
        {
            cuts.Freeze();var result=new List<InkStroke>();
            foreach(var stroke in source)
            {
                if(!bounds.IntersectsWith(InkGeometry.Bounds(stroke))){result.Add(stroke);continue;}
                var next=InkSurface.Subtract(stroke,cuts,diameter);if(next!=null)result.Add(next);
            }
            InkTiming.Measure(()=>{InkGeometry.Validate(result);return true;},(m,t)=>m.Validate+=t);return result;
        }
        internal bool MayAffect(IList<InkStroke> source){if(ReferenceEquals(source,hitSource))return touched;for(int i=0;i<froms.Count;i++)if(InkHitTest.Any(source,froms[i],tos[i],diameter))return true;return false;}
        public void Dispose(){Preview.Dispose();}
        internal InkEraseWork Capture(IList<InkStroke> source)
        {
            cuts.Freeze();return new InkEraseWork(source,cuts.Children.ToArray(),cutBounds.ToArray(),diameter);
        }
    }
}
