using System;
using System.Drawing;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class DetailsScrollPanel : Panel
    {
        internal Control Content;
        protected override void OnLayout(LayoutEventArgs e)
        {
            // Resize before ScrollableControl measures its virtual extent. A Resize
            // handler runs too late when shrinking and leaves the old horizontal extent.
            if (Content != null) Content.Width = Math.Max(200, ClientSize.Width - 48);
            base.OnLayout(e);
        }
    }
    public sealed partial class MainWindow
    {
        int requestedRightWidth = 440, rightResizeOrigin, rightResizeStart;
        bool rightResizing;
        readonly Panel rightResizeGrip = new Panel { Width = 7, Dock = DockStyle.Left, Cursor = Cursors.SizeWE, BackColor = Color.FromArgb(78, 89, 112), AccessibleName = "Zmień szerokość panelu szczegółów" };
        int ClampRightWidth(int width) { return Math.Max(340, Math.Min(Math.Max(340, Math.Min(ClientSize.Width - 280, (int)(ClientSize.Width * .72))), width)); }
        void InitializePanelResize(Panel header)
        {
            var reset = new Button { Dock = DockStyle.Right, Width = 34, FlatStyle = FlatStyle.Flat, Image = EditorIcons.Small("fit"), AccessibleName = "Przywróć szerokość domyślną" };
            StyleButton(reset); tips.SetToolTip(reset, reset.AccessibleName); reset.Click += (s, e) => { EndRightResize(); EndCanvasGesture(true); SetRightWidth(440); }; header.Controls.Add(reset);
            tips.SetToolTip(rightResizeGrip, rightResizeGrip.AccessibleName); right.Controls.Add(rightResizeGrip);
            rightResizeGrip.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) BeginRightResize(rightResizeGrip.PointToScreen(e.Location).X); };
            rightResizeGrip.MouseMove += (s, e) => { if (rightResizing) MoveRightResize(rightResizeGrip.PointToScreen(e.Location).X); };
            rightResizeGrip.MouseUp += (s, e) => { if (e.Button == MouseButtons.Left) EndRightResize(); };
            rightResizeGrip.MouseCaptureChanged += (s, e) => { if (rightResizing && !rightResizeGrip.Capture) EndRightResize(); };
        }
        void SetRightWidth(int width)
        {
            Rectangle previous = right.Bounds; requestedRightWidth = ClampRightWidth(width); PositionOverlays();
            if (!right.Visible || previous == right.Bounds) return;
            // Moving an overlay with an ElementHost does not repaint the buffered
            // sibling canvas during a captured drag. Repaint just the revealed strip
            // now, before another mouse move can copy stale child-window pixels.
            if (right.Left > previous.Left)
            {
                var exposed = Rectangle.Intersect(canvas.ClientRectangle, new Rectangle(previous.Left, 0, right.Left - previous.Left, canvas.Height));
                if (!exposed.IsEmpty) { canvas.Invalidate(exposed); canvas.Update(); }
            }
            right.Update();
        }
        void BeginRightResize(int screenX)
        {
            if (modalDepth > 0) return; EndCanvasGesture(true); rightResizing = true; waitingForMouseRelease = true; requireFreshCanvasDown = true;
            rightResizeOrigin = screenX; rightResizeStart = right.Width; rightResizeGrip.Capture = true;
        }
        void MoveRightResize(int screenX) { if (rightResizing) SetRightWidth(rightResizeStart + rightResizeOrigin - screenX); }
        void EndRightResize()
        {
            if (!rightResizing) return; rightResizing = false; rightResizeGrip.Capture = false;
            waitingForMouseRelease = Control.MouseButtons != MouseButtons.None; requireFreshCanvasDown = true;
        }
    }
}
