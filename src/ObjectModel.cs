using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;

namespace HexMapEditor
{
    public sealed class MapObject
    {
        public string Id { get; set; }
        public string TemplateId { get; set; }
        public string Name { get; set; }
        public string AssetId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Size { get; set; }
        public string Side { get; set; }
        public bool Locked { get; set; }
        public bool RevealsFog { get; set; }
        public int RevealRadius { get; set; }
        public bool LightEnabled {get;set;}
        public int LightRadius {get;set;}
        public MapObject() { Id=Guid.NewGuid().ToString("N"); Name="Obiekt"; Size=30; Side="Neutral"; RevealRadius=1; LightRadius=1; }
        public MapObject Copy() { return (MapObject)MemberwiseClone(); }
    }
    internal sealed class ObjectChange
    {
        public MapObject Before, After;
        public int Index;
        public void Apply(MapData map,bool after)
        {
            var removed=after?Before:After; var added=after?After:Before;
            if(removed!=null) map.Objects.RemoveAll(o=>o.Id==removed.Id);
            if(added!=null) map.Objects.Insert(Math.Min(Index,map.Objects.Count),added.Copy());
        }
    }
    internal static class ObjectGeometry
    {
        public static bool Finite(float n) { return !Single.IsNaN(n)&&!Single.IsInfinity(n); }
        public static void Validate(MapData map,HashSet<string> assets)
        {
            if(map.Objects==null||map.Objects.Count>5000)throw new InvalidDataException("Nieprawidłowa lista obiektów (limit 5000).");
            var ids=new HashSet<string>();
            if(map.ObjectStates==null||map.Groups==null)throw new InvalidDataException("Brak stanów lub grup.");
            foreach(var o in map.Objects) if(o==null||String.IsNullOrWhiteSpace(o.Id)||o.Id.Length>200||!ids.Add(o.Id)||String.IsNullOrWhiteSpace(o.TemplateId)||o.TemplateId.Length>200||String.IsNullOrWhiteSpace(o.Name)||o.Name.Length>200||!assets.Contains(o.AssetId??"")||!Finite(o.X)||!Finite(o.Y)||Math.Abs(o.X)>10000000||Math.Abs(o.Y)>10000000||!Finite(o.Size)||o.Size<8||o.Size>1024||!map.ObjectStates.Any(s=>s!=null&&s.Id==o.Side)||o.RevealRadius<0||o.RevealRadius>20||o.LightRadius<0||o.LightRadius>20)throw new InvalidDataException("Nieprawidłowe dane instancji obiektu.");
            GroupRules.Validate(map);
        }
        public static RectangleF Bounds(MapObject o,IconAsset asset)
        { float ratio=o.Size/Math.Max(asset.Width,asset.Height);return new RectangleF(o.X-asset.Width*ratio/2,o.Y-asset.Height*ratio/2,asset.Width*ratio,asset.Height*ratio); }
        // Infinite offset grid, including negative indices. No synthetic cells enter the document.
        public static Point Nearest(MapData map,PointF world)
        {
            int c,r; double step=Math.Sqrt(3)*28;
            if(map.Orientation=="PointyTop"){r=(int)Math.Round(world.Y/42);c=(int)Math.Round(world.X/step-((r+map.RowParity)&1)*.5);}
            else{c=(int)Math.Round(world.X/42);r=(int)Math.Round(world.Y/step-((c+map.ColumnParity)&1)*.5);}
            Point result=new Point(c,r);double best=Double.MaxValue;
            for(int y=r-1;y<=r+1;y++)for(int x=c-1;x<=c+1;x++){var p=BrushGeometry.Center(map,x,y);double d=(p.X-world.X)*(p.X-world.X)+(p.Y-world.Y)*(p.Y-world.Y);if(d<best){best=d;result=new Point(x,y);}}
            return result;
        }
        public static GraphicsPath Hex(MapData map,PointF center,float radius)
        {
            var p=new PointF[6];for(int i=0;i<6;i++){double a=(60*i+(map.Orientation=="FlatTop"?0:-90))*Math.PI/180;p[i]=new PointF(center.X+radius*(float)Math.Cos(a),center.Y+radius*(float)Math.Sin(a));}
            var path=new GraphicsPath();path.AddPolygon(p);return path;
        }
        public static int Hit(MapData map,PointF world)
        {
            var p=Nearest(map,world);if(p.X<0||p.Y<0||p.X>=map.Columns||p.Y>=map.Rows)return -1;
            using(var path=Hex(map,BrushGeometry.Center(map,p.X,p.Y),28.0001f))return path.IsVisible(world)?p.Y*map.Columns+p.X:-1;
        }
        public static PointF Transform(MapData before,MapData after,PointF point,int dc,int dr)
        {
            var p=Nearest(before,point);var a=BrushGeometry.Center(before,p.X,p.Y);var b=BrushGeometry.Center(after,p.X+dc,p.Y+dr);float x=point.X-a.X,y=point.Y-a.Y;
            // Hex-local offset follows the 30 degree change of the polygon axes.
            // Keeping raw screen XY would move near-edge points into a different hex.
            if(before.Orientation!=after.Orientation){float sin=before.Orientation=="PointyTop"?.5f:-.5f;float nx=x*.8660254f-y*sin;y=x*sin+y*.8660254f;x=nx;}
            return new PointF(b.X+x,b.Y+y);
        }
        public static HashSet<int> Reveal(MapData map,MapObject obj,PointF? from)
        {
            var result=new HashSet<int>();if(!GroupRules.Allows(map,obj.Side)||!obj.RevealsFog)return result;
            var end=new PointF(obj.X,obj.Y);var start=from??end;float dx=end.X-start.X,dy=end.Y-start.Y;
            // Clip sampling to a conservative map envelope, not to the nearest cell.
            // Very distant off-map objects must not create millions of empty samples.
            float t0=0,t1=1;float maxX=map.Columns*(map.Orientation=="FlatTop"?42:48.49743f)+56,maxY=map.Rows*(map.Orientation=="FlatTop"?48.49743f:42)+56;
            if(!Clip(-dx,start.X+28,ref t0,ref t1)||!Clip(dx,maxX-start.X,ref t0,ref t1)||!Clip(-dy,start.Y+28,ref t0,ref t1)||!Clip(dy,maxY-start.Y,ref t0,ref t1))return result;
            end=new PointF(start.X+dx*t1,start.Y+dy*t1);start=new PointF(start.X+dx*t0,start.Y+dy*t0);dx=end.X-start.X;dy=end.Y-start.Y;
            int steps=Math.Max(1,(int)Math.Ceiling(Math.Sqrt(dx*dx+dy*dy)/9));
            for(int s=0;s<=steps;s++){
                var point=new PointF(start.X+dx*s/steps,start.Y+dy*s/steps);int seed=Hit(map,point);if(seed<0)continue;
                var seen=new HashSet<int>{seed};var queue=new Queue<Tuple<int,int>>();queue.Enqueue(Tuple.Create(seed,0));
                while(queue.Count>0){var current=queue.Dequeue();result.Add(current.Item1);if(current.Item2>=obj.RevealRadius)continue;foreach(int n in MapLogic.Neighbors(map,current.Item1))if(seen.Add(n))queue.Enqueue(Tuple.Create(n,current.Item2+1));}
            }
            return result;
        }
        static bool Clip(float p,float q,ref float lower,ref float upper){if(p==0)return q>=0;float t=q/p;if(p<0){if(t>upper)return false;lower=Math.Max(lower,t);}else{if(t<lower)return false;upper=Math.Min(upper,t);}return true;}
    }
}
