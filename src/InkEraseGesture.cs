using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using Media=System.Windows.Media;
namespace HexMapEditor
{
    // Retain every sampled turn. Preview excludes the same union of swept discs; commit does one
    // boolean per intersected stroke, rather than serializing every intermediate contour at every move.
    internal sealed class InkEraseGesture : IDisposable
    {
        internal readonly GraphicsPath Preview=new GraphicsPath(FillMode.Winding);
        readonly Media.GeometryGroup cuts=new Media.GeometryGroup{FillRule=Media.FillRule.Nonzero};
        readonly float diameter;
        RectangleF bounds;
        int count;
        internal InkEraseGesture(float diameter){this.diameter=diameter;}
        internal void Add(PointF from,PointF to)
        {
            if(++count>InkGeometry.MaxStrokePoints)throw new InvalidDataException("Limit gestu gumki: 10000 próbek.");
            var cut=InkSurface.Capsule(from,to,diameter);cuts.Children.Add(cut);
            using(var path=InkSurface.DrawingPath(cut))Preview.AddPath(path,false);
            var area=RectangleF.FromLTRB(Math.Min(from.X,to.X)-diameter/2,Math.Min(from.Y,to.Y)-diameter/2,Math.Max(from.X,to.X)+diameter/2,Math.Max(from.Y,to.Y)+diameter/2);
            bounds=count==1?area:RectangleF.Union(bounds,area);
        }
        internal List<InkStroke> Apply(IList<InkStroke> source)
        {
            cuts.Freeze();var result=new List<InkStroke>();
            foreach(var stroke in source)
            {
                if(!bounds.IntersectsWith(InkGeometry.Bounds(stroke))){result.Add(stroke);continue;}
                var next=InkSurface.Subtract(stroke,cuts,diameter);if(next!=null)result.Add(next);
            }
            InkGeometry.Validate(result);return result;
        }
        public void Dispose(){Preview.Dispose();}
    }
}
