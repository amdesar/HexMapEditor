using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        List<InkStroke> inkBefore;
        InkStroke activeStroke;
        PointF? inkPrevious;
        string inkError;
        string inkColor="#FFD04A",pointerColor="#FF496C";
        float inkWidth=3,eraserSize=28;
        Point? inkCursor;
        sealed class PointerSample { internal Point Point;internal long Time; }
        readonly List<PointerSample> pointerTrail=new List<PointerSample>();
        readonly Timer pointerTimer=new Timer{Interval=33};
        bool pointerActive; Point pointerPosition;
        static long PointerTime {get{return System.Diagnostics.Stopwatch.GetTimestamp()*1000/System.Diagnostics.Stopwatch.Frequency;}}
        void InitInk()
        {
            pointerTimer.Tick+=(s,e)=>AgePointer();Disposed+=(s,e)=>pointerTimer.Dispose();
            Deactivate+=(s,e)=>EndCanvasGesture(true);
        }
        bool BeginInk(MouseEventArgs e)
        {
            if(gestureTool!=EditorTool.Stylus&&gestureTool!=EditorTool.Eraser)return false;
            if(e.Button==MouseButtons.Right){if(gestureTool==EditorTool.Stylus){pointerActive=true;PointerAt(e.Location);}else ClearInk();return true;}
            if(e.Button!=MouseButtons.Left)return true;
            inkBefore=new List<InkStroke>(map.Ink);map.Ink=new List<InkStroke>(map.Ink);inkError=null;inkPrevious=null;MoveInk(e.Location);return true;
        }
        void MoveInk(Point point)
        {
            inkCursor=point;
            if(gestureTool==EditorTool.Stylus&&gestureButton==MouseButtons.Right){PointerAt(point);return;}
            if(inkBefore==null||inkError!=null)return;
            if(!canvas.ClientRectangle.Contains(point)){inkPrevious=null;activeStroke=null;return;}
            var p=World(point);
            try
            {
                if(gestureTool==EditorTool.Eraser)map.Ink=InkGeometry.Erase(map.Ink,inkPrevious??p,p,eraserSize);
                else
                {
                    if(activeStroke==null){activeStroke=new InkStroke{Color=inkColor,Width=inkWidth};map.Ink.Add(activeStroke);}
                    if(activeStroke.Points.Count==0||Math.Abs(activeStroke.Points.Last().X-p.X)+Math.Abs(activeStroke.Points.Last().Y-p.Y)>.05)activeStroke.Points.Add(new InkPoint(p));
                    activeStroke.CachedBounds=null;InkGeometry.Validate(map.Ink);
                }
                inkPrevious=p;canvas.Invalidate();
            }
            catch(Exception ex){inkError=ex.Message;map.Ink=new List<InkStroke>(inkBefore);canvas.Invalidate();}
        }
        void FinishInk(bool commit)
        {
            pointerActive=false;AgePointer();
            if(inkBefore==null)return;
            var before=inkBefore;inkBefore=null;activeStroke=null;inkPrevious=null;
            if(!commit||inkError!=null){map.Ink=new List<InkStroke>(before);if(inkError!=null)statusLabel.Text=inkError;return;}
            if(json.Serialize(before)==json.Serialize(map.Ink))return;
            var retained=new HashSet<InkStroke>(map.Ink.Concat(before));
            foreach(var action in undo){if(action.BeforeInk!=null)retained.UnionWith(action.BeforeInk);if(action.AfterInk!=null)retained.UnionWith(action.AfterInk);}
            if(retained.Sum(s=>(long)s.Points.Count)>2000000){map.Ink=new List<InkStroke>(before);statusLabel.Text="Rysik: limit 2 mln punktów zachowanych w historii sesji. Gest odrzucono. Zapisz i otwórz mapę, aby świadomie wyzerować historię.";canvas.Invalidate();return;}
            PushAction(new EditAction{Label="Rysik / gumka",BeforeInk=before,AfterInk=new List<InkStroke>(map.Ink)});MarkDirty();
        }
        void ClearInk()
        {
            if(map.Ink.Count==0)return;
            var before=new List<InkStroke>(map.Ink);map.Ink=new List<InkStroke>();
            PushAction(new EditAction{Label="Wyczyść rysunki",BeforeInk=before,AfterInk=new List<InkStroke>()});MarkDirty();
        }
        void PointerAt(Point point)
        {
            if(hideInk)return;
            pointerPosition=point;pointerTrail.Add(new PointerSample{Point=point,Time=PointerTime});if(pointerTrail.Count>128)pointerTrail.RemoveAt(0);
            pointerTimer.Start();AgePointer();
        }
        void AgePointer()
        {
            var all=pointerTrail.Select(p=>p.Point).ToList();if(pointerActive)all.Add(pointerPosition);pointerTrail.RemoveAll(p=>PointerTime-p.Time>=1000);
            if(all.Count>0){var box=Rectangle.FromLTRB(all.Min(p=>p.X)-24,all.Min(p=>p.Y)-24,all.Max(p=>p.X)+24,all.Max(p=>p.Y)+24);canvas.Invalidate(box);}
            if(!pointerActive&&pointerTrail.Count==0)pointerTimer.Stop();
        }
        void DrawInk(Graphics graphics)
        {
            if(hideInk)return;var state=graphics.Save();graphics.TranslateTransform(pan.X,pan.Y);graphics.ScaleTransform(scale,scale);
            var clip=graphics.VisibleClipBounds;
            foreach(var s in map.Ink.Where(s=>clip.IntersectsWith(InkGeometry.Bounds(s))))using(var pen=new Pen(ColorTranslator.FromHtml(s.Color),s.Width){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round})
            {if(s.Points.Count==1){var p=s.Points[0];using(var b=new SolidBrush(pen.Color))graphics.FillEllipse(b,p.X-s.Width/2,p.Y-s.Width/2,s.Width,s.Width);}else graphics.DrawLines(pen,s.Points.Select(p=>p.Point).ToArray());}
            graphics.Restore(state);
            if(currentTool==EditorTool.Eraser&&inkCursor.HasValue){float r=eraserSize*scale/2;using(var pen=new Pen(Color.White,1.5f)){pen.DashStyle=DashStyle.Dash;graphics.DrawEllipse(pen,inkCursor.Value.X-r,inkCursor.Value.Y-r,2*r,2*r);}}
            if(pointerTrail.Count==0&&!pointerActive)return;
            var color=ColorTranslator.FromHtml(pointerColor);
            for(int i=1;i<pointerTrail.Count;i++){int alpha=(int)(220*Math.Max(0,1-(PointerTime-pointerTrail[i].Time)/1000.0));using(var pen=new Pen(Color.FromArgb(alpha,color),4))graphics.DrawLine(pen,pointerTrail[i-1].Point,pointerTrail[i].Point);}
            if(!pointerActive&&pointerTrail.Count>0){var last=pointerTrail.Last();int alpha=(int)(220*Math.Max(0,1-(PointerTime-last.Time)/1000.0));using(var b=new SolidBrush(Color.FromArgb(alpha,color)))graphics.FillEllipse(b,last.Point.X-2,last.Point.Y-2,4,4);}
            if(pointerActive){var p=pointerPosition;float diameter=18*graphics.DpiX/96f;using(var b=new SolidBrush(color))graphics.FillEllipse(b,p.X-diameter/2,p.Y-diameter/2,diameter,diameter);using(var pen=new Pen(Color.Black,2))graphics.DrawEllipse(pen,p.X-diameter/2,p.Y-diameter/2,diameter,diameter);}
        }
    }
}
