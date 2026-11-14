using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HexMapEditor
{
    // Only the main editor extends its client area into the native DWM caption.
    // The HWND retains WS_CAPTION, WS_THICKFRAME, the system menu and caption buttons.
    public sealed partial class MainWindow
    {
        int captionHeight, captionMenuEnd, captionButtonsLeft;
        Rectangle captionNameBounds;
        bool captionReady;
        Rectangle? nativeRestoreBounds;
        bool nativeNonNormal;
        internal Action<string> CaptionTrace;
        [StructLayout(LayoutKind.Sequential)] internal struct CaptionRect { public int Left,Top,Right,Bottom; public Rectangle Rectangle { get{return Rectangle.FromLTRB(Left,Top,Right,Bottom);} } }
        [StructLayout(LayoutKind.Sequential)] struct FrameMargins { public int Left,Right,Top,Bottom; }
        [StructLayout(LayoutKind.Sequential)] struct TitleBarInfo
        {
            public int Size;public CaptionRect Bar;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst=6)]public int[] States;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst=6)]public CaptionRect[] Rects;
        }
        [DllImport("user32.dll",EntryPoint="SendMessage")] static extern IntPtr CaptionMessage(IntPtr h,int msg,IntPtr w,ref TitleBarInfo info);
        [DllImport("dwmapi.dll")] static extern int DwmExtendFrameIntoClientArea(IntPtr h,ref FrameMargins m);
        [DllImport("dwmapi.dll")] static extern bool DwmDefWindowProc(IntPtr h,int msg,IntPtr w,IntPtr l,out IntPtr result);
        [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr h,int attr,out CaptionRect r,int size);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h,out CaptionRect r);
        [DllImport("user32.dll")] static extern int GetSystemMetrics(int index);
        [DllImport("user32.dll")] static extern bool IsZoomed(IntPtr h);
        [DllImport("user32.dll")] static extern bool IsIconic(IntPtr h);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int cx,int cy,uint flags);

        void InitializeCaption()
        {
            captionReady=true;
            shellToolbar.Paint+=PaintEditorCaption;
            TextChanged+=(s,e)=>{if(quickStrip!=null)LayoutCaptionBars();};
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if(captionReady){ExtendCaption();SetWindowPos(Handle,IntPtr.Zero,0,0,0,0,0x37);}
        }
        void ExtendCaption()
        {
            if(!captionReady||!IsHandleCreated)return;
            var m=new FrameMargins{Top=Math.Max(SystemInformation.CaptionHeight,captionHeight)};
            DwmExtendFrameIntoClientArea(Handle,ref m);
        }
        void LayoutCaptionBars()
        {
            int gap=Math.Max(6,Font.Height/2),icon=Math.Max(16,Font.Height+2);
            captionHeight=Math.Max(SystemInformation.CaptionHeight,Font.Height+16);
            int appWidth=TextRenderer.MeasureText("HexMapEditor",Font,Size.Empty,TextFormatFlags.NoPadding).Width;
            int menuLeft=gap+icon+gap+appWidth+gap;
            int menuWidth=mainMenu.Padding.Horizontal+mainMenu.Items.Cast<ToolStripItem>().Sum(i=>i.GetPreferredSize(Size.Empty).Width+i.Margin.Horizontal);
            captionMenuEnd=menuLeft+menuWidth;
            captionButtonsLeft=shellToolbar.Width-SystemInformation.CaptionButtonSize.Width*3;
            CaptionRect buttons,outer;
            if(IsHandleCreated&&DwmGetWindowAttribute(Handle,5,out buttons,Marshal.SizeOf(typeof(CaptionRect)))==0&&buttons.Right>buttons.Left&&GetWindowRect(Handle,out outer))
                captionButtonsLeft=PointToClient(new Point(outer.Left+buttons.Left,outer.Top+buttons.Top)).X;
            int qh=Math.Max(36,quickStrip.Font.Height+20);
            shellToolbar.Height=captionHeight+qh;
            mainMenu.SetBounds(menuLeft,Math.Max(0,(captionHeight-Math.Max(27,Font.Height+10))/2),menuWidth,Math.Max(27,Font.Height+10));
            quickStrip.SetBounds(0,captionHeight,shellToolbar.ClientSize.Width,qh);
            int left=captionMenuEnd+gap,right=captionButtonsLeft-gap,available=right-left;
            int desired=TextRenderer.MeasureText(CaptionMapName,Font,Size.Empty,TextFormatFlags.NoPadding).Width+4;
            int width=Math.Min(desired,Math.Max(0,available));
            captionNameBounds=available<80?Rectangle.Empty:new Rectangle(Math.Max(left,Math.Min((shellToolbar.Width-width)/2,right-width)),0,width,captionHeight);
            ((CaptionToolbar)shellToolbar).CaptionHeight=captionHeight;
            shellToolbar.Invalidate(new Rectangle(0,0,shellToolbar.Width,captionHeight));
        }
        string CaptionMapName {get {const string prefix="HexMapEditor — ";return Text.StartsWith(prefix,StringComparison.Ordinal)?Text.Substring(prefix.Length):"";}}
        void PaintEditorCaption(object sender,PaintEventArgs e)
        {
            int gap=Math.Max(6,Font.Height/2),icon=Math.Max(16,Font.Height+2);
            if(Icon!=null)e.Graphics.DrawIcon(Icon,new Rectangle(gap,(captionHeight-icon)/2,icon,icon));
            TextRenderer.DrawText(e.Graphics,"HexMapEditor",Font,new Point(gap+icon+gap,(captionHeight-Font.Height)/2),ForeColor,TextFormatFlags.NoPadding);
            if(!captionNameBounds.IsEmpty)TextRenderer.DrawText(e.Graphics,CaptionMapName,Font,captionNameBounds,ForeColor,TextFormatFlags.NoPadding|TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
        }
        protected override void SetBoundsCore(int x,int y,int width,int height,BoundsSpecified specified)
        {
            if(CaptionTrace!=null)CaptionTrace("SetBounds "+new Rectangle(x,y,width,height)+" specified="+specified+" state="+WindowState);
            // Windows has already restored the outer rectangle before WINDOWPOSCHANGED.
            // Form then restores its cached *client* size using AdjustWindowRectEx,
            // which still counts WS_CAPTION although our client includes that area.
            // During that one native transition, retain the OS rectangle (also its
            // new position on drag-from-maximized), not a guessed caption-height delta.
            if(nativeRestoreBounds.HasValue){var r=nativeRestoreBounds.Value;x=r.X;y=r.Y;width=r.Width;height=r.Height;}
            base.SetBoundsCore(x,y,width,height,specified);
        }
        protected override void WndProc(ref Message m)
        {
            if(!captionReady){base.WndProc(ref m);return;}
            if(m.Msg==0x47)
            {
                bool nextNonNormal=IsZoomed(Handle)||IsIconic(Handle);
                if(nativeNonNormal&&!nextNonNormal){CaptionRect r;GetWindowRect(Handle,out r);nativeRestoreBounds=r.Rectangle;}
                nativeNonNormal=nextNonNormal;
                try{base.WndProc(ref m);}finally{nativeRestoreBounds=null;}
                return;
            }
            if(m.Msg==0x83) // NCCALCSIZE: preserve native side/bottom frame, extend only the caption.
            {
                var before=(CaptionRect)Marshal.PtrToStructure(m.LParam,typeof(CaptionRect));
                base.WndProc(ref m);
                var client=(CaptionRect)Marshal.PtrToStructure(m.LParam,typeof(CaptionRect));
                client.Top=before.Top+(IsZoomed(Handle)?SystemInformation.FrameBorderSize.Height+GetSystemMetrics(92):0);
                Marshal.StructureToPtr(client,m.LParam,false);m.Result=IntPtr.Zero;return;
            }
            IntPtr result;
            bool dwmHandled=DwmDefWindowProc(Handle,m.Msg,m.WParam,m.LParam,out result);
            if(dwmHandled&&m.Msg!=0x84){m.Result=result;return;}
            if(m.Msg==0x84)
            {
                var screen=new Point(unchecked((short)(long)m.LParam),unchecked((short)((long)m.LParam>>16)));
                var p=PointToClient(screen);CaptionRect outer;GetWindowRect(Handle,out outer);
                var title=new TitleBarInfo{Size=Marshal.SizeOf(typeof(TitleBarInfo)),States=new int[6],Rects=new CaptionRect[6]};
                CaptionMessage(Handle,0x33f,IntPtr.Zero,ref title);
                foreach(int i in new[]{2,3,5})if(title.Rects[i].Rectangle.Contains(screen)){m.Result=(IntPtr)(i==2?8:i==3?9:20);return;}
                if(dwmHandled&&result!=IntPtr.Zero&&result!=(IntPtr)1){m.Result=result;return;}
                int border=SystemInformation.FrameBorderSize.Height+GetSystemMetrics(92);
                if(!IsZoomed(Handle)&&screen.Y<outer.Top+border){m.Result=(IntPtr)(screen.X<outer.Left+border?13:screen.X>=outer.Right-border?14:12);return;}
                if(p.Y>=0&&p.Y<captionHeight&&p.X>=captionMenuEnd&&p.X<captionButtonsLeft){m.Result=(IntPtr)2;return;}
            }
            base.WndProc(ref m);
            if(m.Msg==0x6||m.Msg==0x320)ExtendCaption();
        }
    }
    internal sealed class CaptionToolbar:Panel
    {
        internal int CaptionHeight;
        internal CaptionToolbar(){DoubleBuffered=true;}
        protected override void WndProc(ref Message m)
        {
            if(m.Msg==0x84){var p=PointToClient(new Point(unchecked((short)(long)m.LParam),unchecked((short)((long)m.LParam>>16))));if(p.Y<CaptionHeight){m.Result=(IntPtr)(-1);return;}}
            base.WndProc(ref m);
        }
    }
}
