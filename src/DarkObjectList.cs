using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class DarkObjectList : ListView
    {
        HeaderWindow header;
        [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr hwnd,int msg,IntPtr wParam,IntPtr lParam);
        protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);header=new HeaderWindow(this);header.AssignHandle(SendMessage(Handle,0x101f,IntPtr.Zero,IntPtr.Zero));}
        protected override void OnHandleDestroyed(EventArgs e){if(header!=null){header.ReleaseHandle();header=null;}base.OnHandleDestroyed(e);}
        sealed class HeaderWindow : NativeWindow
        {
            readonly DarkObjectList owner;
            [StructLayout(LayoutKind.Sequential)]struct Rect{internal int Left,Top,Right,Bottom;}
            [DllImport("user32.dll")]static extern bool GetClientRect(IntPtr hwnd,out Rect rect);
            [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr hwnd,int msg,IntPtr wParam,ref Rect rect);
            internal HeaderWindow(DarkObjectList owner){this.owner=owner;}
            protected override void WndProc(ref Message m)
            {
                if(m.Msg==0x14){using(var g=Graphics.FromHdc(m.WParam))using(var b=new SolidBrush(Color.FromArgb(49,54,67)))g.FillRectangle(b,g.VisibleClipBounds);m.Result=new IntPtr(1);return;}
                base.WndProc(ref m);
                if(m.Msg!=0xf&&m.Msg!=0x318)return;
                Rect bounds,last=new Rect();GetClientRect(Handle,out bounds);
                int start=0;if(owner.Columns.Count>0){SendMessage(Handle,0x1207,new IntPtr(owner.Columns.Count-1),ref last);start=Math.Max(0,last.Right);}
                if(start>=bounds.Right)return;
                using(var g=m.Msg==0x318?Graphics.FromHdc(m.WParam):Graphics.FromHwnd(Handle))using(var brush=new SolidBrush(Color.FromArgb(49,54,67)))g.FillRectangle(brush,start,0,bounds.Right-start,bounds.Bottom);
            }
        }
    }
}
