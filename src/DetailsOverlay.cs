using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class DetailsOverlay : BufferedPanel
    {
        protected override CreateParams CreateParams
        {
            get
            {
                const int Composited = 0x02000000;
                var parameters = base.CreateParams;
                parameters.ExStyle |= Composited;
                return parameters;
            }
        }

        // Paint the parent and its native child controls in one composited subtree.
        internal void PaintTree()
        {
            if (!IsHandleCreated || !Visible) return;
            const uint Invalidate = 0x0001, Erase = 0x0004, AllChildren = 0x0080, UpdateNow = 0x0100;
            RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero, Invalidate | Erase | AllChildren | UpdateNow);
        }

        [DllImport("user32.dll")]
        static extern bool RedrawWindow(IntPtr handle, IntPtr rect, IntPtr region, uint flags);
    }
}
