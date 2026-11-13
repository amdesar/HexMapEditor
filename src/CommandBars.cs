using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal enum EditorCommand {New,Open,Save,SaveAs,Exit,Undo,Redo,Rename,Resize,Background,FitMap,FitAll,Quality,About}
    public sealed partial class MainWindow
    {
        MenuStrip mainMenu;ToolStrip quickStrip;ContextMenuStrip zoomPopup;
        readonly Dictionary<EditorCommand,ToolStripMenuItem> commandMenus=new Dictionary<EditorCommand,ToolStripMenuItem>();
        ToolStripMenuItem recentMenu,axesMenu,qualityHigh,qualityLow,pointyMenu,flatMenu;
        Control commandFocus,commandTarget;
        readonly Dictionary<EditorCommand,Button> quickCommands=new Dictionary<EditorCommand,Button>();
        internal readonly Dictionary<EditorCommand,int> CommandExecutions=new Dictionary<EditorCommand,int>();
        static readonly Color SaveAccent=ChoiceStyle.ActiveBorder,LowAccent=ChoiceStyle.ActiveBorder;
        ToolStripMenuItem AddCommand(ToolStripMenuItem parent,string text,EditorCommand command,string shortcut=null)
        {var item=new ToolStripMenuItem(text){ShortcutKeyDisplayString=shortcut};item.Click+=(s,e)=>ExecuteCommand(command);parent.DropDownItems.Add(item);commandMenus[command]=item;return item;}
        ToolStripMenuItem Category(string text){var item=new ToolStripMenuItem(text);mainMenu.Items.Add(item);return item;}
        static void Sep(ToolStripMenuItem menu){menu.DropDownItems.Add(new ToolStripSeparator());}
        void MenuFence(ToolStripDropDown menu){menu.Closed+=(s,e)=>{waitingForMouseRelease=Control.MouseButtons!=MouseButtons.None;requireFreshCanvasDown=true;};}
        void BuildCommandBars()
        {
            shellToolbar=new Panel{Dock=DockStyle.Top,Height=66,BackColor=Color.FromArgb(34,38,48)};Controls.Add(shellToolbar);
            mainMenu=new MenuStrip{Dock=DockStyle.None,AutoSize=false,Height=27,GripStyle=ToolStripGripStyle.Hidden,BackColor=shellToolbar.BackColor,ForeColor=Color.WhiteSmoke,Renderer=new ToolStripProfessionalRenderer(new FitMenuColors())};
            MainMenuStrip=mainMenu;shellToolbar.Controls.Add(mainMenu);
            mainMenu.MenuActivate+=(s,e)=>{commandFocus=commandTarget??FocusedEditorControl();UpdateCommandState();};
            var file=Category("&Plik");AddCommand(file,"Nowa…",EditorCommand.New);AddCommand(file,"Otwórz…",EditorCommand.Open);recentMenu=new ToolStripMenuItem("Ostatnie projekty");file.DropDownItems.Add(recentMenu);file.DropDownOpening+=(s,e)=>RefreshRecentMenu();
            Sep(file);AddCommand(file,"Zapisz",EditorCommand.Save,"Ctrl+S");AddCommand(file,"Zapisz jako…",EditorCommand.SaveAs,"Ctrl+Shift+S");Sep(file);AddCommand(file,"Zakończ",EditorCommand.Exit,"Alt+F4");
            var edit=Category("&Edycja");AddCommand(edit,"Cofnij",EditorCommand.Undo,"Ctrl+Z");AddCommand(edit,"Ponów",EditorCommand.Redo,"Ctrl+Y");Sep(edit);var rename=AddCommand(edit,"Zmień nazwę pliku…",EditorCommand.Rename);rename.ToolTipText="Dostępne po pierwszym zapisie mapy. Zmienia nazwę pliku, bez zapisu niezapisanych zmian.";
            var mapMenu=Category("&Mapa");AddCommand(mapMenu,"Rozmiar mapy…",EditorCommand.Resize);var orientation=new ToolStripMenuItem("Orientacja");mapMenu.DropDownItems.Add(orientation);pointyMenu=new ToolStripMenuItem("Pointy Top",null,(s,e)=>ChangeOrientation("PointyTop"));flatMenu=new ToolStripMenuItem("Flat Top",null,(s,e)=>ChangeOrientation("FlatTop"));orientation.DropDownItems.AddRange(new ToolStripItem[]{pointyMenu,flatMenu});AddCommand(mapMenu,"Tło mapy…",EditorCommand.Background);
            var view=Category("&Widok");var layers=new ToolStripMenuItem("Warstwy");view.DropDownItems.Add(layers);layerItems=new ToolStripMenuItem[LayerNames.Length];
            foreach(int i in new[]{4,0,1,2,5,3,6}){var layer=(MapLayer)i;var item=new ToolStripMenuItem(LayerNames[i]);item.Click+=(s,e)=>{if(!BlockInkCommit())SetLayerVisible(layer,!LayerVisible(layer));};layerItems[i]=item;layers.DropDownItems.Add(item);}
            layers.DropDown.Closing+=(s,e)=>{if(e.CloseReason==ToolStripDropDownCloseReason.ItemClicked)e.Cancel=true;};
            axesMenu=new ToolStripMenuItem("Osie");view.DropDownItems.Add(axesMenu);foreach(string mode in new[]{"Axes","InCells","None"}){string value=mode;var item=new ToolStripMenuItem(mode=="Axes"?"Osie":mode=="InCells"?"Współrzędne w heksach":"Brak współrzędnych"){Tag=mode};item.Click+=(s,e)=>{if(BlockInkCommit()||map.LabelMode==value)return;EndCanvasGesture(true);map.LabelMode=value;MarkDirty();};axesMenu.DropDownItems.Add(item);}
            var quality=new ToolStripMenuItem("Jakość");view.DropDownItems.Add(quality);qualityHigh=new ToolStripMenuItem("Wysoka",null,(s,e)=>SetLowQuality(false));qualityLow=new ToolStripMenuItem("Niska",null,(s,e)=>SetLowQuality(true));quality.DropDownItems.AddRange(new ToolStripItem[]{qualityHigh,qualityLow});Sep(view);
            var zoomMenu=new ToolStripMenuItem("Powiększenie");view.DropDownItems.Add(zoomMenu);FillZoomMenu(zoomMenu.DropDownItems);
            var fit=new ToolStripMenuItem("Dopasuj widok");view.DropDownItems.Add(fit);AddCommand(fit,"Dopasuj mapę",EditorCommand.FitMap);AddCommand(fit,"Dopasuj wszystko",EditorCommand.FitAll);
            var library=Category("&Biblioteka");var root=new ToolStripMenuItem("Otwórz główny folder biblioteki",null,(s,e)=>OpenLibraryFolder());library.DropDownItems.Add(root);Sep(library);
            foreach(var folder in LibraryFolders()){string relative=folder.Value;library.DropDownItems.Add(new ToolStripMenuItem(folder.Key,null,(s,e)=>OpenLibraryFolder(relative)));}
            var help=Category("&Pomoc");AddCommand(help,"O programie…",EditorCommand.About);
            foreach(ToolStripMenuItem category in mainMenu.Items){category.DropDownOpening+=(s,e)=>UpdateCommandState();StyleMenus(category);}
            quickStrip=new ToolStrip{Dock=DockStyle.None,AutoSize=false,Height=36,GripStyle=ToolStripGripStyle.Hidden,CanOverflow=true,LayoutStyle=ToolStripLayoutStyle.HorizontalStackWithOverflow,BackColor=shellToolbar.BackColor,ForeColor=Color.WhiteSmoke,Renderer=new FitButtonRenderer(),Padding=new Padding(4,1,4,1)};shellToolbar.Controls.Add(quickStrip);
            Quick(new Button(),EditorCommand.New,"new","Nowa mapa",32);Quick(new Button(),EditorCommand.Open,"open","Otwórz mapę",32);Quick(saveButton,EditorCommand.Save,"save","Zapisz (Ctrl+S)",32);quickStrip.Items.Add(new ToolStripSeparator());
            Quick(undoButton,EditorCommand.Undo,"undo","Cofnij (Ctrl+Z)",32);Quick(redoButton,EditorCommand.Redo,"redo","Ponów (Ctrl+Y)",32);quickStrip.Items.Add(new ToolStripSeparator());
            ConfigureButton(zoomButton,"100% ▾","fit","Powiększenie — presety i wartość niestandardowa",ShowZoomMenu,ZoomButtonWidth());zoomButton.Image=null;zoomButton.MouseDown+=(s,e)=>{if(GetMessageTime()!=zoomClosingStamp)suppressZoomOpen=false;};zoomButton.KeyDown+=(s,e)=>suppressZoomOpen=false;Host(zoomButton);
            Quick(new Button(),EditorCommand.FitMap,"fit","Dopasuj mapę",32);Quick(new Button(),EditorCommand.FitAll,"fitall","Dopasuj wszystko — widoczne warstwy",32);quickStrip.Items.Add(new ToolStripSeparator());qualityButton=new Button();Quick(qualityButton,EditorCommand.Quality,null,"Wysoka / Niska jakość — tylko podgląd sesji",60);
            MenuFence(quickStrip.OverflowButton.DropDown);UpdateQualityButton();UpdateCommandState();
        }
        void StyleMenus(ToolStripMenuItem item){item.DropDown.BackColor=Color.FromArgb(34,38,48);item.DropDown.ForeColor=Color.WhiteSmoke;item.DropDown.Renderer=new ToolStripProfessionalRenderer(new FitMenuColors());MenuFence(item.DropDown);foreach(var child in item.DropDownItems.OfType<ToolStripMenuItem>())StyleMenus(child);}
        void Host(Button button){button.AutoSize=false;quickStrip.Items.Add(new ToolStripControlHost(button){AutoSize=false,Size=button.Size,Margin=new Padding(1,0,1,0),Overflow=ToolStripItemOverflow.AsNeeded});}
        void Quick(Button b,EditorCommand command,string icon,string help,int width){quickCommands[command]=b;ConfigureButton(b,"",icon??"labels",help,()=>ExecuteCommand(command),width);b.MouseDown+=(s,e)=>commandFocus=commandTarget??FocusedEditorControl();if(icon==null)b.Image=null;Host(b);}
        void LayoutCommandBars(){if(mainMenu==null)return;int mh=Math.Max(27,mainMenu.Font.Height+10),qh=Math.Max(36,quickStrip.Font.Height+20);shellToolbar.Height=mh+qh;mainMenu.SetBounds(0,0,shellToolbar.ClientSize.Width,mh);quickStrip.SetBounds(0,mh,shellToolbar.ClientSize.Width,qh);}
        void TrackCommandFocus(Control parent)
        {
            if(parent==shellToolbar||parent is RichNoteEditor)return;
            parent.Enter+=(s,e)=>{commandTarget=(Control)s;UpdateCommandState();};
            foreach(Control child in parent.Controls)TrackCommandFocus(child);
        }
        static void CancelNumberDrags(Control parent){var number=parent as ScrubNumber;if(number!=null)number.CancelDrag();foreach(Control child in parent.Controls)CancelNumberDrags(child);}
        Control FocusedEditorControl(){Control found=this;while(true){var next=found.Controls.Cast<Control>().FirstOrDefault(c=>c.ContainsFocus);if(next==null)return found;found=next;}}
        void CommandHistory(bool repeat)
        {
            var focus=commandFocus??commandTarget??FocusedEditorControl();commandFocus=null;
            if(cellName==focus||cellName.ContainsFocus){TextUndo("name",repeat);return;}if(cellDescription.ContainsFocus||IsWithin(focus,cellDescription)){TextUndo("description",repeat);return;}
            var text=focus as TextBoxBase;if(text!=null){var rich=text as RichTextBox;if(repeat&&rich!=null)rich.Redo();else if(text.CanUndo)text.Undo();return;}
            if(repeat)Redo();else Undo();
        }
        static bool IsWithin(Control child,Control parent){for(var c=child;c!=null;c=c.Parent)if(c==parent)return true;return false;}
        void ExecuteCommand(EditorCommand command)
        {
            if(command!=EditorCommand.Exit&&BlockInkCommit())return;CommandExecutions[command]=CommandExecutions.ContainsKey(command)?CommandExecutions[command]+1:1;
            switch(command){case EditorCommand.New:NewProject();break;case EditorCommand.Open:OpenProject();break;case EditorCommand.Save:SaveProject();break;case EditorCommand.SaveAs:SaveAsProject();break;case EditorCommand.Exit:Close();break;case EditorCommand.Undo:CommandHistory(false);break;case EditorCommand.Redo:CommandHistory(true);break;case EditorCommand.Rename:RenameProject();break;case EditorCommand.Resize:ResizeProject();break;case EditorCommand.Background:EditBackground();break;case EditorCommand.FitMap:EndCanvasGesture(true);FitMap();break;case EditorCommand.FitAll:EndCanvasGesture(true);FitEverything();break;case EditorCommand.Quality:SetLowQuality(!lowQuality);break;case EditorCommand.About:using(var d=new AboutDialog(Icon))RunModal(()=>d.ShowDialog(this));break;}commandFocus=null;
        }
        void RefreshRecentMenu()
        {
            var paths=RecentProjects.Shared.Entries;Action refresh=()=>{if(IsDisposed||recentMenu.IsDisposed)return;foreach(ToolStripItem old in recentMenu.DropDownItems.Cast<ToolStripItem>().ToArray())old.Dispose();recentMenu.DropDownItems.Clear();foreach(string p in RecentAvailability.Visible(paths)){string path=p;var item=new ToolStripMenuItem(Path.GetFileNameWithoutExtension(path)){ToolTipText=path};item.Click+=(s,e)=>{if(BlockInkCommit()||!ConfirmDiscard())return;string error;bool migrated;if(!TryLoadProject(path,out error,out migrated))ShowEditorMessage(error,"Nie można otworzyć projektu",MessageBoxButtons.OK,MessageBoxIcon.Warning);};recentMenu.DropDownItems.Add(item);}if(recentMenu.DropDownItems.Count==0)recentMenu.DropDownItems.Add(new ToolStripMenuItem("Brak dostępnych ostatnich projektów"){Enabled=false});};
            refresh();RecentAvailability.Refresh(paths,()=>{try{if(!IsDisposed&&IsHandleCreated)BeginInvoke(refresh);}catch(InvalidOperationException){}});
        }
        void FillZoomMenu(ToolStripItemCollection items){foreach(int p in new[]{25,50,75,100,125,150,200,300,400}){int percent=p;items.Add(new ToolStripMenuItem(p+"%",null,(s,e)=>SetZoomPercent(percent)));}items.Add(new ToolStripSeparator());items.Add(new ToolStripMenuItem("Niestandardowe…",null,(s,e)=>{using(var d=new ZoomDialog(scale*100))if(RunModal(()=>d.ShowDialog(this))==DialogResult.OK)SetZoomPercent((float)d.Value);}));}
        bool suppressZoomOpen;int zoomClosingStamp;
        int ZoomButtonWidth(){return TextRenderer.MeasureText("12800% ▾",Font,Size.Empty,TextFormatFlags.NoPadding).Width+12;}
        void ShowZoomMenu(){if(BlockInkCommit())return;if(zoomPopup!=null&&zoomPopup.Visible){zoomPopup.Close();return;}if(suppressZoomOpen){suppressZoomOpen=false;return;}EndCanvasGesture(true);if(zoomPopup==null){zoomPopup=new ContextMenuStrip{Renderer=new ToolStripProfessionalRenderer(new FitMenuColors()),BackColor=shellToolbar.BackColor,ForeColor=Color.WhiteSmoke};FillZoomMenu(zoomPopup.Items);MenuFence(zoomPopup);zoomPopup.Closing+=(s,e)=>{if(e.CloseReason==ToolStripDropDownCloseReason.AppClicked&&zoomButton.RectangleToScreen(zoomButton.ClientRectangle).Contains(Control.MousePosition)){suppressZoomOpen=true;zoomClosingStamp=GetMessageTime();}};Disposed+=(s,e)=>zoomPopup.Dispose();}zoomPopup.Show(zoomButton,new Point(0,zoomButton.Height));}
        void SetZoomPercent(float percent){if(BlockInkCommit())return;if(percent<1||percent>12800||!ObjectGeometry.Finite(percent))throw new ArgumentOutOfRangeException("percent");EndCanvasGesture(true);EndCameraInteraction();var center=new PointF(canvas.ClientSize.Width/2f,canvas.ClientSize.Height/2f);float next=percent/100,factor=next/scale;pan=new PointF(center.X-(center.X-pan.X)*factor,center.Y-(center.Y-pan.Y)*factor);scale=next;UpdateZoomCaption();canvas.Invalidate();}
        void UpdateZoomCaption(){zoomButton.Text=Math.Round(scale*100)+"% ▾";}
        void UpdateCommandState()
        {
            if(mainMenu==null)return;bool busy=autoSaving||InkCommitBusy;
            foreach(var pair in commandMenus)pair.Value.Enabled=!busy||pair.Key==EditorCommand.Exit;
            commandMenus[EditorCommand.Rename].Enabled=!busy&&!String.IsNullOrEmpty(currentPath);
            Control focus=commandFocus??commandTarget??FocusedEditorControl();var text=focus as TextBoxBase;bool textContext=text!=null;
            commandMenus[EditorCommand.Undo].Enabled=!busy&&(textContext?text.CanUndo:undo.Count>0);commandMenus[EditorCommand.Redo].Enabled=!busy&&(textContext?text.CanUndo:redo.Count>0);
            foreach(var pair in quickCommands)pair.Value.Enabled=commandMenus.ContainsKey(pair.Key)?commandMenus[pair.Key].Enabled:!busy;
            qualityHigh.Enabled=qualityLow.Enabled=pointyMenu.Enabled=flatMenu.Enabled=!busy;zoomButton.Enabled=!busy;
            pointyMenu.Checked=map.Orientation=="PointyTop";flatMenu.Checked=!pointyMenu.Checked;qualityHigh.Checked=!lowQuality;qualityLow.Checked=lowQuality;
            foreach(ToolStripMenuItem item in axesMenu.DropDownItems)item.Checked=(string)item.Tag==map.LabelMode;UpdateLayerChecks();
        }
        static void Accent(Button b,bool active,Color color){ChoiceStyle.Apply(b,active);}
        void UpdateSaveAccent(){saveButton.Text="";Accent(saveButton,HasUnsavedChanges,SaveAccent);var prior=saveButton.Image;saveButton.Image=SaveImage(HasUnsavedChanges);if(prior!=null&&!ReferenceEquals(prior,EditorIcons.Small("save")))prior.Dispose();}
        static Image SaveImage(bool active){return EditorIcons.Small("save");}
    }
    internal sealed class ZoomDialog:EditorDialog
    {
        readonly ScrubNumber value=new ScrubNumber{Minimum=1,Maximum=12800,DecimalPlaces=2,Increment=1,PixelsPerStep=4};internal decimal Value{get{return value.Value;}}
        internal ZoomDialog(float initial):base("Powiększenie",128){value.Value=(decimal)Math.Max(1,Math.Min(12800,initial));Row(0,"Procent (%)",value,true);Buttons(1,()=>{if(value.CommitText())DialogResult=DialogResult.OK;});}
    }
    internal sealed class AboutDialog:DarkForm
    {
        internal AboutDialog(Icon appIcon){Text="O programie";ClientSize=new Size(390,160);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);try{Icon=appIcon==null?Icon.ExtractAssociatedIcon(Application.ExecutablePath):(Icon)appIcon.Clone();}catch{}var pic=new PictureBox{Image=Icon==null?null:Icon.ToBitmap(),Bounds=new Rectangle(20,24,54,54),SizeMode=PictureBoxSizeMode.Zoom};Controls.Add(pic);Controls.Add(new Label{Text="HexMapEditor\nby amdesar\nWersja "+ReleaseMetadata.Version+" • "+ReleaseMetadata.Date,AutoSize=true,Location=new Point(91,22)});var close=new Button{Text="Zamknij",DialogResult=DialogResult.OK,Bounds=new Rectangle(270,116,100,30)};Controls.Add(close);AcceptButton=CancelButton=close;Disposed+=(s,e)=>{if(pic.Image!=null)pic.Image.Dispose();};}
    }
}
