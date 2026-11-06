using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class IconEditDialog : EditorDialog
    {
        readonly TextBox name = new TextBox { MaxLength = 200 };
        readonly CheckBox update = new CheckBox { Text = "Zaktualizuj wystąpienia na bieżącej mapie", AutoSize = true, Checked = false };
        readonly PictureBox preview = new PictureBox { SizeMode = PictureBoxSizeMode.Zoom };
        public string IconName { get { return name.Text.Trim(); } }
        public IconAsset Asset { get; private set; }
        public bool UpdateMap { get { return update.Checked; } }
        public IconEditDialog(LibraryIcon icon, Func<IWin32Window, LibraryIcon> pick) : base("Edytuj ikonę",335)
        {
            Asset=icon.Asset;name.Text=icon.Name;Row(0,"Nazwa",name,true);
            var file=new Button { Text="Grafika z pliku…" };Row(1,"Podmień",file,true);
            var choices=new Button { Text="Wybierz z biblioteki", AccessibleName="Wybierz grafikę z biblioteki" };Row(2,"Z biblioteki",choices,true);
            Row(3,"Podgląd",preview,true);Layout.RowStyles[3].Height=90;
            Layout.Controls.Add(update,0,4);Layout.SetColumnSpan(update,3);Layout.RowStyles.Add(new RowStyle(SizeType.Absolute,42));Buttons(5,()=>{if(IconName.Length>0)DialogResult=DialogResult.OK;});
            Action show=()=>{var old=preview.Image;preview.Image=IconAssets.Decode(Asset);if(old!=null)old.Dispose();};show();
            choices.Click+=(s,e)=>{var chosen=pick(this);if(chosen!=null){Asset=chosen.Asset;show();}};
            file.Click+=(s,e)=>{using(var d=new OpenFileDialog { Title="Podmień grafikę ikony",Filter="Bitmapy PNG/JPG/BMP/GIF|*.png;*.jpg;*.jpeg;*.bmp;*.gif",RestoreDirectory=true })if(d.ShowDialog(this)==DialogResult.OK)try{var asset=IconAssets.Import(d.FileName);Asset=asset;show();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Nie można podmienić grafiki",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};
            Disposed+=(s,e)=>{if(preview.Image!=null)preview.Image.Dispose();};
        }
    }
}
