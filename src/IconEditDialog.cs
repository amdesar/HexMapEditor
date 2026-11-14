using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class IconEditDialog : EditorDialog
    {
        readonly CheckBox light=new SwitchBox{Text="Pole widzenia nowych instancji",AutoSize=true};
        readonly ScrubNumber radius=new ScrubNumber{Minimum=0,Maximum=20,Value=1};
        public bool DefaultLight{get{return light.Checked;}}
        public int DefaultLightRadius{get{return (int)radius.Value;}}
        public void SetLightDefaults(bool enabled,int value){light.Checked=enabled;radius.Value=value;}
        readonly TextBox name = new DarkTextBox { MaxLength = 200 };
        readonly CheckBox update = new SwitchBox { Text = "Zaktualizuj wystąpienia na bieżącej mapie", AutoSize = true, Checked = false };
        readonly PictureBox preview = new PictureBox { SizeMode = PictureBoxSizeMode.Zoom };
        public string IconName { get { return name.Text.Trim(); } }
        public IconAsset Asset { get; private set; }
        public Func<bool> CanAccept;
        public bool UpdateMap { get { return update.Checked; } }
        public IconEditDialog(LibraryIcon icon, Func<IWin32Window, LibraryIcon> pick, Func<IWin32Window, LibraryIcon> other = null, string title = "Edytuj ikonę") : base(title,other == null ? 335 : 375)
        {
            if(title!="Edytuj ikonę")ClientSize=new Size(ClientSize.Width,ClientSize.Height+72);
            if(title!="Edytuj ikonę")update.Text="Zaktualizuj grafikę instancji na tej mapie";
            Asset=icon.Asset;name.Text=icon.Name;Row(0,"Nazwa",name,true);
            var file=new EditorButton { Text="Grafika z pliku…" };Row(1,"Podmień",file,true);
            var choices=new EditorButton { Text="Wybierz z biblioteki", AccessibleName="Wybierz grafikę z biblioteki" };Row(2,"Z biblioteki",choices,true);
            int row=3; Button cross=null;
            if(other!=null){cross=new EditorButton{Text=title=="Edytuj ikonę"?"Z biblioteki obiektów":"Z biblioteki ikon"};Row(row++,"Inna biblioteka",cross,true);}
            Row(row,"Podgląd",preview,true);Layout.RowStyles[row++].Height=90;
            if(title!="Edytuj ikonę"){Row(row++,"Nowe instancje",light,true);Row(row++,"Promień widzenia",radius,true);light.CheckedChanged+=(s,e)=>radius.Enabled=light.Checked;radius.Enabled=false;}
            Layout.Controls.Add(update,0,row++);Layout.SetColumnSpan(update,3);Layout.RowStyles.Add(new RowStyle(SizeType.Absolute,42));Buttons(row,()=>{if(IconName.Length>0 && (CanAccept == null || CanAccept()))DialogResult=DialogResult.OK;});
            Action show=()=>{var old=preview.Image;preview.Image=IconAssets.Decode(Asset);if(old!=null)old.Dispose();};show();
            choices.Click+=(s,e)=>{var chosen=pick(this);if(chosen!=null){Asset=chosen.Asset;show();}};
            if(cross!=null)cross.Click+=(s,e)=>{var chosen=other(this);if(chosen!=null){Asset=chosen.Asset;show();}};
            file.Click+=(s,e)=>{using(var d=new OpenFileDialog { Title="Podmień grafikę ikony",Filter="Bitmapy PNG/JPG/BMP/GIF|*.png;*.jpg;*.jpeg;*.bmp;*.gif",RestoreDirectory=true })if(d.ShowDialog(this)==DialogResult.OK)try{using(var crop=new SquareImportDialog(d.FileName))if(crop.ShowDialog(this)==DialogResult.OK){Asset=crop.Result;show();}}catch(Exception ex){MessageBox.Show(this,ex.Message,"Nie można podmienić grafiki",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};
            Disposed+=(s,e)=>{if(preview.Image!=null)preview.Image.Dispose();};
        }
    }
}
