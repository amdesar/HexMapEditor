using System.Drawing;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class FitButtonRenderer : ToolStripProfessionalRenderer
    {
        public FitButtonRenderer() : base(new FitMenuColors()) { }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(e.ToolStrip.BackColor))
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var bounds = new Rectangle(Point.Empty, e.Item.Size);
            if (e.Item.Selected || e.Item.Pressed)
                using (var brush = new SolidBrush(Color.FromArgb(65, 73, 90))) e.Graphics.FillRectangle(brush, bounds);
            using (var pen = new Pen(Color.FromArgb(74, 81, 98)))
                e.Graphics.DrawRectangle(pen, 0, 0, bounds.Width - 1, bounds.Height - 1);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }
    }

    internal sealed class FitMenuColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground{get{return Color.FromArgb(34,38,48);}}
        public override Color ImageMarginGradientBegin{get{return ToolStripDropDownBackground;}}
        public override Color ImageMarginGradientMiddle{get{return ToolStripDropDownBackground;}}
        public override Color ImageMarginGradientEnd{get{return ToolStripDropDownBackground;}}
        public override Color MenuItemSelected{get{return Color.FromArgb(70,84,112);}}
        public override Color MenuItemBorder{get{return Color.FromArgb(115,135,174);}}
        public override Color MenuBorder{get{return Color.FromArgb(74,81,98);}}
    }
}
