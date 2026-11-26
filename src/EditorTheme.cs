using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace HexMapEditor
{
    public class DarkForm:Form
    {
        protected override void OnLoad(EventArgs e){EditorTheme.FormReady(this);base.OnLoad(e);}
        protected override void OnDeactivate(EventArgs e){NumberInputRouter.CommitFor(this);base.OnDeactivate(e);}
    }
    internal class DarkTextBox:TextBox
    {
        public DarkTextBox(){BackColor=Color.FromArgb(41,46,58);ForeColor=Color.WhiteSmoke;BorderStyle=BorderStyle.FixedSingle;}
        protected override void OnGotFocus(EventArgs e){BackColor=Color.FromArgb(39,55,78);base.OnGotFocus(e);Invalidate();}
        protected override void OnLostFocus(EventArgs e){BackColor=Color.FromArgb(41,46,58);base.OnLostFocus(e);Invalidate();}
    }
    internal static class EditorTheme
    {
        internal static readonly Color DisabledText=Color.FromArgb(167,175,189),ErrorText=Color.FromArgb(255,145,145),LinkText=Color.FromArgb(137,192,250);
        [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr h,int attr,ref int value,int size);
        internal static void Caption(Form form){try{int yes=1;if(DwmSetWindowAttribute(form.Handle,20,ref yes,4)!=0)DwmSetWindowAttribute(form.Handle,19,ref yes,4);}catch(DllNotFoundException){}catch(EntryPointNotFoundException){} }
        internal static void Apply(Control root)
        {
            if(root is RichNoteEditor||root.GetType().FullName.StartsWith("System.Windows.Forms.Integration."))return;
            if(root is Form){root.BackColor=Color.FromArgb(34,38,48);root.ForeColor=Color.WhiteSmoke;}
            var text=root as TextBoxBase;if(text!=null){text.BackColor=Color.FromArgb(41,46,58);text.ForeColor=Color.WhiteSmoke;}
            var combo=root as ComboBox;if(combo!=null){combo.BackColor=Color.FromArgb(41,46,58);combo.ForeColor=Color.WhiteSmoke;combo.FlatStyle=FlatStyle.Flat;}
            var list=root as ListBox;if(list!=null){list.BackColor=Color.FromArgb(41,46,58);list.ForeColor=Color.WhiteSmoke;}
            var button=root as Button;if(button!=null){if(button.DialogResult==DialogResult.Cancel||button.DialogResult==DialogResult.No)button.CausesValidation=false;if(button.UseVisualStyleBackColor){button.UseVisualStyleBackColor=false;button.FlatStyle=FlatStyle.Flat;ChoiceStyle.Apply(button,false);}}
            if(root is TrackBar||root is TabPage){root.BackColor=Color.FromArgb(34,38,48);root.ForeColor=Color.WhiteSmoke;}
            foreach(Control child in root.Controls)Apply(child);
        }
        internal static void FormReady(Form form){Apply(form);Caption(form);}
    }
}
