using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
namespace HexMapEditor
{
    // Cache only completed, immutable strokes in the current device view; active ink remains separate.
    // Together with the 256px scratch field this stays below the former 24 MiB raster budget.
    internal static class InkLayerRenderer
    {
        static Bitmap image;
        static InkStroke[] snapshot;
        static float[] camera;
        static Rectangle device;
        static bool quality;
        [DllImport("kernel32.dll",EntryPoint="RtlMoveMemory")]static extern void MoveBytes(IntPtr destination,IntPtr source,UIntPtr bytes);
        internal static void Clear(){if(image!=null)image.Dispose();image=null;snapshot=null;camera=null;}
        static void PaintRegion(Rectangle region,InkStroke[] strokes,float[] transform)
        {
            if(region.Width<=0||region.Height<=0)return;
            using(var local=Graphics.FromImage(image))using(var m=new Matrix(transform[0],transform[1],transform[2],transform[3],transform[4]-device.X,transform[5]-device.Y))
            {
                local.SetClip(region);local.CompositingMode=CompositingMode.SourceCopy;using(var empty=new SolidBrush(Color.Transparent))local.FillRectangle(empty,region);local.CompositingMode=CompositingMode.SourceOver;
                local.Transform=m;local.SmoothingMode=SmoothingMode.AntiAlias;foreach(var s in strokes)if(local.VisibleClipBounds.IntersectsWith(InkGeometry.Bounds(s)))InkGeometry.Draw(local,s,quality);
            }
        }
        static bool Pan(InkStroke[] strokes,float[] transform,Rectangle viewport)
        {
            if(image==null||viewport!=device||!strokes.SequenceEqual(snapshot)||!transform.Take(4).SequenceEqual(camera.Take(4)))return false;
            double x=transform[4]-camera[4],y=transform[5]-camera[5];int dx=(int)Math.Round(x),dy=(int)Math.Round(y);
            if(Math.Abs(x-dx)>.0001||Math.Abs(y-dy)>.0001||Math.Abs(dx)>=image.Width||Math.Abs(dy)>=image.Height)return false;
            var data=image.LockBits(new Rectangle(0,0,image.Width,image.Height),System.Drawing.Imaging.ImageLockMode.ReadWrite,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            try
            {
                int rows=image.Height-Math.Abs(dy),width=image.Width-Math.Abs(dx);
                for(int i=dy>0?rows-1:0;dy>0?i>=0:i<rows;i+=dy>0?-1:1)
                {
                    var destination=IntPtr.Add(data.Scan0,(Math.Max(0,dy)+i)*data.Stride+Math.Max(0,dx)*4);
                    var source=IntPtr.Add(data.Scan0,(Math.Max(0,-dy)+i)*data.Stride+Math.Max(0,-dx)*4);
                    MoveBytes(destination,source,new UIntPtr((uint)(width*4)));
                }
            }finally{image.UnlockBits(data);}
            if(dx!=0)PaintRegion(new Rectangle(dx>0?0:image.Width+dx,0,Math.Abs(dx),image.Height),strokes,transform);
            if(dy!=0)PaintRegion(new Rectangle(dx>0?dx:0,dy>0?0:image.Height+dy,image.Width-Math.Abs(dx),Math.Abs(dy)),strokes,transform);
            camera=transform;return true;
        }
        static void Preview(Graphics g,InkStroke[] strokes,float[] transform,Rectangle viewport)
        {
            double ratio=transform[0]/camera[0];var target=new RectangleF((float)(device.X*ratio+transform[4]-camera[4]*ratio),(float)(device.Y*ratio+transform[5]-camera[5]*ratio),(float)(image.Width*ratio),(float)(image.Height*ratio));
            var saved=g.Save();g.ResetTransform();g.InterpolationMode=InterpolationMode.HighQualityBilinear;
            using(var attributes=new System.Drawing.Imaging.ImageAttributes()){attributes.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(image,new[]{new PointF(target.Left,target.Top),new PointF(target.Right,target.Top),new PointF(target.Left,target.Bottom)},new RectangleF(0,0,image.Width,image.Height),GraphicsUnit.Pixel,attributes);}g.Restore(saved);
            // Freshly exposed strips, not the full offscreen drawing. Preview pixels never become the exact cache.
            var inner=Rectangle.Intersect(viewport,Rectangle.FromLTRB((int)Math.Ceiling(target.Left),(int)Math.Ceiling(target.Top),(int)Math.Floor(target.Right),(int)Math.Floor(target.Bottom)));
            var strips=inner.IsEmpty?new[]{viewport}:new[]{new Rectangle(viewport.Left,viewport.Top,viewport.Width,inner.Top-viewport.Top),new Rectangle(viewport.Left,inner.Bottom,viewport.Width,viewport.Bottom-inner.Bottom),new Rectangle(viewport.Left,inner.Top,inner.Left-viewport.Left,inner.Height),new Rectangle(inner.Right,inner.Top,viewport.Right-inner.Right,inner.Height)};
            foreach(var strip in strips)if(strip.Width>0&&strip.Height>0)
            {
                var state=g.Save();g.ResetTransform();g.SetClip(strip,CombineMode.Intersect);using(var m=new Matrix(transform[0],transform[1],transform[2],transform[3],transform[4],transform[5]))g.Transform=m;
                foreach(var s in strokes)if(g.VisibleClipBounds.IntersectsWith(InkGeometry.Bounds(s)))InkGeometry.Draw(g,s,quality);g.Restore(state);
            }
        }
        internal static void Draw(Graphics g,IList<InkStroke> strokes,InkStroke active,Rectangle viewport,bool interaction=false,bool low=false)
        {
            if(image!=null&&quality!=low)Clear();quality=low;
            var complete=strokes.Where(s=>!ReferenceEquals(s,active)).ToArray();
            if(complete.Length==0){Clear();if(active!=null&&g.VisibleClipBounds.IntersectsWith(InkGeometry.Bounds(active)))InkGeometry.Draw(g,active,quality);return;}
            float[] transform;using(var m=g.Transform)transform=m.Elements;
            bool same=image!=null&&viewport==device&&transform.SequenceEqual(camera)&&complete.SequenceEqual(snapshot);
            if(!same&&Pan(complete,transform,viewport))same=true;
            if(!same&&interaction&&image!=null&&viewport==device&&complete.SequenceEqual(snapshot))
            {Preview(g,complete,transform,viewport);if(active!=null)InkGeometry.Draw(g,active,quality);return;}
            if(!same)
            {
                Clear();
                if(viewport.Width>0&&viewport.Height>0&&(long)viewport.Width*viewport.Height<=5800000)
                {
                    image=new Bitmap(viewport.Width,viewport.Height,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                    using(var local=Graphics.FromImage(image))using(var m=new Matrix(transform[0],transform[1],transform[2],transform[3],transform[4]-viewport.X,transform[5]-viewport.Y))
                    {local.Transform=m;local.SmoothingMode=SmoothingMode.AntiAlias;foreach(var s in complete)if(local.VisibleClipBounds.IntersectsWith(InkGeometry.Bounds(s)))InkGeometry.Draw(local,s,quality);}
                    snapshot=complete;camera=transform;device=viewport;
                }
            }
            if(image!=null){var saved=g.Save();g.ResetTransform();g.DrawImageUnscaled(image,device.Location);g.Restore(saved);}
            else foreach(var s in complete)if(g.VisibleClipBounds.IntersectsWith(InkGeometry.Bounds(s)))InkGeometry.Draw(g,s,quality);
            if(active!=null&&g.VisibleClipBounds.IntersectsWith(InkGeometry.Bounds(active)))InkGeometry.Draw(g,active,quality);
        }
    }
}
