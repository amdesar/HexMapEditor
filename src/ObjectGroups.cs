using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HexMapEditor
{
    public sealed class ObjectStateDefinition
    {
        public string Id {get;set;}
        public string Name {get;set;}
        public string Color {get;set;}
        public bool CanReveal {get;set;}
        public ObjectStateDefinition(){Id=Guid.NewGuid().ToString("N");}
        public ObjectStateDefinition Copy(){return (ObjectStateDefinition)MemberwiseClone();}
        public override string ToString(){return Name;}
        public static List<ObjectStateDefinition> Defaults(){return new List<ObjectStateDefinition>{new ObjectStateDefinition{Id="Ally",Name="Sojusznik",Color="#39854B",CanReveal=true},new ObjectStateDefinition{Id="Neutral",Name="Neutralny",Color="#327EAF"},new ObjectStateDefinition{Id="Enemy",Name="Wróg",Color="#B54747"}};}
    }
    public sealed class ObjectGroup
    {
        public string Id {get;set;}
        public string Name {get;set;}
        public List<string> Members {get;set;}
        public string StateId {get;set;}
        public bool? Locked {get;set;}
        public bool? RevealsFog {get;set;}
        public int? RevealRadius {get;set;}
        public string SizeMode {get;set;}
        public float SizeValue {get;set;}
        public bool FrameEnabled { get; set; }
        public string FrameColor { get; set; }
        public ObjectGroup(){Id=Guid.NewGuid().ToString("N");Name="Grupa";Members=new List<string>();SizeMode="Individual";FrameColor="#4BBBCD";}
        public ObjectGroup Copy(){var copy=(ObjectGroup)MemberwiseClone();copy.Members=new List<string>(Members);return copy;}
    }
    internal static class GroupRules
    {
        public static ObjectGroup For(MapData map,string id){return map.Groups.FirstOrDefault(g=>g.Members.Contains(id));}
        public static bool Allows(MapData map,string state){var d=map.ObjectStates.FirstOrDefault(s=>s!=null&&s.Id==state);return d!=null&&d.CanReveal;}
        public static MapObject Effective(MapData map,MapObject obj)
        {
            var effective=obj.Copy();var group=For(map,obj.Id);
            if(group!=null){if(group.StateId!=null)effective.Side=group.StateId;if(group.Locked.HasValue)effective.Locked=group.Locked.Value;if(group.RevealsFog.HasValue)effective.RevealsFog=group.RevealsFog.Value;if(group.RevealRadius.HasValue)effective.RevealRadius=group.RevealRadius.Value;
                if(group.SizeMode=="Offset")effective.Size=Math.Max(8,Math.Min(1024,obj.Size+group.SizeValue));else if(group.SizeMode=="Common")effective.Size=group.SizeValue;
            }
            if(!Allows(map,effective.Side))effective.RevealsFog=false;return effective;
        }
        public static void Validate(MapData map)
        {
            if(map.ObjectStates==null||map.ObjectStates.Count<3||map.ObjectStates.Count>100||map.Groups==null||map.Groups.Count>2500)throw new InvalidDataException("Nieprawidłowe definicje stanów/grup.");
            var states=new HashSet<string>();foreach(var state in map.ObjectStates)if(state==null||String.IsNullOrWhiteSpace(state.Id)||state.Id.Length>200||!states.Add(state.Id)||String.IsNullOrWhiteSpace(state.Name)||state.Name.Length>200||!MapLogic.ValidColor(state.Color))throw new InvalidDataException("Nieprawidłowy lub powtórzony stan obiektu.");
            if (ObjectStateDefinition.Defaults().Any(s => !states.Contains(s.Id))) throw new InvalidDataException("Brak podstawowej definicji stanu.");
            if (map.ExcludedStateIds == null || map.ExcludedStateIds.Count > 5000 ||
                map.ExcludedStateIds.Distinct().Count() != map.ExcludedStateIds.Count ||
                map.ExcludedStateIds.Any(id => String.IsNullOrWhiteSpace(id) || id.Length > 200 || StateLibrary.IsBuiltin(id) || states.Contains(id)))
                throw new InvalidDataException("Nieprawidłowa lista wyłączonych stanów.");
            var objects=new HashSet<string>(map.Objects.Select(o=>o.Id));var groups=new HashSet<string>();var assigned=new HashSet<string>();
            foreach(var group in map.Groups){
                if(group==null||!MapLogic.ValidColor(group.FrameColor)||String.IsNullOrWhiteSpace(group.Id)||group.Id.Length>200||objects.Contains(group.Id)||!groups.Add(group.Id)||String.IsNullOrWhiteSpace(group.Name)||group.Name.Length>200||group.Members==null||group.Members.Count==0||group.Members.Any(id=>!objects.Contains(id)||!assigned.Add(id))||group.StateId!=null&&!states.Contains(group.StateId)||group.RevealRadius.HasValue&&(group.RevealRadius<0||group.RevealRadius>20)||!new[]{"Individual","Offset","Common"}.Contains(group.SizeMode)||!ObjectGeometry.Finite(group.SizeValue)||group.SizeValue< -1016||group.SizeValue>1024||group.SizeMode=="Offset"&&Math.Abs(group.SizeValue)>1016||group.SizeMode=="Common"&&(group.SizeValue<8||group.SizeValue>1024))throw new InvalidDataException("Nieprawidłowa grupa, członkostwo lub nadpisanie.");
            }
        }
    }
    public sealed partial class MainWindow
    {
        string selectedGroupId;
        MapData ProspectiveObjects(EditAction action)
        {
            var model=new MapData{Columns=map.Columns,Rows=map.Rows,Orientation=map.Orientation,RowParity=map.RowParity,ColumnParity=map.ColumnParity,Cells=map.Cells,IconAssets=map.IconAssets,Objects=map.Objects.Select(o=>o.Copy()).ToList(),ObjectStates=action.AfterStates??map.ObjectStates,Groups=action.AfterGroups??map.Groups, ExcludedStateIds=action.AfterExcludedStates??map.ExcludedStateIds};
            foreach(var c in action.Objects)c.Apply(model,true);return model;
        }
        void ReconcileFog(EditAction action,MapData model,bool alreadyApplied)
        {
            var covered=EffectiveFogCoverage(model);
            foreach(int index in covered){var cell=map.Cells[index];var layer=action.Layers.FirstOrDefault(l=>l.CellId==cell.Id&&l.Fog);
                if(layer==null&&cell.Fog){layer=new LayerChange{CellId=cell.Id,Fog=true,BeforeFog=true,AfterFog=false};action.Layers.Add(layer);}else if(layer!=null)layer.AfterFog=false;
                if(alreadyApplied)cell.Fog=false;
            }
            action.Layers.RemoveAll(l=>l.Fog&&!l.Icons&&l.BeforeFog==l.AfterFog);
        }
        HashSet<int> EffectiveFogCoverage(MapData model){var covered=new HashSet<int>();foreach(var obj in model.Objects)covered.UnionWith(ObjectGeometry.Reveal(model,GroupRules.Effective(model,obj),null));return covered;}
        bool CommitObjectTransaction(EditAction action,string mergeKey=null,bool reconcile=true)
        {
            var model=ProspectiveObjects(action);GroupRules.Validate(model);ObjectGeometry.Validate(model,new HashSet<string>(map.IconAssets.Select(a=>a.Id).Concat(action.RequiredAssets.Select(a=>a.Id))));
            if(reconcile)ReconcileFog(action,model,false);
            ApplyAction(action,true);
            if(mergeKey!=null&&mergeKey==propertyEditKey&&propertyEditAction!=null&&undo.Count>0&&Object.ReferenceEquals(undo.Peek(),propertyEditAction)&&redo.Count==0){
                foreach(var change in action.Objects){var old=propertyEditAction.Objects.FirstOrDefault(c=>(c.After??c.Before).Id==(change.After??change.Before).Id);if(old!=null)old.After=change.After;else propertyEditAction.Objects.Add(change);}
                if(action.AfterGroups!=null)propertyEditAction.AfterGroups=action.AfterGroups;
                foreach(var layer in action.Layers){var old=propertyEditAction.Layers.FirstOrDefault(l=>l.CellId==layer.CellId&&l.Fog);if(old!=null)old.AfterFog=layer.AfterFog;else propertyEditAction.Layers.Add(layer);}
                bool empty=propertyEditAction.Layers.Count==0&&propertyEditAction.Objects.All(c=>json.Serialize(c.Before)==json.Serialize(c.After))&&(propertyEditAction.BeforeGroups==null||json.Serialize(propertyEditAction.BeforeGroups)==json.Serialize(propertyEditAction.AfterGroups));
                if(empty){undo.Pop();EndObjectPropertyEdit();}
            }else{EndObjectPropertyEdit();PushAction(action);if(mergeKey!=null){propertyEditKey=mergeKey;propertyEditAction=action;}}
            // Live property edits must not hash every hex/description for each character.
            if (action.Layers.Count != 0)
            {
                var changedCells = new HashSet<string>(action.Layers.Select(l => l.CellId));
                foreach (var cell in map.Cells.Where(c => changedCells.Contains(c.Id))) savedState.CheckCell(cell);
            }
            dirty = savedState.CheckHeader(map);
            UpdateUI();
            return true;
        }
        bool CreateObjectGroup(string name)
        {
            EndCanvasGesture(true);if(selectedObjects.Count<2||map.Groups.Any(g=>g.Members.Any(selectedObjects.Contains)))return false;
            var group=new ObjectGroup{Name=name.Trim(),Members=map.Objects.Where(o=>selectedObjects.Contains(o.Id)).Select(o=>o.Id).ToList()};var after=map.Groups.Select(g=>g.Copy()).ToList();after.Add(group);
            var action=new EditAction{Label="Grupowanie",BeforeGroups=map.Groups.Select(g=>g.Copy()).ToList(),AfterGroups=after};GroupRules.Validate(ProspectiveObjects(action));ApplyAction(action,true);PushAction(action);selectedGroupId=group.Id;rightPanelOpen=true;MarkDirty();return true;
        }
        bool ChangeObjectGroup(ObjectGroup group,string mergeKey=null)
        {
            var before=map.Groups.First(g=>g.Id==group.Id);if(json.Serialize(before)==json.Serialize(group))return false;
            var action=new EditAction{Label="Właściwości grupy",BeforeGroups=map.Groups.Select(g=>g.Copy()).ToList(),AfterGroups=map.Groups.Select(g=>g.Id==group.Id?group.Copy():g.Copy()).ToList()};
            IncludeLibraryState(action, group.StateId);
            return CommitObjectTransaction(action,mergeKey,before.StateId!=group.StateId||before.RevealsFog!=group.RevealsFog||before.RevealRadius!=group.RevealRadius);
        }
        string AddObjectState(string name,string color,bool allows)
        {
            var definition = new ObjectStateDefinition { Name = name.Trim(), Color = color, CanReveal = allows };
            ChangeStateFile(definition, definition.Id, true, StatesDirectory);
            return definition.Id;
        }
        bool DetachObjects(string groupId,IEnumerable<string> members,bool[] keep)
        {
            EndCanvasGesture(true);if(keep==null)return false;if(keep.Length!=5)throw new ArgumentException("Wybierz pięć kategorii.");
            var group=map.Groups.Single(g=>g.Id==groupId);var ids=new HashSet<string>(members);if(!ids.IsSubsetOf(group.Members))throw new InvalidDataException("Nieprawidłowi członkowie grupy.");
            var action=new EditAction{Label=ids.Count==group.Members.Count?"Rozwiązanie grupy":"Odłączenie członka",BeforeGroups=map.Groups.Select(g=>g.Copy()).ToList(),AfterGroups=map.Groups.Select(g=>g.Copy()).ToList()};
            foreach(var obj in map.Objects.Where(o=>ids.Contains(o.Id))){var next=obj.Copy();var effective=GroupRules.Effective(map,obj);if(keep[0])next.Side=effective.Side;if(keep[1])next.Size=effective.Size;if(keep[2])next.Locked=effective.Locked;if(keep[3])next.RevealsFog=effective.RevealsFog;if(keep[4])next.RevealRadius=effective.RevealRadius;if(!GroupRules.Allows(map,next.Side))next.RevealsFog=false;
                if(json.Serialize(next)!=json.Serialize(obj))action.Objects.Add(new ObjectChange{Before=obj.Copy(),After=next,Index=map.Objects.IndexOf(obj)});
            }
            var remaining=action.AfterGroups.Single(g=>g.Id==groupId);remaining.Members.RemoveAll(ids.Contains);if(remaining.Members.Count==0)action.AfterGroups.Remove(remaining);selectedGroupId=null;return CommitObjectTransaction(action);
        }
        HashSet<string> ExpandedObjects(IEnumerable<string> ids)
        {var result=new HashSet<string>(ids);foreach(var group in map.Groups)if(group.Members.Any(result.Contains))result.UnionWith(group.Members);return result;}
        void ResolveSelectedGroup()
        {
            var group = SelectedWholeGroups().FirstOrDefault(g => selectedObjects.SetEquals(g.Members));
            selectedGroupId = group == null ? null : group.Id;
        }
    }
}
