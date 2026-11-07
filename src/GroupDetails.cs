using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class GroupMemberChoice
    {
        public string Id,Name;
        public override string ToString(){return Name;}
    }
    public sealed partial class MainWindow
    {
        TableLayoutPanel groupLayout;
        TextBox groupName;
        ListBox groupMembers;
        ComboBox groupState,groupLock,groupReveal,groupSizeMode;
        NumericUpDown groupRadius,groupSize;
        CheckBox groupRadiusOverride;
        string groupStateSignature,groupMembersSignature;
        ComboBox OverrideChoice(){var c=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList};c.Items.AddRange(new object[]{"Indywidualnie","Tak","Nie"});c.SelectedIndex=0;return c;}
        void BuildGroupDetails(Control parent)
        {
            groupLayout=PropertyTable(parent);groupLayout.Visible=false;groupName=new TextBox{MaxLength=200};PropertyRow(groupLayout,0,"Nazwa grupy",groupName);
            groupMembers=new ListBox{Height=108,IntegralHeight=false,AccessibleName="Członkowie trwałej grupy"};SpanRow(groupLayout,1,groupMembers);
            SpanRow(groupLayout,2,PropertyButton("Wybierz pojedynczego członka",()=>{var chosen=groupMembers.SelectedItem as GroupMemberChoice;if(chosen!=null){EndCanvasGesture(true);selectedGroupId=null;selectedObjects.Clear();selectedObjects.Add(chosen.Id);UpdateUI();}}));
            groupState=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList};PropertyRow(groupLayout,3,"Stan",groupState);
            groupLock=OverrideChoice();PropertyRow(groupLayout,4,"Blokada",groupLock);groupReveal=OverrideChoice();PropertyRow(groupLayout,5,"Odkrywanie",groupReveal);
            groupRadiusOverride=new CheckBox{Text="Nadpisz promień",AutoSize=true};SpanRow(groupLayout,6,groupRadiusOverride);groupRadius=new NumericUpDown{Minimum=0,Maximum=20,Value=1};PropertyRow(groupLayout,7,"Promień",groupRadius);
            groupSizeMode=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList};groupSizeMode.Items.AddRange(new object[]{"Indywidualnie","Przesunięcie","Wspólny"});groupSizeMode.SelectedIndex=0;PropertyRow(groupLayout,8,"Rozmiar",groupSizeMode);
            groupSize=new NumericUpDown{Minimum=-1016,Maximum=1024};PropertyRow(groupLayout,9,"Wartość",groupSize);
            SpanRow(groupLayout,10,new Label{Text="Nakładka nie zmienia wartości indywidualnych. Przesunięcie jest liczone od ich rozmiaru i ograniczane do 8–1024. Stan bez uprawnienia zawsze blokuje skuteczne odkrywanie.",AutoSize=true,MaximumSize=new Size(340,0)});
            SpanRow(groupLayout,11,PropertyButton("Rozwiąż grupę",()=>{var g=CurrentGroup();if(g!=null)UnlinkWithDialog(g,g.Members.ToArray());}));
            SpanRow(groupLayout,12,PropertyButton("Usuń instancje grupy",DeleteSelectedObjects,true));
            groupName.TextChanged+=(s,e)=>{if(!String.IsNullOrWhiteSpace(groupName.Text))EditGroup("name",g=>g.Name=groupName.Text.Trim(),true);};
            groupState.SelectedIndexChanged+=(s,e)=>EditGroup("state",g=>g.StateId=groupState.SelectedIndex<=0?null:((ObjectStateDefinition)groupState.SelectedItem).Id,false);
            groupLock.SelectedIndexChanged+=(s,e)=>EditGroup("lock",g=>g.Locked=ReadOverride(groupLock),false);groupReveal.SelectedIndexChanged+=(s,e)=>EditGroup("reveal",g=>g.RevealsFog=ReadOverride(groupReveal),false);
            groupRadiusOverride.CheckedChanged+=(s,e)=>EditGroup("radius-mode",g=>g.RevealRadius=groupRadiusOverride.Checked?(int?)groupRadius.Value:null,false);groupRadius.ValueChanged+=(s,e)=>{if(groupRadiusOverride.Checked)EditGroup("radius",g=>g.RevealRadius=(int)groupRadius.Value,true);};
            groupSizeMode.SelectedIndexChanged+=(s,e)=>EditGroup("size-mode",g=>{g.SizeMode=new[]{"Individual","Offset","Common"}[groupSizeMode.SelectedIndex];if(g.SizeMode=="Common")g.SizeValue=Math.Max(8,Math.Min(1024,g.SizeValue));else if(g.SizeMode=="Offset")g.SizeValue=Math.Max(-1016,Math.Min(1016,g.SizeValue));},false);
            groupSize.ValueChanged+=(s,e)=>EditGroup("size",g=>g.SizeValue=(float)groupSize.Value,true);
            LiveNumber(groupSize);LiveNumber(groupRadius);
            foreach(Control c in new Control[]{groupName,groupSize,groupRadius})c.LostFocus+=(s,e)=>{if(!updating){EndObjectPropertyEdit();UpdateUI();}};
        }
        static bool? ReadOverride(ComboBox c){return c.SelectedIndex==0?(bool?)null:c.SelectedIndex==1;}
        static int OverrideIndex(bool? b){return !b.HasValue?0:b.Value?1:2;}
        ObjectGroup CurrentGroup(){return map.Groups.FirstOrDefault(g=>g.Id==selectedGroupId);}
        void EditGroup(string field,Action<ObjectGroup> edit,bool merge)
        {
            if(updating)return;var group=CurrentGroup();if(group==null)return;if(!merge)EndObjectPropertyEdit();var next=group.Copy();edit(next);try{ChangeObjectGroup(next,merge?group.Id+":"+field:null);}catch(Exception ex){ShowEditorMessage(ex.Message,"Właściwości grupy",MessageBoxButtons.OK,MessageBoxIcon.Warning);UpdateUI();}
        }
        void UpdateGroupDetails()
        {
            var g=CurrentGroup();if(g==null)return;if(propertyEditKey==null||!groupName.ContainsFocus)groupName.Text=g.Name;
            string states=json.Serialize(map.ObjectStates);if(groupStateSignature!=states){groupState.Items.Clear();groupState.Items.Add("Indywidualnie");foreach(var state in map.ObjectStates)groupState.Items.Add(state);groupStateSignature=states;}
            groupState.SelectedIndex=g.StateId==null?0:map.ObjectStates.FindIndex(s=>s.Id==g.StateId)+1;groupLock.SelectedIndex=OverrideIndex(g.Locked);groupReveal.SelectedIndex=OverrideIndex(g.RevealsFog);groupRadiusOverride.Checked=g.RevealRadius.HasValue;groupRadius.Value=g.RevealRadius??1;
            bool allowed=g.StateId==null?g.Members.Any(id=>GroupRules.Allows(map,map.Objects.Single(o=>o.Id==id).Side)):GroupRules.Allows(map,g.StateId);groupReveal.Enabled=allowed;groupRadiusOverride.Enabled=allowed;groupRadius.Enabled=allowed&&groupRadiusOverride.Checked;
            groupSizeMode.SelectedIndex=Array.IndexOf(new[]{"Individual","Offset","Common"},g.SizeMode);groupSize.Minimum=-1016;groupSize.Maximum=1024;groupSize.Value=(decimal)g.SizeValue;if(g.SizeMode=="Common")groupSize.Minimum=8;else if(g.SizeMode=="Offset")groupSize.Maximum=1016;groupSize.Enabled=g.SizeMode!="Individual";
            var members=g.Members.Select(id=>{var obj=map.Objects.Single(o=>o.Id==id);return new GroupMemberChoice{Id=id,Name=!previewWithoutFog&&ObjectFogLevel(obj)>0?"???":obj.Name};}).ToList();string signature=g.Id+":"+json.Serialize(members.Select(m=>new[]{m.Id,m.Name}).ToArray());
            if(groupMembersSignature!=signature){string chosen=(groupMembers.SelectedItem as GroupMemberChoice)==null?null:((GroupMemberChoice)groupMembers.SelectedItem).Id;groupMembers.Items.Clear();foreach(var member in members)groupMembers.Items.Add(member);groupMembers.SelectedIndex=Math.Max(0,members.FindIndex(m=>m.Id==chosen));groupMembersSignature=signature;}
        }
        void SelectGroup(ObjectGroup group){EndCanvasGesture(true);selectedObjects.Clear();selectedObjects.UnionWith(group.Members);selectedGroupId=group.Id;selected=-1;rightPanelOpen=true;UpdateUI();}
        void CreateGroupFromSelection()
        {
            EndCanvasGesture(true);if(selectedObjects.Count<2)return;if(map.Groups.Any(g=>g.Members.Any(selectedObjects.Contains))){ShowEditorMessage("Część wybranych obiektów już należy do grupy. Najpierw świadomie odłącz je lub rozwiąż grupę. Nie zmieniono członkostwa.","Grupowanie",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
            using(var d=new NameDialog("Nowa trwała grupa","Grupa"))if(RunModal(()=>d.ShowDialog(this))==DialogResult.OK)CreateObjectGroup(d.Value);
        }
        void UnlinkWithDialog(ObjectGroup group,IEnumerable<string> members)
        {var ids=members.ToArray();using(var d=new UngroupDialog(ids.Length==group.Members.Count))if(RunModal(()=>d.ShowDialog(this))==DialogResult.OK)DetachObjects(group.Id,ids,d.Keep);}
    }
}
