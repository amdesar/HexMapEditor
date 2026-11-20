using System;
using System.Drawing;
using System.Windows.Forms;

namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        int requestedRightWidth = 440, rightResizeOrigin, rightResizeStart;
        bool rightResizing;
        readonly Panel rightResizeGrip = new Panel { Width = 7, Dock = DockStyle.Left, Cursor = Cursors.SizeWE, BackColor = Color.FromArgb(78, 89, 112), AccessibleName = "Zmień szerokość panelu szczegółów" };
        int ClampRightWidth(int width) { return Math.Max(340, Math.Min(Math.Max(340, Math.Min(ClientSize.Width - 280, (int)(ClientSize.Width * .72))), width)); }
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
            rightResizeGrip.MouseMove += (s, e) => { if (rightResizing) MoveRightResize(rightResizeGrip.PointToScreen(e.Location).X); };
            rightResizeGrip.MouseUp += (s, e) => { if (e.Button == MouseButtons.Left) EndRightResize(); };
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
            PositionOverlays();
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
