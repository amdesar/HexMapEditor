using System;
using System.Drawing;

namespace HexMapEditor
{
    internal static class CellLabels
    {
        public const float MinimumScale = .8f;
        static StringFormat Format()
        {
            var format = (StringFormat)StringFormat.GenericTypographic.Clone(); format.FormatFlags |= StringFormatFlags.NoWrap;
            format.Alignment = StringAlignment.Center; format.LineAlignment = StringAlignment.Center; format.Trimming = StringTrimming.None; return format;
        }
        internal static bool Layout(Graphics graphics, string text, PointF center, float radius, float scale, Rectangle viewport, out RectangleF bounds, out float pixels)
        {
            bounds = new RectangleF(center.X - radius * .625f, center.Y + radius * .03f, radius * 1.25f, radius * .5f); pixels = 9.5f * scale;
            if (scale < MinimumScale || String.IsNullOrEmpty(text)) return false;
            using (var format = Format())
            {
                SizeF size; using (var font = new Font("Segoe UI", pixels, FontStyle.Regular, GraphicsUnit.Pixel)) size = graphics.MeasureString(text, font, PointF.Empty, format);
                float factor = Math.Min(1, Math.Min(bounds.Width / size.Width, bounds.Height / size.Height) * .96f); pixels *= factor;
                if (pixels < 7.5f) return false;
                using (var font = new Font("Segoe UI", pixels, FontStyle.Regular, GraphicsUnit.Pixel)) size = graphics.MeasureString(text, font, PointF.Empty, format);
                var ink = new RectangleF(bounds.Left + (bounds.Width - size.Width) / 2, bounds.Top + (bounds.Height - size.Height) / 2, size.Width, size.Height);
                return bounds.Contains(ink) && ((RectangleF)viewport).Contains(ink);
            }
        }
        public static void Draw(Graphics graphics, string text, PointF center, float radius, float scale, Rectangle viewport)
        {
            RectangleF bounds; float pixels; if (!Layout(graphics, text, center, radius, scale, viewport, out bounds, out pixels)) return;
            using (var font = new Font("Segoe UI", pixels, FontStyle.Regular, GraphicsUnit.Pixel)) using (var brush = new SolidBrush(Color.FromArgb(220, 245, 245, 245))) using (var format = Format()) graphics.DrawString(text, font, brush, bounds, format);
        }
    }
}
