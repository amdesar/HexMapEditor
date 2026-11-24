using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        TableLayoutPanel groupLayout;
        DetailNameHeader groupName;
        ComboBox groupState;
        Button commonSize, offsetSize;
        ScrubNumber groupSize, groupRadius, groupLightRadius;
        CheckBox groupFrame, groupReveal, groupLight, groupLock;
        Button groupFrameColor;
        MemberMatrix groupMatrix;
        string groupStateSignature, groupMembersSignature;

        void BuildGroupDetails(Control parent)
        {
            groupLayout = PropertyTable(parent);
            groupLayout.Hide();
            groupName = new DetailNameHeader("Nazwa grupy", value => EditGroup("name", g => g.Name=value, false), tips);
            SpanRow(groupLayout,0,groupName);
            SetDetailRowHeight(groupLayout,0,64);
            groupFrame = new SwitchBox { Text="Obramowanie członków", AutoSize=true };
            groupFrameColor = PropertyButton("Kolor",ChooseFrameColor);
            SpanRow(groupLayout,1,PropertyLine(groupFrame,groupFrameColor));
            SetDetailRowHeight(groupLayout,1,40);
            SpanRow(groupLayout,2,DetailSection("Ustawienia grupy"));

            groupState = new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList,DrawMode=DrawMode.OwnerDrawFixed };
            groupState.DrawItem+=DrawGroupState;
            PropertyRow(groupLayout,3,"Stan",groupState);
            groupSize = new ScrubNumber { Minimum=-1016, Maximum=1024, Width=57 };
            commonSize=SizeAction("≡","Rozmiar wspólny",()=>SetGroupSizeMode("Common"));
            offsetSize=SizeAction("±","Przesunięcie rozmiaru",()=>SetGroupSizeMode("Offset"));
            var reset = SizeAction("↺","Reset rozmiaru (wspólny 30, przesunięcie 0)",()=>EditGroup("size",g=>g.SizeValue=g.SizeMode=="Common"?30:0,false));
            PropertyRow(groupLayout,4,"Rozmiar",PropertyLine(groupSize,commonSize,offsetSize,reset));

            groupReveal = PropertySwitch("Odkrywanie mgły");
            groupLight = PropertySwitch("Pole widzenia");
            groupLock = PropertySwitch("Blokada ruchu");
            groupRadius = new ScrubNumber { Minimum=0, Maximum=20, Width=57 };
            groupLightRadius = new ScrubNumber { Minimum=0, Maximum=20, Width=57 };
            PropertyRow(groupLayout,5,"Odkrywanie mgły",PropertyLine(WidthAlignedSwitch(groupReveal),RadiusControl(groupRadius)));
            PropertyRow(groupLayout,6,"Pole widzenia",PropertyLine(WidthAlignedSwitch(groupLight),RadiusControl(groupLightRadius)));
            PropertyRow(groupLayout,7,"Blokada ruchu",PropertyLine(WidthAlignedSwitch(groupLock)));
            SpanRow(groupLayout,8,DetailSection("Ustawienia indywidualne"));
            SpanRow(groupLayout,9,new Label { Text="Włącz, aby użyć wartości grupy.",AutoSize=true });
            groupMatrix = new MemberMatrix("Użyj wartości grupy");
            groupMatrix.ColumnClicked = property => EditGroup("column",g=>{
                var eligible=property==GroupProperty.Fog?map.Objects.Where(o=>g.Members.Contains(o.Id)&&
                    ObjectFogLevel(o)==0&&GroupRules.Allows(map,GroupRules.EffectiveSide(g,o))).Select(o=>o.Id).ToArray():null;
                GroupRules.ToggleColumn(g,property,eligible);
            },false);
            groupMatrix.MemberClicked = SelectPanelMember;
            groupMatrix.ValueChanged = (id,property,on) => EditGroup("member",g=>GroupRules.Set(g,id,property,on),false);
            SpanRow(groupLayout,10,groupMatrix);

            SpanRow(groupLayout,11,DetailSection("Działania"));
            var dissolve = PropertyButton("Rozwiąż grupę",()=>{var g=CurrentGroup();if(g!=null)UnlinkWithDialog(g,g.Members.ToArray());});
            Amber(dissolve);
            SpanRow(groupLayout,12,dissolve);
            SpanRow(groupLayout,13,PropertyButton("Usuń grupę i wszystkie obiekty",ConfirmDeleteSelectedObjects,true));

            groupState.SelectedIndexChanged += (s,e) => { var state=groupState.SelectedItem as ObjectStateDefinition;if(state!=null)EditGroup("state",g=>g.StateId=state.Id,false); };
            groupSize.ValueChanged += (s,e)=>EditGroup("size",g=>g.SizeValue=(float)groupSize.Value,true);
            groupFrame.CheckedChanged += (s,e)=>EditGroup("frame",g=>g.FrameEnabled=groupFrame.Checked,false);
            groupReveal.CheckedChanged += (s,e)=>EditGroup("fog",g=>g.RevealsFog=groupReveal.Checked,false);
            groupLight.CheckedChanged += (s,e)=>EditGroup("sight",g=>g.LightEnabled=groupLight.Checked,false);
            groupLock.CheckedChanged += (s,e)=>EditGroup("lock",g=>g.Locked=groupLock.Checked,false);
            groupRadius.ValueChanged += (s,e)=>EditGroup("fog-radius",g=>g.RevealRadius=(int)groupRadius.Value,true);
            groupLightRadius.ValueChanged += (s,e)=>EditGroup("sight-radius",g=>g.LightRadius=(int)groupLightRadius.Value,true);
            foreach(var number in new[] { groupSize,groupRadius,groupLightRadius }) LiveNumber(number);
        }
        void SetGroupSizeMode(string mode)
        {
            EditGroup("size-mode",g=>{
                if(g.SizeMode==mode)return;
                g.SizeMode=mode;
                g.SizeValue=Math.Max(mode=="Common"?8:-1016,Math.Min(mode=="Common"?1024:1016,g.SizeValue));
            },false);
        }
        void DrawGroupState(object sender,DrawItemEventArgs e)
        {
            e.DrawBackground();
            if(e.Index<0)return;
            var state=(ObjectStateDefinition)groupState.Items[e.Index];
            var color=ColorTranslator.FromHtml(state.Color);
            if(!groupState.Enabled)color=Color.FromArgb(55,60,72);
            using(var brush=new SolidBrush(color))e.Graphics.FillRectangle(brush,e.Bounds);
            var foreground=color.R*.299+color.G*.587+color.B*.114>145?Color.Black:Color.White;
            var label=Rectangle.Inflate(e.Bounds,-4,0);
            TextRenderer.DrawText(e.Graphics,state.Name,e.Font,label,foreground,
                TextFormatFlags.SingleLine|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
            e.DrawFocusRectangle();
        }
        ObjectGroup CurrentGroup(){return map.Groups.FirstOrDefault(g=>g.Id==selectedGroupId);}
        bool GroupMasked(ObjectGroup group){return group.Members.All(id=>ObjectFogLevel(map.Objects.Single(o=>o.Id==id))>0);}
        void EditGroup(string field,Action<ObjectGroup> edit,bool merge)
        {
            if(updating)return;
            var group=CurrentGroup();
            if(group==null||GroupMasked(group))return;
            if(!GuardLayer(MapLayer.Objects)){UpdateUI();return;}
            if(!merge)EndObjectPropertyEdit();
            var next=group.Copy();edit(next);
            try { ChangeObjectGroup(next,merge?group.Id+":"+field:null); }
            catch(Exception ex){ShowEditorMessage(ex.Message,"Właściwości grupy",MessageBoxButtons.OK,MessageBoxIcon.Warning);UpdateUI();}
        }
        void UpdateGroupDetails()
        {
            var group=CurrentGroup();
            if(group==null)return;
            bool masked=GroupMasked(group);
            groupLayout.Enabled=!masked;
            groupName.Display(group.Id,masked?"":group.Name,masked?"???":group.Name,null,
                !masked&&group.FrameEnabled?ColorTranslator.FromHtml(group.FrameColor):ChoiceStyle.ActiveBorder,!masked,!masked&&group.FrameEnabled);
            if(masked)
            {
                groupFrame.Checked=groupReveal.Checked=groupLight.Checked=groupLock.Checked=false;
                groupFrameColor.Visible=false;
                groupState.SelectedIndex=-1;
                groupSize.Minimum=-1016;groupSize.Value=0;
                groupRadius.Parent.Visible=groupLightRadius.Parent.Visible=false;
                groupMatrix.Hide();
                return;
            }
            var states=AvailableStates();
            string signature=json.Serialize(states);
            if(signature!=groupStateSignature)
            {
                groupState.BeginUpdate();groupState.Items.Clear();groupState.Items.AddRange(states.Cast<object>().ToArray());groupState.EndUpdate();
                groupStateSignature=signature;
            }
            groupState.SelectedIndex=states.FindIndex(s=>s.Id==group.StateId);
            groupFrame.Checked=group.FrameEnabled;
            groupFrameColor.Visible=group.FrameEnabled;
            groupFrameColor.BackColor=ColorTranslator.FromHtml(group.FrameColor);
            groupFrameColor.ForeColor=groupFrameColor.BackColor.GetBrightness()>.55?Color.Black:Color.White;
            bool offset=group.SizeMode=="Offset";
            ChoiceStyle.Apply(commonSize,!offset);
            ChoiceStyle.Apply(offsetSize,offset);
            commonSize.AccessibleDescription=offset?"Nieaktywny":"Aktywny";
            offsetSize.AccessibleDescription=offset?"Aktywny":"Nieaktywny";
            groupSize.Minimum=offset?-1016:8;groupSize.Maximum=offset?1016:1024;
            groupSize.Value=(decimal)group.SizeValue;
            groupReveal.Checked=group.RevealsFog.Value;
            groupReveal.Enabled=GroupRules.Allows(map,group.StateId)
                || map.Objects.Any(o=>group.Members.Contains(o.Id)&&GroupRules.Allows(map,o.Side));
            groupRadius.Value=group.RevealRadius.Value;
            groupRadius.Parent.Visible=groupReveal.Checked;
            groupLight.Checked=group.LightEnabled.Value;
            groupLightRadius.Value=group.LightRadius.Value;
            groupLightRadius.Parent.Visible=groupLight.Checked;
            groupLock.Checked=group.Locked.Value;
            var members=group.Members.Select(id=>map.Objects.Single(o=>o.Id==id)).Select(o=>new MatrixMember {
                Id=o.Id, Hidden=ObjectFogLevel(o)>0, Name=ObjectFogLevel(o)>0?"???":o.Name, Value=GroupRules.Used(group,o.Id),
                FogAvailable=GroupRules.Allows(map,GroupRules.EffectiveSide(group,o))
            }).ToArray();
            signature=group.Id+json.Serialize(members.Select(m=>new {m.Id,m.Name,m.Hidden}));
            if(signature!=groupMembersSignature){groupMatrix.SetMembers(members);groupMembersSignature=signature;}
            groupMatrix.SetValues(members);
            groupMatrix.Show();
        }
        void SelectPanelMember(string id)
        {
            var obj=map.Objects.FirstOrDefault(o=>o.Id==id);
            if(obj==null||ObjectFogLevel(obj)>0)return;
            EndCanvasGesture(true);ChooseObjectMember(obj,false,true);
        }
        void SelectGroup(ObjectGroup group){EndCanvasGesture(true);selectedObjects.Clear();selectedObjects.UnionWith(group.Members);groupSelectionOrder.Clear();groupSelectionOrder.Add(group.Id);selectedGroupId=group.Id;selected=-1;rightPanelOpen=true;UpdateUI();}
        void ChooseFrameColor()
        {
            var group=CurrentGroup();
            if(group==null||!GuardLayer(MapLayer.Objects))return;
            using(var dialog=new EditorColorDialog(ColorTranslator.FromHtml(group.FrameColor)))
                if(RunModal(()=>dialog.ShowDialog(this))==DialogResult.OK)
                    EditGroup("frame-color",g=>g.FrameColor=dialog.Hex,false);
        }
        void UnlinkWithDialog(ObjectGroup group,IEnumerable<string> members)
        {
            if(!GuardLayer(MapLayer.Objects))return;
            var ids=members.ToArray();
            var rows=ids.Select(id=>map.Objects.Single(o=>o.Id==id)).Select(o=>new MatrixMember {
                Id=o.Id, Name=ObjectFogLevel(o)>0?"???":o.Name, Value=GroupRules.Used(group,o.Id)
            }).ToArray();
            using(var dialog=new UngroupDialog(rows,ids.Length==group.Members.Count))
                if(RunModal(()=>dialog.ShowDialog(this))==DialogResult.OK)DetachObjects(group.Id,dialog.Keep);
        }
    }
}
