using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class StartupContext : ApplicationContext
    {
        StartWindow start;
        MainWindow candidate;
        RichNoteEditor preparedNotes;
        internal Func<MapData,string,MainWindow> EditorFactory;
        internal Action<Exception> Failure;
        bool opening,exiting;
        Exception transitionError;
        readonly ProjectLaunch launch;
        readonly Timer openTimer;
        readonly UpdateController updates;
        internal VerifiedInstaller PendingInstaller { get; private set; }
        void TransitionException(object sender,System.Threading.ThreadExceptionEventArgs e){transitionError=e.Exception;StartupTrace.Write("transition.nativeCallback.failed",candidate,e.Exception);if(candidate!=null)candidate.InitialDisplayError=e.Exception;}
        void DetachException(){Application.ThreadException-=TransitionException;}
        internal StartupContext(string path,ProjectLaunch launch=null)
        {
            this.launch=launch;
            updates = new UpdateController(window => !opening && !exiting && MainForm == window, FinishForUpdate);
            // Initialize the one WPF text host before the native WinForms message loop.
            // Constructing it for the first time from a post-dialog callback can reenter
            // a WPF DisableProcessing scope. No libraries, images or document are preloaded.
            preparedNotes=MainWindow.CreateNoteEditor();
            EditorFactory=(data,file)=>{var notes=preparedNotes;preparedNotes=null;try{return new MainWindow(data,file,true,notes);}catch{if(notes!=null)notes.Dispose();throw;}};
            CreateWelcome();
            if(launch!=null)
            {
                openTimer=new Timer{Interval=125};
                openTimer.Tick+=(s,e)=>ForwardOpen();openTimer.Start();
            }
            if(!String.IsNullOrEmpty(path))try{MainForm=null;var data=MainWindow.ReadProjectData(path);Open(data,Path.GetFullPath(path));RecentProjects.Shared.Touch(path);return;}catch(Exception ex){ReportFailure(ex);}
            if(String.IsNullOrEmpty(path))start.Shown+=(s,e)=>updates.Check((StartWindow)s,false);
            MainForm=start;start.Show();StartupTrace.Write("start.shown",start);
        }
        void CreateWelcome()
        {
            start=new StartWindow(Open);MainForm=start;
            start.UpdateRequested=()=>updates.Check(start,true);
            start.FormClosed+=(s,e)=>StartupTrace.Write("start.closed",s as Form);
            start.HandleDestroyed+=(s,e)=>StartupTrace.Write("start.handleDestroyed",s as Form);
        }
        internal bool FinishForUpdate(Form window, VerifiedInstaller installer)
        {
            if(opening||exiting||window!=MainForm)return false;
            window.Close(); // Includes the existing map and preset decisions.
            if(!window.IsDisposed)return false;
            PendingInstaller=installer;
            return true;
        }
        protected override void OnMainFormClosed(object sender,EventArgs e)
        {
            var editor=sender as MainWindow;
            if(editor==null||!editor.ReturningToWelcome){base.OnMainFormClosed(sender,e);return;}
            CreateWelcome();start.Show();start.Activate();
            StartupTrace.Write("start.shown",start);
        }
        void ForwardOpen()
        {
            if(opening||exiting)return;
            var editor=MainForm as MainWindow;
            if(editor!=null&&!editor.ReadyForExternalOpen||editor==null&&(start==null||!start.ReadyForExternalOpen))return;
            string path;
            if(!launch.Read(out path))return;
            var target=MainForm;
            if(target.WindowState==FormWindowState.Minimized)target.WindowState=FormWindowState.Normal;
            target.Activate();
            if(path.Length==0)return;
            if(editor==null)start.OpenPath(path);else editor.OpenExternalProject(path);
        }
        internal void Open(MapData data,string path)
        {
            if(opening||exiting)throw new InvalidOperationException("Trwa już otwieranie projektu.");
            opening=true;start.Transition=true;StartupTrace.Write("transition.queued",start);
            try
            {
                // Unwind the native file-dialog/button callback before initializing WPF hosts.
                // The start still owns the application loop until the editor reports ready.
                var handle=start.Handle;
                start.BeginInvoke((Action)(()=>{if(!exiting&&opening&&start!=null&&!start.IsDisposed)OpenCore(data,path);}));
            }
            catch(Exception ex){Rollback(null,ex);}
        }
        void OpenCore(MapData data,string path)
        {
            transitionError=null;Application.ThreadException+=TransitionException;StartupTrace.Write("transition.begin",start);
            MainWindow editor=null;
            try
            {
                editor=EditorFactory(data,path);candidate=editor;if(transitionError!=null)throw transitionError;editor.Owner=null;
                editor.UpdateRequested=()=>updates.Check(editor,true);
                editor.FormClosed+=(s,e)=>{StartupTrace.Write("editor.closed",editor);if(!exiting&&ReferenceEquals(candidate,editor))Rollback(editor,new InvalidOperationException("Edytor zamknięto przed zakończeniem otwierania."));};
                editor.HandleDestroyed+=(s,e)=>StartupTrace.Write("editor.handleDestroyed",editor);
                editor.InitialDisplayComplete=error=>
                {
                    if(!ReferenceEquals(candidate,editor)||exiting)return;
                    if(error!=null){Rollback(editor,error);return;}
                    try
                    {
                        if(editor.IsDisposed||!editor.Visible||!editor.IsHandleCreated)throw new InvalidOperationException("Nie udało się pokazać edytora.");
                        StartupTrace.Write("editor.ready",editor);MainForm=editor;candidate=null;opening=false;DetachException();
                        var previous=start;start=null;previous.Transition=false;previous.Close();StartupTrace.Write("transition.committed",editor);
                    }catch(Exception ex){if(candidate!=null)Rollback(editor,ex);else{StartupTrace.Write("transition.closeStart.failed",editor,ex);ReportFailure(ex);}}
                };
                editor.Show();StartupTrace.Write("editor.showReturned",editor);
                if(editor.InitialDisplayError!=null)throw editor.InitialDisplayError;
                if(editor.IsDisposed||!editor.Visible)throw new InvalidOperationException("Edytor nie został otwarty.");
            }
            catch(Exception ex){Rollback(editor,ex);}
        }
        void Rollback(MainWindow editor,Exception error,bool report=true)
        {
            if(!opening)return;candidate=null;opening=false;DetachException();StartupTrace.Write("transition.rollback",editor,error);
            if(editor!=null&&!editor.IsDisposed){editor.InitialDisplayComplete=null;editor.Dispose();}
            if(start!=null&&!start.IsDisposed){MainForm=start;start.Transition=false;start.Show();start.Activate();}
            if(report)ReportFailure(error);
        }
        void ReportFailure(Exception error){StartupTrace.Write("transition.error",start,error);if(Failure!=null)Failure(error);else MessageBox.Show(start,(start==null?"Edytor jest otwarty, ale wystąpił błąd zamykania startu.":"Nie można otworzyć edytora. Ekran startowy pozostaje dostępny.")+"\n\n"+error.Message,"Błąd otwierania",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        protected override void ExitThreadCore(){exiting=true;if(openTimer!=null)openTimer.Dispose();DetachException();StartupTrace.Write("loop.exit",MainForm);if(preparedNotes!=null){preparedNotes.Dispose();preparedNotes=null;}if(candidate!=null){var old=candidate;candidate=null;old.InitialDisplayComplete=null;old.Dispose();}base.ExitThreadCore();}
    }
    internal sealed class StartWindow : DarkForm
    {
        readonly Action<MapData,string> open;
        readonly FlowLayoutPanel projects=new FlowLayoutPanel{FlowDirection=FlowDirection.TopDown,WrapContents=false};
        readonly ToolTip tips=new ToolTip();
        string[] paths;
        int refreshQueued;
        bool choosing,transition;
        internal Action UpdateRequested;
        internal bool Transition {get{return transition;}set{transition=value;}}
        internal bool ReadyForExternalOpen { get { return !choosing&&!transition&&Visible; } }
        internal StartWindow(Action<MapData,string> open)
        {
            this.open=open;Text="HexMapEditor";ClientSize=new Size(650,550);FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;AutoScaleMode=AutoScaleMode.Dpi;AutoScroll=true;StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(25,28,36);ForeColor=Color.WhiteSmoke;Font=new Font("Segoe UI",10);DoubleBuffered=true;
            Icon icon=null;try{using(var external=new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","app","icon.ico")))icon=(Icon)external.Clone();}catch{try{icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);}catch{}}
            if(icon!=null){Icon=icon;var picture=new PictureBox{Image=icon.ToBitmap(),SizeMode=PictureBoxSizeMode.Zoom,Bounds=new Rectangle(38,36,64,64)};Controls.Add(picture);Disposed+=(s,e)=>{picture.Image.Dispose();icon.Dispose();};}
            var title=new Label{Name="appTitle",Text="HexMapEditor",Font=new Font(Font.FontFamily,28,FontStyle.Bold),AutoSize=true,Location=new Point(120,32)};Controls.Add(title);
            float bigAscent=title.Font.FontFamily.GetCellAscent(title.Font.Style)*title.Font.SizeInPoints*96/72/title.Font.FontFamily.GetEmHeight(title.Font.Style);
            float smallAscent=Font.FontFamily.GetCellAscent(Font.Style)*Font.SizeInPoints*96/72/Font.FontFamily.GetEmHeight(Font.Style);
            int nameWidth=TextRenderer.MeasureText(title.Text,title.Font,Size.Empty,TextFormatFlags.NoPadding).Width;
            var author=new Label{Name="appAuthor",Text="by amdesar",ForeColor=Color.Silver,AutoSize=true,Location=new Point(title.Left+nameWidth+4,title.Top+(int)Math.Round(bigAscent-smallAscent))};Controls.Add(author);
            Shown+=(s,e)=>{using(var g=CreateGraphics()){var flags=TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix;int width=TextRenderer.MeasureText(g,title.Text,title.Font,Size.Empty,flags|TextFormatFlags.NoPadding).Width;int titlePad=(TextRenderer.MeasureText(g,title.Text,title.Font,Size.Empty,flags).Width-width)/2;int authorPad=(TextRenderer.MeasureText(g,author.Text,author.Font,Size.Empty,flags).Width-TextRenderer.MeasureText(g,author.Text,author.Font,Size.Empty,flags|TextFormatFlags.NoPadding).Width)/2;author.Left=title.Left+titlePad+width+6-authorPad;author.BringToFront();}};
            Controls.Add(new Label{Name="appVersion",Text="Wersja "+MainWindow.Version+" • "+ReleaseMetadata.Date,ForeColor=Color.Silver,AutoSize=true,Location=new Point(124,83)});
            var load=ButtonAt("Otwórz projekt",36,132);var create=ButtonAt("Stwórz nowy projekt",334,132);load.Click+=(s,e)=>ChooseOpen();create.Click+=(s,e)=>ChooseNew();
            Controls.Add(new Label{Text="Ostatnie projekty",AutoSize=true,Font=new Font(Font,FontStyle.Bold),Location=new Point(36,198)});
            projects.Bounds=new Rectangle(163,232,324,264);Controls.Add(projects);Shown+=(s,e)=>{StartupTrace.Write("start.visible",this);RefreshProjects();};
            var update=new EditorButton{Text="Sprawdź aktualizacje",Bounds=new Rectangle(434,500,180,30),Anchor=AnchorStyles.Bottom|AnchorStyles.Right};
            update.Click+=(s,e)=>{if(UpdateRequested!=null)UpdateRequested();};Controls.Add(update);
            var github=new ProjectLinkButton{Text="GitHub",Bounds=new Rectangle(36,500,108,30),Anchor=AnchorStyles.Bottom|AnchorStyles.Left};
            tips.SetToolTip(github,"Otwórz stronę projektu HexMapEditor w przeglądarce");
            github.Click+=(s,e)=>{
                try{UpdateController.OpenReleaseLink(new Uri(UpdateChannelConfiguration.ProjectUrl));}
                catch(Exception error){MessageBox.Show(this,error.Message,"Strona projektu",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
            };
            Controls.Add(github);
            Load+=(s,e)=>{var area=Screen.FromControl(this).WorkingArea;Size=new Size(Math.Min(Width,area.Width),Math.Min(Height,area.Height));Location=new Point(area.Left+(area.Width-Width)/2,area.Top+(area.Height-Height)/2);};Disposed+=(s,e)=>tips.Dispose();
        }
        Button ButtonAt(string text,int x,int y){var b=new Button{Text=text,Bounds=new Rectangle(x,y,280,44),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(38,65,96)};b.FlatAppearance.BorderColor=Color.FromArgb(90,145,205);Controls.Add(b);return b;}
        internal DialogResult ShowUpdateDialog(Func<DialogResult> show)
        {
            choosing=true;
            try{return show();}
            finally{choosing=false;}
        }
        internal void RefreshProjects()
        {
            paths=RecentProjects.Shared.Entries;RenderProjects();
            if(paths.Length>0)RecentAvailability.Refresh(paths,()=>{try{if(!IsDisposed&&IsHandleCreated&&System.Threading.Interlocked.Exchange(ref refreshQueued,1)==0){BeginInvoke((Action)(()=>{System.Threading.Interlocked.Exchange(ref refreshQueued,0);if(!IsDisposed&&!transition)RenderProjects();}));}}catch(InvalidOperationException){}});
        }
        void RenderProjects()
        {
            var visible=RecentAvailability.Visible(paths);projects.SuspendLayout();try{while(projects.Controls.Count>0){var c=projects.Controls[0];projects.Controls.RemoveAt(0);c.Dispose();}
                if(visible.Length==0)projects.Controls.Add(new Label{Text=paths.Length==0?"Brak ostatnich projektów. Otwórz mapę lub stwórz nową.":"Brak potwierdzonych dostępnych projektów. Sprawdzanie odbywa się w tle.",AutoSize=false,Size=new Size(projects.Width-8,64),ForeColor=Color.Silver});
                foreach(var path in visible)
                {
                    var name=Path.GetFileNameWithoutExtension(path);
                    var version=RecentAvailability.SavedWith(path);
                    var b=new RecentProjectButton(name,version){Width=projects.Width-8};
                    tips.SetToolTip(b,version==null?path:path+" ("+version+")");
                    b.Click+=(s,e)=>OpenPath(path);projects.Controls.Add(b);
                }
            }finally{projects.ResumeLayout(true);}
        }
        void ChooseOpen(){if(choosing||transition)return;choosing=true;try{using(var dialog=new OpenFileDialog{Filter="Projekt HexMapEditor (*.hexmap)|*.hexmap",RestoreDirectory=true,Title="Otwórz projekt"})if(dialog.ShowDialog(this)==DialogResult.OK)OpenPathCore(dialog.FileName);}finally{choosing=false;}}
        internal bool OpenPath(string path){if(choosing||transition)return false;choosing=true;try{return OpenPathCore(path);}finally{choosing=false;}}
        bool OpenPathCore(string path)
        {
            try{var data=MainWindow.ReadProjectData(path);open(data,Path.GetFullPath(path));RecentProjects.Shared.Touch(path);return true;}catch(Exception ex){MessageBox.Show(this,"Nie można otworzyć projektu.\n\n"+ex.Message,"Błąd odczytu",MessageBoxButtons.OK,MessageBoxIcon.Error);RefreshProjects();return false;}
        }
        void ChooseNew()
        {
            if(choosing||transition)return;choosing=true;
            try{string warning;var presets=PresetCatalog.Load(LibraryPaths.Presets,out warning);if(warning.Length>0)MessageBox.Show(this,warning,"Pominięte presety",MessageBoxButtons.OK,MessageBoxIcon.Warning);string stateWarning;var states=StateLibrary.Load(Path.Combine(LibraryPaths.Root,"Objects","States"),out stateWarning).Select(entry=>entry.Definition.Copy()).ToList();if(stateWarning.Length>0)MessageBox.Show(this,stateWarning,"Pominięte stany",MessageBoxButtons.OK,MessageBoxIcon.Warning);using(var dialog=new NewMapDialog("PointyTop",presets)){ProjectCreation.ConfigureStart(dialog,states);if(dialog.ShowDialog(this)==DialogResult.OK)open(dialog.CreatedProject.Data,dialog.CreatedProject.Path);}}
            catch(Exception ex){MessageBox.Show(this,ex.Message,"Nie można utworzyć projektu",MessageBoxButtons.OK,MessageBoxIcon.Error);}
            finally{choosing=false;}
        }
    }

    internal sealed class ProjectLinkButton : Button
    {
        bool hover;
        internal ProjectLinkButton()
        {
            SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
            FlatStyle=FlatStyle.Flat;
            Cursor=Cursors.Hand;
            ForeColor=Color.WhiteSmoke;
        }
        protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hover=true;Invalidate();}
        protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hover=false;Invalidate();}
        protected override void OnPaint(PaintEventArgs e)
        {
            var bounds=Rectangle.Inflate(ClientRectangle,-1,-1);
            int diameter=Math.Max(4,bounds.Height/2);
            e.Graphics.Clear(Parent.BackColor);
            e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using(var shape=new System.Drawing.Drawing2D.GraphicsPath())
            using(var fill=new SolidBrush(hover?Color.FromArgb(44,49,60):Color.FromArgb(13,16,22)))
            using(var edge=new Pen(Focused&&ShowFocusCues?ChoiceStyle.ActiveBorder:Color.FromArgb(74,81,98)))
            {
                shape.AddArc(bounds.Left,bounds.Top,diameter,diameter,180,90);
                shape.AddArc(bounds.Right-diameter,bounds.Top,diameter,diameter,270,90);
                shape.AddArc(bounds.Right-diameter,bounds.Bottom-diameter,diameter,diameter,0,90);
                shape.AddArc(bounds.Left,bounds.Bottom-diameter,diameter,diameter,90,90);
                shape.CloseFigure();
                e.Graphics.FillPath(fill,shape);e.Graphics.DrawPath(edge,shape);
            }
            TextRenderer.DrawText(e.Graphics,Text,Font,bounds,ForeColor,
                TextFormatFlags.SingleLine|TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
        }
    }

    internal sealed class RecentProjectButton : Button
    {
        readonly string projectName,versionLabel;
        internal RecentProjectButton(string name,string version)
        {
            projectName=name;
            versionLabel=version==null?null:"("+version+")";
            AccessibleName=versionLabel==null?name:name+", "+versionLabel;
            Height=43;Margin=new Padding(0,0,0,8);FlatStyle=FlatStyle.Flat;BackColor=Color.FromArgb(34,38,48);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            const TextFormatFlags flags=TextFormatFlags.SingleLine|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix|TextFormatFlags.NoPadding;
            var row=Rectangle.Inflate(ClientRectangle,-8,-2);
            if(versionLabel!=null)
            {
                int width=TextRenderer.MeasureText(e.Graphics,versionLabel,Font,Size.Empty,flags).Width;
                var reserved=new Rectangle(row.Right-width,row.Top,width,row.Height);
                TextRenderer.DrawText(e.Graphics,versionLabel,Font,reserved,Color.Silver,flags|TextFormatFlags.Right);
                row.Width=Math.Max(0,row.Width-width-8);
            }
            if(row.Width>0)TextRenderer.DrawText(e.Graphics,projectName,Font,row,ForeColor,flags|TextFormatFlags.EndEllipsis);
        }
    }
}
