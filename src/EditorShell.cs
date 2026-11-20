using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    // Icons are drawn locally. No font pictograms, downloads or external assets.
    internal static class EditorIcons
    {
        static readonly Dictionary<string, Bitmap> cache = new Dictionary<string, Bitmap>();
        internal static Bitmap ScrollArrow(bool up)
        {
            Bitmap image; string key = up ? "terrain-up" : "terrain-down";
            if (cache.TryGetValue(key, out image)) return image;
            if (up) { image = new Bitmap(9,5); using (var g = Graphics.FromImage(image)) g.FillPolygon(Brushes.WhiteSmoke, new[] { new Point(0,4), new Point(4,0), new Point(8,4) }); }
            else { image = (Bitmap)ScrollArrow(true).Clone(); image.RotateFlip(RotateFlipType.RotateNoneFlipY); }
            cache.Add(key,image); return image;
        }
        public static Bitmap Small(string key)
        {
            Bitmap image; if (!cache.TryGetValue("small:" + key, out image)) { image = new Bitmap(Draw(key), new Size(18, 18)); cache.Add("small:" + key, image); } return image;
        }
        public static Bitmap Draw(string key)
        {
            Bitmap existing; if (cache.TryGetValue(key, out existing)) return existing;
            var image = new Bitmap(22, 22);
            using (var g = Graphics.FromImage(image)) using (var p = new Pen(Color.WhiteSmoke, 1.8f))
            {
                if (key == "trash") p.Color = Color.FromArgb(244, 113, 113);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                if (key == "undo" || key == "redo")
                { if (key == "redo") { g.TranslateTransform(22, 0); g.ScaleTransform(-1, 1); } g.DrawArc(p, 4, 6, 14, 12, 205, 240); g.DrawLines(p, new[] { new Point(3, 3), new Point(3, 11), new Point(10, 9) }); }
                else if (key == "pointy" || key == "flat")
                { var points = Enumerable.Range(0, 6).Select(i => { double a = (i * 60 + (key == "flat" ? 0 : -90)) * Math.PI / 180; return new PointF(11 + 9 * (float)Math.Cos(a), 11 + 9 * (float)Math.Sin(a)); }).ToArray(); g.DrawPolygon(p, points); }
                else if (key == "new") { g.DrawRectangle(p, 5, 2, 12, 18); g.DrawLine(p, 8, 11, 14, 11); g.DrawLine(p, 11, 8, 11, 14); }
                else if(key=="plus"){g.DrawLine(p,4,11,18,11);g.DrawLine(p,11,4,11,18);}
                else if(key=="restore"){g.DrawRectangle(p,4,3,14,16);g.DrawLine(p,7,7,15,7);g.DrawLines(p,new[]{new Point(15,12),new Point(7,12),new Point(10,9)});g.DrawLine(p,7,12,10,15);}
                else if (key == "open" || key == "library") { g.DrawLines(p, new[] { new Point(2, 18), new Point(2, 5), new Point(9, 5), new Point(11, 8), new Point(20, 8), new Point(17, 18), new Point(2, 18) }); }
                else if (key == "save" || key == "saveas") { g.DrawRectangle(p, 3, 3, 16, 16); g.DrawRectangle(p, 7, 3, 8, 6); g.DrawRectangle(p, 7, 13, 8, 6); if (key == "saveas") g.DrawLine(p, 1, 20, 20, 1); }
                else if (key == "select") g.DrawPolygon(p, new[] { new Point(4, 2), new Point(17, 12), new Point(11, 13), new Point(8, 19) });
                else if(key=="rectangle"){p.DashStyle=DashStyle.Dash;g.DrawRectangle(p,3,4,16,14);}
                else if(key=="lasso"){g.DrawEllipse(p,3,3,16,12);g.DrawBezier(p,new Point(8,14),new Point(2,22),new Point(16,21),new Point(12,15));}
                else if(key=="hand"){
                    using(var hand=new GraphicsPath()){hand.AddLines(new[]{new PointF(6,19),new PointF(2,12),new PointF(3,10),new PointF(6,13),new PointF(6,5)});hand.AddBezier(6,5,6,2,9,2,9,5);hand.AddLine(9,5,9,10);hand.AddLine(9,10,9,3);hand.AddBezier(9,3,9,1,12,1,12,3);hand.AddLine(12,3,12,10);hand.AddLine(12,10,12,4);hand.AddBezier(12,4,12,2,15,2,15,4);hand.AddLine(15,4,15,11);hand.AddLine(15,11,15,7);hand.AddBezier(15,7,15,5,18,5,18,7);hand.AddLines(new[]{new PointF(18,7),new PointF(18,14),new PointF(16,19),new PointF(6,19)});g.DrawPath(p,hand);}
                }
                else if(key=="brush"){g.DrawPolygon(p,new[]{new Point(11,11),new Point(16,2),new Point(19,3),new Point(14,13)});g.DrawLine(p,9,12,14,15);using(var bristles=new GraphicsPath()){bristles.AddBezier(9,13,5,12,7,18,2,19);bristles.AddBezier(2,19,8,21,13,20,13,16);bristles.CloseFigure();g.FillPath(Brushes.WhiteSmoke,bristles);}}
                else if(key=="stylus"){g.DrawPolygon(p,new[]{new Point(3,20),new Point(5,14),new Point(16,2),new Point(20,6),new Point(9,18)});g.DrawLine(p,13,5,17,9);}
                else if(key=="eraser"){g.DrawPolygon(p,new[]{new Point(3,13),new Point(12,3),new Point(20,10),new Point(11,19),new Point(8,19)});g.FillPolygon(Brushes.WhiteSmoke,new[]{new Point(3,13),new Point(8,8),new Point(16,14),new Point(11,19),new Point(8,19)});g.DrawLine(p,8,8,16,14);}
                else if(key=="dark"||key=="darkbucket")
                {
                    using(var eye=new GraphicsPath()){eye.AddBezier(2,11,7,1,15,1,20,11);eye.AddBezier(20,11,15,21,7,21,2,11);if(key=="darkbucket"){eye.AddEllipse(8,8,6,6);eye.FillMode=FillMode.Alternate;g.FillPath(Brushes.WhiteSmoke,eye);}else{g.DrawPath(p,eye);g.DrawEllipse(p,8,8,6,6);}}
                }
                else if (key == "fog" || key == "fogbucket") {using(var cloud=new GraphicsPath()){cloud.AddBezier(3,17,0,12,2,8,6,9);cloud.AddBezier(6,9,5,2,14,2,15,8);cloud.AddBezier(15,8,22,6,23,17,18,18);cloud.AddLine(18,18,3,17);if(key=="fogbucket")g.FillPath(Brushes.WhiteSmoke,cloud);else g.DrawPath(p,cloud);}}
                else if (key == "bucket") { g.DrawPolygon(p, new[] { new Point(2, 10), new Point(10, 2), new Point(18, 10), new Point(10, 18) });g.FillPolygon(Brushes.WhiteSmoke,new[]{new Point(3,10),new Point(17,10),new Point(10,17)});g.DrawLine(p,3,10,17,10);g.FillClosedCurve(Brushes.WhiteSmoke,new[]{new Point(19,12),new Point(17,17),new Point(20,20),new Point(22,17)}); }
                else if (key == "pencil") { g.DrawPolygon(p, new[] { new Point(3, 19), new Point(5, 12), new Point(15, 2), new Point(20, 7), new Point(10, 17) }); g.DrawLine(p, 12, 5, 17, 10); }
                else if (key == "trash") { g.DrawRectangle(p, 6, 7, 11, 13); g.DrawLine(p, 3, 5, 20, 5); g.DrawLine(p, 9, 2, 14, 2); g.DrawLine(p, 9, 10, 9, 17); g.DrawLine(p, 14, 10, 14, 17); }
                else if (key == "fit" || key=="fitall") { foreach (int x in new[] { 2, 20 }) foreach (int y in new[] { 2, 20 }) { g.DrawLine(p, x, y, x == 2 ? 8 : 14, y); g.DrawLine(p, x, y, x, y == 2 ? 8 : 14); } if(key=="fitall"){g.DrawEllipse(p,6,6,10,10);g.DrawLine(p,8,11,14,11);g.DrawLine(p,11,8,11,14);} }
                else if (key == "refresh") { g.DrawArc(p, 4, 4, 14, 14, 40, 285); g.DrawLines(p, new[] { new Point(12, 2), new Point(18, 4), new Point(17, 10) }); }
                else { g.DrawRectangle(p, 3, 3, 16, 16); g.DrawLine(p, 7, 8, 15, 8); g.DrawLine(p, 7, 13, 15, 13); }
            }
            cache.Add(key, image); return image;
        }
    }

    public sealed partial class MainWindow
    {
        Panel shellToolbar;

        readonly FlowLayoutPanel paletteTiles = new BufferedFlowPanel();
        readonly Label presetStateLabel = new Label();
        readonly Dictionary<EditorTool, Button> toolButtons = new Dictionary<EditorTool, Button>();
        FlowLayoutPanel paletteTools, presetActions;
        Panel presetPickerRow;
        Button refreshPresetsButton, addTerrainButton;
        void ConfigureButton(Button button, string text, string icon, string help, Action action, int width)
        {
            button.Text = text; button.Image = EditorIcons.Draw(icon); button.TextImageRelation = TextImageRelation.ImageBeforeText;
            button.Width = width; button.Height = 32; button.Margin = new Padding(1); button.Padding = new Padding(2, 0, 2, 0); button.FlatStyle = FlatStyle.Flat;
            button.Image = EditorIcons.Small(icon); button.ImageAlign = ContentAlignment.MiddleCenter; button.TextAlign = ContentAlignment.MiddleCenter;
            button.AccessibleName = help; tips.SetToolTip(button, help); StyleButton(button); button.Click += (s, e) => action();
        }
        Button ShellButton(Control parent, string text, string icon, string help, Action action, int width)
        { var button = new Button(); ConfigureButton(button, text, icon, help, action, width); parent.Controls.Add(button); return button; }
        void InitializeShell()
        {
            BuildCommandBars();
            var status = new Panel { Dock = DockStyle.Bottom, Height = 27, BackColor = shellToolbar.BackColor }; statusLabel.Dock = DockStyle.Fill; statusLabel.Padding = new Padding(10, 4, 0, 0); status.Controls.Add(statusLabel); Controls.Add(status);
            canvas.BackColor = Color.FromArgb(25, 28, 36); canvas.TabStop = true; Controls.Add(canvas); canvas.SendToBack();
            canvas.Paint += PaintMap; canvas.MouseDown += CanvasMouseDown; canvas.MouseMove += CanvasMouseMove; canvas.MouseUp += CanvasMouseUp; canvas.MouseWheel += CanvasMouseWheel;
            canvas.MouseCaptureChanged += (s, e) => { if (!canvas.Capture && gestureButton != MouseButtons.None){if(pointerActive)ClearPointerContext();EndCanvasGesture(true);} };
            left.BackColor = shellToolbar.BackColor; left.AutoScroll = false; Controls.Add(left);
            leftToggle.FlatStyle = FlatStyle.Flat; StyleButton(leftToggle); leftToggle.AccessibleName = "Zwiń lub rozwiń paletę"; tips.SetToolTip(leftToggle, leftToggle.AccessibleName); leftToggle.Click += (s, e) => { leftCollapsed = !leftCollapsed; PositionOverlays(); canvas.Invalidate(); }; Controls.Add(leftToggle);
            BuildPalettePanel(); BuildDetailsPanel(); Controls.Add(right);
            left.BringToFront(); right.BringToFront(); leftToggle.BringToFront();
        }
        void LayoutMainToolbar()
        {
            LayoutCommandBars();
        }
        void BuildPalettePanel() { BuildSections(); }
        Button SmallButton(Control parent, string text, Action action, int width)
        { var b = new EditorButton { Text = text, Width = width, Height = 28, Margin = new Padding(2), FlatStyle = FlatStyle.Flat, AccessibleName = text }; StyleButton(b); b.Click += (s, e) => action(); parent.Controls.Add(b); return b; }
        void SelectTool(EditorTool tool)
        {
            EndCanvasGesture(true);if(currentTool!=tool)ClearPointerContext(); currentTool = tool;
            foreach (var pair in toolButtons) { ChoiceStyle.Apply(pair.Value,pair.Key==tool); pair.Value.FlatAppearance.BorderSize = pair.Key == tool ? 3 : 1; pair.Value.AccessibleDescription = pair.Key == tool ? "Aktywne" : "Nieaktywne"; }
            UpdateBrushAvailability();
        }
        void BuildDetailsPanel()
        {
            right.BackColor = Color.FromArgb(34, 38, 48); right.Visible = false;
            BuildHexDetails();
            cellDescription.Changed = DescriptionEdited; cellDescription.HistoryRequested = repeat => TextUndo("description", repeat);
            cellDescription.SaveRequested = asNew => { if (asNew) SaveAsProject(); else SaveProject(); };
            cellDescription.BreakHistoryGroup = () => textHistory.BreakGroups();
            cellDescription.Error = message => ShowEditorMessage(message, "Opis heksu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            cellDescription.TableSizeRequested = () => { using (var d = new TableSizeDialog()) return RunModal(() => d.ShowDialog(this)) == DialogResult.OK ? new[] { d.Rows, d.Columns } : null; };
            cellDescription.ImagePathRequested=()=>{
                using(var dialog=new OpenFileDialog{Title="Wstaw obraz do opisu",Filter="Obrazy PNG i JPEG|*.png;*.jpg;*.jpeg",RestoreDirectory=true})
                    return RunModal(()=>dialog.ShowDialog(this))==DialogResult.OK?dialog.FileName:null;
            };
            BuildObjectDetails();
            InitializePanelResize();
        }
    }
}
