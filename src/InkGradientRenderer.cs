using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
namespace HexMapEditor
{
    internal sealed class InkGradientTrace
    {
        internal readonly List<InkPoint> Points;internal readonly GradientSampler Palette;internal readonly double Cycle;internal readonly float Width;
        internal InkGradientTrace(InkStroke stroke){Points=stroke.Points;Palette=new GradientSampler(stroke.Gradient);Cycle=stroke.CycleWorld;Width=stroke.Width;}
    }
    internal static class InkGradientRenderer
    {
        const int Side=256;
        sealed class Tile : IDisposable
        {
            internal InkGradientTrace Trace;internal int X,Y,Count;internal double Scale;
            internal readonly Bitmap Image=new Bitmap(Side,Side,PixelFormat.Format32bppArgb);
            internal readonly float[] Distances=Enumerable.Repeat(Single.PositiveInfinity,Side*Side).ToArray();
            internal readonly int[] Pixels=new int[Side*Side];
            internal void Update()
            {
                var list=Trace.Points;if(Count==list.Count)return;
                for(int n=Count;n<list.Count;n++)
                {
                    var a=list[Math.Max(0,n-1)];var b=list[n];double ax=a.X*Scale-X*Side,ay=a.Y*Scale-Y*Side,bx=b.X*Scale-X*Side,by=b.Y*Scale-Y*Side,dx=bx-ax,dy=by-ay,len2=dx*dx+dy*dy,r=Trace.Width*Scale/2+3;
                    int y0=Math.Max(0,(int)Math.Max(Int32.MinValue,Math.Floor(Math.Min(ay,by)-r))),y1=Math.Min(Side-1,(int)Math.Min(Int32.MaxValue,Math.Ceiling(Math.Max(ay,by)+r)));if(y1<y0)continue;
                    for(int y=y0;y<=y1;y++)
                    {
                        double lo=0,hi=1;if(Math.Abs(dy)>.00001){lo=Math.Max(0,Math.Min((y+.5-r-ay)/dy,(y+.5+r-ay)/dy));hi=Math.Min(1,Math.Max((y+.5-r-ay)/dy,(y+.5+r-ay)/dy));if(lo>hi)continue;}
                        int x0=Math.Max(0,(int)Math.Max(Int32.MinValue,Math.Floor(Math.Min(ax+dx*lo,ax+dx*hi)-r))),x1=Math.Min(Side-1,(int)Math.Min(Int32.MaxValue,Math.Ceiling(Math.Max(ax+dx*lo,ax+dx*hi)+r)));
                        for(int x=x0;x<=x1;x++){double t=len2<.000001?1:Math.Max(0,Math.Min(1,((x+.5-ax)*dx+(y+.5-ay)*dy)/len2)),px=ax+t*dx,py=ay+t*dy,d=(x+.5-px)*(x+.5-px)+(y+.5-py)*(y+.5-py);int i=y*Side+x;if(d>Distances[i])continue;Distances[i]=(float)d;Pixels[i]=Trace.Palette.Cycle((a.Distance+(b.Distance-a.Distance)*t)/Trace.Cycle).ToArgb();}
                    }
                }
                Count=list.Count;var data=Image.LockBits(new Rectangle(0,0,Side,Side),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);try{Marshal.Copy(Pixels,0,data.Scan0,Pixels.Length);}finally{Image.UnlockBits(data);}
            }
            public void Dispose(){Image.Dispose();}
        }
        // 32 tiles x (ARGB bitmap + ARGB working data + distance) = 24 MiB, independent of world size.
        static readonly LinkedList<Tile> tiles=new LinkedList<Tile>();
        internal static void Clear(){foreach(var t in tiles)t.Dispose();tiles.Clear();}
        internal static void Validate(InkStroke stroke)
        {
            if(stroke.Gradient==null){if(stroke.CycleWorld!=0)throw new InvalidDataException("Przebieg koloru bez gradientu.");return;}
            GradientRules.Validate(stroke.Gradient);
            if(!GraphicSources.Finite(stroke.CycleWorld)||stroke.CycleWorld<=0||stroke.CycleWorld>3000000000||stroke.Points.Count<1)throw new InvalidDataException("Nieprawidłowa światowa długość cyklu kreski.");
            double previous=0;for(int i=0;i<stroke.Points.Count;i++){var p=stroke.Points[i];if(!GraphicSources.Finite(p.Distance)||p.Distance<0||p.Distance>300000000000||i==0&&p.Distance!=0)throw new InvalidDataException("Nieprawidłowy przebieg drogi kreski.");if(i>0){var a=stroke.Points[i-1];double d=Math.Sqrt((p.X-a.X)*(double)(p.X-a.X)+(p.Y-a.Y)*(double)(p.Y-a.Y));if(Math.Abs(p.Distance-previous-d)>.001+Math.Abs(p.Distance)*1e-7)throw new InvalidDataException("Przebieg gradientu nie odpowiada zapisanej drodze.");}previous=p.Distance;}
        }
        internal static void Draw(Graphics graphics,InkStroke stroke)
        {
            if(stroke.ColorTrace==null)stroke.ColorTrace=new InkGradientTrace(stroke);
            var trace=stroke.ColorTrace;var visible=RectangleF.Intersect(graphics.VisibleClipBounds,InkGeometry.Bounds(stroke));if(visible.IsEmpty)return;
            float[] e;using(var transform=graphics.Transform)e=transform.Elements;double zoom=Math.Sqrt(e[0]*e[0]+e[1]*e[1]);double scale=Math.Max(.000001,Math.Min(64,Math.Pow(2,Math.Floor(Math.Log(Math.Max(.000001,zoom),2)))));
            int x0=(int)Math.Floor(visible.Left*scale/Side),x1=(int)Math.Floor(visible.Right*scale/Side),y0=(int)Math.Floor(visible.Top*scale/Side),y1=(int)Math.Floor(visible.Bottom*scale/Side);
            for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
            {
                var node=tiles.First;while(node!=null&&!(ReferenceEquals(node.Value.Trace,trace)&&node.Value.X==x&&node.Value.Y==y&&node.Value.Scale==scale))node=node.Next;
                if(node==null){if(tiles.Count>=32){tiles.First.Value.Dispose();tiles.RemoveFirst();}node=tiles.AddLast(new Tile{Trace=trace,X=x,Y=y,Scale=scale});}else{tiles.Remove(node);tiles.AddLast(node);}
                var tile=node.Value;tile.Update();var state=graphics.Save();
                try
                {
                    var box=new RectangleF((float)(x*Side/scale),(float)(y*Side/scale),(float)(Side/scale),(float)(Side/scale));graphics.SetClip(box,CombineMode.Intersect);
                    using(var texture=new TextureBrush(tile.Image,WrapMode.TileFlipXY))using(var matrix=new Matrix((float)(1/scale),0,0,(float)(1/scale),box.X,box.Y))
                    {texture.Transform=matrix;if(stroke.Surface!=null)graphics.FillPath(texture,InkPathCache.Get(stroke));else if(stroke.Points.Count==1){var p=stroke.Points[0];graphics.FillEllipse(texture,p.X-stroke.Width/2,p.Y-stroke.Width/2,stroke.Width,stroke.Width);}else using(var pen=new Pen(texture,stroke.Width){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round})graphics.DrawLines(pen,stroke.Points.Select(p=>p.Point).ToArray());}
                }finally{graphics.Restore(state);}
            }
        }
    }
}

