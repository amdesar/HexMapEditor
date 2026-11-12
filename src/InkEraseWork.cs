using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using Media=System.Windows.Media;
namespace HexMapEditor
{
    internal sealed class InkEraseResult
    {
        internal readonly List<InkStroke> Strokes=new List<InkStroke>();
        internal readonly List<int> OriginalIndices=new List<int>();
        internal readonly HashSet<int> Changed=new HashSet<int>();
        internal bool HasChange;
        internal string Serialized;
        internal InkTiming Timing;
    }
    internal sealed class InkEraseWork
    {
        readonly List<InkStroke> source;
        readonly Media.Geometry[] cuts;
        readonly RectangleF[] bounds;
        readonly float diameter;
        internal InkEraseWork(IList<InkStroke> original,Media.Geometry[] cuts,RectangleF[] bounds,float diameter)
        {
            // Fresh mutable model and thread-local caches. Only strings and frozen Freezables cross threads.
            source=original.Select(s=>new InkStroke{Id=s.Id,Color=s.Color,Width=s.Width,Surface=s.Surface,
                Gradient=s.Gradient==null?null:s.Gradient.Copy(),CycleWorld=s.CycleWorld,
                Points=s.Points.Select(p=>new InkPoint{X=p.X,Y=p.Y,Distance=p.Distance}).ToList(),CachedBounds=InkGeometry.Bounds(s)}).ToList();
            this.cuts=cuts;this.bounds=bounds;this.diameter=diameter;
        }
        internal InkEraseResult Execute(CancellationToken cancellation)
        {
            var result=new InkEraseResult{Timing=new InkTiming()};InkTiming.Current=result.Timing;
            try
            {
                for(int i=0;i<source.Count;i++)
                {
                    cancellation.ThrowIfCancellationRequested();var stroke=source[i];var box=InkGeometry.Bounds(stroke);
                    var local=new Media.GeometryGroup{FillRule=Media.FillRule.Nonzero};
                    for(int j=0;j<cuts.Length;j++)if(box.IntersectsWith(bounds[j]))local.Children.Add(cuts[j]);
                    InkStroke next=stroke;
                    if(local.Children.Count>0){local.Freeze();next=InkSurface.Subtract(stroke,local,diameter);}
                    if(!ReferenceEquals(stroke,next)){result.HasChange=true;result.Changed.Add(i);}
                    if(next!=null){result.Strokes.Add(next);result.OriginalIndices.Add(i);}
                }
                cancellation.ThrowIfCancellationRequested();
                InkTiming.Measure(()=>{InkGeometry.Validate(result.Strokes);return true;},(m,t)=>m.Validate+=t);
                result.Serialized=InkTiming.Measure(()=>MapLogic.Serializer().Serialize(result.Strokes),(m,t)=>m.Serialize+=t);
                return result;
            }finally{InkTiming.Current=null;}
        }
    }
}
