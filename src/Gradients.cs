using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
namespace HexMapEditor
{
    public sealed class GradientStop
    {
        public double Position {get;set;}
        public string Color {get;set;}
        internal GradientStop Copy(){return (GradientStop)MemberwiseClone();}
    }
    public sealed class GradientDefinition
    {
        public List<GradientStop> Stops {get;set;}
        public double Angle {get;set;}
        public GradientDefinition(){Stops=new List<GradientStop>();}
        internal GradientDefinition Copy(){return new GradientDefinition{Angle=Angle,Stops=Stops.Select(s=>s.Copy()).ToList()};}
    }
    internal static class GradientRules
    {
        internal static void Budget(IEnumerable<GradientDefinition> gradients,int limit,string scope){if(gradients.Where(g=>g!=null).Sum(g=>(long)(g.Stops==null?limit+1:g.Stops.Count))>limit)throw new InvalidDataException("Limit punktów gradientów "+scope+": "+limit+".");}
        internal static GradientDefinition Two(string color){return new GradientDefinition{Stops=new List<GradientStop>{new GradientStop{Position=0,Color=color},new GradientStop{Position=1,Color="#FFFFFF"}}};}
        internal static GradientDefinition Rainbow(){return new GradientDefinition{Stops=new[]{"#FF0000","#FFFF00","#00FF00","#00FFFF","#0000FF","#FF00FF","#FF0000"}.Select((c,i)=>new GradientStop{Position=i/6.0,Color=c}).ToList()};}
        internal static void Validate(GradientDefinition gradient)
        {
            if(gradient==null)return;
            if(gradient.Stops==null||gradient.Stops.Count<2||gradient.Stops.Count>16||!GraphicSources.Finite(gradient.Angle)||gradient.Angle<0||gradient.Angle>360)throw new InvalidDataException("Gradient wymaga 2–16 punktów i kąta 0–360°.");
            double previous=-1;
            foreach(var stop in gradient.Stops){if(stop==null||!GraphicSources.Finite(stop.Position)||stop.Position<0||stop.Position>1||stop.Position-previous<.01-1e-9||!MapLogic.ValidColor(stop.Color))throw new InvalidDataException("Punkty gradientu: poprawne kolory, kolejność i odstęp co najmniej 0,01.");previous=stop.Position;}
            if(gradient.Stops[0].Position!=0||gradient.Stops.Last().Position!=1)throw new InvalidDataException("Skrajne punkty gradientu muszą być na 0 i 1.");
        }
        internal static bool Same(GradientDefinition a,GradientDefinition b){if(ReferenceEquals(a,b))return true;if(a==null||b==null||a.Angle!=b.Angle||a.Stops.Count!=b.Stops.Count)return false;return a.Stops.Select((s,i)=>s.Position==b.Stops[i].Position&&String.Equals(s.Color,b.Stops[i].Color,StringComparison.OrdinalIgnoreCase)).All(v=>v); }
        internal static Color Lerp(Color a,Color b,double t){return Color.FromArgb((int)Math.Round(a.R+(b.R-a.R)*t),(int)Math.Round(a.G+(b.G-a.G)*t),(int)Math.Round(a.B+(b.B-a.B)*t));}
        internal static Color At(GradientDefinition gradient,double position)
        {
            position=Math.Max(0,Math.Min(1,position));var stops=gradient.Stops;
            for(int i=1;i<stops.Count;i++)if(position<=stops[i].Position)return Lerp(ColorTranslator.FromHtml(stops[i-1].Color),ColorTranslator.FromHtml(stops[i].Color),(position-stops[i-1].Position)/(stops[i].Position-stops[i-1].Position));
            return ColorTranslator.FromHtml(stops.Last().Color);
        }
        // Last fifth is a visible return section; the user's final stop is never overwritten.
        internal static Color Cycle(GradientDefinition gradient,double phase)
        {double t=phase-Math.Floor(phase);return t<=.8?At(gradient,t/.8):Lerp(At(gradient,1),At(gradient,0),(t-.8)/.2);}
        internal static void Fill(Graphics graphics,GraphicsPath mask,GradientDefinition gradient)
        {
            var bounds=mask.GetBounds();double angle=gradient.Angle*Math.PI/180,dx=Math.Cos(angle),dy=Math.Sin(angle);
            double extent=mask.PathPoints.Max(p=>Math.Abs((p.X-(bounds.Left+bounds.Width/2))*dx+(p.Y-(bounds.Top+bounds.Height/2))*dy));extent=Math.Max(.001,extent);
            var center=new PointF(bounds.Left+bounds.Width/2,bounds.Top+bounds.Height/2);
            using(var brush=new LinearGradientBrush(new RectangleF(0,0,(float)(2*extent),1),Color.Black,Color.White,0f))
            using(var transform=new Matrix((float)dx,(float)dy,(float)-dy,(float)dx,center.X-(float)(dx*extent),center.Y-(float)(dy*extent)))
            {brush.Transform=transform;brush.WrapMode=WrapMode.TileFlipXY;brush.InterpolationColors=new ColorBlend{Colors=gradient.Stops.Select(s=>ColorTranslator.FromHtml(s.Color)).ToArray(),Positions=gradient.Stops.Select(s=>(float)s.Position).ToArray()};graphics.FillPath(brush,mask);}
        }
        internal static Bitmap Thumbnail(GradientDefinition gradient,Size size)
        {var image=new Bitmap(size.Width,size.Height);using(var g=Graphics.FromImage(image))using(var path=new GraphicsPath()){path.AddRectangle(new Rectangle(Point.Empty,size));Fill(g,path,gradient);}return image;}
    }
    internal sealed class GradientSampler
    {
        readonly double[] positions;readonly Color[] colors;
        internal GradientSampler(GradientDefinition definition){positions=definition.Stops.Select(s=>s.Position).ToArray();colors=definition.Stops.Select(s=>ColorTranslator.FromHtml(s.Color)).ToArray();}
        internal Color At(double t){for(int i=1;i<positions.Length;i++)if(t<=positions[i])return GradientRules.Lerp(colors[i-1],colors[i],Math.Max(0,(t-positions[i-1])/(positions[i]-positions[i-1])));return colors.Last();}
        internal Color Cycle(double phase){double t=phase-Math.Floor(phase);return t<=.8?At(t/.8):GradientRules.Lerp(colors.Last(),colors[0],(t-.8)/.2);}
    }
}
