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
        EditAction objectGesture;
        Dictionary<string,MapObject> objectDragBefore;
        PointF objectDragStart;
        PointF? objectPreviousCursor;
        readonly List<PointF> selectionPath=new List<PointF>();
        bool objectSelecting,objectMoving,objectClick,selectionControl;
        int selectionKind;
        PointF World(Point p){return new PointF((p.X-pan.X)/scale,(p.Y-pan.Y)/scale);}
        PointF Screen(PointF p){return new PointF(pan.X+p.X*scale,pan.Y+p.Y*scale);}
        bool HiddenPoint(PointF world){int at=ObjectGeometry.Hit(map,world);return at>=0&&map.Cells[at].Fog;}
        Region VisibleObjectRegion()
        {
            var region=new Region();for(int i=0;i<map.Cells.Count;i++)if(map.Cells[i].Fog)using(var path=ObjectGeometry.Hex(map,BrushGeometry.Center(map,i%map.Columns,i/map.Columns),28.03f))region.Exclude(path);return region;
        }
        bool ObjectVisible(MapObject o)
        {using(var region=VisibleObjectRegion()){region.Intersect(ObjectBounds(o));using(var matrix=new Matrix())return region.GetRegionScans(matrix).Any(r=>r.Width>.001f&&r.Height>.001f);}}
        MapObject HitObject(PointF world)
        {if(HiddenPoint(world))return null;return map.Objects.LastOrDefault(o=>ObjectBounds(o).Contains(world));}
        void ChooseObject(MapObject hit,bool control)
        {if(control){if(!selectedObjects.Add(hit.Id))selectedObjects.Remove(hit.Id);}else if(!selectedObjects.Contains(hit.Id)){selectedObjects.Clear();selectedObjects.Add(hit.Id);}selected=-1;rightPanelOpen=selectedObjects.Count>0;UpdateUI();}
        void DrawObjects(Graphics g)
        {
            var state=g.Save();g.TranslateTransform(pan.X,pan.Y);g.ScaleTransform(scale,scale);
            using(var visible=VisibleObjectRegion()){g.SetClip(visible,CombineMode.Intersect);
                foreach(var o in map.Objects){var bounds=ObjectBounds(o);g.DrawImage(AssetBitmap(map.IconAssets.First(a=>a.Id==o.AssetId),false),bounds);if(selectedObjects.Contains(o.Id))using(var pen=new Pen(Color.Gold,2/scale))g.DrawRectangle(pen,bounds.X,bounds.Y,bounds.Width,bounds.Height);}
            }g.Restore(state);
            if(objectSelecting&&selectionPath.Count>1)using(var pen=new Pen(Color.CornflowerBlue,1.5f)){var points=selectionPath.Select(Screen).ToArray();if(selectionKind==1){g.DrawLines(pen,points);g.DrawLine(pen,points[points.Length-1],points[0]);}else{var r=SelectionRectangle(selectionPath[0],selectionPath[selectionPath.Count-1]);var a=Screen(r.Location);g.DrawRectangle(pen,a.X,a.Y,r.Width*scale,r.Height*scale);}}
        }
        static RectangleF SelectionRectangle(PointF a,PointF b){return RectangleF.FromLTRB(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Max(a.X,b.X),Math.Max(a.Y,b.Y));}
        bool ObjectMouseDown(MouseEventArgs e)
        {
            if(gestureTool==EditorTool.Brush&&objectMaterial){try{PlaceObject(World(e.Location));}catch(Exception ex){ShowEditorMessage(ex.Message,"Nie można umieścić obiektu",MessageBoxButtons.OK,MessageBoxIcon.Warning);}return true;}
            if(gestureTool!=EditorTool.Select)return false;
            var point=World(e.Location);var hit=HitObject(point);selectionControl=(ModifierKeys&Keys.Control)!=0;objectClick=hit!=null;objectMoving=false;objectSelecting=false;
            if(hit!=null){ChooseObject(hit,selectionControl);
                if(selectedObjects.Contains(hit.Id)){
                    objectDragBefore=map.Objects.Where(o=>selectedObjects.Contains(o.Id)&&!o.Locked).ToDictionary(o=>o.Id,o=>o.Copy());objectDragStart=point;objectPreviousCursor=point;objectGesture=new EditAction{Label="Ruch obiektów"};objectMoving=true;
                    int locked=selectedObjects.Count-objectDragBefore.Count;if(locked>0)statusLabel.Text="Pominięto zablokowane obiekty: "+locked;
                }return true;
            }
            if(!selectionControl)selectedObjects.Clear();objectSelecting=true;selectionPath.Clear();selectionPath.Add(point);canvas.Invalidate();return true;
        }
        bool ObjectMouseMove(MouseEventArgs e)
        {
            if(gestureTool==EditorTool.Brush&&objectMaterial)return true;
            if(gestureTool!=EditorTool.Select)return false;
            if(!canvas.ClientRectangle.Contains(e.Location)){objectPreviousCursor=null;return true;}
            var p=World(e.Location);if(objectMoving&&objectDragBefore!=null){
                if(Math.Abs(e.X-dragOrigin.X)+Math.Abs(e.Y-dragOrigin.Y)<=3&&!dragging)return true;dragging=true;
                float dx=p.X-objectDragStart.X,dy=p.Y-objectDragStart.Y;
                if(objectDragBefore.Count>0){dx=Math.Max(objectDragBefore.Values.Max(o=>-10000000-o.X),Math.Min(objectDragBefore.Values.Min(o=>10000000-o.X),dx));dy=Math.Max(objectDragBefore.Values.Max(o=>-10000000-o.Y),Math.Min(objectDragBefore.Values.Min(o=>10000000-o.Y),dy));}
                foreach(var before in objectDragBefore.Values){var obj=map.Objects.First(o=>o.Id==before.Id);var from=objectPreviousCursor.HasValue?new PointF?(new PointF(obj.X,obj.Y)):null;obj.X=before.X+dx;obj.Y=before.Y+dy;
                    AddReveal(objectGesture,obj,from);
                }
                foreach(var layer in objectGesture.Layers)layer.Apply(map.Cells.First(c=>c.Id==layer.CellId),true);
                objectPreviousCursor=p;canvas.Invalidate();return true;
            }
            if(objectSelecting){if(Math.Abs(e.X-dragOrigin.X)+Math.Abs(e.Y-dragOrigin.Y)>3)dragging=true;if(selectionKind==1){if(selectionPath.Count==1||Distance(selectionPath.Last(),p)>3/scale)selectionPath.Add(p);}else{if(selectionPath.Count==1)selectionPath.Add(p);else selectionPath[1]=p;}canvas.Invalidate();return true;}
            return objectClick;
        }
        static double Distance(PointF a,PointF b){return Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));}
        bool ObjectMouseUp(MouseEventArgs e)
        {
            if(gestureTool!=EditorTool.Select)return false;
            if(objectSelecting&&dragging){
                var matches=new List<MapObject>();if(selectionKind==1&&selectionPath.Count>=3){using(var path=new GraphicsPath()){path.AddPolygon(selectionPath.ToArray());matches=map.Objects.Where(o=>path.IsVisible(o.X,o.Y)&&ObjectVisible(o)).ToList();}}
                else if(selectionKind==0){var r=SelectionRectangle(selectionPath[0],World(e.Location));matches=map.Objects.Where(o=>r.Contains(o.X,o.Y)&&ObjectVisible(o)).ToList();}
                if(!selectionControl)selectedObjects.Clear();foreach(var o in matches){if(selectionControl&&!selectedObjects.Add(o.Id))selectedObjects.Remove(o.Id);else selectedObjects.Add(o.Id);}selected=-1;rightPanelOpen=selectedObjects.Count>0;UpdateUI();return true;
            }
            if(objectClick)return true;return false;
        }
        void FinishObjectGesture(bool commit)
        {
            if(objectGesture!=null&&objectDragBefore!=null){
                foreach(var before in objectDragBefore.Values){var now=map.Objects.FirstOrDefault(o=>o.Id==before.Id);if(now!=null&&(now.X!=before.X||now.Y!=before.Y))objectGesture.Objects.Add(new ObjectChange{Before=before,After=now.Copy(),Index=map.Objects.IndexOf(now)});}
                if(commit&&(objectGesture.Objects.Count>0||objectGesture.Layers.Count>0)){PushAction(objectGesture);MarkDirty();}
                else if(!commit){foreach(var before in objectDragBefore.Values){var index=map.Objects.FindIndex(o=>o.Id==before.Id);if(index>=0)map.Objects[index]=before.Copy();}foreach(var layer in objectGesture.Layers)layer.Apply(map.Cells.First(c=>c.Id==layer.CellId),false);}
            }
            objectGesture=null;objectDragBefore=null;objectMoving=false;objectSelecting=false;objectClick=false;objectPreviousCursor=null;selectionPath.Clear();canvas.Invalidate();
        }
    }
}
