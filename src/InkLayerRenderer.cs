using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
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
        internal static void Clear(){if(image!=null)image.Dispose();image=null;snapshot=null;camera=null;}
        internal static void Draw(Graphics g,IList<InkStroke> strokes,InkStroke active,Rectangle viewport)
        {
            var complete=strokes.Where(s=>!ReferenceEquals(s,active)).ToArray();
            if(complete.Length==0){Clear();if(active!=null&&g.VisibleClipBounds.IntersectsWith(InkGeometry.Bounds(active)))InkGeometry.Draw(g,active);return;}
            float[] transform;using(var m=g.Transform)transform=m.Elements;
            bool same=image!=null&&viewport==device&&transform.SequenceEqual(camera)&&complete.SequenceEqual(snapshot);
            if(!same)
            {
                Clear();
                if(viewport.Width>0&&viewport.Height>0&&(long)viewport.Width*viewport.Height<=5800000)
                {
                    image=new Bitmap(viewport.Width,viewport.Height,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                    using(var local=Graphics.FromImage(image))using(var m=new Matrix(transform[0],transform[1],transform[2],transform[3],transform[4]-viewport.X,transform[5]-viewport.Y))
                    {local.Transform=m;local.SmoothingMode=SmoothingMode.AntiAlias;foreach(var s in complete)if(local.VisibleClipBounds.IntersectsWith(InkGeometry.Bounds(s)))InkGeometry.Draw(local,s);}
                    snapshot=complete;camera=transform;device=viewport;
                }
            }
            if(image!=null){var saved=g.Save();g.ResetTransform();g.DrawImageUnscaled(image,device.Location);g.Restore(saved);}
            else foreach(var s in complete)if(g.VisibleClipBounds.IntersectsWith(InkGeometry.Bounds(s)))InkGeometry.Draw(g,s);
            if(active!=null&&g.VisibleClipBounds.IntersectsWith(InkGeometry.Bounds(active)))InkGeometry.Draw(g,active);
        }
    }
}
