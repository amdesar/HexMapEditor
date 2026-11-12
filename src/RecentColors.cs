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
        internal static void Apply(RadioButton control)
        {
            control.FlatStyle=FlatStyle.Flat;control.UseVisualStyleBackColor=false;
            Action sync=()=>{bool active=control.Checked;control.BackColor=active?Color.FromArgb(38,65,96):Color.FromArgb(49,54,67);control.ForeColor=Color.WhiteSmoke;control.FlatAppearance.BorderColor=active?Color.FromArgb(90,145,205):Color.FromArgb(74,81,98);control.FlatAppearance.BorderSize=active?2:1;control.FlatAppearance.CheckedBackColor=Color.FromArgb(38,65,96);control.FlatAppearance.MouseOverBackColor=active?Color.FromArgb(43,77,114):Color.FromArgb(60,67,82);};
            control.CheckedChanged+=(s,e)=>sync();sync();
        }
    }
}
