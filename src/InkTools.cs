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
        float inkToolSize=5,inkGestureSize;
        float InkWorldSize(){using(var g=canvas.CreateGraphics())return InkSizeMath.World(inkToolSize,scale,g.DpiX);}
        void ClearPointerContext(){pointerActive=false;pointerTrail.Clear();pointerTimer.Stop();((InkCanvas)canvas).PointerHidden=false;canvas.Invalidate();}
        Point? inkCursor;
        sealed class PointerSample { internal Point Point;internal long Time; }
        readonly List<PointerSample> pointerTrail=new List<PointerSample>();
        readonly CometRenderer comet=new CometRenderer();
        readonly Timer pointerTimer=new Timer{Interval=33};
        bool pointerActive; Point pointerPosition;float pointerDiameter=20;
        static long PointerTime {get{return System.Diagnostics.Stopwatch.GetTimestamp()*1000/System.Diagnostics.Stopwatch.Frequency;}}
        void InitInk()
        {
            pointerTimer.Tick+=(s,e)=>AgePointer();Disposed+=(s,e)=>{pointerTimer.Dispose();comet.Dispose();};
            Deactivate+=(s,e)=>{EndCanvasGesture(true);ClearPointerContext();};
            canvas.LostFocus+=(s,e)=>{if(pointerActive){EndCanvasGesture(true);ClearPointerContext();}};
        }
        bool BeginInk(MouseEventArgs e)
        {
            if(gestureTool!=EditorTool.Stylus&&gestureTool!=EditorTool.Eraser)return false;
            if(e.Button==MouseButtons.Right){if(gestureTool==EditorTool.Stylus){using(var g=canvas.CreateGraphics())pointerDiameter=18*g.DpiX/96f+2;AgePointer();pointerActive=true;((InkCanvas)canvas).PointerHidden=true;PointerAt(e.Location);}else ClearInk();return true;}
            if(e.Button!=MouseButtons.Left)return true;
            inkGestureSize=InkWorldSize();inkBefore=new List<InkStroke>(map.Ink);map.Ink=new List<InkStroke>(map.Ink);inkError=null;inkPrevious=null;MoveInk(e.Location);return true;
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
                if(gestureTool==EditorTool.Eraser)map.Ink=InkGeometry.Erase(map.Ink,inkPrevious??p,p,inkGestureSize);
                else
                {
                    if(activeStroke==null){activeStroke=new InkStroke{Color=inkColor,Width=inkGestureSize};map.Ink.Add(activeStroke);}
                    if(activeStroke.Points.Count==0||Math.Abs(activeStroke.Points.Last().X-p.X)+Math.Abs(activeStroke.Points.Last().Y-p.Y)>.05)activeStroke.Points.Add(new InkPoint(p));
                    activeStroke.CachedBounds=null;InkGeometry.Validate(map.Ink);
                }
                inkPrevious=p;canvas.Invalidate();
            }
            catch(Exception ex){inkError=ex.Message;map.Ink=new List<InkStroke>(inkBefore);canvas.Invalidate();}
        }
        void FinishInk(bool commit)
        {
            pointerActive=false;((InkCanvas)canvas).PointerHidden=false;AgePointer();
            if(inkBefore==null)return;
            var before=inkBefore;inkBefore=null;activeStroke=null;inkPrevious=null;
            if(!commit||inkError!=null){map.Ink=new List<InkStroke>(before);if(inkError!=null)statusLabel.Text=inkError;return;}
            if(json.Serialize(before)==json.Serialize(map.Ink))return;
            var retained=new HashSet<InkStroke>(map.Ink.Concat(before));
            foreach(var action in undo){if(action.BeforeInk!=null)retained.UnionWith(action.BeforeInk);if(action.AfterInk!=null)retained.UnionWith(action.AfterInk);}
            if(retained.Sum(s=>(long)InkGeometry.PointCost(s))>2000000||retained.Sum(s=>(long)(s.Surface==null?0:s.Surface.Length))>64*1024*1024){map.Ink=new List<InkStroke>(before);statusLabel.Text="Rysik: limit 2 mln punktów kontrolnych / 64 MiB powierzchni zachowanych w historii sesji. Gest odrzucono. Zapisz i otwórz mapę, aby świadomie wyzerować historię.";canvas.Invalidate();return;}
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
            AgePointer();pointerPosition=point;pointerTrail.Add(new PointerSample{Point=point,Time=PointerTime});if(pointerTrail.Count>128)pointerTrail.RemoveAt(0);
            pointerTimer.Start();AgePointer();
        }
        void AgePointer()
        {
            var all=pointerTrail.Select(p=>p.Point).ToList();if(pointerActive)all.Add(pointerPosition);if(pointerTrail.Count>0&&PointerTime-pointerTrail.Last().Time>=750)pointerTrail.Clear();
            else while(pointerTrail.Count>1&&PointerTime-pointerTrail[1].Time>=750)pointerTrail.RemoveAt(0);
            if(all.Count>0){int margin=(int)Math.Ceiling(pointerDiameter/2)+3;var box=Rectangle.FromLTRB(all.Min(p=>p.X)-margin,all.Min(p=>p.Y)-margin,all.Max(p=>p.X)+margin,all.Max(p=>p.Y)+margin);canvas.Invalidate(box);}
            if(!pointerActive&&pointerTrail.Count==0)pointerTimer.Stop();
        }
        void DrawInk(Graphics graphics)
        {
            if(hideInk)return;var state=graphics.Save();graphics.TranslateTransform(pan.X,pan.Y);graphics.ScaleTransform(scale,scale);
            var clip=graphics.VisibleClipBounds;
            foreach(var s in map.Ink.Where(s=>clip.IntersectsWith(InkGeometry.Bounds(s))))InkGeometry.Draw(graphics,s);
            graphics.Restore(state);
            if((currentTool==EditorTool.Eraser||currentTool==EditorTool.Stylus)&&inkCursor.HasValue&&!pointerActive)
            {
                float r=inkToolSize*graphics.DpiX/96f/2;
                var p=inkCursor.Value;
                using(var edge=new Pen(Color.Black,3))graphics.DrawEllipse(edge,p.X-r,p.Y-r,2*r,2*r);
                using(var pen=new Pen(Color.White,1.3f)){if(currentTool==EditorTool.Eraser)pen.DashStyle=DashStyle.Dash;graphics.DrawEllipse(pen,p.X-r,p.Y-r,2*r,2*r);}
            }
            if(pointerTrail.Count==0&&!pointerActive)return;
            long now=PointerTime;
            comet.Draw(graphics,canvas.ClientRectangle,pointerTrail.Select(p=>new CometPoint(p.Point,now-p.Time)).ToList(),pointerActive?(PointF?)pointerPosition:null,pointerDiameter,ColorTranslator.FromHtml(pointerColor));
        }
    }
}
