using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        DetailsOverlay objectDetailPanel;
        Control hexDetailScroll;
        DetailNameHeader objectName;
        readonly ScrubNumber objectSize=new ScrubNumber{Minimum=8,Maximum=1024,Value=30,Width=57},
            objectRadius=new ScrubNumber{Minimum=0,Maximum=20,Value=1,Width=57},
            objectLightRadius=new ScrubNumber{Minimum=0,Maximum=20,Value=1,Width=57};
        readonly CheckBox objectLocked=PropertySwitch("Blokada ruchu"),objectReveal=PropertySwitch("Odkrywanie mgły"),
            objectLight=PropertySwitch("Pole widzenia");
        Label objectTerrain,effectiveLabel;
        LinkLabel memberToGroup;
        PictureBox objectThumbnail;
        FlowLayoutPanel stateTiles;
        Button addStateTile,instanceDelete,memberDetach;
        TableLayoutPanel instanceLayout;
        Panel detailBody;
        bool arrangingDetails;
        string stateTileSignature,propertyEditKey;
        EditAction propertyEditAction;
        bool clearAfterIconEdit;
        static bool TextInputFocused(Control parent){if(parent.ContainsFocus&&(parent is TextBoxBase||parent is ScrubNumber||parent is RichNoteEditor))return true;return parent.Controls.Cast<Control>().Any(TextInputFocused);}
        void EndObjectPropertyEdit(){propertyEditKey=null;propertyEditAction=null;}
        void LiveNumber(ScrubNumber number)
        {
            EditAction[] beforeUndo=null,beforeRedo=null;
            number.EditStarting+=(s,e)=>{EndObjectPropertyEdit();beforeUndo=undo.ToArray();beforeRedo=redo.ToArray();};
            number.EditFinished+=(s,e)=>{commandTarget=number;EndObjectPropertyEdit();if(beforeUndo!=null&&(e.Cancelled||!e.Changed)){foreach(var a in undo.TakeWhile(a=>!beforeUndo.Contains(a)).ToArray())ApplyAction(a,false);undo.Clear();foreach(var a in beforeUndo.Reverse())undo.Push(a);redo.Clear();foreach(var a in beforeRedo.Reverse())redo.Push(a);dirty=savedState.CheckAll(map);}beforeUndo=null;beforeRedo=null;UpdateUI();};
        }
        void ClearIconMaterial(){activeIcon=null;iconMaterial=false;objectMaterial=false;noMaterial=true;UpdateMaterialIndicator();canvas.Invalidate();}
        void UpdateBrushAvailability(){if(shapePicker==null)return;bool applicable=currentTool==EditorTool.FogBrush||currentTool==EditorTool.DarkBrush||currentTool==EditorTool.Brush&&!objectMaterial&&!noMaterial;shapePicker.Enabled=applicable;sizePicker.Enabled=applicable&&brushShape!=BrushShape.Point;heightPicker.Enabled=applicable;rotationPicker.Visible=apexPicker.Visible=shapePicker.Visible=sizePicker.Visible=heightPicker.Visible=fillPicker.Visible=applicable;rotationPicker.Enabled=applicable&&brushShape!=BrushShape.Point&&brushShape!=BrushShape.Circle;apexPicker.Visible=applicable&&brushShape==BrushShape.Triangle;heightPicker.Visible=applicable&&brushShape==BrushShape.Rectangle;fillPicker.Enabled=applicable&&brushShape!=BrushShape.Point&&brushShape!=BrushShape.Line&&brushShape!=BrushShape.Horizontal&&brushShape!=BrushShape.Vertical;if(shapeCaption!=null)shapeCaption.Visible=applicable;if(sizeCaption!=null){sizeCaption.Visible=rotationCaption.Visible=applicable;sizeCaption.Text=brushShape==BrushShape.Rectangle?"A":"Rozm.";secondCaption.Text=brushShape==BrushShape.Triangle?"Kąt":"B";secondCaption.Visible=applicable&&(brushShape==BrushShape.Triangle||brushShape==BrushShape.Rectangle);}if(leftSections!=null)leftSections.RowStyles[0].Height=applicable?MaxToolSection:(currentTool==EditorTool.Stylus||currentTool==EditorTool.Eraser?176:112);LayoutLeftSections();UpdateInkSettings();}
        TableLayoutPanel PropertyTable(Control parent){var t=new TableLayoutPanel{ColumnCount=2,Padding=new Padding(4)};t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,111));t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));parent.Controls.Add(t);return t;}
        static void EnsurePropertyRows(TableLayoutPanel table,int row){while(table.RowStyles.Count<=row)table.RowStyles.Add(new RowStyle(SizeType.AutoSize));}
        void PropertyRow(TableLayoutPanel t,int row,string label,Control input){EnsurePropertyRows(t,row);SetDetailRowHeight(t,row,36);t.Controls.Add(new Label{Text=label,AutoSize=true,Anchor=AnchorStyles.Left},0,row);input.Dock=DockStyle.Fill;input.Margin=new Padding(4,3,4,3);input.MinimumSize=new Size(0,26);input.AccessibleName="Obiekt/grupa: "+label;t.Controls.Add(input,1,row);}
        void SpanRow(TableLayoutPanel t,int row,Control input){EnsurePropertyRows(t,row);input.Dock=DockStyle.Fill;input.Margin=new Padding(4,5,4,5);t.Controls.Add(input,0,row);t.SetColumnSpan(input,2);}
        static void SetDetailRowHeight(TableLayoutPanel table,int row,int height)
        {
            var style=table.RowStyles[row];
            if(style.SizeType!=SizeType.Absolute)style.SizeType=SizeType.Absolute;
            if(style.Height!=height)style.Height=height;
        }
        Button PropertyButton(string text,Action action,bool red=false){var b=new EditorButton{Text=text,Height=30,MinimumSize=new Size(0,30),FlatStyle=FlatStyle.Flat,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink};StyleButton(b);if(red)b.Destructive=true;b.Click+=(s,e)=>action();return b;}
        static SwitchBox PropertySwitch(string name)
        {return new SwitchBox{Width=32,Height=26,AccessibleName=name,Margin=new Padding(0,2,3,2)};}
        static FlowLayoutPanel PropertyLine(params Control[] fields)
        {
            var row=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,WrapContents=false};
            foreach(var field in fields)row.Controls.Add(field);
            return row;
        }
        static Control WidthAlignedSwitch(Control box)
        {
            var slot=new Panel{Width=57,Height=26,Margin=new Padding(3)};box.Dock=DockStyle.Right;slot.Controls.Add(box);return slot;
        }
        static Control RadiusControl(ScrubNumber number)
        {
            number.AccessibleName="Promień";
            var row=PropertyLine(new Label{Text="Promień",AutoSize=true,Margin=new Padding(2,7,2,0)},number);
            row.Margin=new Padding(3,0,3,0);
            return row;
        }
        static Label DetailSection(string title)
        {
            var heading=new Label{Text=title,AutoSize=false,Height=28,TextAlign=ContentAlignment.MiddleLeft};
            heading.Font=new Font("Segoe UI",12,FontStyle.Bold);
            heading.Disposed+=(s,e)=>heading.Font.Dispose();
            return heading;
        }
        static Font DetailMetadataFont(){return new Font("Segoe UI",15,FontStyle.Bold);}
        static void Amber(Button button){button.ForeColor=Color.FromArgb(255,202,125);button.FlatAppearance.BorderColor=button.ForeColor;}
        Button SizeAction(string text,string tip,Action action)
        {
            var button=PropertyButton(text,action);
            button.AutoSize=false;button.MinimumSize=new Size(0,objectSize.Height);
            button.Size=new Size(34,objectSize.Height);button.Margin=new Padding(2,3,2,3);
            button.AccessibleName=tip;tips.SetToolTip(button,tip);
            return button;
        }
        void BuildObjectDetails()
        {
            objectDetailPanel=new DetailsOverlay{Dock=DockStyle.Fill,Padding=new Padding(10),AutoScroll=true,Visible=false};
            right.Controls.Add(objectDetailPanel);
            detailBody=new BufferedPanel();
            objectDetailPanel.Controls.Add(detailBody);
            instanceLayout=PropertyTable(detailBody);
            objectName=new DetailNameHeader("Nazwa obiektu",value=>EditInstance("name",o=>o.Name=value,false),tips);
            SpanRow(instanceLayout,0,objectName);
            SetDetailRowHeight(instanceLayout,0,64);
            var summary=new TableLayoutPanel{ColumnCount=2,RowCount=2,Height=60};
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));summary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,64));
            var metadataFont=DetailMetadataFont();
            objectTerrain=new Label{Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true,Font=metadataFont};
            memberToGroup=new LinkLabel{Dock=DockStyle.Fill,LinkColor=Color.LightSteelBlue,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true,Font=metadataFont};
            summary.Disposed+=(s,e)=>metadataFont.Dispose();
            memberToGroup.LinkClicked+=(s,e)=>{var obj=SingleSelectedObject();var group=obj==null?null:GroupRules.For(map,obj.Id);if(group!=null)SelectGroup(group);};
            objectThumbnail=new PictureBox{Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom};
            summary.Controls.Add(objectTerrain,0,0);summary.Controls.Add(memberToGroup,0,1);
            summary.Controls.Add(objectThumbnail,1,0);summary.SetRowSpan(objectThumbnail,2);
            SpanRow(instanceLayout,1,summary);
            SetDetailRowHeight(instanceLayout,1,70);
            SpanRow(instanceLayout,2,DetailSection("Stany"));
            stateTiles=new BufferedFlowPanel{WrapContents=true,Padding=Padding.Empty,Margin=Padding.Empty};
            SpanRow(instanceLayout,3,stateTiles);
            addStateTile=PropertyButton("",()=>ChangeStateFromMenu(null,false));
            addStateTile.Size=new Size(30,30);addStateTile.AutoSize=false;addStateTile.AccessibleName="Dodaj stan do biblioteki";
            addStateTile.Paint+=(s,e)=>{
                int x=addStateTile.ClientSize.Width/2,y=addStateTile.ClientSize.Height/2;
                using(var pen=new Pen(addStateTile.Enabled?addStateTile.ForeColor:Color.Silver,2))
                {e.Graphics.DrawLine(pen,x-6,y,x+6,y);e.Graphics.DrawLine(pen,x,y-6,x,y+6);}
            };
            tips.SetToolTip(addStateTile,"Kliknij, aby utworzyć stan. Prawy przycisk: Dodaj stan z biblioteki.");
            AttachStateRestoreMenu(addStateTile);stateTiles.Controls.Add(addStateTile);

            SpanRow(instanceLayout,4,DetailSection("Rozmiar i właściwości"));
            PropertyRow(instanceLayout,5,"Rozmiar",PropertyLine(objectSize,
                SizeAction("×2","Podwój rozmiar",()=>ScaleObjectSize(2)),SizeAction("÷2","Zmniejsz rozmiar o połowę",()=>ScaleObjectSize(.5)),
                SizeAction("30","Przywróć rozmiar 30",()=>ScaleObjectSize(0))));
            PropertyRow(instanceLayout,6,"Odkrywanie mgły",PropertyLine(WidthAlignedSwitch(objectReveal),RadiusControl(objectRadius)));
            PropertyRow(instanceLayout,7,"Pole widzenia",PropertyLine(WidthAlignedSwitch(objectLight),RadiusControl(objectLightRadius)));
            effectiveLabel=new Label{AutoSize=true,MaximumSize=new Size(290,0),ForeColor=Color.Silver};
            SpanRow(instanceLayout,8,effectiveLabel);
            SpanRow(instanceLayout,9,DetailSection("Działania"));
            PropertyRow(instanceLayout,10,"Blokada ruchu",PropertyLine(WidthAlignedSwitch(objectLocked)));
            memberDetach=PropertyButton("Odłącz od grupy",()=>{
                var obj=SingleSelectedObject();var group=obj==null?null:GroupRules.For(map,obj.Id);
                if(group!=null)UnlinkWithDialog(group,new[]{obj.Id});
            });
            Amber(memberDetach);SpanRow(instanceLayout,11,memberDetach);
            instanceDelete=PropertyButton("Usuń obiekt",ConfirmDeleteSelectedObjects,true);
            SpanRow(instanceLayout,12,instanceDelete);
            SetDetailRowHeight(instanceLayout,12,40);
            objectSize.ValueChanged+=(s,e)=>EditInstance("size",o=>o.Size=(float)objectSize.Value,true);
            objectRadius.ValueChanged+=(s,e)=>EditInstance("radius",o=>o.RevealRadius=(int)objectRadius.Value,true);
            objectLocked.CheckedChanged+=(s,e)=>EditInstance("lock",o=>o.Locked=objectLocked.Checked,false);
            objectReveal.CheckedChanged+=(s,e)=>EditInstance("reveal",o=>o.RevealsFog=objectReveal.Checked,false);
            objectLight.CheckedChanged+=(s,e)=>EditInstance("light",o=>o.LightEnabled=objectLight.Checked,false);
            objectLightRadius.ValueChanged+=(s,e)=>EditInstance("light-radius",o=>o.LightRadius=(int)objectLightRadius.Value,true);
            foreach(var number in new[] {objectSize,objectRadius,objectLightRadius})LiveNumber(number);
            BuildGroupDetails(detailBody);
            objectDetailPanel.Layout+=(s,e)=>LayoutObjectDetails();
        }
        int StateRowsHeight(int width)
        {
            int rows=1,x=0;
            foreach(Control tile in stateTiles.Controls)
            {
                int advance=tile.Width+tile.Margin.Horizontal;
                if(x!=0&&x+advance>width){rows++;x=0;}
                x+=advance;
            }
            return rows*(30+addStateTile.Margin.Vertical)+stateTiles.Margin.Vertical;
        }
        void LayoutObjectDetails()
        {
            if(arrangingDetails||detailBody==null||groupLayout==null)return;
            arrangingDetails=true;
            try
            {
                bool group=CurrentGroup()!=null;
                var table=group?groupLayout:instanceLayout;
                var padding=objectDetailPanel.Padding;
                // Width at the viewport before a scrollbar is applied. Measure again with its
                // gutter only when the content needs scrolling at the full width.
                int width=objectDetailPanel.Width-padding.Horizontal;
                int height=MeasureDetails(table,width,group);
                if(height+padding.Vertical>objectDetailPanel.Height)
                {
                    width-=SystemInformation.VerticalScrollBarWidth;
                    height=MeasureDetails(table,width,group);
                }
                var offset=objectDetailPanel.AutoScrollPosition;
                detailBody.Bounds=new Rectangle(padding.Left+offset.X,padding.Top+offset.Y,Math.Max(1,width),height);
                table.Bounds=new Rectangle(0,0,Math.Max(1,width),height);
            }
            finally { arrangingDetails=false; }
        }
        int MeasureDetails(TableLayoutPanel table,int width,bool group)
        {
            if(!group)
                SetDetailRowHeight(table,3,StateRowsHeight(Math.Max(1,width-table.Padding.Horizontal-stateTiles.Margin.Horizontal)));
            return table.GetPreferredSize(new Size(Math.Max(1,width),0)).Height;
        }
        void ScaleObjectSize(double multiplier)
        {
            EditInstance("size",o=>o.Size=multiplier==0?30:(float)Math.Max(8,Math.Min(1024,
                Math.Round(o.Size*multiplier,MidpointRounding.AwayFromZero))),false);
        }
        MapObject SingleSelectedObject(){return selectedGroupId==null&&selectedObjects.Count==1?map.Objects.FirstOrDefault(o=>selectedObjects.Contains(o.Id)):null;}
        void EditInstance(string field,Action<MapObject> edit,bool merge)
        {
            if(updating)return;var obj=SingleSelectedObject();if(obj==null||ObjectFogLevel(obj)>0)return;if (!GuardLayer(MapLayer.Objects)) { UpdateUI(); return; } if(!merge)EndObjectPropertyEdit();var next=obj.Copy();edit(next);try{LiveChangeObject(next,merge?obj.Id+":"+field:null);}catch(Exception ex){ShowEditorMessage(ex.Message,"Właściwości obiektu",MessageBoxButtons.OK,MessageBoxIcon.Warning);UpdateUI();}
        }
        void RefreshStateTiles()
        {
            var states = AvailableStates();
            string signature = json.Serialize(states);
            if (stateTileSignature == signature) return;
            var tiles = stateTiles.Controls.OfType<Button>().Where(b => b.Tag is string).ToDictionary(b => (string)b.Tag);
            var ids = new HashSet<string>(states.Select(s => s.Id));
            stateTiles.SuspendLayout();
            try
            {
                foreach (var old in tiles.Where(t => !ids.Contains(t.Key))) old.Value.Dispose();
                for (int i = 0; i < states.Count; i++)
                {
                    var state = states[i];
                    Button button;
                    if (!tiles.TryGetValue(state.Id, out button))
                    {
                        string id = state.Id;
                        button = PropertyButton(state.Name, () => EditInstance("state", o => o.Side = id, false));
                        button.Size = new Size(96, 30);
                        button.AutoSize = false;
                        button.AutoEllipsis = true;
                        button.Tag = id;
                        var menu = StateMenu(id);
                        button.ContextMenuStrip = menu;
                        button.Disposed += (sender, args) => menu.Dispose();
                        stateTiles.Controls.Add(button);
                    }
                    if (stateTiles.Controls.GetChildIndex(button) != i) stateTiles.Controls.SetChildIndex(button, i);
                    button.Text = state.Name;
                    button.BackColor = ColorTranslator.FromHtml(state.Color);
                    var color = button.BackColor;
                    button.ForeColor = color.R * .299 + color.G * .587 + color.B * .114 > 145 ? Color.Black : Color.White;
                    button.AccessibleName = "Stan: " + state.Name;
                    tips.SetToolTip(button, state.Name + (state.CanReveal ? " — pozwala odkrywać mgłę" : " — nie pozwala odkrywać mgły")
                        + ". Prawy przycisk: biblioteka stanu.");
                }
                stateTileSignature = signature;
            }
            finally { stateTiles.ResumeLayout(true); }
        }

        ContextMenuStrip StateMenu(string id)
        {
            var menu = new ContextMenuStrip();
            var edit = menu.Items.Add("Edytuj stan biblioteczny", null, (s, e) => ChangeStateFromMenu(id, false));
            var remove = menu.Items.Add("Usuń stan z mapy", null, (s, e) => ChangeStateFromMenu(id, true));
            menu.Opening += (s, e) =>
            {
                EndCanvasGesture(true);
                bool stored = libraryStates.Any(state => state.Definition.Id == id);
                edit.Text = stored ? "Edytuj stan biblioteczny" : "Zapisz stan w bibliotece";
                remove.Enabled = !StateLibrary.IsBuiltin(id);
            };
            menu.Closed += (s, e) =>
            {
                waitingForMouseRelease = Control.MouseButtons != MouseButtons.None;
                requireFreshCanvasDown = true;
            };
            return menu;
        }
        void UpdateObjectDetails()
        {
            bool group=CurrentGroup()!=null;
            objectDetailPanel.SuspendLayout();detailBody.SuspendLayout();
            try
            {
                if(group)UpdateGroupDetails();else UpdateInstanceDetails();
                var next=group?groupLayout:instanceLayout;
                next.Visible=true;next.BringToFront();
                LayoutObjectDetails();
                (group?instanceLayout:groupLayout).Visible=false;
            }
            finally { detailBody.ResumeLayout(false);objectDetailPanel.ResumeLayout(true); }
        }
        void UpdateInstanceDetails()
        {
            var obj=SingleSelectedObject();
            if(obj==null)return;
            RefreshStateTiles();
            bool masked=ObjectFogLevel(obj)>0;
            instanceLayout.Enabled=!masked;
            objectName.Display(obj.Id,masked?"":obj.Name,masked?"???":obj.Name,null,ChoiceStyle.ActiveBorder,!masked);
            int cell=ObjectGeometry.Hit(map,new PointF(obj.X,obj.Y));
            objectTerrain.Text=masked?"Obiekt ukryty przez mgłę":cell<0?"Poza mapą":
                CellMasked(cell)?"Teren ukryty":MapLogic.IsUnpainted(map.Cells[cell])?"Brak terenu":map.Cells[cell].Terrain;
            objectThumbnail.Image=masked?null:AssetBitmap(map.IconAssets.First(a=>a.Id==obj.AssetId),false);
            objectSize.Value=masked?8:(decimal)obj.Size;
            objectLocked.Checked=!masked&&obj.Locked;
            objectReveal.Checked=!masked&&obj.RevealsFog;
            objectRadius.Value=masked?0:obj.RevealRadius;
            objectLight.Checked=!masked&&obj.LightEnabled;
            objectLightRadius.Value=masked?0:obj.LightRadius;
            objectRadius.Parent.Visible=objectReveal.Checked;
            objectLightRadius.Parent.Visible=objectLight.Checked;
            objectReveal.Enabled=!masked&&GroupRules.Allows(map,obj.Side);
            objectRadius.Enabled=objectReveal.Enabled;
            foreach(Button b in stateTiles.Controls)
            {
                bool chosen=!masked&&(string)b.Tag==obj.Side;
                b.FlatAppearance.BorderSize=chosen?3:1;
                b.FlatAppearance.BorderColor=chosen?ChoiceStyle.ActiveBorder:Color.Gray;
            }
            var membership=GroupRules.For(map,obj.Id);
            memberToGroup.Text=masked?"":membership==null?"Brak grupy":membership.Name;
            memberToGroup.Links.Clear();
            if(!masked&&membership!=null)memberToGroup.Links.Add(0,memberToGroup.Text.Length);
            memberDetach.Visible=!masked&&membership!=null;
            var overrides=GroupRules.Used(membership,obj.Id);
            effectiveLabel.Visible=!masked&&overrides!=0;
            effectiveLabel.Text=overrides==0?"":"Edytujesz wartości indywidualne. Grupa ustala: "+
                String.Join(", ",GroupRules.Columns.Select((column,i)=>new{column,i}).Where(x=>(overrides&x.column)!=0).Select(x=>GroupRules.ColumnNames[x.i].ToLowerInvariant()))+".";
        }
    }
}
