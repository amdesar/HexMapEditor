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
        InkEraseGesture eraseGesture;
        PointF? inkPrevious;
        string inkError;
        string inkColor="#FFD04A",pointerColor="#FF496C";
        GradientDefinition inkGradient,pointerGradient;
        GradientSampler pointerPalette;
        double inkCycleWorld;
        float pointerDpiFactor=1;
        double pointerPhase;Point? pointerLast;
        float inkToolSize=5,inkGestureSize;
        int inkBasePointCost;
        float InkWorldSize(){using(var g=canvas.CreateGraphics())return InkSizeMath.World(inkToolSize,scale,g.DpiX);}
        void ClearPointerContext(){pointerPhase=0;pointerLast=null;pointerActive=false;pointerTrail.Clear();pointerTimer.Stop();((InkCanvas)canvas).PointerHidden=false;canvas.Invalidate();}
        Point? inkCursor;
        sealed class PointerSample { internal Point Point;internal long Time;internal double Phase;internal GradientSampler Palette;internal Color Color; }
        readonly List<PointerSample> pointerTrail=new List<PointerSample>();
        readonly CometRenderer comet=new CometRenderer();
        readonly Timer pointerTimer=new Timer{Interval=33};
        bool pointerActive; Point pointerPosition;float pointerDiameter=20;
        static long PointerTime {get{return System.Diagnostics.Stopwatch.GetTimestamp()*1000/System.Diagnostics.Stopwatch.Frequency;}}
        void InitInk()
        {
            pointerTimer.Tick+=(s,e)=>AgePointer();Disposed+=(s,e)=>{pointerTimer.Dispose();comet.Dispose();InkPathCache.Clear();InkGradientRenderer.Clear();TerrainGraphicThumbnails.Clear();if(eraseGesture!=null)eraseGesture.Dispose();};
            Deactivate+=(s,e)=>{EndCanvasGesture(true);ClearPointerContext();};
            canvas.LostFocus+=(s,e)=>{if(pointerActive){EndCanvasGesture(true);ClearPointerContext();}};
        }
        bool BeginInk(MouseEventArgs e)
        {
            if(gestureTool!=EditorTool.Stylus&&gestureTool!=EditorTool.Eraser)return false;
            if(e.Button==MouseButtons.Right){if(gestureTool==EditorTool.Stylus){using(var g=canvas.CreateGraphics()){pointerDpiFactor=g.DpiX/96f;pointerDiameter=18*pointerDpiFactor+2;}AgePointer();pointerActive=true;((InkCanvas)canvas).PointerHidden=true;PointerAt(e.Location);}else ClearInk();return true;}
            if(e.Button!=MouseButtons.Left)return true;
            InkGeometry.Validate(map.Ink);inkBasePointCost=map.Ink.Sum(s=>InkGeometry.PointCost(s));
            inkGestureSize=InkWorldSize();inkCycleWorld=300.0*inkGestureSize/inkToolSize;inkBefore=new List<InkStroke>(map.Ink);map.Ink=new List<InkStroke>(map.Ink);inkError=null;inkPrevious=null;if(gestureTool==EditorTool.Eraser)eraseGesture=new InkEraseGesture(inkGestureSize);MoveInk(e.Location);return true;
        }
        void MoveInk(Point point)
        {
            inkCursor=point;
            if(gestureTool==EditorTool.Stylus&&gestureButton==MouseButtons.Right){PointerAt(point);return;}
            if(inkBefore==null||inkError!=null)return;
            if(!canvas.ClientRectangle.Contains(point)){inkPrevious=null;if(activeStroke!=null)inkBasePointCost+=activeStroke.Points.Count;activeStroke=null;return;}
            var p=World(point);
            try
            {
                if(gestureTool==EditorTool.Eraser)eraseGesture.Add(inkPrevious??p,p);
                else
                {
                    if(activeStroke==null){activeStroke=new InkStroke{Color=inkColor,Width=inkGestureSize,Gradient=inkGradient==null?null:inkGradient.Copy(),CycleWorld=inkGradient==null?0:inkCycleWorld};map.Ink.Add(activeStroke);}
                    if(activeStroke.Points.Count==0||Math.Abs(activeStroke.Points.Last().X-p.X)+Math.Abs(activeStroke.Points.Last().Y-p.Y)>.05){var next=new InkPoint(p);if(activeStroke.Gradient!=null&&activeStroke.Points.Count>0){var last=activeStroke.Points.Last();next.Distance=last.Distance+Distance(last.Point,p);}activeStroke.Points.Add(next);}
                    activeStroke.CachedBounds=null;InkGeometry.Validate(new List<InkStroke>{activeStroke});
                    if(map.Ink.Count>InkGeometry.MaxStrokes||inkBasePointCost+activeStroke.Points.Count>InkGeometry.MaxPoints)throw new System.IO.InvalidDataException("Limit rysunków: 5000 powierzchni / 200000 punktów kontrolnych.");
                }
                inkPrevious=p;canvas.Invalidate();
            }
            catch(Exception ex){inkError=ex.Message;map.Ink=new List<InkStroke>(inkBefore);canvas.Invalidate();}
        }
        void FinishInk(bool commit)
        {
            pointerActive=false;((InkCanvas)canvas).PointerHidden=false;AgePointer();
            if(inkBefore==null)return;
            if(eraseGesture!=null)
            {
                try{if(commit&&inkError==null)map.Ink=eraseGesture.Apply(inkBefore);}
                catch(Exception ex){inkError=ex.Message;}
                finally{eraseGesture.Dispose();eraseGesture=null;canvas.Invalidate();}
            }
            var before=inkBefore;inkBefore=null;activeStroke=null;inkPrevious=null;
            if(!commit||inkError!=null){map.Ink=new List<InkStroke>(before);if(inkError!=null)statusLabel.Text=inkError;return;}
            InkGeometry.Validate(map.Ink);
            if(json.Serialize(before)==json.Serialize(map.Ink))return;
            var retained=new HashSet<InkStroke>(map.Ink.Concat(before));
            foreach(var action in undo){if(action.BeforeInk!=null)retained.UnionWith(action.BeforeInk);if(action.AfterInk!=null)retained.UnionWith(action.AfterInk);}
            if(retained.Sum(s=>(long)InkGeometry.PointCost(s))>2000000||retained.Sum(s=>(long)(s.Surface==null?0:s.Surface.Length))>64*1024*1024||retained.Sum(s=>s.Gradient==null?0:s.Gradient.Stops.Count)>100000){map.Ink=new List<InkStroke>(before);statusLabel.Text="Rysik: limit 2 mln punktów kontrolnych / 64 Mi znaków powierzchni / 100000 punktów gradientów zachowanych w historii sesji. Gest odrzucono. Zapisz i otwórz mapę, aby świadomie wyzerować historię.";canvas.Invalidate();return;}
            PushAction(new EditAction{Label="Rysik / gumka",BeforeInk=before,AfterInk=new List<InkStroke>(map.Ink)});MarkDirty();
        }
        void ClearInk()
        {
            if(map.Ink.Count==0)return;
            InkPathCache.Clear();InkGradientRenderer.Clear();
            var before=new List<InkStroke>(map.Ink);map.Ink=new List<InkStroke>();
            PushAction(new EditAction{Label="Wyczyść rysunki",BeforeInk=before,AfterInk=new List<InkStroke>()});MarkDirty();
        }
        void PointerAt(Point point)
        {
            if(hideInk)return;
            AgePointer();if(pointerLast.HasValue)pointerPhase+=Distance(pointerLast.Value,point)/(300*pointerDpiFactor);pointerLast=point;double phase=pointerPhase;
            pointerPosition=point;pointerTrail.Add(new PointerSample{Point=point,Time=PointerTime,Phase=phase,Palette=pointerPalette,Color=ColorTranslator.FromHtml(pointerColor)});if(pointerTrail.Count>128)pointerTrail.RemoveAt(0);
            pointerTimer.Start();AgePointer();
        }
        void AgePointer()
        {
            var all=pointerTrail.Select(p=>p.Point).ToList();if(pointerActive)all.Add(pointerPosition);if(pointerTrail.Count>0&&PointerTime-pointerTrail.Last().Time>=750)pointerTrail.Clear();
            else while(pointerTrail.Count>1&&PointerTime-pointerTrail[1].Time>=750)pointerTrail.RemoveAt(0);
            if(all.Count>0){int margin=(int)Math.Ceiling(pointerDiameter/2)+3;var box=Rectangle.FromLTRB(all.Min(p=>p.X)-margin,all.Min(p=>p.Y)-margin,all.Max(p=>p.X)+margin,all.Max(p=>p.Y)+margin);canvas.Invalidate(box);}
            if(!pointerActive&&pointerTrail.Count==0){pointerTimer.Stop();pointerPhase=0;pointerLast=null;}
            else if(pointerTrail.Count==0)pointerTimer.Stop(); // A stationary head has no animation once its trail is gone.
        }
        void DrawInk(Graphics graphics)
        {
            if(hideInk)return;var state=graphics.Save();graphics.TranslateTransform(pan.X,pan.Y);graphics.ScaleTransform(scale,scale);
            if(eraseGesture!=null)graphics.SetClip(eraseGesture.Preview,CombineMode.Exclude);
            InkLayerRenderer.Draw(graphics,map.Ink,activeStroke,canvas.ClientRectangle);
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
            var points=pointerTrail.Select(p=>new CometPoint(p.Point,now-p.Time,p.Phase,p.Palette,p.Color)).ToList();
            CometPoint? head=pointerActive?(CometPoint?)new CometPoint(pointerPosition,0,pointerPhase,pointerPalette,ColorTranslator.FromHtml(pointerColor)):null;
            comet.DrawColored(graphics,canvas.ClientRectangle,points,head,pointerDiameter);
        }
    }
}
