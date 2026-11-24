using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        sealed class DocumentSession
        {
            internal readonly MapData Map;
            internal readonly Stack<EditAction> Undo=new Stack<EditAction>(), Redo=new Stack<EditAction>();
            internal readonly TextHistoryStore TextHistory=new TextHistoryStore();
            internal readonly PresetWorkspace Panel;
            internal Dictionary<string,QuickPair> QuickSets { get { return Panel.QuickSets; } }
            internal List<TerrainPreset> Presets { get { return Panel.Presets; } set { Panel.Presets=value; } }
            internal SavedMapState Saved=new SavedMapState();
            internal ProjectFileVersion Version;
            internal string Path, Hex, Group;
            internal bool Dirty, FirstSave, Details, Collapsed, Low, HasCamera, AutoSaveConflict;
            internal long Revision;
            internal PointF Pan;
            internal float Scale;
            internal int CanvasLeft;
            internal string[] Objects=new string[0], GroupOrder=new string[0];
            internal bool[] Layers;
            internal bool Unsaved { get { return Dirty||FirstSave; } }

            internal DocumentSession(MapData map,string path)
            {
                Map=map;Path=String.IsNullOrEmpty(path)?null:System.IO.Path.GetFullPath(path);Version=map.LoadedFileVersion;
                FirstSave=Path==null;
                Panel=new PresetWorkspace{Presets=map.Presets.Select(p=>p.Copy()).ToList()};
            }
        }

        readonly List<DocumentSession> documents=new List<DocumentSession>();
        DocumentSession activeDocument;
        MapData map { get { return activeDocument.Map; } }
        string currentPath { get { return activeDocument.Path; } set { activeDocument.Path=value; } }
        bool dirty { get { return activeDocument.Dirty; } set { activeDocument.Dirty=value; } }
        bool needsFirstSave { get { return activeDocument.FirstSave; } set { activeDocument.FirstSave=value; } }
        bool HasDocuments { get { return documents.Count>0; } }
        bool HasUnsavedChanges { get { return HasDocuments&&activeDocument.Unsaved; } }
        SavedMapState savedState { get { return activeDocument.Saved; } }
        Stack<EditAction> undo { get { return activeDocument.Undo; } }
        Stack<EditAction> redo { get { return activeDocument.Redo; } }
        TextHistoryStore textHistory { get { return activeDocument.TextHistory; } }
        long documentRevision { get { return activeDocument.Revision; } set { activeDocument.Revision=value; } }
        ProjectFileVersion fileVersion { get { return activeDocument.Version; } }
        Dictionary<string,QuickPair> quickSets { get { return activeDocument.QuickSets; } }
        List<TerrainPreset> workingPresets { get { return activeDocument.Presets; } }

        Panel tabBar, tabViewport;
        Button tabsBack,tabsForward;
        int firstTab;
        readonly Dictionary<DocumentSession,Panel> tabControls=new Dictionary<DocumentSession,Panel>();
        bool reviewingExit,returningToWelcome;
        internal bool ReturningToWelcome { get { return returningToWelcome; } }
        const int TabWidth=LeftPanelWidth*3/4;
        int BottomHeight { get { return tabBar==null?36:tabBar.Height; } }

        void BuildDocumentBar()
        {
            tabBar=new BufferedPanel{Dock=DockStyle.Bottom,Height=quickStrip.Height,BackColor=shellToolbar.BackColor};
            tabBar.Paint+=(s,e)=>{
                if(leftCollapsed)return;
                using(var pen=new Pen(ChoiceStyle.NeutralBorder))e.Graphics.DrawLine(pen,4,0,LeftPanelWidth-4,0);
            };
            tabViewport=new Panel{TabStop=false};
            Controls.Add(tabBar);
            var menu=new ContextMenuStrip{Renderer=new FitMenuRenderer(),BackColor=tabBar.BackColor,ForeColor=Color.WhiteSmoke};
            menu.Items.Add("Utwórz nową",null,(s,e)=>NewProject());menu.Items.Add("Otwórz z pliku",null,(s,e)=>OpenProject());
            MenuFence(menu);Disposed+=(s,e)=>menu.Dispose();
            var add=new TabPartButton{Glyph="+",BackColor=ChoiceStyle.NeutralBack,AccessibleName="Utwórz lub otwórz mapę"};
            tabBar.Controls.Add(add);tips.SetToolTip(add,add.AccessibleName);
            add.Click+=(s,e)=>menu.Show(add,new Point(0,0),ToolStripDropDownDirection.AboveRight);
            add.SetBounds(4,2,32,BottomHeight-4);
            tabsBack=TabButton(tabBar,"‹","Przewiń karty w lewo",()=>{firstTab--;LayoutTabs();});
            tabsBack.SetBounds(add.Right+3,2,24,BottomHeight-4);
            tabsForward=TabButton(tabBar,"›","Przewiń karty w prawo",()=>{firstTab++;LayoutTabs();});
            statusLabel.TextAlign=ContentAlignment.MiddleLeft;statusLabel.AutoEllipsis=true;
            statusLabel.Padding=new Padding(8,0,4,0);
            statusLabel.TextChanged+=(s,e)=>tips.SetToolTip(statusLabel,statusLabel.Text);
            tabBar.Controls.Add(tabViewport);tabBar.Controls.Add(statusLabel);
            tabBar.Layout+=(s,e)=>LayoutTabs();
            AddTab(activeDocument);
        }

        Button TabButton(Control parent,string text,string accessible,Action action)
        {
            var button=new EditorButton{Text=text,AccessibleName=accessible};
            parent.Controls.Add(button);StyleButton(button);tips.SetToolTip(button,accessible);
            if(action!=null)button.Click+=(s,e)=>action();
            return button;
        }
        void AddTab(DocumentSession document)
        {
            var tab=new BufferedPanel{Size=new Size(TabWidth,BottomHeight-4)};
            tab.Paint+=(s,e)=>{using(var pen=new Pen(tab.BackColor==ChoiceStyle.ActiveBack?ChoiceStyle.ActiveBorder:ChoiceStyle.NeutralBorder))
                e.Graphics.DrawRectangle(pen,0,0,tab.Width-1,tab.Height-1);};
            tabControls.Add(document,tab);tabViewport.Controls.Add(tab);
            var title=new TabPartButton{Bounds=new Rectangle(1,1,TabWidth-58,tab.Height-2),TabIndex=0};
            tab.Controls.Add(title);
            title.Click+=(s,e)=>ActivateDocument(document);
            var rename=new TabPartButton{Image=EditorIcons.Small("pencil"),AccessibleName="Zmień nazwę mapy",TabIndex=1};
            var close=new TabPartButton{Glyph="×",AccessibleName="Zamknij kartę",TabIndex=2,ForeColor=Color.FromArgb(244,113,113)};
            tab.Controls.Add(rename);tab.Controls.Add(close);
            tips.SetToolTip(rename,rename.AccessibleName);tips.SetToolTip(close,close.AccessibleName);
            rename.Click+=(s,e)=>RenameDocument(document);close.Click+=(s,e)=>CloseDocument(document);
            rename.SetBounds(TabWidth-57,1,28,tab.Height-2);
            close.SetBounds(TabWidth-29,1,28,tab.Height-2);
            UpdateTabTitle(document);LayoutTabs();
        }
        void UpdateTabTitle(DocumentSession document)
        {
            Panel tab;
            if(!tabControls.TryGetValue(document,out tab))return;
            string name=ProjectTitle(document.Path,document.Map.Name);
            var title=(TabPartButton)tab.Controls[0];
            title.Text=name;title.Dirty=document.Unsaved;
            title.AccessibleName=name+(document.Unsaved?" — niezapisane zmiany":"");
            title.AccessibleDescription=ReferenceEquals(document,activeDocument)?"Aktywna karta":"";
            tips.SetToolTip(title,title.AccessibleName+(document.Path==null?"":"\n"+document.Path));
            tab.BackColor=ReferenceEquals(document,activeDocument)?ChoiceStyle.ActiveBack:ChoiceStyle.NeutralBack;
            foreach(Control part in tab.Controls)part.BackColor=tab.BackColor;
            tab.Invalidate(true);
            tab.Controls[1].Enabled=document.Path!=null;
        }
        void LayoutTabs(bool reveal=false)
        {
            if(tabViewport==null||statusLabel.Parent==null)return;
            int statusWidth=TextRenderer.MeasureText("Kursor: GR200     200 × 200 pól     Flat Top     Łącznie heksów: 40000",statusLabel.Font).Width+statusLabel.Padding.Horizontal;
            const int gap=2, arrowWidth=24;
            int start=tabsBack.Right+gap;
            int edge=Math.Max(start+arrowWidth+gap,tabBar.ClientSize.Width-statusWidth);
            statusLabel.SetBounds(edge,0,Math.Max(0,tabBar.ClientSize.Width-edge),BottomHeight);
            int visible=Math.Max(0,(edge-start-arrowWidth-2*gap)/TabWidth);
            if(reveal&&visible>0)
            {
                int index=documents.IndexOf(activeDocument);
                if(index<firstTab)firstTab=index;
                if(index>=firstTab+visible)firstTab=index-visible+1;
            }
            firstTab=Math.Max(0,Math.Min(firstTab,documents.Count-visible));
            int shown=Math.Min(visible,documents.Count-firstTab);
            tabViewport.SetBounds(start,2,shown*TabWidth,BottomHeight-4);
            tabsForward.SetBounds(tabViewport.Right+gap,2,arrowWidth,BottomHeight-4);
            for(int i=0;i<documents.Count;i++)
            {
                Panel tab;
                if(!tabControls.TryGetValue(documents[i],out tab))continue;
                tab.Left=(i-firstTab)*TabWidth;
                tab.Visible=i>=firstTab&&i<firstTab+visible;
            }
            SetTabArrow(tabsBack,visible>0&&firstTab>0);SetTabArrow(tabsForward,visible>0&&firstTab+visible<documents.Count);
        }
        static void SetTabArrow(Button button,bool canScroll)
        {if(button.Enabled!=canScroll||button.BackColor!=(canScroll?ChoiceStyle.ActiveBack:ChoiceStyle.NeutralBack)){ChoiceStyle.Apply(button,canScroll);button.Enabled=canScroll;}}

        DocumentSession FindDocument(string path)
        {return documents.FirstOrDefault(doc=>SameProjectPath(doc.Path,path));}
        bool CanChangeDocument()
        {
            if(modalDepth>0||reviewingExit||returningToWelcome)return false;
            if(!InkCommitBusy)return true;
            statusLabel.Text=InkBusyMessage;return false;
        }
        internal bool ReadyForExternalOpen { get { return modalDepth==0&&!reviewingExit&&!returningToWelcome&&!InkCommitBusy&&!rightResizing&&gestureButton==MouseButtons.None; } }
        bool FinishDocumentInteraction()
        {
            EndRightResize();
            CommitDocumentInputs();
            EndCanvasGesture(true);
            if(InkCommitBusy){statusLabel.Text=InkBusyMessage;return false;}
            textHistory.BreakGroups();ArmCanvasInput();
            return true;
        }
        void CommitDocumentInputs()
        {
            CancelNumberDrags(this);NumberInputRouter.CommitFor(this);
            hexDetailName.Cancel();objectName.Cancel();groupName.Cancel();
        }
        void CaptureDocumentView(DocumentSession document)
        {
            document.HasCamera=true;document.Pan=pan;document.Scale=scale;document.CanvasLeft=canvas.Left;
            document.Hex=selected>=0&&selected<map.Cells.Count?map.Cells[selected].Id:null;
            document.Group=selectedGroupId;document.Objects=selectedObjects.ToArray();document.GroupOrder=groupSelectionOrder.ToArray();
            document.Details=rightPanelOpen;document.Collapsed=rightCollapsed;document.Low=lowQuality;
            document.Layers=new[]{previewWithoutFog,hideTerrain,hideIcons,hideObjects,hideBackground,hideDarkness,hideInk};
        }
        bool ActivateDocument(DocumentSession document)
        {
            if(ReferenceEquals(document,activeDocument)){LayoutTabs(true);return true;}
            if(!CanChangeDocument()||!FinishDocumentInteraction())return false;
            SwitchDocument(document);
            return true;
        }
        void SwitchDocument(DocumentSession document)
        {
            var previous=activeDocument;
            CaptureDocumentView(previous);
            ResetObjectDocument();ClearDocumentIcons();InkLayerRenderer.Clear();
            descriptionExpanded=false;editorCellId=null;quickPreset=null;commandFocus=null;commandTarget=null;
            activeDocument=document;
            leftSections.Visible=true;canvas.Visible=true;
            ResolveWorkingPresets(document);
            selected=map.Cells.FindIndex(cell=>cell.Id==document.Hex);
            selectedObjects.UnionWith(document.Objects.Where(id=>map.Objects.Any(item=>item.Id==id)));
            groupSelectionOrder.AddRange(document.GroupOrder.Where(id=>map.Groups.Any(group=>group.Id==id)));
            selectedGroupId=map.Groups.Any(group=>group.Id==document.Group&&selectedObjects.SetEquals(group.Members))?document.Group:null;
            rightPanelOpen=document.Details&&(selected>=0||selectedObjects.Count>0);
            rightCollapsed=rightPanelOpen&&document.Collapsed;
            lowQuality=document.Low;
            if(document.Layers!=null)
            {
                var layers=document.Layers;
                previewWithoutFog=layers[0];hideTerrain=layers[1];hideIcons=layers[2];hideObjects=layers[3];
                hideBackground=layers[4];hideDarkness=layers[5];hideInk=layers[6];
            }
            if(document.HasCamera)
            {
                scale=document.Scale;
                pan=new PointF(document.Pan.X+document.CanvasLeft-canvas.Left,document.Pan.Y);
            }
            else RecalculateFitTransform();
            RefreshPresetPicker();
            activeIcon=activeObject=null;iconMaterial=objectMaterial=false;noMaterial=true;
            UpdateMaterialIndicator();UpdateTerrainSelection();UpdateQualityButton();UpdateLayerChecks();UpdateUI();
            UpdateZoomCaption();UpdateTabTitle(previous);LayoutTabs(true);
        }
        bool AddDocument(MapData data,string path)
        {
            if(!CanChangeDocument()||!FinishDocumentInteraction())return false;
            var document=new DocumentSession(data,path);
            AdoptLibraryPanel(document);
            document.Saved.Capture(data);
            bool first=!HasDocuments;
            if(first){autoSchedule.Reset();AutoSaveError=null;HidePresetReview();}
            documents.Add(document);AddTab(document);SwitchDocument(document);
            if(first)autoTimer.Start();
            if(document.Path!=null)RecentProjects.Shared.Touch(document.Path);
            return true;
        }

        void RenameDocument(DocumentSession doc)
        {
            if(!CanChangeDocument()||doc.Path==null||!FinishDocumentInteraction())return;
            if(IsAutoSaving(doc)){statusLabel.Text=AutoSaveBusyMessage;return;}
            ShowRenameProject(doc);
        }
        void CloseDocument(DocumentSession doc)
        {
            if(!CanChangeDocument()||!documents.Contains(doc))return;
            if(IsAutoSaving(doc)){statusLabel.Text=AutoSaveBusyMessage;return;}
            if(doc.Unsaved)
            {
                if(!ActivateDocument(doc)||!FinishDocumentInteraction())return;
            }
            else if(ReferenceEquals(doc,activeDocument)&&!FinishDocumentInteraction())return;
            reviewingExit=true;
            try
            {
                if(!ConfirmDiscard(doc))return;
                if(documents.Count==1)
                {
                    if(!ReviewPresetDrafts(retainedPresets.Concat(new[]{doc.Panel})))return;
                    returningToWelcome=true;Close();return;
                }
                if(!ReviewPresetDrafts(new[]{doc.Panel},doc))return;
                DetachDocument(doc);
            }
            finally { reviewingExit=false;if(HasDocuments)HidePresetReview(); }
        }
        bool ReviewWindowExit()
        {
            foreach(var doc in documents.Where(d=>d.Unsaved).ToArray())
            {
                if(!ReferenceEquals(doc,activeDocument))SwitchDocument(doc);
                if(!ConfirmDiscard(doc))return false;
                retainedPresets.Add(doc.Panel);
                DetachDocument(doc);
            }
            return ReviewPresetDrafts(retainedPresets.Concat(documents.Select(d=>d.Panel)));
        }
        void DetachDocument(DocumentSession doc)
        {
            int index=documents.IndexOf(doc);
            bool wasActive=ReferenceEquals(doc,activeDocument);
            documents.RemoveAt(index);
            tabControls[doc].Dispose();tabControls.Remove(doc);
            if(wasActive)
            {
                if(HasDocuments)SwitchDocument(documents[Math.Min(index,documents.Count-1)]);
                else ShowEmptyEditor();
            }
            LayoutTabs(wasActive);
        }
        void ShowEmptyEditor()
        {
            // The shared controls keep their ordinary binding while there is no
            // open document. This placeholder is never saved or added as a tab.
            autoTimer.Stop();autoBatch.Clear();autoCycle=false;
            ResetObjectDocument();ClearDocumentIcons();InkLayerRenderer.Clear();
            activeDocument=new DocumentSession(new MapData(),null);
            selected=-1;rightPanelOpen=rightCollapsed=false;editorCellId=null;
            commandFocus=commandTarget=null;
            canvas.Visible=false;leftSections.Visible=false;
            UpdateUI();
        }
        void PaintBeforeClosePrompt()
        {
            // Force pending paints behind the modal owner without dispatching input.
            Refresh();
        }
    }

    internal sealed class TabPartButton:Button
    {
        internal string Glyph;
        internal bool Dirty;
        bool hover,pressed;
        internal TabPartButton()
        {
            SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
            FlatStyle=FlatStyle.Flat;
            FlatAppearance.BorderSize=0;
            ForeColor=Color.WhiteSmoke;Cursor=Cursors.Hand;
        }
        protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hover=true;Invalidate();}
        protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hover=false;Invalidate();}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){pressed=true;Invalidate();}}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);pressed=false;Invalidate();}
        protected override void OnPaint(PaintEventArgs e)
        {
            Color back=BackColor;
            if(Enabled&&hover)back=pressed?ControlPaint.Dark(back):ControlPaint.Light(back,.13f);
            e.Graphics.Clear(back);
            Color ink=Enabled?ForeColor:Color.FromArgb(153,160,174);
            int x=Width/2,y=Height/2;
            if(Glyph!=null)
            {
                bool plus=Glyph=="+";
                int extent=Math.Max(4,Height/(plus?5:6));
                using(var pen=new Pen(ink,Math.Max(2,Height/15f)))
                {
                    e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    if(plus){e.Graphics.DrawLine(pen,x-extent,y,x+extent,y);e.Graphics.DrawLine(pen,x,y-extent,x,y+extent);}
                    else {e.Graphics.DrawLine(pen,x-extent,y-extent,x+extent,y+extent);e.Graphics.DrawLine(pen,x+extent,y-extent,x-extent,y+extent);}
                }
            }
            else if(Image!=null)
            {
                var p=new Point(x-Image.Width/2,y-Image.Height/2);
                if(Enabled)e.Graphics.DrawImageUnscaled(Image,p);
                else ControlPaint.DrawImageDisabled(e.Graphics,Image,p.X,p.Y,back);
            }
            else
            {
                const TextFormatFlags flags=TextFormatFlags.SingleLine|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix;
                const int inset=7,gap=4;
                int star=Dirty?TextRenderer.MeasureText(e.Graphics,"*",Font,Size.Empty,flags).Width:0;
                int nameWidth=Math.Max(0,Width-inset-3-(Dirty?star+gap:0));
                if(nameWidth>0)TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(inset,0,nameWidth,Height),ink,flags|TextFormatFlags.EndEllipsis);
                if(Dirty)
                {
                    int width=TextRenderer.MeasureText(e.Graphics,Text,Font,Size.Empty,flags).Width;
                    TextRenderer.DrawText(e.Graphics,"*",Font,new Rectangle(inset+Math.Min(width,nameWidth)+gap,0,star,Height),ink,flags);
                }
            }
            if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(ClientRectangle,-2,-2),ink,back);
        }
    }
}
