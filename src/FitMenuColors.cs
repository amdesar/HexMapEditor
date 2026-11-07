using System.Drawing;
using System.Windows.Forms;
namespace HexMapEditor
{
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
