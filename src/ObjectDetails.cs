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
        Label detailTitle;
        readonly Label detailIcon=new Label();
        readonly TextBox objectName=new TextBox{MaxLength=200};
        readonly NumericUpDown objectSize=new NumericUpDown{Minimum=8,Maximum=1024,Value=30},objectRadius=new NumericUpDown{Minimum=0,Maximum=20,Value=1};
        readonly CheckBox objectLocked=new CheckBox{Text="Blokada ruchu",AutoSize=true},objectReveal=new CheckBox{Text="Odkrywa mgłę",AutoSize=true};
        Label objectCount,memberGroupLabel,effectiveLabel;
        FlowLayoutPanel stateTiles;
        Button addStateTile;
        bool? showingGroupDetails;
        bool detailStructureChanged;
        TableLayoutPanel instanceLayout;
        Button instanceDelete,groupSelectionButton,memberToGroup,memberDetach;
        string stateTileSignature,propertyEditKey;
        EditAction propertyEditAction;
        bool clearAfterIconEdit;
        static bool TextInputFocused(Control parent){if(parent.ContainsFocus&&(parent is TextBoxBase||parent is NumericUpDown||parent is RichNoteEditor))return true;return parent.Controls.Cast<Control>().Any(TextInputFocused);}
        void EndObjectPropertyEdit(){propertyEditKey=null;propertyEditAction=null;}
        void LiveNumber(NumericUpDown number){number.TextChanged+=(s,e)=>{decimal value;if(!updating&&Decimal.TryParse(number.Text,System.Globalization.NumberStyles.Integer,System.Globalization.CultureInfo.CurrentCulture,out value)&&value>=number.Minimum&&value<=number.Maximum)number.Value=value;};}
        void ClearIconMaterial(){activeIcon=null;iconMaterial=false;objectMaterial=false;noMaterial=true;UpdateMaterialIndicator();canvas.Invalidate();}
        void UpdateBrushAvailability(){if(shapePicker==null)return;bool applicable=currentTool==EditorTool.FogBrush||currentTool==EditorTool.Brush&&!objectMaterial&&!noMaterial;shapePicker.Enabled=applicable;sizePicker.Enabled=applicable&&brushShape!=BrushShape.Point;heightPicker.Enabled=applicable;fillPicker.Enabled=applicable&&brushShape!=BrushShape.Point&&brushShape!=BrushShape.Horizontal&&brushShape!=BrushShape.Vertical;}
        TableLayoutPanel PropertyTable(Control parent){var t=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,Padding=new Padding(4)};t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,90));t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));parent.Controls.Add(t);return t;}
        void PropertyRow(TableLayoutPanel t,int row,string label,Control input){t.RowStyles.Add(new RowStyle(SizeType.AutoSize));t.Controls.Add(new Label{Text=label,AutoSize=true,Padding=new Padding(0,9,0,0)},0,row);input.Dock=DockStyle.Fill;input.Margin=new Padding(4,5,4,5);input.MinimumSize=new Size(0,26);input.AccessibleName="Obiekt/grupa: "+label;t.Controls.Add(input,1,row);}
        void SpanRow(TableLayoutPanel t,int row,Control input){input.Dock=DockStyle.Fill;input.Margin=new Padding(4,5,4,5);t.Controls.Add(input,0,row);t.SetColumnSpan(input,2);t.RowStyles.Add(new RowStyle(SizeType.AutoSize));}
        Button PropertyButton(string text,Action action,bool red=false){var b=new Button{Text=text,Height=30,FlatStyle=FlatStyle.Flat,AutoSize=true};StyleButton(b);if(red){b.BackColor=Color.FromArgb(140,44,44);b.TextAlign=ContentAlignment.MiddleCenter;}b.Click+=(s,e)=>action();return b;}
        void BuildObjectDetails()
        {
            objectDetailPanel=new DetailsOverlay{Dock=DockStyle.Fill,Padding=new Padding(16),AutoScroll=true,Visible=false};right.Controls.Add(objectDetailPanel);objectDetailPanel.BringToFront();
            var container=new BufferedPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink};objectDetailPanel.Controls.Add(container);
            var top=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false};objectDetailPanel.Controls.Add(top);top.SendToBack();
            objectCount=new Label{AutoSize=true};top.Controls.Add(objectCount);groupSelectionButton=PropertyButton("Grupuj wybrane",JoinGroupsFromPanel);top.Controls.Add(groupSelectionButton);
            instanceLayout=PropertyTable(container);
            PropertyRow(instanceLayout,0,"Nazwa",objectName);
            stateTiles=new BufferedFlowPanel{AutoSize=true,WrapContents=true,Padding=Padding.Empty,Margin=Padding.Empty};SpanRow(instanceLayout,1,stateTiles);
            addStateTile = PropertyButton("+", () => ChangeStateFromMenu(null, false));
            addStateTile.Width = 34;
            addStateTile.AutoSize = false;
            addStateTile.AccessibleName = "Dodaj stan do biblioteki";
            tips.SetToolTip(addStateTile, "Kliknij, aby utworzyć stan. Prawy przycisk: Dodaj stan z biblioteki.");
            AttachStateRestoreMenu(addStateTile);
            stateTiles.Controls.Add(addStateTile);
            PropertyRow(instanceLayout,2,"Rozmiar",objectSize);PropertyRow(instanceLayout,3,"Ruch",objectLocked);PropertyRow(instanceLayout,4,"Mgła",objectReveal);PropertyRow(instanceLayout,5,"Promień",objectRadius);
            effectiveLabel=new Label{AutoSize=true,MaximumSize=new Size(350,0)};SpanRow(instanceLayout,6,effectiveLabel);
            memberGroupLabel=new Label{AutoSize=true};SpanRow(instanceLayout,7,memberGroupLabel);
            var membership=new FlowLayoutPanel{AutoSize=true};memberToGroup=PropertyButton("Przejdź do grupy",()=>{var o=SingleSelectedObject();var g=o==null?null:GroupRules.For(map,o.Id);if(g!=null)SelectGroup(g);});memberDetach=PropertyButton("Odłącz od grupy",()=>{var o=SingleSelectedObject();var g=o==null?null:GroupRules.For(map,o.Id);if(g!=null)UnlinkWithDialog(g,new[]{o.Id});});membership.Controls.Add(memberToGroup);membership.Controls.Add(memberDetach);SpanRow(instanceLayout,8,membership);
            instanceDelete=PropertyButton("Usuń instancję",ConfirmDeleteSelectedObjects,true);SpanRow(instanceLayout,9,instanceDelete);
            SpanRow(instanceLayout,10,new Label{Text="Zmiany działają na bieżąco. Rozmiar 8–1024 jednostek świata; promień 0–20 pierścieni (0 = heks centralny).",AutoSize=true,MaximumSize=new Size(340,0)});
            objectName.TextChanged+=(s,e)=>{if(!String.IsNullOrWhiteSpace(objectName.Text))EditInstance("name",o=>o.Name=objectName.Text.Trim(),true);};objectSize.ValueChanged+=(s,e)=>EditInstance("size",o=>o.Size=(float)objectSize.Value,true);objectRadius.ValueChanged+=(s,e)=>EditInstance("radius",o=>o.RevealRadius=(int)objectRadius.Value,true);objectLocked.CheckedChanged+=(s,e)=>EditInstance("lock",o=>o.Locked=objectLocked.Checked,false);objectReveal.CheckedChanged+=(s,e)=>EditInstance("reveal",o=>o.RevealsFog=objectReveal.Checked,false);
            LiveNumber(objectSize);LiveNumber(objectRadius);
            foreach(Control c in new Control[]{objectName,objectSize,objectRadius})c.LostFocus+=(s,e)=>{if(!updating){EndObjectPropertyEdit();UpdateUI();}};
            BuildGroupDetails(container);
            objectDetailPanel.Layout+=(s,e)=>{int width=Math.Max(200,objectDetailPanel.ClientSize.Width-40-(objectDetailPanel.VerticalScroll.Visible?SystemInformation.VerticalScrollBarWidth:0));container.Width=width;stateTiles.MaximumSize=new Size(width-12,0);};
        }
        MapObject SingleSelectedObject(){return selectedGroupId==null&&selectedObjects.Count==1?map.Objects.FirstOrDefault(o=>selectedObjects.Contains(o.Id)):null;}
        void EditInstance(string field,Action<MapObject> edit,bool merge)
        {
            if(updating)return;var obj=SingleSelectedObject();if(obj==null||!previewWithoutFog&&ObjectFogLevel(obj)>0)return;if (!GuardLayer(MapLayer.Objects)) { UpdateUI(); return; } if(!merge)EndObjectPropertyEdit();var next=obj.Copy();edit(next);try{LiveChangeObject(next,merge?obj.Id+":"+field:null);}catch(Exception ex){ShowEditorMessage(ex.Message,"Właściwości obiektu",MessageBoxButtons.OK,MessageBoxIcon.Warning);UpdateUI();}
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
            if (objectDetailPanel == null) return;
            var groups = SelectedWholeGroups();
            bool group = CurrentGroup() != null;
            bool instance = !group && selectedObjects.Count == 1;
            bool structureChanges = showingGroupDetails != group || instanceLayout.Visible != instance
                || (instance && stateTileSignature != json.Serialize(AvailableStates()));
            detailStructureChanged |= structureChanges;
            objectDetailPanel.SuspendLayout();
            groupLayout.SuspendLayout();
            instanceLayout.SuspendLayout();
            try
            {
                groupLayout.Visible = group;
                instanceLayout.Visible = instance;
                showingGroupDetails = group;
                UpdateDetailValues(group, groups.Count);
            }
            finally
            {
                groupLayout.ResumeLayout(group);
                instanceLayout.ResumeLayout(instance);
                objectDetailPanel.ResumeLayout(true);
            }
        }

        void UpdateDetailValues(bool group, int groups)
        {
            groupSelectionButton.Text = groups > 1 ? "Połącz grupy" : groups == 1 ? "Dodaj do grupy" : "Grupuj wybrane";
            groupSelectionButton.Visible=!group&&selectedObjects.Count>=2;objectCount.Text=group?"Trwała grupa: "+map.Groups.Single(g=>g.Id==selectedGroupId).Members.Count+" obiektów":selectedObjects.Count==1?"Instancja obiektu":"Wybrane obiekty: "+selectedObjects.Count;
            if(group){UpdateGroupDetails();return;}var obj=SingleSelectedObject();if(obj==null)return;RefreshStateTiles();bool masked=!previewWithoutFog&&ObjectFogLevel(obj)>0;
            if(propertyEditKey==null||!objectName.ContainsFocus)objectName.Text=masked?"???":obj.Name;objectSize.Value=masked?8:(decimal)obj.Size;objectLocked.Checked=!masked&&obj.Locked;objectReveal.Checked=!masked&&obj.RevealsFog;objectRadius.Value=masked?0:obj.RevealRadius;
            foreach(Control c in new Control[]{objectName,objectSize,objectLocked,instanceDelete})c.Enabled=!masked;
            foreach(Button b in stateTiles.Controls){bool chosen=!masked&&(string)b.Tag==obj.Side;b.FlatAppearance.BorderSize=chosen?3:1;b.FlatAppearance.BorderColor=chosen?Color.Gold:Color.Gray;b.Enabled=b.Tag==null||!masked;}
            objectReveal.Enabled=!masked&&GroupRules.Allows(map,obj.Side);objectRadius.Enabled=objectReveal.Enabled&&obj.RevealsFog;
            var membership=GroupRules.For(map,obj.Id);memberGroupLabel.Text=membership==null?"Bez grupy":masked?"Członek grupy (dane ukryte)":"Grupa: "+membership.Name;memberToGroup.Visible=memberDetach.Visible=membership!=null;
            if(masked){objectCount.Text="Obiekt ukryty przez mgłę";effectiveLabel.Text="Włącz Podgląd bez mgły, aby zobaczyć i edytować właściwości.";}
            else if(membership!=null){var effective=GroupRules.Effective(map,obj);effectiveLabel.Text="Wartości indywidualne. Efektywnie: "+map.ObjectStates.Single(s=>s.Id==effective.Side).Name+", rozmiar "+effective.Size+", ruch "+(effective.Locked?"zablokowany":"odblokowany")+", odkrywanie "+(effective.RevealsFog?"tak":"nie")+", promień "+effective.RevealRadius+". Nadpisania grupy mogą przykrywać te pola.";}
            else effectiveLabel.Text="Wartości indywidualne (bez nakładki grupy).";
        }
    }
}
