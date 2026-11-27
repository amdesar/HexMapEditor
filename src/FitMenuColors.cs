using System.Drawing;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class FitMenuRenderer : ToolStripProfessionalRenderer
    {
        internal FitMenuRenderer() : base(new FitMenuColors()) { }
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = e.Item.Enabled ? Color.WhiteSmoke : Color.FromArgb(145, 149, 158);
            base.OnRenderArrow(e);
        }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {if(!e.Item.Enabled)e.TextColor=EditorTheme.DisabledText;base.OnRenderItemText(e);}
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            var rect=Rectangle.Inflate(e.ImageRectangle,-1,-1);
            using(var background=new SolidBrush(ChoiceStyle.ActiveBack))e.Graphics.FillRectangle(background,rect);
            using(var pen=new Pen(e.Item.Enabled?Color.WhiteSmoke:EditorTheme.DisabledText,2))
                e.Graphics.DrawLines(pen,new[]{new Point(rect.Left+3,rect.Top+rect.Height/2),
                    new Point(rect.Left+rect.Width/2-1,rect.Bottom-4),new Point(rect.Right-2,rect.Top+3)});
        }
    }
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
            {
                e.Graphics.DrawRectangle(pen, 0, 0, bounds.Width - 1, bounds.Height - 1);
                if(e.Item.Placement==ToolStripItemPlacement.Main&&bounds.Width>bounds.Height)
                    e.Graphics.DrawLine(pen,bounds.Height,2,bounds.Height,bounds.Bottom-3);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {e.ArrowColor=e.Item.Enabled?Color.WhiteSmoke:EditorTheme.DisabledText;base.OnRenderArrow(e);}
    }

    internal sealed class FitMenuColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground{get{return Color.FromArgb(34,38,48);}}
        public override Color ImageMarginGradientBegin{get{return ToolStripDropDownBackground;}}
        public override Color ImageMarginGradientMiddle{get{return ToolStripDropDownBackground;}}
        public override Color ImageMarginGradientEnd{get{return ToolStripDropDownBackground;}}
        public override Color MenuItemSelected{get{return Color.FromArgb(70,84,112);}}
        public override Color MenuItemSelectedGradientBegin{get{return MenuItemSelected;}}
        public override Color MenuItemSelectedGradientEnd{get{return MenuItemSelected;}}
        public override Color MenuItemPressedGradientBegin{get{return Color.FromArgb(45,57,77);}}
        public override Color MenuItemPressedGradientMiddle{get{return MenuItemPressedGradientBegin;}}
        public override Color MenuItemPressedGradientEnd{get{return MenuItemPressedGradientBegin;}}
        public override Color MenuStripGradientBegin{get{return ToolStripDropDownBackground;}}
        public override Color MenuStripGradientEnd{get{return ToolStripDropDownBackground;}}
        public override Color ToolStripBorder{get{return Color.FromArgb(74,81,98);}}
        public override Color ToolStripGradientBegin{get{return ToolStripDropDownBackground;}}
        public override Color ToolStripGradientMiddle{get{return ToolStripDropDownBackground;}}
        public override Color ToolStripGradientEnd{get{return ToolStripDropDownBackground;}}
        public override Color MenuItemBorder{get{return Color.FromArgb(115,135,174);}}
        public override Color MenuBorder{get{return Color.FromArgb(74,81,98);}}
    }
}
