using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace HexMapEditor
{
    internal enum BrushShape { Point, Square, Rectangle, Circle, Hexagon, Pentagon, Horizontal, Vertical }
    internal static class BrushGeometry
    {
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
        {
            size = Math.Max(1, Math.Min(15, size)); height = Math.Max(1, Math.Min(15, height));
            if (shape == BrushShape.Point) return grid.X>=0&&grid.Y>=0&&grid.X<map.Columns&&grid.Y<map.Rows?new List<int> { grid.Y*map.Columns+grid.X }:new List<int>();
            int c = grid.X, r = grid.Y, extent = 2 * Math.Max(size, height) + 3;
            var origin = Center(map, c, r); var axial = Axial(map, c, r); var points = new HashSet<Point>();
            float halfX = size * 24.24871f, halfY = (shape == BrushShape.Rectangle ? height : size) * 24.24871f;
            var candidates = new List<Point>();
            for (int y = r - extent; y <= r + extent; y++) for (int x = c - extent; x <= c + extent; x++) candidates.Add(new Point(x, y));
            if (shape == BrushShape.Horizontal || shape == BrushShape.Vertical)
            {
                float length = (size - 1) * 48.49742f;
                int steps = Math.Max(1, (int)(length / 8));
                for (int step = 0; step <= steps; step++)
                {
                    float offset = -length / 2 + length * step / steps; var target = new PointF(origin.X + (shape == BrushShape.Horizontal ? offset : 0), origin.Y + (shape == BrushShape.Vertical ? offset : 0));
                    double best = Double.MaxValue; Point nearest = new Point(c, r);
                    foreach (var p in candidates) { var v = Center(map, p.X, p.Y); double distance = (v.X - target.X) * (v.X - target.X) + (v.Y - target.Y) * (v.Y - target.Y); if (distance < best) { best = distance; nearest = p; } }
                    points.Add(nearest);
                }
            }
            else foreach (var p in candidates)
            {
                var center = Center(map, p.X, p.Y); double x = center.X - origin.X, y = center.Y - origin.Y; bool inside;
                if (shape == BrushShape.Hexagon) { var a = Axial(map, p.X, p.Y); int q = a.X - axial.X, s = a.Y - axial.Y; inside = Math.Max(Math.Abs(q), Math.Max(Math.Abs(s), Math.Abs(q + s))) < size; }
                else if (shape == BrushShape.Circle) inside = x * x + y * y <= halfX * halfX + .01;
                else if (shape == BrushShape.Pentagon)
                {
                    inside = true;
                    for (int i = 0; i < 5; i++) { double a = (i * 72 - 90) * Math.PI / 180, b = ((i + 1) * 72 - 90) * Math.PI / 180; double ax = halfX * Math.Cos(a), ay = halfX * Math.Sin(a), bx = halfX * Math.Cos(b), by = halfX * Math.Sin(b); if ((bx - ax) * (y - ay) - (by - ay) * (x - ax) < -.01) { inside = false; break; } }
                }
                else inside = Math.Abs(x) <= halfX + .01 && Math.Abs(y) <= halfY + .01;
                if (inside) points.Add(p);
            }
            if (points.Count == 0) points.Add(new Point(c, r));
            if (!filled && shape != BrushShape.Horizontal && shape != BrushShape.Vertical) points = new HashSet<Point>(points.Where(p => Neighbors(map, p).Any(n => !points.Contains(n))).ToArray());
            return points.Where(p => p.X >= 0 && p.Y >= 0 && p.X < map.Columns && p.Y < map.Rows).Select(p => p.Y * map.Columns + p.X).OrderBy(i => i).ToList();
        }
    }
}
