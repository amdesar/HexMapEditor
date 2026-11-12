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
        readonly Timer pointerTimer=new Timer{Interval=33};
        bool pointerActive; Point pointerPosition;float pointerDiameter=20;
        static long PointerTime {get{return System.Diagnostics.Stopwatch.GetTimestamp()*1000/System.Diagnostics.Stopwatch.Frequency;}}
        void InitInk()
        {
            pointerTimer.Tick+=(s,e)=>AgePointer();Disposed+=(s,e)=>pointerTimer.Dispose();
            Deactivate+=(s,e)=>{EndCanvasGesture(true);ClearPointerContext();};
            canvas.LostFocus+=(s,e)=>{if(pointerActive){EndCanvasGesture(true);ClearPointerContext();}};
        }
        bool BeginInk(MouseEventArgs e)
        {
            if(gestureTool!=EditorTool.Stylus&&gestureTool!=EditorTool.Eraser)return false;
            if(e.Button==MouseButtons.Right){if(gestureTool==EditorTool.Stylus){using(var g=canvas.CreateGraphics())pointerDiameter=18*g.DpiX/96f+2;pointerActive=true;((InkCanvas)canvas).PointerHidden=true;PointerAt(e.Location);}else ClearInk();return true;}
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
            AgePointer();pointerPosition=point;pointerTrail.Add(new PointerSample{Point=point,Time=PointerTime});if(pointerTrail.Count>128)pointerTrail.RemoveAt(0);
            pointerTimer.Start();AgePointer();
        }
        void AgePointer()
        {
            var all=pointerTrail.Select(p=>p.Point).ToList();if(pointerActive)all.Add(pointerPosition);pointerTrail.RemoveAll(p=>PointerTime-p.Time>=750);
            if(all.Count>0){int margin=(int)Math.Ceiling(pointerDiameter/2)+3;var box=Rectangle.FromLTRB(all.Min(p=>p.X)-margin,all.Min(p=>p.Y)-margin,all.Max(p=>p.X)+margin,all.Max(p=>p.Y)+margin);canvas.Invalidate(box);}
            if(!pointerActive&&pointerTrail.Count==0)pointerTimer.Stop();
        }
        void DrawInk(Graphics graphics)
        {
            if(hideInk)return;var state=graphics.Save();graphics.TranslateTransform(pan.X,pan.Y);graphics.ScaleTransform(scale,scale);
            var clip=graphics.VisibleClipBounds;
            foreach(var s in map.Ink.Where(s=>clip.IntersectsWith(InkGeometry.Bounds(s))))using(var pen=new Pen(ColorTranslator.FromHtml(s.Color),s.Width){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round})
            {if(s.Points.Count==1){var p=s.Points[0];using(var b=new SolidBrush(pen.Color))graphics.FillEllipse(b,p.X-s.Width/2,p.Y-s.Width/2,s.Width,s.Width);}else graphics.DrawLines(pen,s.Points.Select(p=>p.Point).ToArray());}
            graphics.Restore(state);
            if((currentTool==EditorTool.Eraser||currentTool==EditorTool.Stylus)&&inkCursor.HasValue&&!pointerActive)
            {
                float r=inkToolSize*graphics.DpiX/96f/2;
                var p=inkCursor.Value;
                using(var edge=new Pen(Color.Black,3))graphics.DrawEllipse(edge,p.X-r,p.Y-r,2*r,2*r);
                using(var pen=new Pen(Color.White,1.3f)){if(currentTool==EditorTool.Eraser)pen.DashStyle=DashStyle.Dash;graphics.DrawEllipse(pen,p.X-r,p.Y-r,2*r,2*r);}
            }
            if(pointerTrail.Count==0&&!pointerActive)return;
            var color=ColorTranslator.FromHtml(pointerColor);float diameter=pointerDiameter;
            for(int i=1;i<pointerTrail.Count;i++)
            {
                var a=pointerTrail[i-1];var b=pointerTrail[i];
                float lifeA=(float)Math.Max(0,1-(PointerTime-a.Time)/750.0),lifeB=(float)Math.Max(0,1-(PointerTime-b.Time)/750.0);
                float ra=Math.Max(.35f,diameter*lifeA/2),rb=Math.Max(.35f,diameter*lifeB/2);
                float dx=b.Point.X-a.Point.X,dy=b.Point.Y-a.Point.Y,length=(float)Math.Sqrt(dx*dx+dy*dy);
                using(var brush=new SolidBrush(Color.FromArgb((int)(210*lifeB),color)))
                {
                    if(length>.01f){float nx=-dy/length,ny=dx/length;graphics.FillPolygon(brush,new[]{new PointF(a.Point.X+nx*ra,a.Point.Y+ny*ra),new PointF(b.Point.X+nx*rb,b.Point.Y+ny*rb),new PointF(b.Point.X-nx*rb,b.Point.Y-ny*rb),new PointF(a.Point.X-nx*ra,a.Point.Y-ny*ra)});}
                    graphics.FillEllipse(brush,b.Point.X-rb,b.Point.Y-rb,2*rb,2*rb);
                }
            }
            if(!pointerActive&&pointerTrail.Count==1){var last=pointerTrail.Last();float life=(float)Math.Max(0,1-(PointerTime-last.Time)/750.0);float r=diameter*life/2;using(var brush=new SolidBrush(Color.FromArgb((int)(210*life),color)))graphics.FillEllipse(brush,last.Point.X-r,last.Point.Y-r,2*r,2*r);}
            if(pointerActive){var p=pointerPosition;using(var brush=new SolidBrush(color))graphics.FillEllipse(brush,p.X-diameter/2,p.Y-diameter/2,diameter,diameter);}
        }
    }
}
