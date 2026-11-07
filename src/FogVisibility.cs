using System;
using System.Collections.Generic;
using System.Drawing;

namespace HexMapEditor
{
    internal static class FogVisibility
    {
        const double Radius = 28;
        static readonly double Width = Radius * Math.Sqrt(3);
        static readonly double[,] Pointy = {
            { 0, -Radius }, { Width / 2, -Radius / 2 }, { Width / 2, Radius / 2 },
            { 0, Radius }, { -Width / 2, Radius / 2 }, { -Width / 2, -Radius / 2 }
        };

        public static double VisibleFraction(MapData map, RectangleF bounds)
        {
            double area = (double)bounds.Width * bounds.Height;
            if (area <= 0) return 1;
            bool flat = map.Orientation == "FlatTop";
            double stepX = flat ? 42 : Width;
            double stepY = flat ? Width : 42;
            int firstCol = Math.Max(0, (int)Math.Floor((bounds.Left - 2 * Radius) / stepX));
            int lastCol = Math.Min(map.Columns - 1, (int)Math.Ceiling((bounds.Right + Radius) / stepX));
            int firstRow = Math.Max(0, (int)Math.Floor((bounds.Top - 2 * Radius) / stepY));
            int lastRow = Math.Min(map.Rows - 1, (int)Math.Ceiling((bounds.Bottom + Radius) / stepY));
            double covered = 0;
            for (int row = firstRow; row <= lastRow; row++)
            for (int col = firstCol; col <= lastCol; col++)
            {
                if (!map.Cells[row * map.Columns + col].Fog) continue;
                double x = stepX * (col + (flat ? 0 : ((row + map.RowParity) & 1) * .5));
                double y = stepY * (row + (flat ? ((col + map.ColumnParity) & 1) * .5 : 0));
                if (x + Radius <= bounds.Left || x - Radius >= bounds.Right ||
                    y + Radius <= bounds.Top || y - Radius >= bounds.Bottom) continue;
                // Ideal hexagons share edges only. Painting adds a slight overlap to cover seams.
                covered += IntersectionArea(bounds, x, y, flat);
            }
            double visible = 1 - covered / area;
            if (visible < 1e-10) return 0;
            if (visible > 1 - 1e-10) return 1;
            return Math.Abs(visible - .5) < 1e-12 ? .5 : visible;
        }

        struct Vertex
        {
            public double X, Y;
            public Vertex(double x, double y) { X = x; Y = y; }
        }

        static double IntersectionArea(RectangleF bounds, double x, double y, bool flat)
        {
            var polygon = new List<Vertex>(6);
            for (int i = 0; i < 6; i++)
                polygon.Add(new Vertex(Pointy[i, flat ? 1 : 0], Pointy[i, flat ? 0 : 1]));
            polygon = Clip(polygon, true, bounds.Left - x, true);
            polygon = Clip(polygon, true, bounds.Left + (double)bounds.Width - x, false);
            polygon = Clip(polygon, false, bounds.Top - y, true);
            polygon = Clip(polygon, false, bounds.Top + (double)bounds.Height - y, false);
            double twiceArea = 0;
            for (int i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                twiceArea += a.X * b.Y - b.X * a.Y;
            }
            return Math.Abs(twiceArea) * .5;
        }

        static List<Vertex> Clip(List<Vertex> input, bool horizontal, double limit, bool keepGreater)
        {
            if (input.Count == 0) return input;
            var output = new List<Vertex>(input.Count + 1);
            var previous = input[input.Count - 1];
            foreach (var current in input)
            {
                double a = (horizontal ? previous.X : previous.Y) - limit;
                double b = (horizontal ? current.X : current.Y) - limit;
                bool insideA = keepGreater ? a >= 0 : a <= 0;
                bool insideB = keepGreater ? b >= 0 : b <= 0;
                if (insideA != insideB)
                {
                    double t = a / (a - b);
                    output.Add(new Vertex(previous.X + (current.X - previous.X) * t,
                        previous.Y + (current.Y - previous.Y) * t));
                }
                if (insideB) output.Add(current);
                previous = current;
            }
            return output;
        }

        public static bool MasksDetails(double visible, bool preview) { return !preview && visible <= .5; }

        public static string Label(double visible)
        {
            if (visible == 1) return "Widoczny";
            if (visible == 0) return "Ukryty";
            return "Częściowo ukryty (" + Math.Round((1 - visible) * 100, MidpointRounding.AwayFromZero) + "% ukrycia)";
        }
    }
}
