using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace HexMapEditor
{
    internal enum BrushShape { Point, Square, Rectangle, Circle, Hexagon, Pentagon, Horizontal, Vertical, Line, Triangle, Octagon }
    internal static class BrushGeometry
    {
        internal static bool InsideCircle(double dx,double dy,double radius)
        {return dx*dx+dy*dy<=radius*radius+.01;}

        internal static IEnumerable<int> CircleCells(MapData map,Point grid,double radius)
        {
            int extent=(int)Math.Ceiling(radius/42)+2;
            var origin=Center(map,grid.X,grid.Y);
            for(int row=Math.Max(0,grid.Y-extent);row<=Math.Min(map.Rows-1,grid.Y+extent);row++)
                for(int column=Math.Max(0,grid.X-extent);column<=Math.Min(map.Columns-1,grid.X+extent);column++)
                {
                    var center=Center(map,column,row);
                    if(InsideCircle(center.X-origin.X,center.Y-origin.Y,radius))yield return row*map.Columns+column;
                }
        }
        public static PointF Center(MapData map, int c, int r)
        {
            return map.Orientation == "FlatTop" ? new PointF(42 * c, (float)(Math.Sqrt(3) * 28 * (r + ((c + map.ColumnParity) & 1) * .5))) : new PointF((float)(Math.Sqrt(3) * 28 * (c + ((r + map.RowParity) & 1) * .5)), 42 * r);
        }
        static Point Axial(MapData map, int c, int r) { return map.Orientation == "PointyTop" ? new Point(c - (r + map.RowParity - ((r + map.RowParity) & 1)) / 2, r) : new Point(c, r - (c + map.ColumnParity - ((c + map.ColumnParity) & 1)) / 2); }
        static Point[] Neighbors(MapData map, Point p)
        {
            int shift = map.Orientation == "PointyTop" ? (((p.Y + map.RowParity) & 1) == 1 ? 1 : -1) : (((p.X + map.ColumnParity) & 1) == 1 ? 1 : -1);
            return map.Orientation == "PointyTop" ? new[] { new Point(p.X - 1, p.Y), new Point(p.X + 1, p.Y), new Point(p.X, p.Y - 1), new Point(p.X + shift, p.Y - 1), new Point(p.X, p.Y + 1), new Point(p.X + shift, p.Y + 1) } : new[] { new Point(p.X, p.Y - 1), new Point(p.X, p.Y + 1), new Point(p.X - 1, p.Y), new Point(p.X - 1, p.Y + shift), new Point(p.X + 1, p.Y), new Point(p.X + 1, p.Y + shift) };
        }
        public static List<int> Stamp(MapData map, int start, BrushShape shape, int size, int height, bool filled)
        {
            if (start < 0 || start >= map.Cells.Count) return new List<int>();
            return Stamp(map,new Point(start%map.Columns,start/map.Columns),shape,size,height,filled);
        }
        public static List<int> Stamp(MapData map, Point grid, BrushShape shape, int size, int height, bool filled)
        { return Stamp(map,grid,shape,size,height,filled,0,60); }
        public static List<int> Stamp(MapData map, Point grid, BrushShape shape, int size, int height, bool filled, int rotation, int apex)
        {
            size = Math.Max(1, Math.Min(100, size)); height = Math.Max(1, Math.Min(100, height));
            if (shape == BrushShape.Point) return grid.X>=0&&grid.Y>=0&&grid.X<map.Columns&&grid.Y<map.Rows?new List<int> { grid.Y*map.Columns+grid.X }:new List<int>();
            int c = grid.X, r = grid.Y, extent = 2 * Math.Max(size, height) + 3;
            var origin = Center(map, c, r); var axial = Axial(map, c, r); var points = new HashSet<Point>();
            float halfX = size * 24.24871f, halfY = (shape == BrushShape.Rectangle ? height : size) * 24.24871f;
            var candidates = new List<Point>();
            // Include one virtual ring for outline topology; only real cells are returned.
            for (int y = Math.Max(-1,r - extent); y <= Math.Min(map.Rows,r + extent); y++) for (int x = Math.Max(-1,c - extent); x <= Math.Min(map.Columns,c + extent); x++) candidates.Add(new Point(x, y));
            double angle=rotation*Math.PI/180,cos=Math.Cos(angle),sin=Math.Sin(angle);
            if (shape == BrushShape.Horizontal || shape == BrushShape.Vertical || shape==BrushShape.Line)
            {
                float length = (size - 1) * 48.49742f;
                int steps = Math.Max(1, (int)(length / 8));
                for (int step = 0; step <= steps; step++)
                {
                    float offset = -length / 2 + length * step / steps; double direction=angle+(shape==BrushShape.Vertical?Math.PI/2:0);var target = new PointF(origin.X + (float)(offset*Math.Cos(direction)), origin.Y + (float)(offset*Math.Sin(direction)));
                    Point nearest=ObjectGeometry.Nearest(map,target);
                    points.Add(nearest);
                }
            }
            else foreach (var p in candidates)
            {
                var center = Center(map, p.X, p.Y); double dx = center.X - origin.X, dy = center.Y - origin.Y,x=dx*cos+dy*sin,y=-dx*sin+dy*cos; bool inside;
                if (shape == BrushShape.Hexagon) { double q,s;if(map.Orientation=="PointyTop"){s=y/42;q=x/48.4974226-s/2;}else{q=x/42;s=y/48.4974226-q/2;}inside=Math.Max(Math.Abs(q),Math.Max(Math.Abs(s),Math.Abs(q+s)))<=size-0.5; }
                else if (shape == BrushShape.Circle) inside = InsideCircle(x,y,halfX);
                else if(shape==BrushShape.Triangle)
                {
                    double a=Math.Max(1,Math.Min(179,apex))*Math.PI/360,L=size*48.4974226,h=L*Math.Cos(a),b=L*Math.Sin(a);
                    inside=InPolygon(x,y,new[]{new PointF(0,(float)(-2*h/3)),new PointF((float)b,(float)(h/3)),new PointF((float)-b,(float)(h/3))});
                }
                else if (shape == BrushShape.Pentagon || shape==BrushShape.Octagon)
                {
                    inside = true;
                    int n=shape==BrushShape.Pentagon?5:8;for (int i = 0; i < n; i++) { double a = (i * 360.0/n - 90) * Math.PI / 180, b = ((i + 1) * 360.0/n - 90) * Math.PI / 180; double ax = halfX * Math.Cos(a), ay = halfX * Math.Sin(a), bx = halfX * Math.Cos(b), by = halfX * Math.Sin(b); if ((bx - ax) * (y - ay) - (by - ay) * (x - ax) < -.01) { inside = false; break; } }
                }
                else inside = Math.Abs(x) <= halfX + .01 && Math.Abs(y) <= halfY + .01;
                if (inside) points.Add(p);
            }
            if (!filled && shape != BrushShape.Horizontal && shape != BrushShape.Vertical && shape!=BrushShape.Line) points = new HashSet<Point>(points.Where(p => Neighbors(map, p).Any(n => !points.Contains(n))).ToArray());
            return points.Where(p => p.X >= 0 && p.Y >= 0 && p.X < map.Columns && p.Y < map.Rows).Select(p => p.Y * map.Columns + p.X).OrderBy(i => i).ToList();
        }
        static bool InPolygon(double x,double y,PointF[] vertices){for(int i=0;i<vertices.Length;i++){var a=vertices[i];var b=vertices[(i+1)%vertices.Length];if((b.X-a.X)*(y-a.Y)-(b.Y-a.Y)*(x-a.X)<-.01)return false;}return true;}
    }
}
