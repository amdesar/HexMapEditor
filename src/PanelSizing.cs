using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class DetailsDock : BufferedPanel
    {
        [StructLayout(LayoutKind.Sequential)]
        struct NativePosition
        {
            internal IntPtr Window,InsertAfter;
            internal int X,Y,Width,Height,Flags;
        }

        protected override void WndProc(ref Message message)
        {
            const int Changing=0x0046,NoCopyBits=0x0100;
            if(message.Msg==Changing&&message.LParam!=IntPtr.Zero)
            {
                var position=(NativePosition)Marshal.PtrToStructure(message.LParam,typeof(NativePosition));
                position.Flags|=NoCopyBits;
                Marshal.StructureToPtr(position,message.LParam,false);
            }
            base.WndProc(ref message);
        }
    }

    public sealed partial class MainWindow
    {
        int requestedRightWidth = 440, rightResizeOrigin, rightResizeStart;
        bool rightResizing;
        readonly Panel rightResizeGrip = new Panel { Width = 7, Dock = DockStyle.Left, Cursor = Cursors.SizeWE, BackColor = Color.FromArgb(78, 89, 112), AccessibleName = "Zmień szerokość panelu szczegółów" };
        int ClampRightWidth(int width)
        {
            int min=25+20+8+8+SystemInformation.VerticalScrollBarWidth+58+5*MemberMatrix.PropertyWidth(Font);
            return Math.Max(min,Math.Min(Math.Min(ClientSize.Width-280,(int)(ClientSize.Width*.72)),width));
        }
        bool rightCollapsed;
        readonly Button rightToggle = new EditorButton { Width=18, Dock=DockStyle.Left, AccessibleName="Zwiń lub rozwiń szczegóły" };
        ToolStripControlHost detailsCloseHost, detailsResetHost, detailsGroupHost;
        void InitializePanelResize()
        {
            StyleButton(rightToggle);
            tips.SetToolTip(rightToggle,rightToggle.AccessibleName);
            rightToggle.Click+=(s,e)=>{EndRightResize();EndCanvasGesture(true);rightCollapsed=!rightCollapsed;UpdateUI();};
            tips.SetToolTip(rightResizeGrip,rightResizeGrip.AccessibleName);
            right.Controls.Add(rightResizeGrip);right.Controls.Add(rightToggle);
            rightResizeGrip.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) BeginRightResize(rightResizeGrip.PointToScreen(e.Location).X); };
            rightResizeGrip.MouseMove += (s, e) => { if (rightResizing) MoveRightResize(MousePosition.X); };
            rightResizeGrip.MouseUp += (s, e) => { if (e.Button == MouseButtons.Left) { MoveRightResize(MousePosition.X); EndRightResize(); } };
            rightResizeGrip.MouseCaptureChanged += (s, e) => { if (rightResizing && !rightResizeGrip.Capture) EndRightResize(); };
        }
        void AddDetailsCommands()
        {
            detailsCloseHost=DetailsCommand("Zamknij",90,CloseDetailsPanel);
            detailsResetHost=DetailsCommand("Dostosuj",90,()=>{EndRightResize();EndCanvasGesture(true);SetRightWidth(440);});
            detailsGroupHost=DetailsCommand("Grupuj wybrane",132,JoinGroupsFromPanel);
        }
        ToolStripControlHost DetailsCommand(string label,int width,Action action)
        {
            var button=new EditorButton{Text=label,AccessibleName=label,Size=new Size(width,30)};
            button.Click+=(s,e)=>action();
            var host=new ToolStripControlHost(button){Size=button.Size,AutoSize=false,Alignment=ToolStripItemAlignment.Right,Available=false};
            quickStrip.Items.Add(host);return host;
        }
        void UpdateDetailsCommands(bool active,bool multiple)
        {
            detailsCloseHost.Available=detailsResetHost.Available=active;
            detailsGroupHost.Available=multiple;
            if(multiple)
            {
                int groups=SelectedWholeGroups().Count;
                detailsGroupHost.Control.Text=groups>1?"Połącz grupy":groups==1?"Dodaj do grupy":"Grupuj wybrane";
                detailsGroupHost.Control.AccessibleName=detailsGroupHost.Control.Text;
            }
        }
        void SetRightWidth(int width)
        {
            requestedRightWidth=ClampRightWidth(width);
            PositionRightPanel(canvas.Top,canvas.Height,rightResizing);
        }
        void PositionRightPanel(int top,int height,bool live)
        {
            int width=rightCollapsed?18:ClampRightWidth(requestedRightWidth);
            var next=new Rectangle(Math.Max(0,ClientSize.Width-width),top,width,height);
            var previous=right.Bounds;
            if(previous==next)return;
            right.Bounds=next;
            if(!right.Visible)return;

            if(previous.Left<next.Left)
            {
                var exposed=new Rectangle(previous.Left-canvas.Left,top-canvas.Top,next.Left-previous.Left,height);
                exposed.Intersect(canvas.ClientRectangle);
                if(!exposed.IsEmpty)
                {
                    canvas.Invalidate(exposed);
                    // Complete the exposed strip before handling the next mouse move.
                    if(live)canvas.Update();
                }
            }
            if(live)
            {
                // The moving edge is a pair of native child windows. Repaint them
                // now, leaving the rest of the details to the normal paint queue.
                rightToggle.Invalidate();rightToggle.Update();
                rightResizeGrip.Invalidate();rightResizeGrip.Update();
            }
            else right.Invalidate(true);
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
