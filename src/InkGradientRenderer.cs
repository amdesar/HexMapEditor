using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
namespace HexMapEditor
{
    internal sealed class InkGradientTrace
    {
        internal readonly List<InkPoint> Points;
        internal readonly GradientSampler Palette;
        internal readonly GradientDefinition Definition;
        internal readonly ColorBlend Blend;
        internal readonly double Cycle;
        internal readonly float Width;
        internal InkGradientTrace(InkStroke stroke){Points=stroke.Points;Definition=stroke.Gradient;Palette=new GradientSampler(Definition);Cycle=stroke.CycleWorld;Width=stroke.Width;Blend=new ColorBlend{Colors=Definition.Stops.Select(s=>ColorTranslator.FromHtml(s.Color)).Concat(new[]{ColorTranslator.FromHtml(Definition.Stops[0].Color)}).ToArray(),Positions=Definition.Stops.Select(s=>(float)(s.Position*.8)).Concat(new[]{1f}).ToArray()};}
    }
    internal static class InkGradientRenderer
    {
        const int Side=256;
        static Bitmap scratch;
        internal static long BuiltTiles,PixelVisits,SegmentVisits,PathDraws;
        internal static void ResetMetrics(){BuiltTiles=PixelVisits=SegmentVisits=PathDraws=0;}
        internal static void Clear(){if(scratch!=null){scratch.Dispose();scratch=null;}InkLayerRenderer.Clear();}
        internal static void Validate(InkStroke stroke)
        {
            if(stroke.Gradient==null){if(stroke.CycleWorld!=0)throw new InvalidDataException("Przebieg koloru bez gradientu.");return;}
            GradientRules.Validate(stroke.Gradient);
            if(!GraphicSources.Finite(stroke.CycleWorld)||stroke.CycleWorld<=0||stroke.CycleWorld>3000000000||stroke.Points.Count<1)throw new InvalidDataException("Nieprawidłowa światowa długość cyklu kreski.");
            double previous=0;for(int i=0;i<stroke.Points.Count;i++){var p=stroke.Points[i];if(!GraphicSources.Finite(p.Distance)||p.Distance<0||p.Distance>300000000000||i==0&&p.Distance!=0)throw new InvalidDataException("Nieprawidłowy przebieg drogi kreski.");if(i>0){var a=stroke.Points[i-1];double d=Math.Sqrt((p.X-a.X)*(double)(p.X-a.X)+(p.Y-a.Y)*(double)(p.Y-a.Y));if(Math.Abs(p.Distance-previous-d)>.001+Math.Abs(p.Distance)*1e-7)throw new InvalidDataException("Przebieg gradientu nie odpowiada zapisanej drodze.");}previous=p.Distance;}
        }
        // Native linear RGB brushes, in recorded order. No nearest-axis arbitration or per-pixel scan.
        static void Body(Graphics g,InkGradientTrace trace,InkPoint a,InkPoint b,double zoom,double ox,double oy,double r)
        {
            double ax=a.X*zoom+ox,ay=a.Y*zoom+oy,bx=b.X*zoom+ox,by=b.Y*zoom+oy,dx=bx-ax,dy=by-ay,len=Math.Sqrt(dx*dx+dy*dy);
            if(len<.00001)return;double ux=dx/len,uy=dy/len,cycle=trace.Cycle*zoom,phase=a.Distance/trace.Cycle;
            using(var brush=new LinearGradientBrush(new RectangleF(0,0,(float)cycle,1),Color.Black,Color.White,0f))
            using(var matrix=new Matrix((float)ux,(float)uy,(float)-uy,(float)ux,(float)(ax-ux*(phase-Math.Floor(phase))*cycle),(float)(ay-uy*(phase-Math.Floor(phase))*cycle)))
            {
                brush.InterpolationColors=trace.Blend;brush.WrapMode=WrapMode.Tile;brush.Transform=matrix;
                using(var pen=new Pen(brush,(float)(2*r)){StartCap=LineCap.Flat,EndCap=LineCap.Flat})g.DrawLine(pen,(float)ax,(float)ay,(float)bx,(float)by);
            }
        }
        static void Segment(Graphics g,InkGradientTrace trace,int index,double zoom,double ox,double oy,RectangleF area)
        {
            var a=trace.Points[Math.Max(0,index-1)];var b=trace.Points[index];
            double ax=a.X*zoom+ox,ay=a.Y*zoom+oy,bx=b.X*zoom+ox,by=b.Y*zoom+oy,r=trace.Width*zoom/2+1.5;
            if(Math.Max(ax,bx)+r<area.Left||Math.Min(ax,bx)-r>area.Right||Math.Max(ay,by)+r<area.Top||Math.Min(ay,by)-r>area.Bottom)return;
            SegmentVisits++;
            double dx=bx-ax,dy=by-ay,len=Math.Sqrt(dx*dx+dy*dy);
            if(len<.00001){if(trace.Points.Count==1)using(var dot=new SolidBrush(trace.Palette.Cycle(b.Distance/trace.Cycle)))g.FillEllipse(dot,(float)(bx-r),(float)(by-r),(float)(2*r),(float)(2*r));return;}
            double ux=dx/len,uy=dy/len;
            if(index>1)
            {
                // Round only the join's uncovered sector. Restore the preceding body inside the disc,
                // then paint the current body last; full colored sample-discs must not make beads.
                using(var disc=new GraphicsPath())using(var fill=new SolidBrush(trace.Palette.Cycle(a.Distance/trace.Cycle)))
                {
                    disc.AddEllipse((float)(ax-r),(float)(ay-r),(float)(2*r),(float)(2*r));g.FillPath(fill,disc);
                    var state=g.Save();g.SetClip(disc,CombineMode.Intersect);Body(g,trace,trace.Points[index-2],a,zoom,ox,oy,r);g.Restore(state);
                }
            }
            else Cap(g,trace.Palette.Cycle(a.Distance/trace.Cycle),ax,ay,ux,uy,r,false);
            Body(g,trace,a,b,zoom,ox,oy,r);
            if(index==trace.Points.Count-1)Cap(g,trace.Palette.Cycle(b.Distance/trace.Cycle),bx,by,ux,uy,r,true);
        }
        static void Cap(Graphics g,Color color,double x,double y,double ux,double uy,double r,bool end)
        {
            using(var shape=new GraphicsPath())using(var fill=new SolidBrush(color))
            {
                double angle=Math.Atan2(uy,ux)*180/Math.PI;
                shape.AddPie((float)(x-r),(float)(y-r),(float)(2*r),(float)(2*r),(float)(angle+(end?-90:90)),180);
                g.FillPath(fill,shape);
            }
        }
        internal static void Draw(Graphics graphics,InkStroke stroke)
        {
            if(stroke.ColorTrace==null)stroke.ColorTrace=new InkGradientTrace(stroke);
            var trace=stroke.ColorTrace;
            var visible=RectangleF.Intersect(graphics.VisibleClipBounds,InkGeometry.Bounds(stroke));if(visible.IsEmpty)return;
            float[] e;using(var transform=graphics.Transform)e=transform.Elements;
            double zoom=Math.Sqrt(e[0]*e[0]+e[1]*e[1]);
            // Include the AA fringe and anchor samples to integer device pixels, independent of contour bounds.
            var device=Rectangle.FromLTRB((int)Math.Floor(visible.Left*zoom+e[4]-1),(int)Math.Floor(visible.Top*zoom+e[5]-1),(int)Math.Ceiling(visible.Right*zoom+e[4]+1),(int)Math.Ceiling(visible.Bottom*zoom+e[5]+1));
            if(device.Width<=0||device.Height<=0)return;
            if(scratch==null)scratch=new Bitmap(Side,Side,PixelFormat.Format32bppArgb);
            for(int y=device.Top;y<device.Bottom;y+=Side)for(int x=device.Left;x<device.Right;x+=Side)
            {
                var size=new Size(Math.Min(Side,device.Right-x),Math.Min(Side,device.Bottom-y));BuiltTiles++;
                using(var g=Graphics.FromImage(scratch))
                {
                    // Color field is opaque; actual vector surface below supplies the only antialiased edge.
                    g.Clear(trace.Palette.Cycle(0));g.SmoothingMode=SmoothingMode.None;
                    for(int n=trace.Points.Count==1?0:1;n<trace.Points.Count;n++)Segment(g,trace,n,zoom,e[4]-x,e[5]-y,new RectangleF(0,0,size.Width,size.Height));
                }
                var state=graphics.Save();
                try
                {
                    var box=new RectangleF((float)((x-e[4])/zoom),(float)((y-e[5])/zoom),(float)(size.Width/zoom),(float)(size.Height/zoom));graphics.SetClip(box,CombineMode.Intersect);
                    using(var texture=new TextureBrush(scratch,WrapMode.TileFlipXY))using(var matrix=new Matrix((float)(1/zoom),0,0,(float)(1/zoom),box.X,box.Y))
                    {
                        texture.Transform=matrix;PathDraws++;
                        if(stroke.Surface!=null)graphics.FillPath(texture,InkPathCache.Get(stroke));
                        else if(stroke.Points.Count==1){var p=stroke.Points[0];graphics.FillEllipse(texture,p.X-stroke.Width/2,p.Y-stroke.Width/2,stroke.Width,stroke.Width);}
                        else using(var pen=new Pen(texture,stroke.Width){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round})graphics.DrawLines(pen,stroke.Points.Select(p=>p.Point).ToArray());
                    }
                }finally{graphics.Restore(state);}
            }
        }
    }
}
