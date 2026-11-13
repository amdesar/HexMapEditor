using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class IconPickerDialog : DarkForm
    {
        public LibraryIcon Chosen { get; private set; }
        public IconPickerDialog(IEnumerable<LibraryIcon> icons, Func<LibraryIcon,Action,Button> makeButton, Color background)
        {
            Text="Biblioteka ikon — wybierz"; Size=new Size(520,420); MinimumSize=new Size(350,240); StartPosition=FormStartPosition.CenterParent; BackColor=background;
            var cancel=new Button { Text="Anuluj", Dock=DockStyle.Bottom, Height=30, DialogResult=DialogResult.Cancel, FlatStyle=FlatStyle.Flat, ForeColor=Color.WhiteSmoke, BackColor=Color.FromArgb(49,54,67) };Controls.Add(cancel);CancelButton=cancel;
            var list=new FlowLayoutPanel { Dock=DockStyle.Fill, AutoScroll=true, Padding=new Padding(12) };Controls.Add(list);list.BringToFront();
            foreach(var icon in icons){var entry=icon;var button=makeButton(entry,()=>{Chosen=entry;DialogResult=DialogResult.OK;});button.Width=88;button.Height=76;button.Text=entry.Name;button.AutoEllipsis=true;button.TextImageRelation=TextImageRelation.ImageAboveText;list.Controls.Add(button);}
        }
    }
}
