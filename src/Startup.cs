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
        internal StartupContext(string path)
        {
            if(!String.IsNullOrEmpty(path))try{var data=MainWindow.ReadProjectData(path);Open(data,Path.GetFullPath(path));RecentProjects.Shared.Touch(path);return;}catch(Exception ex){MessageBox.Show("Nie można otworzyć projektu.\n\n"+ex.Message,"Błąd odczytu",MessageBoxButtons.OK,MessageBoxIcon.Error);}
            start=new StartWindow(Open);MainForm=start;start.Show();
        }
        void Open(MapData data,string path)
        {
            var editor=new MainWindow(data,path); // Layout prepared before Show, no placeholder map.
            MainForm=editor;editor.Show();if(start!=null){start.Close();start=null;}
        }
    }
    internal sealed class StartWindow : Form
    {
        readonly Action<MapData,string> open;
        readonly FlowLayoutPanel projects=new FlowLayoutPanel{FlowDirection=FlowDirection.TopDown,WrapContents=false};
        readonly ToolTip tips=new ToolTip();
        string[] paths;
        int refreshQueued;
        bool choosing;
        internal StartWindow(Action<MapData,string> open)
        {
            this.open=open;Text="HexMapEditor";ClientSize=new Size(650,550);MinimumSize=new Size(620,570);StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(25,28,36);ForeColor=Color.WhiteSmoke;Font=new Font("Segoe UI",10);DoubleBuffered=true;
            Icon icon=null;try{using(var external=new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","app","icon.ico")))icon=(Icon)external.Clone();}catch{try{icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);}catch{}}
            if(icon!=null){Icon=icon;var picture=new PictureBox{Image=icon.ToBitmap(),SizeMode=PictureBoxSizeMode.Zoom,Bounds=new Rectangle(38,36,64,64)};Controls.Add(picture);Disposed+=(s,e)=>{picture.Image.Dispose();icon.Dispose();};}
            Controls.Add(new Label{Text="HexMapEditor",Font=new Font(Font.FontFamily,28,FontStyle.Bold),AutoSize=true,Location=new Point(120,32)});
            Controls.Add(new Label{Text="Wersja "+MainWindow.Version,ForeColor=Color.Silver,AutoSize=true,Location=new Point(124,83)});
            var load=ButtonAt("Otwórz projekt",36,132);var create=ButtonAt("Stwórz nowy projekt",334,132);load.Click+=(s,e)=>ChooseOpen();create.Click+=(s,e)=>ChooseNew();
            Controls.Add(new Label{Text="Ostatnie projekty",AutoSize=true,Font=new Font(Font,FontStyle.Bold),Location=new Point(36,198)});
            projects.Bounds=new Rectangle(36,232,578,280);Controls.Add(projects);Shown+=(s,e)=>RefreshProjects();
            Resize+=(s,e)=>{projects.Width=ClientSize.Width-72;};Disposed+=(s,e)=>tips.Dispose();
        }
        Button ButtonAt(string text,int x,int y){var b=new Button{Text=text,Bounds=new Rectangle(x,y,280,44),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(38,65,96)};b.FlatAppearance.BorderColor=Color.FromArgb(90,145,205);Controls.Add(b);return b;}
        internal void RefreshProjects()
        {
            paths=RecentProjects.Shared.Entries;RenderProjects();
            if(paths.Length>0)RecentAvailability.Refresh(paths,()=>{try{if(!IsDisposed&&IsHandleCreated&&System.Threading.Interlocked.Exchange(ref refreshQueued,1)==0){BeginInvoke((Action)(()=>{System.Threading.Interlocked.Exchange(ref refreshQueued,0);if(!IsDisposed)RenderProjects();}));}}catch(InvalidOperationException){}});
        }
        void RenderProjects()
        {
            var visible=RecentAvailability.Visible(paths);projects.SuspendLayout();try{while(projects.Controls.Count>0){var c=projects.Controls[0];projects.Controls.RemoveAt(0);c.Dispose();}
                if(visible.Length==0)projects.Controls.Add(new Label{Text=paths.Length==0?"Brak ostatnich projektów. Otwórz mapę lub stwórz nową.":"Brak potwierdzonych dostępnych projektów. Sprawdzanie odbywa się w tle.",AutoSize=false,Size=new Size(projects.Width-8,64),ForeColor=Color.Silver});
                foreach(var path in visible){var b=new Button{Text=Path.GetFileNameWithoutExtension(path),Width=projects.Width-8,Height=43,Margin=new Padding(0,0,0,8),TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(34,38,48)};tips.SetToolTip(b,path);b.Click+=(s,e)=>OpenPath(path);projects.Controls.Add(b);}
            }finally{projects.ResumeLayout(true);}
        }
        void ChooseOpen(){if(choosing)return;choosing=true;try{using(var dialog=new OpenFileDialog{Filter="Projekt HexMapEditor (*.hexmap)|*.hexmap",RestoreDirectory=true,Title="Otwórz projekt"})if(dialog.ShowDialog(this)==DialogResult.OK)OpenPathCore(dialog.FileName);}finally{choosing=false;}}
        internal bool OpenPath(string path){if(choosing)return false;choosing=true;try{return OpenPathCore(path);}finally{choosing=false;}}
        bool OpenPathCore(string path)
        {
            try{var data=MainWindow.ReadProjectData(path);open(data,Path.GetFullPath(path));RecentProjects.Shared.Touch(path);return true;}catch(Exception ex){MessageBox.Show(this,"Nie można otworzyć projektu.\n\n"+ex.Message,"Błąd odczytu",MessageBoxButtons.OK,MessageBoxIcon.Error);RefreshProjects();return false;}
        }
        void ChooseNew()
        {
            if(choosing)return;choosing=true;
            try{string warning;var presets=PresetCatalog.Load(LibraryPaths.Presets,out warning);if(warning.Length>0)MessageBox.Show(this,warning,"Pominięte presety",MessageBoxButtons.OK,MessageBoxIcon.Warning);var data=MainWindow.ChooseNewProject(this,"PointyTop",presets);if(data!=null)open(data,null);}
            catch(Exception ex){MessageBox.Show(this,ex.Message,"Nie można utworzyć projektu",MessageBoxButtons.OK,MessageBoxIcon.Error);}
            finally{choosing=false;}
        }
    }
}
