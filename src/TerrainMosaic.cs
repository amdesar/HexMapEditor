using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace HexMapEditor
{
    internal static class TerrainMosaic
    {
        internal const int Capacity = 12;
        internal static string Key(TerrainDefinition terrain) { return terrain.TerrainId + ":" + FillRules.Signature(terrain.Variants.OrderBy(f=>f.Id,StringComparer.Ordinal)); }
        internal static List<FillVariant> Representatives(TerrainDefinition terrain)
        {
            return terrain.Variants.OrderByDescending(f=>f.Weight).ThenBy(f => MapLogic.Hash(terrain.TerrainId + ":" + f.Id), StringComparer.Ordinal).Take(Capacity).ToList();
        }
        internal static Bitmap Render(TerrainDefinition terrain, Size size, bool flat,bool low=false)
        {
            var result = new Bitmap(Math.Max(1,size.Width),Math.Max(1,size.Height));
            var decoded = new Dictionary<string,Bitmap>();
            try
            {
                var fills = Representatives(terrain);
                int slot = 0;
                using (var g = Graphics.FromImage(result))
                {
                    g.Clear(ColorTranslator.FromHtml(terrain.Variants[0].Color));
                    float radius = size.Height / (flat ? (float)(2.5*Math.Sqrt(3)) : 3.5f);
                    float dx = flat ? 1.5f*radius : (float)Math.Sqrt(3)*radius;
                    float dy = flat ? (float)Math.Sqrt(3)*radius : 1.5f*radius;
                    int columns=(int)Math.Ceiling((size.Width+radius)/dx);
                    var visible=new List<PointF>();
                    float halfWidth=flat?radius:(float)Math.Sqrt(3)*radius/2;
                    float halfHeight=flat?(float)Math.Sqrt(3)*radius/2:radius;
                    for (int row=-1;row<3;row++) for (int col=-1;col<columns+1;col++)
                    {
                        var center = new PointF(col*dx+(flat ? 0 : row*dx/2), flat ? dy/2+row*dy+(col%2)*dy/2 : radius+row*dy);
                        if(center.X+halfWidth>0&&center.X-halfWidth<size.Width&&center.Y+halfHeight>0&&center.Y-halfHeight<size.Height)visible.Add(center);
                    }
                    var order=Assignments(terrain,visible.Count);
                    foreach(var center in visible)
                    {
                        var fill = fills[order[slot++]];
                        Bitmap image = null;
                        if (!low && fill.Graphic != null && !decoded.TryGetValue(fill.GraphicId,out image)) decoded.Add(fill.GraphicId,image=TerrainTextures.Decode(fill.Graphic,flat));
                        if(low){using(var path=TerrainTextures.Mask(center,radius,flat)){using(var basis=new SolidBrush(ColorTranslator.FromHtml(fill.Color)))g.FillPath(basis,path);using(var brush=new SolidBrush(RepresentativeColors.Get(fill.Color,fill.Graphic,fill.Gradient,flat)))g.FillPath(brush,path);}}
                        else TerrainPreview.PaintHex(g,center,radius,flat,ColorTranslator.FromHtml(fill.Color),image,false,fill.Gradient);
                    }
                }
                return result;
            }
            catch { result.Dispose(); throw; }
            finally { foreach (var bitmap in decoded.Values) bitmap.Dispose(); }
        }
        internal static int[] Assignments(TerrainDefinition terrain,int slots)
        {
            var fills=Representatives(terrain);
            int total=fills.Sum(f=>f.Weight);
            var quotas=fills.Select(f=>slots*(decimal)f.Weight/total).ToArray();
            var counts=quotas.Select(q=>Math.Max(slots>=fills.Count?1:0,(int)q)).ToArray();
            for(int n=counts.Sum();n<slots;n++)counts[Enumerable.Range(0,counts.Length).OrderByDescending(i=>quotas[i]-counts[i]).First()]++;
            for(int n=counts.Sum();n>slots;n--)counts[Enumerable.Range(0,counts.Length).Where(i=>counts[i]>(slots>=fills.Count?1:0)).OrderByDescending(i=>counts[i]-quotas[i]).First()]--;
            var result=counts.SelectMany((count,i)=>Enumerable.Repeat(i,count)).ToArray();
            uint seed=UInt32.Parse(MapLogic.Hash(Key(terrain)).Substring(0,8),NumberStyles.HexNumber,CultureInfo.InvariantCulture);
            for(int i=result.Length-1;i>0;i--){seed^=seed<<13;seed^=seed>>17;seed^=seed<<5;int j=(int)(seed%(uint)(i+1));int value=result[i];result[i]=result[j];result[j]=value;}
            return result;
        }
    }
}
