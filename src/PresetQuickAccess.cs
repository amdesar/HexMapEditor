using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class QuickSnapshot
    {
        internal string[] Icons,Objects;internal Dictionary<string,LibraryIcon> IconRefs;internal Dictionary<string,ObjectTemplate> ObjectRefs;
    }
    public sealed partial class MainWindow
    {
        sealed class QuickPair{internal readonly QuickAccess Icons=new QuickAccess(),Objects=new QuickAccess();internal readonly Dictionary<string,LibraryIcon> IconRefs=new Dictionary<string,LibraryIcon>();internal readonly Dictionary<string,ObjectTemplate> ObjectRefs=new Dictionary<string,ObjectTemplate>();}
        readonly Dictionary<string,QuickPair> quickSets=new Dictionary<string,QuickPair>();


        string quickPreset;
        QuickPair CurrentQuick{get{EnsureQuickPreset();return quickSets[map.ActivePresetId];}}
        LibraryIcon FindIcon(string id){LibraryIcon i;return id!=null&&CurrentQuick.IconRefs.TryGetValue(id,out i)?i:libraryIcons.FirstOrDefault(a=>a.Id==id);}
        ObjectTemplate FindTemplate(string id){ObjectTemplate o;return id!=null&&CurrentQuick.ObjectRefs.TryGetValue(id,out o)?o:libraryObjects.FirstOrDefault(a=>a.Id==id);}
        void EnsureQuickPreset()
        {
            if(map==null)return;var p=ActivePreset();QuickPair set;if(!quickSets.TryGetValue(p.Id,out set)){quickSets[p.Id]=set=new QuickPair();set.Icons.Restore(p.Icons.Select(i=>i.Id));set.Objects.Restore(p.Objects.Select(o=>o.Id));foreach(var i in p.Icons)set.IconRefs[i.Id]=i.Icon();foreach(var o in p.Objects)set.ObjectRefs[o.Id]=o.Template();}
            if(quickPreset==p.Id)return;quickPreset=p.Id;iconMaterial=objectMaterial=false;activeIcon=activeObject=null;noMaterial=p.Terrains.Count==0;
        }
        TerrainPreset WithVisibleSets(TerrainPreset p)
        {
            var c=p.Copy();c.Icons=CurrentQuick.Icons.Visible.Select(FindIcon).Where(i=>i!=null).Select(i=>new PresetIcon{Id=i.Id,Name=i.Name,AssetId=i.Asset.Id,Asset=i.Asset}).ToList();
            c.Objects=CurrentQuick.Objects.Visible.Select(FindTemplate).Where(o=>o!=null).Select(o=>new PresetObject{Id=o.Id,Name=o.Name,AssetId=o.Asset.Id,Asset=o.Asset,LightEnabled=o.LightEnabled,LightRadius=o.LightRadius}).ToList();return c;
        }
        void RestoreQuickSections(TerrainPreset p,PresetSections sections)
        {
            var q=CurrentQuick;if(sections.HasFlag(PresetSections.Icons)){q.IconRefs.Clear();foreach(var i in p.Icons)q.IconRefs[i.Id]=i.Icon();q.Icons.Restore(p.Icons.Select(i=>i.Id));}
            if(sections.HasFlag(PresetSections.Objects)){q.ObjectRefs.Clear();foreach(var o in p.Objects)q.ObjectRefs[o.Id]=o.Template();q.Objects.Restore(p.Objects.Select(o=>o.Id));}RefreshIconPreview();RefreshObjectPreview();
        }
        void UsedIcon(string id,bool added=false){var q=CurrentQuick;var entry=FindIcon(id);if(entry==null)return;string before=String.Join("|",q.Icons.Visible);q.IconRefs[id]=entry.Copy();if(added)q.Icons.Added(id);else q.Icons.Used(id);if(before!=String.Join("|",q.Icons.Visible))RefreshIconPreview();}
        void UsedObject(string id,bool added=false){var q=CurrentQuick;var entry=FindTemplate(id);if(entry==null)return;string before=String.Join("|",q.Objects.Visible);q.ObjectRefs[id]=entry.Copy();if(added)q.Objects.Added(id);else q.Objects.Used(id);if(before!=String.Join("|",q.Objects.Visible))RefreshObjectPreview();}
        void QuickMenu(Button b,string id,bool objects)
        {
            b.Size=new Size(40,28);b.Margin=new Padding(2);if(b.Image!=null){var image=b.Image;b.Image=new Bitmap(image,24,24);image.Dispose();}
            var menu=new ContextMenuStrip{Renderer=new FitMenuRenderer(),BackColor=Color.FromArgb(34,38,48),ForeColor=Color.WhiteSmoke};
            menu.Items.Add("Usuń z szybkiego dostępu",null,(s,e)=>{if(objects){CurrentQuick.Objects.Remove(id);RefreshObjectPreview();}else{CurrentQuick.Icons.Remove(id);RefreshIconPreview();}});b.ContextMenuStrip=menu;b.Disposed+=(s,e)=>menu.Dispose();
        }
        void QuickPlaceholders(FlowLayoutPanel panel){while(panel.Controls.Count<12)panel.Controls.Add(new Button{Size=new Size(40,28),Margin=new Padding(2),Enabled=false,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(34,38,48),AccessibleName="Puste miejsce szybkiego dostępu"});}
        QuickSnapshot CaptureQuick(){var q=CurrentQuick;return new QuickSnapshot{Icons=q.Icons.All,Objects=q.Objects.All,IconRefs=q.IconRefs.ToDictionary(x=>x.Key,x=>x.Value.Copy()),ObjectRefs=q.ObjectRefs.ToDictionary(x=>x.Key,x=>x.Value.Copy())};}
        void ApplyQuick(string id,QuickSnapshot snapshot){QuickPair q;if(!quickSets.TryGetValue(id,out q))quickSets[id]=q=new QuickPair();q.Icons.RestoreHistory(snapshot.Icons);q.Objects.RestoreHistory(snapshot.Objects);q.IconRefs.Clear();q.ObjectRefs.Clear();foreach(var i in snapshot.IconRefs)q.IconRefs[i.Key]=i.Value.Copy();foreach(var o in snapshot.ObjectRefs)q.ObjectRefs[o.Key]=o.Value.Copy();if(id==map.ActivePresetId){RefreshIconPreview();RefreshObjectPreview();}}
    }
}
