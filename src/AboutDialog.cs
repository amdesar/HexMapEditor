using System;
using System.Drawing;
using System.Windows.Forms;

namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        void ShowAbout(){AboutDialog.ShowFor(this);}
    }

    internal sealed class AboutDialog : DarkForm
    {
        internal static void ShowFor(Form owner)
        {
            foreach(Form owned in owner.OwnedForms)
                if(owned is AboutDialog){owned.Activate();return;}
            new AboutDialog(owner.Icon).Show(owner);
        }
        internal static Rectangle Position(Rectangle owner,Rectangle work,Size wanted)
        {
            int width=Math.Min(wanted.Width,work.Width),height=Math.Min(wanted.Height,work.Height);
            int x=Math.Max(work.Left,Math.Min(work.Right-width,owner.Left+(owner.Width-width)/2));
            int y=Math.Max(work.Top,Math.Min(work.Bottom-height,owner.Top+(owner.Height-height)/2));
            return new Rectangle(x,y,width,height);
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if(Owner!=null)Bounds=Position(Owner.Bounds,Screen.FromControl(Owner).WorkingArea,Size);
        }
        internal AboutDialog(Icon appIcon)
        {
            Text="O programie";AutoScaleMode=AutoScaleMode.Dpi;AutoScaleDimensions=new SizeF(96,96);
            ClientSize=new Size(400,155);FormBorderStyle=FormBorderStyle.FixedDialog;
            MaximizeBox=MinimizeBox=false;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;
            Font=new Font("Segoe UI",10);
            Icon icon=appIcon==null?null:(Icon)appIcon.Clone();
            if(icon!=null)Icon=icon;
            var picture=new PictureBox{Image=icon==null?null:icon.ToBitmap(),Bounds=new Rectangle(22,24,54,54),SizeMode=PictureBoxSizeMode.Zoom};
            var heading=new Label{Text="HexMapEditor",AutoSize=true,Font=new Font(Font.FontFamily,17,FontStyle.Bold),Location=new Point(91,17)};
            var details=new Label{Text="Wersja "+ReleaseMetadata.Version+" • "+ReleaseMetadata.Date+"\nAutor amdesar • Licencja MIT",
                Location=new Point(94,58),AutoSize=true};
            var link=new EditorButton{Text="Repozytorium projektu",Bounds=new Rectangle(22,107,192,30),
                AccessibleName="Otwórz repozytorium HexMapEditor w domyślnej przeglądarce"};
            link.Click+=(s,e)=>{
                try{UpdateController.OpenReleaseLink(new Uri(UpdateChannelConfiguration.ProjectUrl));}
                catch(Exception error){MessageBox.Show(this,error.Message,"Repozytorium projektu",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
            };
            var close=new EditorButton{Text="Zamknij",DialogResult=DialogResult.Cancel,Bounds=new Rectangle(278,107,100,30)};
            close.Click+=(s,e)=>Close();
            Controls.AddRange(new Control[]{picture,heading,details,link,close});CancelButton=close;
            Disposed+=(s,e)=>{if(picture.Image!=null)picture.Image.Dispose();if(icon!=null)icon.Dispose();heading.Font.Dispose();};
        }
    }
}
