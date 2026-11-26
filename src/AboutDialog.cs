using System;
using System.Drawing;
using System.Windows.Forms;

namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        AboutDialog aboutDialog;
        void ShowAbout()
        {
            if(aboutDialog!=null){aboutDialog.Activate();return;}
            aboutDialog=new AboutDialog(Icon);
            aboutDialog.FormClosed+=(s,e)=>aboutDialog=null;
            aboutDialog.Show(this);
        }
    }

    internal sealed class AboutDialog : DarkForm
    {
        internal AboutDialog(Icon appIcon)
        {
            Text="O programie";ClientSize=new Size(470,253);FormBorderStyle=FormBorderStyle.FixedDialog;
            MaximizeBox=MinimizeBox=false;ShowInTaskbar=false;StartPosition=FormStartPosition.CenterParent;
            Font=new Font("Segoe UI",10);
            Icon icon=appIcon==null?null:(Icon)appIcon.Clone();
            if(icon!=null)Icon=icon;
            var picture=new PictureBox{Image=icon==null?null:icon.ToBitmap(),Bounds=new Rectangle(22,24,54,54),SizeMode=PictureBoxSizeMode.Zoom};
            var heading=new Label{Text="HexMapEditor",AutoSize=true,Font=new Font(Font.FontFamily,17,FontStyle.Bold),Location=new Point(91,17)};
            var details=new Label{Text="Wersja "+ReleaseMetadata.Version+" • "+ReleaseMetadata.Date+"\nAutor amdesar • Licencja MIT",
                Location=new Point(94,58),AutoSize=true};
            var credits=new Label{Text="Projekt rozwijano z pomocą ChatGPT i Codexa.\nIkonę przygotowano z pomocą ChatGPT.",
                Location=new Point(22,119),AutoSize=true};
            var link=new LinkLabel{Text="GitHub — amdesar/HexMapEditor",AutoSize=true,Location=new Point(22,174),
                LinkColor=EditorTheme.LinkText,ActiveLinkColor=Color.White,VisitedLinkColor=EditorTheme.LinkText,
                AccessibleName="Otwórz repozytorium HexMapEditor w domyślnej przeglądarce"};
            link.LinkClicked+=(s,e)=>{
                try{UpdateController.OpenReleaseLink(new Uri(UpdateChannelConfiguration.ProjectUrl));}
                catch(Exception error){MessageBox.Show(this,error.Message,"Repozytorium projektu",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
            };
            var close=new EditorButton{Text="Zamknij",DialogResult=DialogResult.Cancel,Bounds=new Rectangle(350,207,100,30)};
            close.Click+=(s,e)=>Close();
            Controls.AddRange(new Control[]{picture,heading,details,credits,link,close});CancelButton=close;
            Disposed+=(s,e)=>{if(picture.Image!=null)picture.Image.Dispose();if(icon!=null)icon.Dispose();heading.Font.Dispose();};
        }
    }
}
