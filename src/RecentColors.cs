using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal static class RecentColors
    {
        static readonly List<Color> values=new List<Color>();
        internal static event Action Changed;
        internal static Color[] Values {get{return values.ToArray();}}
        internal static void Add(Color color){color=Color.FromArgb(color.R,color.G,color.B);values.RemoveAll(c=>c.ToArgb()==color.ToArgb());values.Insert(0,color);if(values.Count>12)values.RemoveAt(12);if(Changed!=null)Changed();}
        internal static void Clear(){values.Clear();if(Changed!=null)Changed();}
    }
    internal static class ChoiceStyle
    {
        internal static readonly Color ActiveBack=Color.FromArgb(38,65,96),ActiveBorder=Color.FromArgb(90,145,205),ActiveHover=Color.FromArgb(43,77,114),NeutralBack=Color.FromArgb(49,54,67),NeutralBorder=Color.FromArgb(74,81,98);
        internal static void Apply(Button control,bool active){control.ForeColor=Color.WhiteSmoke;control.BackColor=active?ActiveBack:NeutralBack;control.FlatAppearance.BorderColor=active?ActiveBorder:NeutralBorder;control.FlatAppearance.BorderSize=active?2:1;control.FlatAppearance.MouseOverBackColor=active?ActiveHover:Color.FromArgb(60,67,82);}
        internal static void Apply(RadioButton control)
        {
            control.FlatStyle=FlatStyle.Flat;control.UseVisualStyleBackColor=false;
            Action sync=()=>{bool active=control.Checked;control.BackColor=active?ActiveBack:NeutralBack;control.ForeColor=Color.WhiteSmoke;control.FlatAppearance.BorderColor=active?ActiveBorder:NeutralBorder;control.FlatAppearance.BorderSize=active?2:1;control.FlatAppearance.CheckedBackColor=ActiveBack;control.FlatAppearance.MouseOverBackColor=active?ActiveHover:Color.FromArgb(60,67,82);};
            control.CheckedChanged+=(s,e)=>sync();sync();
        }
    }
}
