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
            internal readonly Dictionary<string,QuickPair> QuickSets=new Dictionary<string,QuickPair>();
            internal readonly AutoSaveSchedule Schedule=new AutoSaveSchedule();
            internal List<TerrainPreset> Presets;
            internal SavedMapState Saved=new SavedMapState();
            internal ProjectFileVersion Version;
            internal string Path, Hex, Group;
            internal bool Dirty, FirstSave, Details, Collapsed, Low, HasCamera;
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
                FirstSave=Path==null;Schedule.Reset();
                Presets=map.Presets.Select(p=>p.Copy()).ToList();
            }
        }

        readonly List<DocumentSession> documents=new List<DocumentSession>();
        DocumentSession activeDocument;
        MapData map { get { return activeDocument.Map; } }
        string currentPath { get { return activeDocument.Path; } set { activeDocument.Path=value; } }
        bool dirty { get { return activeDocument.Dirty; } set { activeDocument.Dirty=value; } }
        bool needsFirstSave { get { return activeDocument.FirstSave; } set { activeDocument.FirstSave=value; } }
        bool HasUnsavedChanges { get { return activeDocument.Unsaved; } }
        SavedMapState savedState { get { return activeDocument.Saved; } }
        Stack<EditAction> undo { get { return activeDocument.Undo; } }
        Stack<EditAction> redo { get { return activeDocument.Redo; } }
        TextHistoryStore textHistory { get { return activeDocument.TextHistory; } }
        long documentRevision { get { return activeDocument.Revision; } set { activeDocument.Revision=value; } }
        ProjectFileVersion fileVersion { get { return activeDocument.Version; } }
        AutoSaveSchedule autoSchedule { get { return activeDocument.Schedule; } }
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
            tabBar=new Panel{Dock=DockStyle.Bottom,Height=quickStrip.Height,BackColor=shellToolbar.BackColor};
            tabViewport=new Panel{TabStop=false};
            Controls.Add(tabBar);
            var menu=new ContextMenuStrip{Renderer=new FitMenuRenderer(),BackColor=tabBar.BackColor,ForeColor=Color.WhiteSmoke};
            menu.Items.Add("Utwórz nową",null,(s,e)=>NewProject());menu.Items.Add("Otwórz z pliku",null,(s,e)=>OpenProject());
            MenuFence(menu);Disposed+=(s,e)=>menu.Dispose();
            var add=TabButton(tabBar,"+","Utwórz lub otwórz mapę",null);
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
            var tab=new Panel{Size=new Size(TabWidth,BottomHeight-4)};
            tabControls.Add(document,tab);tabViewport.Controls.Add(tab);
            var title=new SingleLineButton{FlatStyle=FlatStyle.Flat,Bounds=new Rectangle(0,0,TabWidth-57,tab.Height)};
            StyleButton(title);tab.Controls.Add(title);
            title.Click+=(s,e)=>ActivateDocument(document);
            var rename=TabButton(tab,"","Zmień nazwę mapy",()=>RenameDocument(document));
            rename.Image=EditorIcons.Small("pencil");rename.SetBounds(TabWidth-56,0,28,tab.Height);
            var close=TabButton(tab,"×","Zamknij kartę",()=>CloseDocument(document));
            close.SetBounds(TabWidth-28,0,27,tab.Height);
            UpdateTabTitle(document);LayoutTabs();
        }
        void UpdateTabTitle(DocumentSession document)
        {
            Panel tab;
            if(!tabControls.TryGetValue(document,out tab))return;
            string name=ProjectTitle(document.Path,document.Map.Name);
            var title=(Button)tab.Controls[0];
            // Keep the dirty indicator outside the part made shorter by ellipsis.
            title.Text=(document.Unsaved?"* ":"")+name;
            title.AccessibleName=name+(document.Unsaved?" — niezapisane zmiany":"");
            title.AccessibleDescription=ReferenceEquals(document,activeDocument)?"Aktywna karta":"";
            tips.SetToolTip(title,title.AccessibleName+(document.Path==null?"":"\n"+document.Path));
            ChoiceStyle.Apply(title,ReferenceEquals(document,activeDocument));title.Invalidate();
            tab.Controls[1].Enabled=document.Path!=null;
        }
        void LayoutTabs(bool reveal=false)
        {
            if(tabViewport==null||statusLabel.Parent==null)return;
            int statusWidth=TextRenderer.MeasureText("Kursor: GR200     200 × 200 pól     Flat Top     Łącznie heksów: 40000",statusLabel.Font).Width+statusLabel.Padding.Horizontal;
            int edge=Math.Max(tabsBack.Right+27,tabBar.ClientSize.Width-statusWidth);
            statusLabel.SetBounds(edge,0,Math.Max(0,tabBar.ClientSize.Width-edge),BottomHeight);
            tabsForward.SetBounds(edge-25,2,24,BottomHeight-4);
            tabViewport.SetBounds(tabsBack.Right+2,2,Math.Max(0,tabsForward.Left-tabsBack.Right-4),BottomHeight-4);
            int visible=Math.Max(1,tabViewport.Width/TabWidth);
            if(reveal)
            {
                int index=documents.IndexOf(activeDocument);
                if(index<firstTab)firstTab=index;
                if(index>=firstTab+visible)firstTab=index-visible+1;
            }
            firstTab=Math.Max(0,Math.Min(firstTab,documents.Count-visible));
            for(int i=0;i<documents.Count;i++)
            {
                Panel tab;
                if(!tabControls.TryGetValue(documents[i],out tab))continue;
                tab.Left=(i-firstTab)*TabWidth;
                tab.Visible=i>=firstTab&&i<firstTab+visible;
            }
            tabsBack.Enabled=firstTab>0;tabsForward.Enabled=firstTab+visible<documents.Count;
        }

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
            documents.Add(document);AddTab(document);SwitchDocument(document);
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
            if(!CanChangeDocument())return;
            if(ReferenceEquals(doc,activeDocument)&&!FinishDocumentInteraction())return;
            if(IsAutoSaving(doc)){statusLabel.Text=AutoSaveBusyMessage;return;}
            reviewingExit=true;
            try
            {
                if(!ConfirmDiscard(doc)||!ReviewPresetDrafts(new[]{doc},true))return;
                int index=documents.IndexOf(doc);
                if(documents.Count==1){returningToWelcome=true;Close();return;}
                if(ReferenceEquals(doc,activeDocument))
                    SwitchDocument(documents[index+1<documents.Count?index+1:index-1]);
                documents.Remove(doc);
                tabControls[doc].Dispose();tabControls.Remove(doc);LayoutTabs(true);
            }
            finally { reviewingExit=false; }
        }
    }
}
