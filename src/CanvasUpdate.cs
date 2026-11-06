using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HexMapEditor
{
    // A synchronous native redraw boundary, not SuspendLayout alone: SetBounds
    // may otherwise paint/copy the old client area between the camera and HWND move.
    internal sealed class CanvasUpdate : IDisposable
    {
        readonly Control canvas;
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int message, IntPtr w, IntPtr l);
        public CanvasUpdate(Control canvas, bool changing)
        {
            if (changing && canvas.IsHandleCreated && canvas.Visible) { this.canvas = canvas; SendMessage(canvas.Handle, 0x000B, IntPtr.Zero, IntPtr.Zero); }
        }
        public void Dispose()
        {
            if (canvas == null || canvas.IsDisposed) return;
            SendMessage(canvas.Handle, 0x000B, new IntPtr(1), IntPtr.Zero);
            canvas.Invalidate(true); canvas.Update();
        }
    }
}
