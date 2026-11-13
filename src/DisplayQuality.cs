using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
namespace HexMapEditor
{
    // Applied appearances are immutable. Only small colors, never source images, survive sampling.
    internal static class RepresentativeColors
    {
        sealed class Signature { internal string Value; }
        static readonly ConditionalWeakTable<GradientDefinition,Signature> signatures=new ConditionalWeakTable<GradientDefinition,Signature>();
        static readonly Dictionary<string,Color> colors=new Dictionary<string,Color>();
        static readonly Queue<string> order=new Queue<string>();
        internal static int Count {get{return colors.Count;}}
        internal static Color Get(string color,GraphicDefinition graphic,GradientDefinition gradient,bool flat)
        {
            if(graphic==null&&gradient==null)return ColorTranslator.FromHtml(color);
            string key=(graphic!=null?graphic.Id:signatures.GetValue(gradient,g=>new Signature{Value=MapLogic.Serializer().Serialize(g)}).Value)+":"+flat;
            Color cached;if(colors.TryGetValue(key,out cached))return cached;
            using(var bitmap=new Bitmap(64,64))using(var g=Graphics.FromImage(bitmap))using(var mask=TerrainTextures.Mask(new PointF(32,32),31,flat))
            {
                if(graphic!=null){using(var source=GraphicSources.Decode(graphic.Source)){CropMath.Draw(g,source,flat?graphic.Flat:graphic.Pointy,new RectangleF(1,1,62,62));}}
                // The mask selects sample centers; do not average antialias coverage at its artificial 64px boundary.
                double dx=gradient==null?0:Math.Cos(gradient.Angle*Math.PI/180),dy=gradient==null?0:Math.Sin(gradient.Angle*Math.PI/180);
                double extent=gradient==null?1:mask.PathPoints.Max(p=>Math.Abs((p.X-32)*dx+(p.Y-32)*dy));
                long a=0,r=0,green=0,b=0,n=0;
                for(int y=0;y<64;y++)for(int x=0;x<64;x++)if(mask.IsVisible(x+.5f,y+.5f)){var p=graphic!=null?bitmap.GetPixel(x,y):GradientRules.At(gradient,.5+((x+.5-32)*dx+(y+.5-32)*dy)/(2*extent));n++;a+=p.A;r+=p.R*p.A;green+=p.G*p.A;b+=p.B*p.A;}
                cached=a==0?Color.Transparent:Color.FromArgb((int)((a+n/2)/n),(int)((r+a/2)/a),(int)((green+a/2)/a),(int)((b+a/2)/a));
            }
            while(colors.Count>=256)colors.Remove(order.Dequeue());colors.Add(key,cached);order.Enqueue(key);return cached;
        }
    }
    public sealed partial class MainWindow
    {
        bool lowQuality;
        Button qualityButton;
        void SetLowQuality(bool low)
        {
            if(BlockInkCommit())return;EndCanvasGesture(true);if(BlockInkCommit()||lowQuality==low)return;
            lowQuality=low;InkLayerRenderer.Clear();UpdateQualityButton();UpdateTerrainSelection();canvas.Invalidate();
        }
        void ResetQuality(){lowQuality=false;InkLayerRenderer.Clear();UpdateQualityButton();UpdateTerrainSelection();}
        void UpdateQualityButton(){if(qualityButton==null)return;qualityButton.Text=lowQuality?"Low":"High";qualityButton.AccessibleDescription=lowQuality?"Niska jakość":"Wysoka jakość";Accent(qualityButton,lowQuality,LowAccent);UpdateCommandState();}
    }
}
