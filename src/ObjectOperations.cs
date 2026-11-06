using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        readonly HashSet<string> selectedObjects=new HashSet<string>();
        void RequireObjectAsset(EditAction action,IconAsset asset)
        {
            if(map.IconAssets.Any(a=>a.Id==asset.Id)||action.RequiredAssets.Any(a=>a.Id==asset.Id))return;
            if(map.IconAssets.Count+action.RequiredAssets.Count>=IconAssets.MaxAssets||map.IconAssets.Sum(a=>(long)a.Png.Length)+action.RequiredAssets.Sum(a=>(long)a.Png.Length)+asset.Png.Length>44L*1024*1024)throw new IOException("Limit wspólnych zasobów mapy. Zapisz i ponownie otwórz mapę, aby zwolnić zasoby Undo.");
            action.RequiredAssets.Add(asset);
        }
        void AddReveal(EditAction action,MapObject obj,PointF? from)
        {
            foreach(int i in ObjectGeometry.Reveal(map,obj,from))if(map.Cells[i].Fog&&!action.Layers.Any(l=>l.CellId==map.Cells[i].Id&&l.Fog))action.Layers.Add(new LayerChange{CellId=map.Cells[i].Id,Fog=true,BeforeFog=true,AfterFog=false});
        }
        MapObject PlaceObject(PointF point)
        {
            var template=libraryObjects.FirstOrDefault(o=>o.Id==activeObject);if(!objectMaterial||template==null)return null;
            if(map.Objects.Count>=5000)throw new IOException("Limit 5000 instancji na mapie.");
            var obj=new MapObject{TemplateId=template.Id,Name=template.Name,AssetId=template.Asset.Id,X=point.X,Y=point.Y};
            ObjectGeometry.Validate(new MapData{Objects=new List<MapObject>{obj}},new HashSet<string>{template.Asset.Id});
            var action=new EditAction{Label="Umieszczenie obiektu"};RequireObjectAsset(action,template.Asset);action.Objects.Add(new ObjectChange{After=obj,Index=map.Objects.Count});AddReveal(action,obj,null);
            ApplyAction(action,true);PushAction(action);MarkDirty();return map.Objects.First(o=>o.Id==obj.Id);
        }
        bool ChangeObject(MapObject next)
        {
            EndCanvasGesture(true);var before=map.Objects.Single(o=>o.Id==next.Id);next=next.Copy();
            if(next.Side!="Ally")next.RevealsFog=false;
            ObjectGeometry.Validate(new MapData{Objects=new List<MapObject>{next}},new HashSet<string>(map.IconAssets.Select(a=>a.Id)));
            if(json.Serialize(before)==json.Serialize(next))return false;
            var action=new EditAction{Label="Właściwości obiektu"};action.Objects.Add(new ObjectChange{Before=before.Copy(),After=next,Index=map.Objects.IndexOf(before)});
            if(next.Side=="Ally"&&next.RevealsFog&&(!before.RevealsFog||before.Side!="Ally"||next.RevealRadius>before.RevealRadius))AddReveal(action,next,null);
            ApplyAction(action,true);PushAction(action);MarkDirty();return true;
        }
        void DeleteSelectedObjects()
        {
            EndCanvasGesture(true);var action=new EditAction{Label="Usunięcie instancji"};
            for(int i=0;i<map.Objects.Count;i++)if(selectedObjects.Contains(map.Objects[i].Id))action.Objects.Add(new ObjectChange{Before=map.Objects[i].Copy(),Index=i});
            if(action.Objects.Count==0)return;ApplyAction(action,true);selectedObjects.Clear();PushAction(action);MarkDirty();
        }
        List<ObjectChange> TransformObjects(MapData geometry,int dc,int dr)
        {
            var changes=new List<ObjectChange>();for(int i=0;i<map.Objects.Count;i++){var o=map.Objects[i];var p=ObjectGeometry.Transform(map,geometry,new PointF(o.X,o.Y),dc,dr);var next=o.Copy();next.X=p.X;next.Y=p.Y;changes.Add(new ObjectChange{Before=o.Copy(),After=next,Index=i});}return changes;
        }
        void ResetObjectDocument(){selectedObjects.Clear();objectMaterial=false;noMaterial=false;activeObject=null;objectGesture=null;objectDragBefore=null;selectionPath.Clear();}
        void FitEverything()
        {
            EndCanvasGesture(true);if(map.Objects.Count==0){FitMap();return;}RectangleF bounds=MapBounds();foreach(var o in map.Objects)bounds=RectangleF.Union(bounds,ObjectBounds(o));FitWorldBounds(bounds,false);UpdateUI();
        }
        RectangleF ObjectBounds(MapObject o){return ObjectGeometry.Bounds(o,map.IconAssets.First(a=>a.Id==o.AssetId));}
        void FitWorldBounds(RectangleF bounds,bool accountRight)
        {
            float width=canvas.ClientSize.Width-(accountRight&&right.Visible?right.Width:0);var viewport=new RectangleF(36,36,Math.Max(1,width-72),Math.Max(1,canvas.ClientSize.Height-72));
            scale=Math.Max(.000001f,Math.Min(viewport.Width/Math.Max(1,bounds.Width),viewport.Height/Math.Max(1,bounds.Height)));pan=new PointF(viewport.Left+viewport.Width/2-(bounds.Left+bounds.Width/2)*scale,viewport.Top+viewport.Height/2-(bounds.Top+bounds.Height/2)*scale);
        }
        void ShowObjectOnMap(string id)
        {
            var obj=map.Objects.FirstOrDefault(o=>o.Id==id);if(obj==null)return;selectedObjects.Clear();selectedObjects.Add(id);selected=-1;rightPanelOpen=true;UpdateUI();
            var bounds=ObjectBounds(obj);float width=Math.Max(1,canvas.Width-(rightPanelOpen?right.Width:0));scale=Math.Min(2f,Math.Min(Math.Max(1,width-80)/Math.Max(1,bounds.Width),Math.Max(1,canvas.Height-80)/Math.Max(1,bounds.Height)));
            pan=new PointF(width/2-obj.X*scale,canvas.Height/2-obj.Y*scale);UpdateUI();
        }
    }
}
