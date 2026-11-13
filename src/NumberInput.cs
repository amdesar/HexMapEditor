using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class NumberTextBox:TextBox
    {
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SendMessage(IntPtr h,int message,IntPtr w,string text);
        internal static bool Draft(string text)
        {bool separator=false;for(int i=0;i<text.Length;i++){char c=text[i];if(c>='0'&&c<='9')continue;if(c=='-'&&i==0)continue;if((c=='.'||c==',')&&!separator){separator=true;continue;}return false;}return true;}
        internal bool Insert(string text){string next=Text.Remove(SelectionStart,SelectionLength).Insert(SelectionStart,text);if(!Draft(next))return false;SendMessage(Handle,0xc2,(IntPtr)1,text);return true;}
        protected override void OnKeyPress(KeyPressEventArgs e){if(!Char.IsControl(e.KeyChar)){string next=Text.Remove(SelectionStart,SelectionLength).Insert(SelectionStart,e.KeyChar.ToString());if(!Draft(next))e.Handled=true;}base.OnKeyPress(e);}
        protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Decimal&&e.Modifiers==Keys.None){Insert(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);e.Handled=e.SuppressKeyPress=true;}base.OnKeyDown(e);}
        protected override void WndProc(ref Message m){if(m.Msg==0x302){try{Insert(Clipboard.GetText(TextDataFormat.UnicodeText));}catch(System.Runtime.InteropServices.ExternalException){}return;}base.WndProc(ref m);}
    }
    // Application-local message filter, not a mouse hook. Registered before editor filters.
    internal sealed class NumberInputRouter:IMessageFilter
    {
        static readonly HashSet<ScrubNumber> active=new HashSet<ScrubNumber>();static bool installed;
        internal static event Action<Control> PointerDown;
        internal static void Ensure(){if(!installed){Application.AddMessageFilter(new NumberInputRouter());installed=true;}}
        internal static void Enter(ScrubNumber number){active.Add(number);}
        internal static void Leave(ScrubNumber number){active.Remove(number);}
        internal static bool Within(Control target,Control parent){for(var c=target;c!=null;c=c.Parent)if(c==parent)return true;return false;}
        internal static void CommitFor(Control parent){foreach(var n in active.ToArray())if(!n.IsDisposed&&Within(n,parent))n.CommitText();}
        public bool PreFilterMessage(ref Message m){if(m.Msg==0x201||m.Msg==0x204||m.Msg==0x207||m.Msg==0xa1){var target=Control.FromChildHandle(m.HWnd);foreach(var n in active.ToArray())if(!n.IsDisposed&&!Within(target,n))n.CommitText();if(PointerDown!=null)PointerDown(target);}return false;}
    }
}
